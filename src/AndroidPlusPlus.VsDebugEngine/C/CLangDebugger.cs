////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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

    private GdbSetup m_gdbSetup;

    private uint m_interruptOperationCounter = 0;

    private bool m_interruptOperationWasRunning = false;

    private ManualResetEvent m_interruptOperationCompleted = null;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebugger (DebugEngine debugEngine, DebuggeeProgram debugProgram)
    {
      Engine = debugEngine;

      NativeProgram = new CLangDebuggeeProgram (this, debugProgram);

      NativeMemoryBytes = new CLangDebuggeeMemoryBytes (this);

      string gdbToolPath = Engine.LaunchConfiguration ["GdbTool"];

      string [] libraryPaths = Engine.LaunchConfiguration ["LibraryPaths"].Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries);

      m_gdbSetup = new GdbSetup (debugProgram.DebugProcess.NativeProcess, gdbToolPath, libraryPaths);

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

      lock (this)
      {
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
        lock (this)
        {
          if ((--m_interruptOperationCounter == 0) && m_interruptOperationWasRunning)
          {
            GdbClient.Continue ();
          }
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

          Debug.WriteLine (string.Format ("[CLangDebugger] Console: {0}", streamRecord.Stream));

          break;
        }

        case MiStreamRecord.StreamType.Target:
        {
          // The console output stream contains text that should be displayed in the CLI console window. It contains the textual responses to CLI commands. 

          Debug.WriteLine (string.Format ("[CLangDebugger] Target: {0}", streamRecord.Stream));

          break;
        }

        case MiStreamRecord.StreamType.Log:
        {
          // The log stream contains debugging messages being produced by gdb's internals.

          Debug.WriteLine (string.Format ("[CLangDebugger] Log: {0}", streamRecord.Stream));

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

              string threadId = asyncRecord ["thread-id"].GetString ();

              if (threadId.Equals ("all"))
              {
                NativeProgram.SetRunning (true, true);
              }
              else
              {
                NativeProgram.SetRunning (true, false);

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

              CLangDebuggeeThread thread = null;

              if (asyncRecord.HasField ("thread-id"))
              {
                uint threadId = asyncRecord ["thread-id"].GetUnsignedInt ();

                thread = NativeProgram.GetThread (threadId);

                NativeProgram.CurrentThreadId = threadId;
              }

              bool hasStoppedThreads = asyncRecord.HasField ("stopped-threads");

              // 
              // Flag some or all of the program's threads as stopped, directed by 'stopped-threads' field.
              // 

              if (hasStoppedThreads && (asyncRecord ["stopped-threads"] is MiResultValueList))
              {
                MiResultValueList stoppedThreads = asyncRecord ["stopped-threads"] as MiResultValueList;

                //Engine.Broadcast (new DebugEngineEvent.StopComplete (), NativeProgram.DebugProgram, NativeProgram.GetThread (NativeProgram.CurrentThreadId));

                NativeProgram.SetRunning (true, false);

                throw new NotImplementedException ();
              }
              else 
              {
                NativeProgram.SetRunning (false, true);

                //Engine.Broadcast (new DebugEngineEvent.StopComplete (), NativeProgram.DebugProgram, NativeProgram.GetThread (NativeProgram.CurrentThreadId));
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
                  switch (asyncRecord ["reason"].GetString ())
                  {
                    case "breakpoint-hit":
                    case "watchpoint-trigger":
                    {
                      bool canContinue = true;

                      uint breakpointId = asyncRecord ["bkptno"].GetUnsignedInt ();

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

                        DebugEngineEvent.Exception exception = new DebugEngineEvent.Exception (NativeProgram.DebugProgram, "Breakpoint #" + breakpointId, asyncRecord ["reason"].GetString (), 0x80000000, canContinue);

                        Engine.Broadcast (exception, NativeProgram.DebugProgram, NativeProgram.GetThread (NativeProgram.CurrentThreadId));
                      }
                      else
                      {
                        // 
                        // Hit a breakpoint which is known about. Issue break event.
                        // 

                        IEnumDebugBoundBreakpoints2 enumeratedBoundBreakpoint = new DebuggeeBreakpointBound.Enumerator (new List<IDebugBoundBreakpoint2> { boundBreakpoint });

                        Engine.Broadcast (new DebugEngineEvent.BreakpointHit (enumeratedBoundBreakpoint), NativeProgram.DebugProgram, NativeProgram.GetThread (NativeProgram.CurrentThreadId));
                      }

                      break;
                    }

                    case "end-stepping-range":
                    case "function-finished":
                    {
                      Engine.Broadcast (new DebugEngineEvent.StepComplete (), NativeProgram.DebugProgram, NativeProgram.GetThread (NativeProgram.CurrentThreadId));

                      break;
                    }

                    case "signal-received":
                    {
                      string signalName = asyncRecord ["signal-name"].GetString ();

                      string signalMeaning = asyncRecord ["signal-meaning"].GetString ();

                      switch (signalName)
                      {
                        case null:
                        case "SIGINT":
                        {
                          Engine.Broadcast (new DebugEngineEvent.Break (), NativeProgram.DebugProgram, NativeProgram.GetThread (NativeProgram.CurrentThreadId));

                          break;
                        }

                        default:
                        {
                          bool canContinue = true;

                          string signalDescription = string.Format ("{0} ({1})", signalName, signalMeaning);

                          DebugEngineEvent.Exception exception = new DebugEngineEvent.Exception (NativeProgram.DebugProgram, signalName, signalDescription, 0x80000000, canContinue);

                          Engine.Broadcast (exception, NativeProgram.DebugProgram, NativeProgram.GetThread (NativeProgram.CurrentThreadId));

                          break;
                        }
                      }

                      break;
                    }

                    case "exited-signalled":
                    {
                      Engine.TerminateProcess (NativeProgram.DebugProgram.DebugProcess);

                      break;
                    }

                    case "read-watchpoint-trigger":
                    case "access-watchpoint-trigger":
                    case "location-reached":
                    case "watchpoint-scope":
                    case "exited":
                    case "exited-normally":
                    case "solib-event":
                    case "fork":
                    case "vfork":
                    case "syscall-entry":
                    case "exec":
                    {
                      Engine.Broadcast (new DebugEngineEvent.Break (), NativeProgram.DebugProgram, NativeProgram.GetThread (NativeProgram.CurrentThreadId));

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
            {
              break;
            }

            case "thread-group-removed":
            {
              break;
            }

            case "thread-group-started":
            {
              break;
            }

            case "thread-group-exited":
            {
              break;
            }

            case "thread-created":
            {
              // 
              // A thread either was created. The id field contains the gdb identifier of the thread. The gid field identifies the thread group this thread belongs to. 
              // 

              uint threadId = asyncRecord ["id"].GetUnsignedInt ();

              CLangDebuggeeThread thread = new CLangDebuggeeThread (NativeProgram, threadId);

              NativeProgram.AddThread (thread);

              Engine.Broadcast (new DebugEngineEvent.ThreadCreate (), NativeProgram.DebugProgram, thread);

              break;
            }

            case "thread-exited":
            {
              // 
              // A thread has exited. The id field contains the gdb identifier of the thread. The gid field identifies the thread group this thread belongs to. 
              // 

              uint exitCode = 0;

              uint threadId = asyncRecord ["id"].GetUnsignedInt ();

              CLangDebuggeeThread thread = NativeProgram.GetThread (threadId);

              Engine.Broadcast (new DebugEngineEvent.ThreadDestroy (exitCode), NativeProgram.DebugProgram, thread);

              break;
            }

            case "thread-selected":
            {
              // 
              // Informs that the selected thread was changed as result of the last command.
              // 

              uint threadId = asyncRecord ["id"].GetUnsignedInt ();

              NativeProgram.CurrentThreadId = threadId;

              break;
            }

            case "library-loaded":
            {
              // 
              // Reports that a new library file was loaded by the program.
              // 

              CLangDebuggeeModule module = new CLangDebuggeeModule (Engine, asyncRecord);

              NativeProgram.AddModule (module);

              Engine.Broadcast (new DebugEngineEvent.ModuleLoad (module as IDebugModule2, true), NativeProgram.DebugProgram, null);

              if (module.SymbolsLoaded)
              {
                Engine.Broadcast (new DebugEngineEvent.BeforeSymbolSearch (module as IDebugModule3), NativeProgram.DebugProgram, null);

                Engine.Broadcast (new DebugEngineEvent.SymbolSearch (module as IDebugModule3, module.Name), NativeProgram.DebugProgram, null);
              }

              Engine.BreakpointManager.RefreshBoundBreakpoints ();

              break;
            }

            case "library-unloaded":
            {
              // 
              // Reports that a library was unloaded by the program.
              // 

              string moduleName = asyncRecord ["id"].GetString ();

              CLangDebuggeeModule module = NativeProgram.GetModule (moduleName);

              Engine.Broadcast (new DebugEngineEvent.ModuleLoad (module as IDebugModule2, false), NativeProgram.DebugProgram, null);

              NativeProgram.RemoveModule (module);

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

  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
