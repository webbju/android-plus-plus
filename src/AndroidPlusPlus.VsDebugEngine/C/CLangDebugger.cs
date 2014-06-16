////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.VisualStudio.Debugger.Interop;
using AndroidPlusPlus.Common;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugEngine
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class CLangDebugger : IDisposable
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public delegate void InterruptOperation ();

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private GdbSetup m_gdbSetup = null;

    private uint m_interruptOperationCounter = 0;

    private bool m_interruptOperationWasRunning = false;

    private ManualResetEvent m_interruptOperationCompleted = null;

    private Dictionary<string, uint> m_threadGroupStatus = new Dictionary<string, uint> ();

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebugger (DebugEngine debugEngine, DebuggeeProgram debugProgram)
    {
      Engine = debugEngine;

      NativeProgram = new CLangDebuggeeProgram (this, debugProgram);

      NativeMemoryBytes = new CLangDebuggeeMemoryBytes (this);

      // 
      // Evaluate the most up-to-date deployment of GDB provided in the registered SDK. Look at the target device and determine architecture.
      // 

      string androidNdkRoot = AndroidSettings.NdkRoot;

      string androidNdkToolchains = Path.Combine (androidNdkRoot, "toolchains");

      string archGdbToolPrefix = string.Empty;

      switch (debugProgram.DebugProcess.NativeProcess.HostDevice.GetProperty ("ro.product.cpu.abi"))
      {
        case "armeabi":
        case "armeabi-v7a":
        {
          archGdbToolPrefix = "arm-linux-androideabi";

          break;
        }

        case "x86":
        {
          archGdbToolPrefix = "i686";

          break;
        }

        case "mips":
        {
          archGdbToolPrefix = "mipsel-linux-android";

          break;
        }
      }

      string gdbExecutablePattern = string.Format ("{0}-gdb.exe", archGdbToolPrefix);

      string [] gdbMatches = Directory.GetFiles (androidNdkToolchains, gdbExecutablePattern, SearchOption.AllDirectories);

      if (gdbMatches.Length == 0)
      {
        throw new FileNotFoundException ("Could not find location for GDB: " + gdbExecutablePattern);
      }

      for (int i = gdbMatches.Length - 1; i >= 0; --i)
      {
        if (gdbMatches [i].Contains ("_x86_64") && !Environment.Is64BitOperatingSystem)
        {
          continue;
        }

        m_gdbSetup = new GdbSetup (debugProgram.DebugProcess.NativeProcess, gdbMatches [i]);

        break;
      }

      if (m_gdbSetup == null)
      {
        throw new InvalidOperationException ("Could not evaluate GDB instance.");
      }

      GdbServer = new GdbServer (m_gdbSetup);

      GdbClient = new GdbClient (m_gdbSetup);

      GdbClient.OnResultRecord = OnClientResultRecord;

      GdbClient.OnAsyncRecord = OnClientAsyncRecord;

      GdbClient.OnStreamRecord = OnClientStreamRecord;

      GdbClient.Start ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Dispose ()
    {
      if (GdbClient != null)
      {
        //GdbClient.Stop ();

        GdbClient.Dispose ();

        GdbClient = null;
      }

      if (GdbServer != null)
      {
        //GdbServer.Stop ();

        GdbServer.Dispose ();

        GdbServer = null;
      }

      if (m_gdbSetup != null)
      {
        m_gdbSetup.Dispose ();

        m_gdbSetup = null;
      }
   }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public GdbServer GdbServer { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public GdbClient GdbClient { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebugEngine Engine { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebuggeeProgram NativeProgram { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebuggeeMemoryBytes NativeMemoryBytes { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void RunInterruptOperation (InterruptOperation operation)
    {
      // 
      // Interrupt the GDB session in order to execute the provided delegate in a 'stopped' state.
      // 

      if (m_interruptOperationCounter++ == 0)
      {
        m_interruptOperationWasRunning = NativeProgram.IsRunning;

        if (m_interruptOperationWasRunning)
        {
          // 
          // GDB 'stopped' events usually don't provide a token to which they are associated (only get ^done confirmation).
          // This should block until the requested interrupt has been received and completely handled by VS.
          // 

          m_interruptOperationCompleted = new ManualResetEvent (false);

          GdbClient.Stop ();

          while (!m_interruptOperationCompleted.WaitOne (0))
          {
            Thread.Yield ();
          }

          m_interruptOperationCompleted = null;
        }
      }

      try
      {
        if (operation != null)
        {
          operation ();
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw;
      }
      finally
      {
        if ((--m_interruptOperationCounter == 0) && m_interruptOperationWasRunning)
        {
          GdbClient.Continue ();
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void OnClientResultRecord (MiResultRecord resultRecord)
    {
      LoggingUtils.PrintFunction ();

      switch (resultRecord.Class)
      {
        case "done":
        case "running": // same behaviour (backward compatibility)
        {
          // 
          // "^done" [ "," results ]: The synchronous operation was successful, results are the return values.
          // 

          break;
        }

        case "connected":
        {
          // 
          // ^connected: GDB has connected to a remote target.
          // 

          break;
        }
        
        case "error":
        {
          // 
          // "^error" "," c-string: The operation failed. The c-string contains the corresponding error message.
          // 

          break;
        }
        
        case "exit":
        {
          // 
          // ^exit: GDB has terminated.
          // 

          break;
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void OnClientStreamRecord (MiStreamRecord streamRecord)
    {
      switch (streamRecord.Type)
      {
        case MiStreamRecord.StreamType.Console:
        {
          // The console output stream contains text that should be displayed in the CLI console window. It contains the textual responses to CLI commands. 

          LoggingUtils.Print (string.Format ("[CLangDebugger] Console: {0}", streamRecord.Stream));

          break;
        }

        case MiStreamRecord.StreamType.Target:
        {
          // The console output stream contains text that should be displayed in the CLI console window. It contains the textual responses to CLI commands. 

          LoggingUtils.Print (string.Format ("[CLangDebugger] Target: {0}", streamRecord.Stream));

          break;
        }

        case MiStreamRecord.StreamType.Log:
        {
          // The log stream contains debugging messages being produced by gdb's internals.

          LoggingUtils.Print (string.Format ("[CLangDebugger] Log: {0}", streamRecord.Stream));

          break;
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void OnClientAsyncRecord (MiAsyncRecord asyncRecord)
    {
      LoggingUtils.PrintFunction ();

      switch (asyncRecord.Type)
      {
        case MiAsyncRecord.AsyncType.Exec: 
        {
          // 
          // Records prefixed '*'.
          // 

          switch (asyncRecord.Class)
          {
            case "running":
            {
              // 
              // The target is now running. The thread field tells which specific thread is now running, can be 'all' if every thread is running.
              // 

              NativeProgram.SetRunning (true);

              string threadId = asyncRecord ["thread-id"] [0].GetString ();

              if (threadId.Equals ("all"))
              {
                Dictionary<uint, DebuggeeThread> programThreads = NativeProgram.GetThreads ();

                foreach (DebuggeeThread thread in programThreads.Values)
                {
                  thread.SetRunning (true);
                }
              }
              else
              {
                uint numericThreadId = uint.Parse (threadId);

                NativeProgram.CurrentThreadId = numericThreadId;

                CLangDebuggeeThread thread = NativeProgram.GetThread (numericThreadId);

                if (thread != null)
                {
                  thread.SetRunning (true);
                }
              }

              break;
            }

            case "stopped":
            {
              // 
              // The target has stopped.
              // 

              NativeProgram.SetRunning (false);

              CLangDebuggeeThread stoppedThread = null;

              if (asyncRecord.HasField ("thread-id"))
              {
                uint threadId = asyncRecord ["thread-id"] [0].GetUnsignedInt ();

                stoppedThread = NativeProgram.GetThread (threadId);

                stoppedThread.SetRunning (false);

                NativeProgram.CurrentThreadId = threadId;
              }

              // 
              // Flag some or all of the program's threads as stopped, directed by 'stopped-threads' field.
              // 

              bool hasStoppedThreads = asyncRecord.HasField ("stopped-threads");

              if (hasStoppedThreads)
              {
                // 
                // If all threads are stopped, the stopped field will have the value of "all". 
                // Otherwise, the value of the stopped field will be a list of thread identifiers.
                // 

                MiResultValue stoppedThreadsRecord = asyncRecord ["stopped-threads"] [0];

                if (stoppedThreadsRecord is MiResultValueList)
                {
                  MiResultValueList stoppedThreads = stoppedThreadsRecord as MiResultValueList;

                  foreach (MiResultValue stoppedThreadValue in stoppedThreads.List)
                  {
                    uint stoppedThreadId = stoppedThreadValue.GetUnsignedInt ();

                    CLangDebuggeeThread thread = NativeProgram.GetThread (stoppedThreadId);

                    if (thread != null)
                    {
                      thread.SetRunning (false);
                    }
                  }
                }
                else
                {
                  Dictionary<uint, DebuggeeThread> programThreads = NativeProgram.GetThreads ();

                  foreach (DebuggeeThread thread in programThreads.Values)
                  {
                    thread.SetRunning (false);
                  }
                }
              }

              if (m_interruptOperationCompleted != null)
              {
                // 
                // Unblocks waiting for 'stopped' to be processed. Skipping event handling during interrupt requests as it confuses VS debugger flow.
                // 

                m_interruptOperationCompleted.Set ();
              }
              else
              {
                // 
                // The reason field can have one of the following values:
                // 

                if (asyncRecord.HasField ("reason"))
                {
                  switch (asyncRecord ["reason"] [0].GetString ())
                  {
                    case "breakpoint-hit":
                    case "watchpoint-trigger":
                    {
                      bool canContinue = true;

                      uint breakpointId = asyncRecord ["bkptno"] [0].GetUnsignedInt ();

                      DebuggeeBreakpointBound boundBreakpoint = Engine.BreakpointManager.GetBoundBreakpoint (breakpointId);

                      if (boundBreakpoint == null)
                      {
                        throw new InvalidOperationException ("Could not locate a registered breakpoint with matching id: " + breakpointId);
                      }

                      enum_BP_STATE [] breakpointState = new enum_BP_STATE [1];

                      LoggingUtils.RequireOk (boundBreakpoint.GetState (breakpointState));

                      if (breakpointState [0] == enum_BP_STATE.BPS_DELETED)
                      {
                        // 
                        // Hit a breakpoint which internally is flagged as deleted. Oh noes!
                        // 

                        DebugEngineEvent.Exception exception = new DebugEngineEvent.Exception (NativeProgram.DebugProgram, "Breakpoint #" + breakpointId, asyncRecord ["reason"] [0].GetString (), 0x80000000, canContinue);

                        Engine.Broadcast (exception, NativeProgram.DebugProgram, stoppedThread);
                      }
                      else
                      {
                        // 
                        // Hit a breakpoint which is known about. Issue break event.
                        // 

                        IEnumDebugBoundBreakpoints2 enumeratedBoundBreakpoint = new DebuggeeBreakpointBound.Enumerator (new List<IDebugBoundBreakpoint2> { boundBreakpoint });

                        Engine.Broadcast (new DebugEngineEvent.BreakpointHit (enumeratedBoundBreakpoint), NativeProgram.DebugProgram, stoppedThread);
                      }

                      break;
                    }

                    case "end-stepping-range":
                    case "function-finished":
                    {
                      Engine.Broadcast (new DebugEngineEvent.StepComplete (), NativeProgram.DebugProgram, stoppedThread);

                      break;
                    }

                    case "signal-received":
                    {
                      string signalName = asyncRecord ["signal-name"] [0].GetString ();

                      string signalMeaning = asyncRecord ["signal-meaning"] [0].GetString ();

                      switch (signalName)
                      {
                        case null:
                        case "SIGINT":
                        {
                          Engine.Broadcast (new DebugEngineEvent.Break (), NativeProgram.DebugProgram, stoppedThread);

                          break;
                        }

                        default:
                        {
                          bool canContinue = true;

                          string signalDescription = string.Format ("{0} ({1})", signalName, signalMeaning);

                          DebugEngineEvent.Exception exception = new DebugEngineEvent.Exception (NativeProgram.DebugProgram, signalName, signalDescription, 0x80000000, canContinue);

                          Engine.Broadcast (exception, NativeProgram.DebugProgram, stoppedThread);

                          break;
                        }
                      }

                      break;
                    }

                    case "read-watchpoint-trigger":
                    case "access-watchpoint-trigger":
                    case "location-reached":
                    case "watchpoint-scope":
                    case "solib-event":
                    case "fork":
                    case "vfork":
                    case "syscall-entry":
                    case "exec":
                    {
                      Engine.Broadcast (new DebugEngineEvent.Break (), NativeProgram.DebugProgram, stoppedThread);

                      break;
                    }

                    case "exited":
                    case "exited-normally":
                    case "exited-signalled":
                    {
                      //Engine.Broadcast (new DebugEngineEvent.Break (), NativeProgram.DebugProgram, NativeProgram.GetThread (NativeProgram.CurrentThreadId));

                      uint exitCode = 0;

                      Engine.Broadcast (new DebugEngineEvent.ProgramDestroy (exitCode), NativeProgram.DebugProgram, null);

                      break;
                    }
                  }
                }
              }

              break;
            }
          }

          break;
        }

        case MiAsyncRecord.AsyncType.Status:
        {
          // 
          // Records prefixed '+'.
          // 

          break;
        }


        case MiAsyncRecord.AsyncType.Notify:
        {
          // 
          // Records prefixed '='.
          // 

          switch (asyncRecord.Class)
          {
            case "thread-group-added":
            case "thread-group-started":
              {
                // 
                // A thread group became associated with a running program, either because the program was just started or the thread group was attached to a program.
                // 

                string threadGroupId = asyncRecord ["id"] [0].GetString ();

                m_threadGroupStatus [threadGroupId] = 0;

                break;
              }

            case "thread-group-removed":
            case "thread-group-exited":
            {
              // 
              // A thread group is no longer associated with a running program, either because the program has exited, or because it was detached from.
              // 

              string threadGroupId = asyncRecord ["id"] [0].GetString ();

              if (asyncRecord.HasField ("exit-code"))
              {
                m_threadGroupStatus [threadGroupId] = asyncRecord ["exit-code"] [0].GetUnsignedInt ();
              }

              break;
            }

            case "thread-created":
            {
              // 
              // A thread either was created. The id field contains the gdb identifier of the thread. The gid field identifies the thread group this thread belongs to. 
              // 

              uint threadId = asyncRecord ["id"] [0].GetUnsignedInt ();

              string threadGroupId = asyncRecord ["group-id"] [0].GetString ();

              CLangDebuggeeThread createdThread = new CLangDebuggeeThread (NativeProgram, threadId);

              NativeProgram.AddThread (createdThread);

              Engine.Broadcast (new DebugEngineEvent.ThreadCreate (), NativeProgram.DebugProgram, createdThread);

              break;
            }

            case "thread-exited":
            {
              // 
              // A thread has exited. The 'id' field contains the GDB identifier of the thread. The 'group-id' field identifies the thread group this thread belongs to. 
              // 

              uint threadId = asyncRecord ["id"] [0].GetUnsignedInt ();

              string threadGroupId = asyncRecord ["group-id"] [0].GetString ();

              uint exitCode = m_threadGroupStatus [threadGroupId];

              CLangDebuggeeThread exitedThread = NativeProgram.GetThread (threadId);

              NativeProgram.RemoveThread (exitedThread, exitCode);

              break;
            }

            case "thread-selected":
            {
              // 
              // Informs that the selected thread was changed as result of the last command.
              // 

              uint threadId = asyncRecord ["id"] [0].GetUnsignedInt ();

              NativeProgram.CurrentThreadId = threadId;

              break;
            }

            case "library-loaded":
            {
              // 
              // Reports that a new library file was loaded by the program.
              // 

              try
              {
                ThreadPool.QueueUserWorkItem (delegate (object state)
                {
                  try
                  {
                    CLangDebuggeeModule module = new CLangDebuggeeModule (Engine, asyncRecord);

                    NativeProgram.AddModule (module);

                    Engine.Broadcast (new DebugEngineEvent.ModuleLoad (module as IDebugModule2, true), NativeProgram.DebugProgram, null);

                    if (module.SymbolsLoaded)
                    {
                      Engine.Broadcast (new DebugEngineEvent.BeforeSymbolSearch (module as IDebugModule3), NativeProgram.DebugProgram, null);

                      Engine.Broadcast (new DebugEngineEvent.SymbolSearch (module as IDebugModule3, module.Name), NativeProgram.DebugProgram, null);
                    }

                    Engine.BreakpointManager.RefreshBoundBreakpoints ();
                  }
                  catch (Exception e)
                  {
                    LoggingUtils.HandleException (e);
                  }
                });
              }
              catch (Exception e)
              {
                LoggingUtils.HandleException (e);
              }

              break;
            }

            case "library-unloaded":
            {
              // 
              // Reports that a library was unloaded by the program.
              // 

              try
              {
                ThreadPool.QueueUserWorkItem (delegate (object state)
                {
                  string moduleName = asyncRecord ["id"] [0].GetString ();

                  CLangDebuggeeModule module = NativeProgram.GetModule (moduleName);

                  Engine.Broadcast (new DebugEngineEvent.ModuleLoad (module as IDebugModule2, false), NativeProgram.DebugProgram, null);

                  NativeProgram.RemoveModule (module);
                });
              }
              catch (Exception e)
              {
                LoggingUtils.HandleException (e);
              }

              break;
            }

            case "breakpoint-created":
            {
              break;
            }

            case "breakpoint-modified":
            {
              break;
            }

            case "breakpoint-deleted":
            {
              break;
            }
          }

          break;
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebuggeeCodeContext GetCodeContextForLocation (string location)
    {
      if (location.StartsWith ("0x"))
      {
        location = "*" + location;
      }

      MiResultRecord resultInfoLine = GdbClient.SendCommand ("info line " + location);

      if ((resultInfoLine == null) || ((resultInfoLine != null) && resultInfoLine.IsError ()))
      {
        throw new InvalidOperationException ();
      }

      string infoRegExPattern = "Line (?<line>[0-9]+) of \\\\\"(?<file>[^\"]+)\\\\\" starts at address (?<start>[^ ]+) (?<startsym>[^ ]+) and ends at (?<end>[^ ]+) (?<endsym>[^ .]+).";

      Regex regExMatcher = new Regex (infoRegExPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

      foreach (MiStreamRecord record in resultInfoLine.Records)
      {
        Match regExLineMatch = regExMatcher.Match (record.Stream);

        if (regExLineMatch.Success)
        {
          uint line = uint.Parse (regExLineMatch.Result ("${line}"));

          string filename = regExLineMatch.Result ("${file}");

          DebuggeeAddress startAddress = new DebuggeeAddress (regExLineMatch.Result ("${start}"));

          DebuggeeAddress endAddress = new DebuggeeAddress (regExLineMatch.Result ("${end}"));

          TEXT_POSITION [] documentPositions = new TEXT_POSITION [2];

          documentPositions [0].dwLine = line - 1;

          documentPositions [0].dwColumn = 0;

          documentPositions [1].dwLine = documentPositions [0].dwLine;

          documentPositions [1].dwColumn = uint.MaxValue;

          DebuggeeDocumentContext documentContext = new DebuggeeDocumentContext (Engine, filename, documentPositions [0], documentPositions [1], DebugEngineGuids.guidLanguageCpp, startAddress);

          return new DebuggeeCodeContext (Engine, documentContext, startAddress);
        }
      }

      return null;
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
