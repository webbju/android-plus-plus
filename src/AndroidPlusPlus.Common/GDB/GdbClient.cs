////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.Common
{

  public class GdbClient : RedirectEventListener, IDisposable
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
      public string Command { get; set; }

      public List<MiStreamRecord> StreamRecords { get; set; } = new List<MiStreamRecord>();

      public OnResultRecordDelegate ResultDelegate { get; set; }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class Signal
    {
      private readonly GdbClient m_gdbClient;

      public Signal (GdbClient gdbClient, string name, string description, bool shouldStop, bool shouldPassToProgram)
      {
        m_gdbClient = gdbClient;

        Name = name;

        Description = description;

        ShouldStop = shouldStop;

        ShouldPassToProgram = shouldPassToProgram;
      }

      public void SetShouldStop (bool shouldStop)
      {
        if (ShouldStop != shouldStop)
        {
          string command = string.Format ("-interpreter-exec console \"handle {0} {1}\"", Name, shouldStop ? "stop" : "nostop");

          m_gdbClient.SendCommand (command, (MiResultRecord resultRecord) =>
          {
            MiResultRecord.RequireOk (resultRecord, command);

            ShouldStop = shouldStop;
          });
        }
      }

      public void SetShouldPassToProgram (bool shouldPassToProgram)
      {
        if (ShouldPassToProgram != shouldPassToProgram)
        {
          string command = $"-interpreter-exec console \"handle {Name} {(shouldPassToProgram ? "pass" : "nopass")}\"";

          m_gdbClient.SendCommand (command, (MiResultRecord resultRecord) =>
          {
            MiResultRecord.RequireOk (resultRecord, command);

            ShouldPassToProgram = shouldPassToProgram;
          });
        }
      }

      public string Name { get; protected set; }

      public string Description { get; protected set; }

      public bool ShouldStop { get; protected set; }

      public bool ShouldPassToProgram { get; protected set; }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly GdbSetup m_gdbSetup;

    private AsyncRedirectProcess m_gdbClientInstance = null;

    private HashSet<string> m_gdbSupportedClientMiFeatures = new HashSet<string> ();

    private HashSet<string> m_gdbSupportedTargetMiFeatures = new HashSet<string> ();

    private ConcurrentDictionary<string, Signal> m_gdbSupportedClientSignals = new ConcurrentDictionary<string, Signal> ();

    private ConcurrentDictionary<uint, AsyncCommandData> m_asyncCommandData = new ConcurrentDictionary<uint, AsyncCommandData> ();

    private Stopwatch m_timeSinceLastOperation = new Stopwatch ();

    private ManualResetEvent m_sessionStarted = new ManualResetEvent (false);

    private uint m_sessionCommandToken = 0;

    private ConcurrentDictionary<uint, string> m_registerIdMapping = new ConcurrentDictionary<uint, string> ();

    private ConcurrentQueue<MiAsyncRecord> m_asyncRecordWorkerQueue = new ConcurrentQueue<MiAsyncRecord> ();

    private Thread m_asyncRecordWorkerThread = null;

    private ManualResetEvent m_asyncRecordWorkerThreadSignal = new ManualResetEvent (false);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool IsAttached { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public GdbClient (GdbSetup gdbSetup)
    {
      LoggingUtils.PrintFunction ();

      m_gdbSetup = gdbSetup;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Dispose ()
    {
      Dispose (true);

      GC.SuppressFinalize (this);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual void Dispose (bool disposing)
    {
      if (disposing)
      {
        if (m_gdbClientInstance != null)
        {
          m_gdbClientInstance.Dispose ();

          m_gdbClientInstance = null;
        }

        if (m_asyncRecordWorkerThreadSignal != null)
        {
          m_asyncRecordWorkerThreadSignal.Dispose ();

          m_asyncRecordWorkerThreadSignal = null;
        }

        if (m_sessionStarted != null)
        {
          m_sessionStarted.Dispose ();

          m_sessionStarted = null;
        }
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

      var gdbExecutionCommands = new List<string>
      {
        "set target-async on",
        "set logging overwrite on",
        "set logging on",
        "set logging file " + PathUtils.SantiseWindowsPath(Path.Combine(m_gdbSetup.CacheDirectory, "gdb.log")),
#if DEBUG
        //"set debug remote 1",
        //"set debug infrun 1",
        "set verbose on",
#endif
      };

      string gdbExecutionScript = Path.Combine(m_gdbSetup.CacheDirectory, "gdb.setup");

      Directory.CreateDirectory(Path.GetDirectoryName(gdbExecutionScript));

      using (var writer = new StreamWriter (gdbExecutionScript))
      {
        foreach (string command in gdbExecutionCommands)
        {
          writer.WriteLine (command);
        }
      }

      //
      // Spawn a new GDB instance which executes gdb.setup.
      //

      string clientArguments = m_gdbSetup.GdbToolArguments + string.Format (@" -fullname -x {0}", PathUtils.SantiseWindowsPath (Path.Combine (m_gdbSetup.CacheDirectory, "gdb.setup")));

      m_gdbClientInstance = new AsyncRedirectProcess (m_gdbSetup.GdbToolPath, clientArguments);

      m_gdbClientInstance.Start (this);

      m_timeSinceLastOperation.Start ();

      uint timeout = 15000;

      bool responseSignaled = false;

      while ((!responseSignaled) && (m_timeSinceLastOperation.ElapsedMilliseconds < timeout))
      {
        responseSignaled = m_sessionStarted.WaitOne (0);

        if (!responseSignaled)
        {
          Thread.Sleep (100);
        }
      }

      if (!responseSignaled)
      {
        throw new TimeoutException ("Timed out waiting for GDB client to execute");
      }

      //
      // Create asynchronous job queue thread.
      //

      if (m_asyncRecordWorkerThread == null)
      {
        m_asyncRecordWorkerThread = new Thread (AsyncOutputWorkerThreadBody);

        m_asyncRecordWorkerThread.Start ();
      }

      //
      // Evaluate this client's GDB/MI support and capabilities.
      //

      try
      {
        SendCommand ("-list-features", (MiResultRecord resultRecord) =>
        {
          MiResultRecord.RequireOk (resultRecord, "-list-features");

          if (resultRecord.HasField ("features"))
          {
            foreach (MiResultValue feature in resultRecord ["features"] [0].Values)
            {
              m_gdbSupportedClientMiFeatures.Add (feature.GetString ());
            }
          }
        });
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw;
      }

      //
      // Evaluate available signals and their current 'should stop' status.
      //

      try
      {
        string command = string.Format ("-interpreter-exec console \"info signals\"");

        SendCommand (command, (MiResultRecord resultRecord) =>
        {
          MiResultRecord.RequireOk (resultRecord, command);

          string pattern = @"(?<sig>[^ ]+)[ ]+(?<stop>[^\\t]+)\\t(?<print>[^\\t]+)\\t(?<pass>[^\\t]+)\\t\\t(?<desc>[^\\]+)";

          Regex regExMatcher = new Regex (pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

          for (int i = 2; i < resultRecord.Records.Count; ++i) // Skip the first rows (2) of headers
          {
            MiStreamRecord record = resultRecord.Records [i];

            Match regExLineMatch = regExMatcher.Match (record.Stream);

            if (regExLineMatch.Success)
            {
              string sig = regExLineMatch.Result ("${sig}");

              bool stop = regExLineMatch.Result ("${stop}").Equals ("Yes", StringComparison.OrdinalIgnoreCase);

              bool print = regExLineMatch.Result ("${print}").Equals ("Yes", StringComparison.OrdinalIgnoreCase);

              bool passToProgram = regExLineMatch.Result ("${pass}").Equals ("Yes", StringComparison.OrdinalIgnoreCase);

              string desc = regExLineMatch.Result ("${desc}");

              Signal signal = new Signal (this, sig, desc, stop, passToProgram);

              m_gdbSupportedClientSignals.TryAdd (sig, signal);
            }
          }
        });
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Kill ()
    {
      LoggingUtils.PrintFunction ();

      try
      {
        string command = "-gdb-exit";

        SendCommand (command, (MiResultRecord resultRecord) =>
        {
          MiResultRecord.RequireOk (resultRecord, command);
        });
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async Task Attach (GdbServer gdbServer)
    {
      LoggingUtils.PrintFunction ();

      await m_gdbSetup.ClearPortForwarding ();

      await m_gdbSetup.SetupPortForwarding ();

      SetSetting ("auto-solib-add", "on", false);

      SetSetting ("stop-on-solib-events", "0", false);

      SetSetting ("breakpoint pending", "on", false); // an unrecognized breakpoint location should result in a "pending" breakpoint being created.

      //
      // Pull required system and application binaries from the device.
      // TODO: Use 'ndk-depends' to figure out which system shared libraries are used and pull these.
      //

      var sharedLibrarySearchPaths = new HashSet<string> ();

      var debugFileDirectoryPaths = new HashSet<string> ();

      var cachedSystemBinaries = await m_gdbSetup.CacheSystemBinaries ();

      var cachedSystemLibraries = await m_gdbSetup.CacheSystemLibraries ();

      var cachedApplicationLibraries = await m_gdbSetup.CacheApplicationLibraries();

      sharedLibrarySearchPaths.Add (PathUtils.SantiseWindowsPath (m_gdbSetup.CacheSysRoot)); // prioritise sysroot parent.

      foreach (string systemBinary in cachedSystemLibraries)
      {
        sharedLibrarySearchPaths.Add(PathUtils.SantiseWindowsPath(Path.GetDirectoryName(systemBinary)));
      }

      foreach (string systemLibrary in cachedSystemLibraries)
      {
        sharedLibrarySearchPaths.Add (PathUtils.SantiseWindowsPath(Path.GetDirectoryName(systemLibrary)));
      }

      foreach (string applicationLibrary in cachedApplicationLibraries)
      {
        sharedLibrarySearchPaths.Add (PathUtils.SantiseWindowsPath(Path.GetDirectoryName(applicationLibrary)));
      }

      SetSetting ("osabi", "GNU/Linux", false);

      SetSetting ("sysroot", PathUtils.SantiseWindowsPath (m_gdbSetup.CacheSysRoot), false);

      SetSetting ("solib-absolute-prefix", PathUtils.SantiseWindowsPath (m_gdbSetup.CacheSysRoot), false);

      SetSetting ("solib-search-path", string.Join (";", sharedLibrarySearchPaths), true);

      SetSetting ("debug-file-directory", string.Join (";", m_gdbSetup.DebugFileDirectories.Select(path => PathUtils.SantiseWindowsPath(path))), true);

      try
      {
        bool is64Bit = m_gdbSetup.Process.GetSupportedCpuAbis().Where(abi => abi.Contains("64")).Any();

        string cachedAppProcess = Path.Combine(m_gdbSetup.CacheSysRoot, is64Bit ? @"system\bin\app_process64" : @"system\bin\app_process");

        if (!File.Exists(cachedAppProcess))
        {
          throw new InvalidOperationException(string.Format("Could not locate target binary: {0}", cachedAppProcess));
        }

        string command = "-file-exec-and-symbols " + PathUtils.SantiseWindowsPath (cachedAppProcess);

        MiResultRecord resultRecord = SendSyncCommand (command);

        MiResultRecord.RequireOk (resultRecord, command);
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw;
      }

      //
      // Connect to the remote target.
      //

      try
      {
        string command = string.Format ("-target-select remote {0}:{1}", m_gdbSetup.Host, m_gdbSetup.Port);

        SendCommand (command, (MiResultRecord resultRecord) =>
        {
          MiResultRecord.RequireOk(resultRecord, command);
        });

        IsAttached = gdbServer.WaitAttached(60000); // 60 seconds

        if (!IsAttached)
        {
          throw new TimeoutException("GdbServer did not report attach status.");
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw;
      }

      //
      // Evaluate target's GDB/MI support and capabilities.
      //

      try
      {
        string command = "-list-target-features";

        SendCommand(command, (MiResultRecord resultRecord) =>
        {
          MiResultRecord.RequireOk(resultRecord, command);

          if (resultRecord.HasField("features"))
          {
            foreach (MiResultValue feature in resultRecord["features"][0].Values)
            {
              m_gdbSupportedTargetMiFeatures.Add(feature.GetString());
            }
          }
        });
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw;
      }

      //
      // Evaluate target's registers (and their names).
      //

      try
      {
        string command = "-data-list-register-names";

        SendCommand(command, (MiResultRecord resultRecord) =>
        {
          MiResultRecord.RequireOk(resultRecord, command);

          if (resultRecord.HasField("register-names"))
          {
            MiResultValue registerNames = resultRecord["register-names"][0];

            for (int i = 0; i < registerNames.Values.Count; ++i)
            {
              string register = registerNames[i].GetString();

              m_registerIdMapping.TryAdd((uint)i, register);
            }
          }
        });
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Detach ()
    {
      LoggingUtils.PrintFunction ();

      try
      {
        string command = "-target-detach";

        SendCommand(command, (MiResultRecord resultRecord) =>
        {
          MiResultRecord.RequireOk(resultRecord, command);
        });
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw;
      }
      finally
      {
        IsAttached = false;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Stop ()
    {
      LoggingUtils.PrintFunction ();

      try
      {
        string command = "-exec-interrupt";

        SendCommand(command, (MiResultRecord resultRecord) =>
        {
          MiResultRecord.RequireOk(resultRecord, command);
        });
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Continue ()
    {
      LoggingUtils.PrintFunction ();

      try
      {
        string command = "-exec-continue";

        SendCommand(command, (MiResultRecord resultRecord) =>
        {
          MiResultRecord.RequireOk(resultRecord, command);
        });
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Terminate ()
    {
      //
      // If the program has already terminated, the 'kill' command will error with a description - treat this as success.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        string command = "-interpreter-exec console \"kill\"";

        SendCommand(command, (MiResultRecord resultRecord) =>
        {
          MiResultRecord.RequireOk(resultRecord, command);
        });
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void StepInto (uint threadId, StepType stepType, bool reverse)
    {
      LoggingUtils.PrintFunction ();

      switch (stepType)
      {
        case StepType.Statement:
        case StepType.Line:
        {
          //
          // -exec-step
          // Resumes execution of the inferior program, stopping when the beginning of the next source line is reached, if the next source line is not a function call.
          // If it is, stop at the first instruction of the called function.
          //

          string command = string.Format ("-exec-step --thread {0} {1}", threadId, ((reverse) ? "--reverse" : ""));

          SendCommand(command, (MiResultRecord resultRecord) =>
          {
            MiResultRecord.RequireOk(resultRecord, command);
          });

          break;
        }

        case StepType.Instruction:
        {
          //
          // -exec-step-instruction
          // Resumes the inferior which executes one machine instruction.
          //

          string command = string.Format ("-exec-step-instruction --thread {0} {1}", threadId, ((reverse) ? "--reverse" : ""));

          SendCommand (command, (MiResultRecord resultRecord) =>
          {
            MiResultRecord.RequireOk(resultRecord, command);
          });

          break;
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void StepOut (uint threadId, StepType stepType, bool reverse)
    {
      LoggingUtils.PrintFunction ();

      switch (stepType)
      {
        case StepType.Statement:
        case StepType.Line:
        case StepType.Instruction:
        {
          string command = string.Format ("-exec-finish --thread {0} {1}", threadId, ((reverse) ? "--reverse" : ""));

          SendCommand (command, (MiResultRecord resultRecord) =>
          {
            MiResultRecord.RequireOk (resultRecord, command);
          });

          break;
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void StepOver (uint threadId, StepType stepType, bool reverse)
    {
      LoggingUtils.PrintFunction ();

      switch (stepType)
      {
        case StepType.Line:
        case StepType.Statement:
        {
          //
          // -exec-next
          // Resumes execution of the inferior program, stopping when the beginning of the next source line is reached.
          //

          string command = string.Format ("-exec-next --thread {0} {1}", threadId, ((reverse) ? "--reverse" : ""));

          SendCommand (command, (MiResultRecord resultRecord) =>
          {
            MiResultRecord.RequireOk (resultRecord, command);
          });

          break;
        }

        case StepType.Instruction:
        {
          //
          // -exec-next-instruction
          // Executes one machine instruction. If the instruction is a function call, continues until the function returns.
          //

          string command = string.Format ("-exec-next-instruction --thread {0} {1}", threadId, ((reverse) ? "--reverse" : ""));

          SendCommand (command, (MiResultRecord resultRecord) => MiResultRecord.RequireOk (resultRecord, command));

          break;
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ConcurrentDictionary <uint, string> GetRegisterIdMapping ()
    {
      LoggingUtils.Print (string.Format ("[GdbClient] GetRegisterNameFromId"));

      return m_registerIdMapping;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public Signal GetClientSignal (string sig)
    {
      LoggingUtils.Print (string.Format ("[GdbClient] GetClientSignal: " + sig));

      m_gdbSupportedClientSignals.TryGetValue(sig, out Signal signal);

      return signal;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool GetClientFeatureSupported (string feature)
    {
      LoggingUtils.Print (string.Format ("[GdbClient] GetClientFeatureSupported: " + feature));

      return m_gdbSupportedClientMiFeatures.Contains (feature);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool GetTargetFeatureSupported (string feature)
    {
      LoggingUtils.Print (string.Format ("[GdbClient] GetTargetFeatureSupported: " + feature));

      return m_gdbSupportedTargetMiFeatures.Contains (feature);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string GetSetting (string setting)
    {
      LoggingUtils.Print (string.Format ("[GdbClient] GetSetting ({0})", setting));

      if (string.IsNullOrWhiteSpace (setting))
      {
        throw new ArgumentNullException (nameof(setting));
      }

      string command = string.Format ("-gdb-show {0}", setting);

      MiResultRecord resultRecord = SendSyncCommand (command);

      MiResultRecord.RequireOk (resultRecord, command);

      if (resultRecord.HasField ("value"))
      {
        return resultRecord ["value"] [0].GetString ();
      }

      return string.Empty;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetSetting (string setting, string value, bool appendToExisting)
    {
      LoggingUtils.Print ($"[GdbClient] SetSetting ({setting}): {value}");

      if (string.IsNullOrWhiteSpace (setting))
      {
        throw new ArgumentNullException (nameof(setting));
      }

      if (appendToExisting)
      {
        //
        // Validate that the requested value isn't already set before joining.
        //

        string existingSettingValue = GetSetting (setting);

        if (!string.IsNullOrWhiteSpace (existingSettingValue))
        {
          var existingValues = existingSettingValue.Split (new char [] { ';' });

          foreach (string existing in existingValues)
          {
            if (existing.Equals (value))
            {
              appendToExisting = false;

              break;
            }
          }

          if (appendToExisting && !string.IsNullOrWhiteSpace (value))
          {
            // Prefix the new value so it takes precedence. This is usually what's intended.
            value = string.Join (";", new string [] { value, existingSettingValue });
          }
        }
      }

      SendCommand($"-gdb-set {setting} {value}", (MiResultRecord resultRecord) =>
      {
        MiResultRecord.RequireOk(resultRecord, $"-gdb-set {setting} {value}");
      });
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public MiResultRecord SendSyncCommand (string command, int timeout = 30000)
    {
      //
      // Perform a synchronous command request; issue a standard async command and keep alive whilst still receiving output.
      //

      LoggingUtils.Print (string.Format ("[GdbClient] SendCommand: {0}", command));

      if (string.IsNullOrWhiteSpace (command))
      {
        throw new ArgumentNullException (nameof(command));
      }

      MiResultRecord syncResultRecord = null;

      if (m_gdbClientInstance == null)
      {
        return syncResultRecord;
      }

      var syncCommandLock = new ManualResetEvent(false);

      SendCommand (command, timeout, (MiResultRecord record)  =>
      {
        syncResultRecord = record;

        syncCommandLock.Set();
      });

      //
      // Wait for asynchronous record response (or exit), reset timeout each time new activity occurs.
      //

      bool responseSignaled = false;

      while ((!responseSignaled) && (m_timeSinceLastOperation.ElapsedMilliseconds < timeout))
      {
        responseSignaled = syncCommandLock.WaitOne(0);

        if (!responseSignaled)
        {
          Thread.Sleep(100);
        }
      }

      if (!responseSignaled)
      {
        throw new TimeoutException ("Timed out waiting for synchronous response for command: " + command);
      }

      return syncResultRecord;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public uint SendCommand (string command, OnResultRecordDelegate asyncDelegate)
    {
      return SendCommand (command, 30000, asyncDelegate);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public uint SendCommand (string command, int timeout, OnResultRecordDelegate asyncDelegate = null)
    {
      //
      // Keep track of this command, and associated token-id, so results can be tracked asynchronously.
      //

      LoggingUtils.Print (string.Format ("[GdbClient] SendCommand: {0}", command));

      if (string.IsNullOrWhiteSpace (command))
      {
        throw new ArgumentNullException (nameof(command));
      }

      if (m_gdbClientInstance == null)
      {
        throw new InvalidOperationException ("No GdbClient instance bound");
      }

      m_timeSinceLastOperation.Restart ();

      var commandData = new AsyncCommandData
      {
        Command = command,

        ResultDelegate = asyncDelegate
      };

      ++m_sessionCommandToken;

      if (!m_asyncCommandData.TryAdd (m_sessionCommandToken, commandData))
      {
        throw new Exception("Failed to add tracked async command.");
      }

      //
      // Prepend (and increment) GDB/MI token.
      //

      command = m_sessionCommandToken + command;

      m_gdbClientInstance.SendCommand (command);

      m_timeSinceLastOperation.Restart ();

      return m_sessionCommandToken;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ProcessStdout (object sendingProcess, DataReceivedEventArgs args)
    {
      if (string.IsNullOrEmpty(args.Data))
      {
        return;
      }

      LoggingUtils.Print (string.Format ("[GdbClient] ProcessStdout: {0}", args.Data));

      try
      {
        //
        // Distribute result records to registered delegate callbacks.
        //

        m_timeSinceLastOperation.Restart ();

        MiRecord record = MiInterpreter.ParseGdbOutputRecord (args.Data);

        if ((record is MiPromptRecord) && (m_sessionStarted != null))
        {
          m_sessionStarted.Set ();
        }
        else if ((record is MiAsyncRecord asyncRecord) && (OnAsyncRecord != null))
        {
          m_asyncRecordWorkerQueue.Enqueue (asyncRecord); // Offload async processing.
        }
        else if ((record is MiResultRecord resultRecord) && (OnResultRecord != null))
        {
          OnResultRecord (resultRecord);
        }
        else if ((record is MiStreamRecord streamRecord) && (OnStreamRecord != null))
        {
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
            else if (asyncCommand.Value.Command.StartsWith ("-interpreter-exec console"))
            {
              asyncCommand.Value.StreamRecords.Add (streamRecord);
            }
          }
        }

        //
        // Call the corresponding registered delegate for the token response.
        //

        if (record is MiResultRecord callbackRecord && (callbackRecord.Token != 0))
        {
          if (m_asyncCommandData.TryRemove (callbackRecord.Token, out AsyncCommandData callbackCommandData))
          {
            callbackRecord.Records.AddRange (callbackCommandData.StreamRecords);
          }

          //
          // Spawn any registered callback handlers on a dedicated thread, as not to block GDB output.
          //

          if ((callbackCommandData != null) && (callbackCommandData.ResultDelegate != null))
          {
            Func<Task> resultDelegateHandler = async () =>
            {
              try
              {
                callbackCommandData.ResultDelegate(callbackRecord);
              }
              catch (Exception e)
              {
                LoggingUtils.HandleException(e);
              }
            };

            Task.Run(resultDelegateHandler);
          }
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

    public void ProcessStderr (object sendingProcess, DataReceivedEventArgs args)
    {
      try
      {
        m_timeSinceLastOperation.Restart ();

        LoggingUtils.Print (string.Format ("[GdbClient] ProcessStderr: {0}", args.Data));
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
        m_timeSinceLastOperation.Restart ();

        LoggingUtils.Print (string.Format ("[GdbClient] ProcessExited"));

        if (m_asyncRecordWorkerThreadSignal != null)
        {
          m_asyncRecordWorkerThreadSignal.Set ();
        }

        m_gdbClientInstance = null;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }
      finally
      {
        IsAttached = false;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void AsyncOutputWorkerThreadBody ()
    {
      //
      // Thread body - parses GDB/MI output. If a command has be completely processed, call its associated registered delegate.
      //

      LoggingUtils.Print (string.Format ("[GdbClient] AsyncOutputWorkerThreadBody: Entered"));

      while (!m_asyncRecordWorkerThreadSignal?.WaitOne(0) ?? false)
      {
        if (m_asyncRecordWorkerQueue.TryDequeue (out MiAsyncRecord asyncRecord))
        {
          try
          {
            OnAsyncRecord (asyncRecord);
          }
          catch (Exception e)
          {
            LoggingUtils.HandleException (e);
          }
        }

        Thread.Sleep (10);
      }

      LoggingUtils.Print (string.Format ("[GdbClient] AsyncOutputWorkerThreadBody: Exited"));
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

}
