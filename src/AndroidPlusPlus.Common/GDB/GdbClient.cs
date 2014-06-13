////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.Common
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public sealed class GdbClient : AsyncRedirectProcess.EventListener, IDisposable
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public enum StepType
    {
      Statement,
      Line,
      Instruction
    };

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public delegate void OnResultRecordDelegate (MiResultRecord resultRecord);

    public delegate void OnAsyncRecordDelegate (MiAsyncRecord asyncRecord);

    public delegate void OnStreamRecordDelegate (MiStreamRecord streamRecord);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public OnResultRecordDelegate OnResultRecord { get; set; }

    public OnAsyncRecordDelegate OnAsyncRecord { get; set; }

    public OnStreamRecordDelegate OnStreamRecord { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private class AsyncCommandData
    {
      public AsyncCommandData ()
      {
        StreamRecords = new List<MiStreamRecord> ();
      }

      public string Command { get; set; }

      public List<MiStreamRecord> StreamRecords;

      public OnResultRecordDelegate ResultDelegate { get; set; }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly GdbSetup m_gdbSetup;

    private GdbServer m_gdbServer = null;

    private AsyncRedirectProcess m_gdbClientInstance = null;

    private Dictionary<uint, AsyncCommandData> m_asyncCommandData = new Dictionary<uint,AsyncCommandData> ();

    private ManualResetEvent m_syncCommandLock = null;

    private int m_lastOperationTimestamp = 0;

    private uint m_sessionCommandToken = 1; // Start at 1 so 0 can represent an invalid token.

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public GdbClient (GdbSetup gdbSetup)
    {
      m_gdbSetup = gdbSetup;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Dispose ()
    {
      LoggingUtils.PrintFunction ();

      SendAsyncCommand ("-gdb-exit");

      if (m_gdbClientInstance != null)
      {
        m_gdbClientInstance.Dispose ();

        m_gdbClientInstance = null;
      }

      if (m_syncCommandLock != null)
      {
        m_syncCommandLock.Dispose ();

        m_syncCommandLock = null;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Start ()
    {
      LoggingUtils.PrintFunction ();

      // 
      // Export an execution script ('gdb.setup') for standard start-up properties.
      // 

      string [] execCommands = m_gdbSetup.CreateGdbExecutionScript ();

      using (StreamWriter writer = new StreamWriter (Path.Combine (m_gdbSetup.CacheDirectory, "gdb.setup")))
      {
        foreach (string command in execCommands)
        {
          writer.WriteLine (command);
        }

        writer.Close ();
      }

      // 
      // Spawn a new GDB instance which executes gdb.setup and begins debugging file 'app_process'.
      // 

      m_syncCommandLock = new ManualResetEvent (false);

      m_lastOperationTimestamp = Environment.TickCount;

      m_gdbClientInstance = new AsyncRedirectProcess (m_gdbSetup.GdbToolPath, m_gdbSetup.GdbToolArguments);

      m_gdbClientInstance.Listener = this;

      m_gdbClientInstance.StartInfo.Arguments += string.Format (@" -fullname -x {0}\gdb.setup", StringUtils.ConvertPathWindowsToPosix (m_gdbSetup.CacheDirectory));

      m_gdbClientInstance.Start ();

      // 
      // Request symbol loading and probe each library for special embedded 'gdb.setup' sections.
      // 

      SetSetting ("solib-search-path", StringUtils.ConvertPathWindowsToPosix (m_gdbSetup.CacheDirectory), true);

      SetSetting ("debug-file-directory", StringUtils.ConvertPathWindowsToPosix (m_gdbSetup.CacheDirectory), true);

      string [] cachedBinaries = m_gdbSetup.CacheDeviceBinaries ();

      string gnuObjdumpToolPath = m_gdbSetup.GdbToolPath.Replace ("-gdb", "-objdump");

      foreach (string binary in cachedBinaries)
      {
        string embeddedGdbSetupSection = GnuObjdump.GetElfSectionData (gnuObjdumpToolPath, binary, "gdb.setup");

        SetSetting ("solib-search-path", StringUtils.ConvertPathWindowsToPosix (Path.GetDirectoryName (binary)), true);

        SetSetting ("debug-file-directory", StringUtils.ConvertPathWindowsToPosix (Path.GetDirectoryName (binary)), true);

        if (!string.IsNullOrWhiteSpace (embeddedGdbSetupSection))
        {
          string [] embeddedCommands = embeddedGdbSetupSection.Replace ("\r", "").Split (new char [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

          foreach (string command in embeddedCommands)
          {
            // 
            // Sanitise path arguments, but only if they are rooted.
            // 

            bool setCommand = command.StartsWith ("set ");

            string [] arguments = command.Replace ("set ", "").Split (new char [] { ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder joinedArguments = new StringBuilder ();

            for (int i = 1; i < arguments.Length; ++i)
            {
              if (Path.IsPathRooted (arguments [i]))
              {
                arguments [i] = StringUtils.ConvertPathWindowsToPosix (arguments [i]);
              }

              joinedArguments.Append (";" + arguments [i]);
            }

            if (joinedArguments.Length > 0)
            {
              joinedArguments.Remove (0, 1); // clear leading ';'
            }

            if (setCommand)
            {
              SetSetting (arguments [0], joinedArguments.ToString (), true);
            }
            else
            {
              SendCommand (arguments [0] + " " + joinedArguments.ToString ());
            }
          }
        }

        SendCommand ("symbol-file " + StringUtils.ConvertPathWindowsToPosix (binary));
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Attach (GdbServer gdbServer)
    {
      LoggingUtils.PrintFunction ();

      m_gdbServer = gdbServer;

      m_gdbSetup.ClearPortForwarding ();

      m_gdbSetup.SetupPortForwarding ();

      SendCommand ("file " + StringUtils.ConvertPathWindowsToPosix (Path.Combine (m_gdbSetup.CacheDirectory, "app_process")));

      SendCommand (string.Format ("target remote {0}:{1}", m_gdbSetup.Host, m_gdbSetup.Port), 60000);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Detach ()
    {
      LoggingUtils.PrintFunction ();

      SendCommand ("-target-detach");

      m_gdbServer = null;

      m_gdbSetup.ClearPortForwarding ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Stop ()
    {
      LoggingUtils.PrintFunction ();

      SendCommand ("-exec-interrupt");
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Continue ()
    {
      LoggingUtils.PrintFunction ();

      SendCommand ("-exec-continue");
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Terminate ()
    {
      LoggingUtils.PrintFunction ();

      SendCommand ("-exec-interrupt");

      SendCommand ("kill");
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void StepInto (StepType stepType, bool reverse)
    {
      LoggingUtils.PrintFunction ();

      switch (stepType)
      {
        case StepType.Statement:
        case StepType.Line:
        {
          MiResultRecord resultRecord = SendCommand (string.Format ("-exec-step {0}", ((reverse) ? "--reverse" : "")));

          if ((resultRecord == null) || ((resultRecord != null) && resultRecord.IsError ()))
          {
            throw new InvalidOperationException ();
          }

          break;
        }
        case StepType.Instruction:
        {
          MiResultRecord resultRecord = SendCommand (string.Format ("-exec-step-instruction {0}", ((reverse) ? "--reverse" : "")));

          if ((resultRecord == null) || ((resultRecord != null) && resultRecord.IsError ()))
          {
            throw new InvalidOperationException ();
          }

          break;
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void StepOut (StepType stepType, bool reverse)
    {
      LoggingUtils.PrintFunction ();

      switch (stepType)
      {
        case StepType.Statement:
        case StepType.Line:
        case StepType.Instruction:
        {
          MiResultRecord resultRecord = SendCommand (string.Format ("-exec-finish {0}", ((reverse) ? "--reverse" : "")));

          if ((resultRecord == null) || ((resultRecord != null) && resultRecord.IsError ()))
          {
            throw new InvalidOperationException ();
          }

          break;
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void StepOver (StepType stepType, bool reverse)
    {
      LoggingUtils.PrintFunction ();

      switch (stepType)
      {
        case StepType.Statement:
        case StepType.Line:
        {
          MiResultRecord resultRecord = SendCommand (string.Format ("-exec-next {0}", ((reverse) ? "--reverse" : "")));

          if ((resultRecord == null) || ((resultRecord != null) && resultRecord.IsError ()))
          {
            throw new InvalidOperationException ();
          }

          break;
        }
        case StepType.Instruction:
        {
          MiResultRecord resultRecord = SendCommand (string.Format ("-exec-next-instruction {0}", ((reverse) ? "--reverse" : "")));

          if ((resultRecord == null) || ((resultRecord != null) && resultRecord.IsError ()))
          {
            throw new InvalidOperationException ();
          }

          break;
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string GetSetting (string setting)
    {
      LoggingUtils.Print (string.Format ("[GdbClient] GetSetting: " + setting));

      MiResultRecord result = SendCommand (string.Format ("-gdb-show {0}", setting));

      if ((result != null) && (!result.IsError ()))
      {
        return result ["value"].GetString ();
      }

      return string.Empty;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetSetting (string setting, string value, bool appendToExisting)
    {
      LoggingUtils.Print (string.Format ("[GdbClient] SetSetting: " + setting + "=" + value));

      if (appendToExisting)
      {
        string existingSettingValue = GetSetting (setting);

        if (!existingSettingValue.Contains (value))
        {
          value = string.Join (";", new string [] { existingSettingValue, value }); 
        }
      }

      SendCommand (string.Format ("-gdb-set {0} {1}", setting, value));
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public MiResultRecord SendCommand (string command, int timeout = 60000)
    {
      // 
      // Perform a synchronous command request; issue a standard async command and keep alive whilst still receiving output.
      // 

      LoggingUtils.Print (string.Format ("[GdbClient] SendCommand: {0}", command));

      MiResultRecord syncResultRecord = null;

      if (m_gdbClientInstance == null)
      {
        return syncResultRecord;
      }

      m_syncCommandLock.Reset ();

      SendAsyncCommand (command, delegate (MiResultRecord record) 
      {
        syncResultRecord = record;

        m_syncCommandLock.Set ();
      });

      // 
      // Wait for asynchronous record response (or exit), reset timeout each time new activity occurs.
      // 

      int timeoutFromCurrentTick = (timeout + m_lastOperationTimestamp) - Environment.TickCount;

      bool responseSignaled = false;

      while ((!responseSignaled) && (timeoutFromCurrentTick > 0))
      {
        responseSignaled = m_syncCommandLock.WaitOne (0);

        if (!responseSignaled)
        {
          timeoutFromCurrentTick = (timeout + m_lastOperationTimestamp) - Environment.TickCount;

          Thread.Yield ();
        }
      }

      if (!responseSignaled)
      {
        throw new TimeoutException ("Timed out waiting for synchronous response (" + command + ").");
      }

      return syncResultRecord;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SendAsyncCommand (string command, OnResultRecordDelegate asyncDelegate = null)
    {
      // 
      // Keep track of this command, and associated token-id, so results can be tracked asynchronously.
      // 

      LoggingUtils.PrintFunction ();

      if (m_gdbClientInstance == null)
      {
        return;
      }

      m_lastOperationTimestamp = Environment.TickCount;

      ThreadPool.QueueUserWorkItem (delegate (object state)
      {
        try
        {
          AsyncCommandData commandData = new AsyncCommandData ();

          commandData.Command = command;

          commandData.ResultDelegate = asyncDelegate;

          m_asyncCommandData.Add (m_sessionCommandToken, commandData);

          // 
          // Prepend (and increment) GDB/MI token.
          // 

          command = m_sessionCommandToken + command;

          ++m_sessionCommandToken;

          m_gdbClientInstance.SendCommand (command);

          m_lastOperationTimestamp = Environment.TickCount;

          LoggingUtils.Print (string.Format ("[GdbClient] SendAsyncCommand: {0}", command));
        }
        catch (Exception e)
        {
          LoggingUtils.HandleException (e);
        }
      });
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ProcessStdout (object sendingProcess, DataReceivedEventArgs args)
    {
      m_lastOperationTimestamp = Environment.TickCount;

      if (!string.IsNullOrEmpty (args.Data))
      {
        LoggingUtils.Print (string.Format ("[GdbClient] ProcessStdout: {0}", args.Data));

        // 
        // Distribute result records to registered delegate callbacks.
        // 

        MiRecord record = MiInterpreter.ParseGdbOutputRecord (args.Data);

        if (record is MiPromptRecord)
        {
          //m_asyncCommandLock.Set ();
        }
        else if ((record is MiAsyncRecord) && (OnAsyncRecord != null))
        {
          MiAsyncRecord asyncRecord = record as MiAsyncRecord;

          OnAsyncRecord (asyncRecord);
        }
        else if ((record is MiResultRecord) && (OnResultRecord != null))
        {
          MiResultRecord resultRecord = record as MiResultRecord;

          OnResultRecord (resultRecord);
        }
        else if ((record is MiStreamRecord) && (OnStreamRecord != null))
        {
          MiStreamRecord streamRecord = record as MiStreamRecord;

          OnStreamRecord (streamRecord);

          // 
          // Non-GDB/MI commands (standard client interface commands) report their output using standard stream records.
          // We cache these outputs for any active CLI commands, identifiable as the commands don't start with '-'.
          // 

          foreach (KeyValuePair<uint, AsyncCommandData> asyncCommand in m_asyncCommandData)
          {
            if (!asyncCommand.Value.Command.StartsWith ("-"))
            {
              asyncCommand.Value.StreamRecords.Add (streamRecord);
            }
          }
        }

        // 
        // Call the corresponding registered delegate for the token response.
        // 

        MiResultRecord callbackRecord = record as MiResultRecord;

        if ((callbackRecord != null) && (callbackRecord.Token != 0))
        {
          AsyncCommandData callbackCommandData = null;

          if (m_asyncCommandData.TryGetValue (callbackRecord.Token, out callbackCommandData))
          {
            callbackRecord.Records.AddRange (callbackCommandData.StreamRecords);

            if (callbackCommandData.ResultDelegate != null)
            {
              callbackCommandData.ResultDelegate (callbackRecord);
            }

            lock (m_asyncCommandData)
            {
              m_asyncCommandData.Remove (callbackRecord.Token);
            }
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ProcessStderr (object sendingProcess, DataReceivedEventArgs args)
    {
      try
      {
        m_lastOperationTimestamp = Environment.TickCount;

        if (!string.IsNullOrWhiteSpace (args.Data))
        {
          LoggingUtils.Print (string.Format ("[GdbClient] ProcessStderr: {0}", args.Data));
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ProcessExited (object sendingProcess, EventArgs args)
    {
      try
      {
        m_lastOperationTimestamp = Environment.TickCount;

        LoggingUtils.Print (string.Format ("[GdbClient] ProcessExited: {0}", args));

        m_gdbClientInstance = null;

        // 
        // If we're waiting on a synchronous command, signal a finish to process termination.
        // 

        m_syncCommandLock.Set ();
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
