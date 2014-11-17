////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

    private int m_interruptOperationCounter = 0;

    private ManualResetEvent m_interruptOperationCompleted = null;

    private Dictionary<string, uint> m_threadGroupStatus = new Dictionary<string, uint> ();

    private Dictionary<string, Tuple<ulong, ulong, bool>> m_mappedSharedLibraries = new Dictionary<string, Tuple<ulong, ulong, bool>> ();

    private readonly LaunchConfiguration m_launchConfiguration;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebugger (DebugEngine debugEngine, LaunchConfiguration launchConfiguration, DebuggeeProgram debugProgram)
    {
      Engine = debugEngine;

      m_launchConfiguration = launchConfiguration;

      NativeProgram = new CLangDebuggeeProgram (this, debugProgram);

      NativeMemoryBytes = new CLangDebuggeeMemoryBytes (this);

      VariableManager = new CLangDebuggerVariableManager (this);

      // 
      // Evaluate target device's architecture triple.
      // 

      string [] supportedDeviceAbis = debugProgram.DebugProcess.NativeProcess.HostDevice.SupportedCpuAbis;

      string preferedDeviceAbi = string.Empty;

      bool preferedDeviceAbiIs64Bit = false;

      string preferedDeviceAbiGdbToolPrefix = string.Empty;

      foreach (string deviceAbi in supportedDeviceAbis)
      {
        preferedDeviceAbi = deviceAbi;

        switch (deviceAbi)
        {
          case "armeabi":
          case "armeabi-v7a":
          {
            preferedDeviceAbiGdbToolPrefix = "arm-linux-androideabi";

            preferedDeviceAbiIs64Bit = false;

            break;
          }

          case "arm64-v8a":
          {
            preferedDeviceAbiGdbToolPrefix = "aarch64-linux-android";

            preferedDeviceAbiIs64Bit = true;

            break;
          }

          case "x86":
          {
            preferedDeviceAbiGdbToolPrefix = "i686-linux-android";

            preferedDeviceAbiIs64Bit = false;

            break;
          }

          case "x86_64":
          {
            preferedDeviceAbiGdbToolPrefix = "x86_64-linux-android";

            preferedDeviceAbiIs64Bit = true;

            break;
          }

          case "mips":
          {
            preferedDeviceAbiGdbToolPrefix = "mipsel-linux-android";

            preferedDeviceAbiIs64Bit = false;

            break;
          }

          case "mips64":
          {
            preferedDeviceAbiGdbToolPrefix = "mips64el-linux-android";

            preferedDeviceAbiIs64Bit = true;

            break;
          }
        }

        if (!string.IsNullOrEmpty (preferedDeviceAbiGdbToolPrefix))
        {
          break;
        }
      }

      Engine.Broadcast (new DebugEngineEvent.UiDebugLaunchServiceEvent (DebugEngineEvent.UiDebugLaunchServiceEvent.EventType.LogStatus, string.Format ("Configuring GDB for '{0}' target...", preferedDeviceAbi)), null, null);

      // 
      // Android++ bundles its own copies of GDB to get round various NDK issues. Search for these.
      // 

      string androidPlusPlusRoot = Environment.GetEnvironmentVariable ("ANDROID_PLUS_PLUS");

      string [] contribGdbMatches;

      string contribGdbCommandPath;

      string contribGdbCommandFilePattern = string.Format ("{0}-gdb.cmd", preferedDeviceAbiGdbToolPrefix);

      bool forceNdkR9dClient = (debugProgram.DebugProcess.NativeProcess.HostDevice.SdkVersion <= AndroidSettings.VersionCode.JELLY_BEAN);

      if (/*archIs64Bit && */Environment.Is64BitOperatingSystem)
      {
        string clientIdentifier = (forceNdkR9dClient) ? "7.3.1-x86_64-ndk_r9d" : "7.6.0-x86_64-ndk_r10b";

        contribGdbCommandPath = Path.Combine (androidPlusPlusRoot, "contrib", "redist-gdb-python-x86_64", clientIdentifier);

        contribGdbMatches = Directory.GetFiles (contribGdbCommandPath, contribGdbCommandFilePattern, SearchOption.TopDirectoryOnly);
      }
      else
      {
        string clientIdentifier = (forceNdkR9dClient) ? "7.3.1-x86-ndk_r9d" : "7.6.0-x86-ndk_r10b";

        contribGdbCommandPath = Path.Combine (androidPlusPlusRoot, "contrib", "redist-gdb-python-x86", clientIdentifier);

        contribGdbMatches = Directory.GetFiles (contribGdbCommandPath, contribGdbCommandFilePattern, SearchOption.TopDirectoryOnly);
      }

      if ((contribGdbMatches == null) || (contribGdbMatches.Length == 0))
      {
        throw new InvalidOperationException ("Could not locate required 32/64-bit GDB deployment. Tried: " + contribGdbCommandPath);
      }
      else if (contribGdbMatches.Length > 1)
      {
        throw new InvalidOperationException ("Found multiple files matching GDB search criteria.");
      }
      else
      {
        m_gdbSetup = new GdbSetup (debugProgram.DebugProcess.NativeProcess, contribGdbMatches [0]);
      }

      // 
      // Evaluate the most up-to-date deployment of GDB provided in the registered SDK. Look at the target device and determine architecture.
      // 

      string androidNdkRoot = AndroidSettings.NdkRoot;

      string androidNdkToolchains = Path.Combine (androidNdkRoot, "toolchains");

      string gdbExecutablePattern = string.Format ("{0}-gdb.exe", preferedDeviceAbiGdbToolPrefix);

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

        // 
        // Found a matching (and appropriate) GDB executable. Register this if one wasn't found previously.
        // 

        if (m_gdbSetup == null)
        {
          m_gdbSetup = new GdbSetup (debugProgram.DebugProcess.NativeProcess, gdbMatches [i]);
        }

#if false
        string toolchainSysRoot = Path.GetFullPath (Path.Combine (Path.GetDirectoryName (gdbMatches [i]), ".."));

        string pythonGdbScriptsPath = Path.Combine (toolchainSysRoot, "share", "gdb");

        m_gdbSetup.GdbToolArguments += " --data-directory " + PathUtils.SantiseWindowsPath (pythonGdbScriptsPath);
#endif

        break;
      }

      if (m_gdbSetup == null)
      {
        throw new InvalidOperationException ("Could not evaluate a suitable GDB instance. Ensure you have the correct NDK delpoyment for your system's architecture.");
      }

      if (m_launchConfiguration != null)
      {
        string launchDirectory;

        if (m_launchConfiguration.TryGetValue ("LaunchSuspendedDir", out launchDirectory))
        {
          m_gdbSetup.SymbolDirectories.Add (launchDirectory);
        }
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
      LoggingUtils.PrintFunction ();

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

    public CLangDebuggerVariableManager VariableManager { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void RunInterruptOperation (InterruptOperation operation, bool shouldContinue = true)
    {
      // 
      // Interrupt the GDB session in order to execute the provided delegate in a 'stopped' state.
      // 

      LoggingUtils.PrintFunction ();

      bool targetWasRunning = false;

      try
      {
        if (Interlocked.Increment (ref m_interruptOperationCounter) == 1)
        {
          targetWasRunning = NativeProgram.IsRunning;

          if (targetWasRunning)
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
        try
        {
          if ((Interlocked.Decrement (ref m_interruptOperationCounter) == 0) && targetWasRunning && shouldContinue)
          {
            GdbClient.Continue ();
          }
        }
        catch (Exception e)
        {
          LoggingUtils.HandleException (e);

          throw;
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

          try
          {
            // 
            // If notifications are unsupported, we should assume that we need to refresh breakpoints when connected.
            //

            if (!GdbClient.GetClientFeatureSupported ("breakpoint-notifications"))
            {
              Engine.BreakpointManager.SetDirty ();
            }

            Engine.BreakpointManager.RefreshBreakpoints ();
          }
          catch (Exception e)
          {
            LoggingUtils.HandleException (e);
          }

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
      LoggingUtils.PrintFunction ();

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

          if (streamRecord.Stream.Contains ("Remote communication error"))
          {
            ThreadPool.QueueUserWorkItem (delegate (object state)
            {
              try
              {
                LoggingUtils.RequireOk (Engine.TerminateProcess (NativeProgram.DebugProgram.DebugProcess));
              }
              catch (Exception e)
              {
                LoggingUtils.HandleException (e);
              }
            });
          }

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

                lock (programThreads)
                {
                  foreach (DebuggeeThread thread in programThreads.Values)
                  {
                    thread.SetRunning (true);
                  }
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

                if (stoppedThread != null)
                {
                  stoppedThread.SetRunning (false);
                }

                NativeProgram.CurrentThreadId = threadId;
              }

              if (stoppedThread == null)
              {
                stoppedThread = NativeProgram.GetThread (NativeProgram.CurrentThreadId);
              }

              if (stoppedThread == null)
              {
                throw new InvalidOperationException ("Could not evaluate a thread on which we stopped");
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

                  lock (programThreads)
                  {
                    foreach (DebuggeeThread thread in programThreads.Values)
                    {
                      thread.SetRunning (false);
                    }
                  }
                }
              }

              // 
              // Unblocks waiting for 'stopped' to be processed. Skipping event handling during interrupt requests as it confuses VS debugger flow.
              // 

              bool ignoreInterruptSignal = false;

              if (m_interruptOperationCompleted != null)
              {
                m_interruptOperationCompleted.Set ();

                ignoreInterruptSignal = true;
              }

              // 
              // Process any pending requests to refresh registered breakpoints.
              // 

#if false
              RefreshSharedLibraries ();
#endif

              Engine.BreakpointManager.RefreshBreakpoints ();

              if (true)
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

                      string breakpointMode = asyncRecord ["disp"] [0].GetString ();

                      if (breakpointMode.Equals ("del"))
                      {
                        // 
                        // For temporary breakpoints, we won't have a valid managed object - so will just enforce a break event.
                        // 

                        //Engine.Broadcast (new DebugEngineEvent.Break (), NativeProgram.DebugProgram, stoppedThread);

                        Engine.Broadcast (new DebugEngineEvent.BreakpointHit (null), NativeProgram.DebugProgram, stoppedThread);
                      }
                      else
                      {
                        DebuggeeBreakpointBound boundBreakpoint = Engine.BreakpointManager.FindBoundBreakpoint (breakpointId);

                        if (boundBreakpoint == null)
                        {
                          // 
                          // Could not find the breakpoint we're looking for. Refresh everything and try again.
                          // 

                          Engine.BreakpointManager.SetDirty ();

                          Engine.BreakpointManager.RefreshBreakpoints ();

                          boundBreakpoint = Engine.BreakpointManager.FindBoundBreakpoint (breakpointId);
                        }

                        if (boundBreakpoint == null)
                        {
                          // 
                          // Could not locate a registered breakpoint with matching id.
                          // 

                          DebugEngineEvent.Exception exception = new DebugEngineEvent.Exception (NativeProgram.DebugProgram, "Breakpoint #" + breakpointId, asyncRecord ["reason"] [0].GetString (), 0x00000000, canContinue);

                          Engine.Broadcast (exception, NativeProgram.DebugProgram, stoppedThread);
                        }
                        else
                        {
                          enum_BP_STATE [] breakpointState = new enum_BP_STATE [1];

                          LoggingUtils.RequireOk (boundBreakpoint.GetState (breakpointState));

                          if (breakpointState [0] == enum_BP_STATE.BPS_DELETED)
                          {
                            // 
                            // Hit a breakpoint which internally is flagged as deleted. Oh noes!
                            // 

                            DebugEngineEvent.Exception exception = new DebugEngineEvent.Exception (NativeProgram.DebugProgram, "Breakpoint #" + breakpointId + " [deleted]", asyncRecord ["reason"] [0].GetString (), 0x00000000, canContinue);

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
                        }
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
                          if (!ignoreInterruptSignal)
                          {
                            Engine.Broadcast (new DebugEngineEvent.Break (), NativeProgram.DebugProgram, stoppedThread);
                          }

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
                      // 
                      // React to program termination, but defer this so it doesn't consume the async output thread.
                      // 

                      ThreadPool.QueueUserWorkItem (delegate (object state)
                      {
                        try
                        {
                          LoggingUtils.RequireOk (Engine.TerminateProcess (NativeProgram.DebugProgram.DebugProcess));
                        }
                        catch (Exception e)
                        {
                          LoggingUtils.HandleException (e);
                        }
                      });

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

              try
              {
                string threadGroupId = asyncRecord ["id"] [0].GetString ();

                m_threadGroupStatus [threadGroupId] = 0;
              }
              catch (Exception e)
              {
                LoggingUtils.HandleException (e);
              }

              break;
            }

            case "thread-group-removed":
            case "thread-group-exited":
            {
              // 
              // A thread group is no longer associated with a running program, either because the program has exited, or because it was detached from.
              // 

              try
              {
                string threadGroupId = asyncRecord ["id"] [0].GetString ();

                if (asyncRecord.HasField ("exit-code"))
                {
                  m_threadGroupStatus [threadGroupId] = asyncRecord ["exit-code"] [0].GetUnsignedInt ();
                }
              }
              catch (Exception e)
              {
                LoggingUtils.HandleException (e);
              }

              break;
            }

            case "thread-created":
            {
              // 
              // A thread either was created. The id field contains the gdb identifier of the thread. The gid field identifies the thread group this thread belongs to. 
              // 

              try
              {
                uint threadId = asyncRecord ["id"] [0].GetUnsignedInt ();

                //string threadGroupId = asyncRecord ["group-id"] [0].GetString ();

                NativeProgram.AddThread (threadId);
              }
              catch (Exception e)
              {
                LoggingUtils.HandleException (e);
              }

              break;
            }

            case "thread-exited":
            {
              // 
              // A thread has exited. The 'id' field contains the GDB identifier of the thread. The 'group-id' field identifies the thread group this thread belongs to. 
              // 

              try
              {
                uint threadId = asyncRecord ["id"] [0].GetUnsignedInt ();

                string threadGroupId = asyncRecord ["group-id"] [0].GetString ();

                uint exitCode = m_threadGroupStatus [threadGroupId];

                NativeProgram.RemoveThread (threadId, exitCode);
              }
              catch (Exception e)
              {
                LoggingUtils.HandleException (e);
              }

              break;
            }

            case "thread-selected":
            {
              // 
              // Informs that the selected thread was changed as result of the last command.
              // 

              try
              {
                uint threadId = asyncRecord ["id"] [0].GetUnsignedInt ();

                NativeProgram.CurrentThreadId = threadId;
              }
              catch (Exception e)
              {
                LoggingUtils.HandleException (e);
              }

              break;
            }

            case "library-loaded":
            {
              // 
              // Reports that a new library file was loaded by the program.
              // 

              try
              {
                CLangDebuggeeModule module = new CLangDebuggeeModule (Engine, asyncRecord);

                string moduleName = asyncRecord ["id"] [0].GetString ();

                NativeProgram.AddModule (module);

                Engine.Broadcast (new DebugEngineEvent.ModuleLoad (module as IDebugModule2, true), NativeProgram.DebugProgram, null);

                if (module.SymbolsLoaded)
                {
                  Engine.Broadcast (new DebugEngineEvent.BeforeSymbolSearch (module as IDebugModule3), NativeProgram.DebugProgram, null);

                  Engine.Broadcast (new DebugEngineEvent.SymbolSearch (module as IDebugModule3, module.Name), NativeProgram.DebugProgram, null);
                }

                if (!GdbClient.GetClientFeatureSupported ("breakpoint-notifications"))
                {
                  Engine.BreakpointManager.SetDirty ();
                }
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
                string moduleName = asyncRecord ["id"] [0].GetString ();

                CLangDebuggeeModule module = NativeProgram.GetModule (moduleName);

                NativeProgram.RemoveModule (module);

                Engine.Broadcast (new DebugEngineEvent.ModuleLoad (module as IDebugModule2, false), NativeProgram.DebugProgram, null);

                if (!GdbClient.GetClientFeatureSupported ("breakpoint-notifications"))
                {
                  Engine.BreakpointManager.SetDirty ();
                }
              }
              catch (Exception e)
              {
                LoggingUtils.HandleException (e);
              }

              break;
            }

            case "breakpoint-created":
            case "breakpoint-deleted":
            {
              break;
            }

            case "breakpoint-modified":
            {
              try
              {
                uint number = asyncRecord ["bkpt"] [0] ["number"] [0].GetUnsignedInt ();

                DebuggeeBreakpointPending pendingBreakpoint = Engine.BreakpointManager.FindPendingBreakpoint (number);

                if (pendingBreakpoint != null)
                {
                  pendingBreakpoint.RefreshBoundBreakpoints ();

                  pendingBreakpoint.RefreshErrorBreakpoints ();
                }
                else
                {
                  Engine.BreakpointManager.SetDirty ();
                }
              }
              catch (Exception e)
              {
                LoggingUtils.HandleException (e);
              }

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

    public void RefreshSharedLibraries ()
    {
      // 
      // Retrieve a list of actively mapped shared libraries.
      // - This also triggers GDB to tell us about libraries which it may have missed.
      // 

      try
      {
        string command = string.Format ("-interpreter-exec console \"info sharedlibrary\"");

        MiResultRecord resultRecord = GdbClient.SendCommand (command);

        MiResultRecord.RequireOk (resultRecord, command);

        string pattern = "(?<from>0x[0-9a-fA-F]+)[ ]+(?<to>0x[0-9a-fA-F]+)[ ]+(?<syms>Yes|No)[ ]+(?<lib>[^ $]+)";

        Regex regExMatcher = new Regex (pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        for (int i = 0; i < resultRecord.Records.Count; ++i)
        {
          MiStreamRecord record = resultRecord.Records [i];

          if (!record.Stream.StartsWith ("0x"))
          {
            continue; // early rejection.
          }

          string unescapedStream = Regex.Unescape (record.Stream);

          Match regExLineMatch = regExMatcher.Match (unescapedStream);

          if (regExLineMatch.Success)
          {
            ulong from = ulong.Parse (regExLineMatch.Result ("${from}").Substring (2), NumberStyles.HexNumber);

            ulong to = ulong.Parse (regExLineMatch.Result ("${to}").Substring (2), NumberStyles.HexNumber);

            bool syms = regExLineMatch.Result ("${syms}") == "Yes";

            string lib = regExLineMatch.Result ("${lib}").Replace ("\r", "").Replace ("\n", "");

            m_mappedSharedLibraries [lib] = new Tuple<ulong, ulong, bool> (from, to, syms);
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

    public DebuggeeCodeContext GetCodeContextForLocation (string location)
    {
      try
      {
        if (string.IsNullOrEmpty (location))
        {
          throw new ArgumentNullException ("location");
        }

        if (location.StartsWith ("0x"))
        {
          location = "*" + location;
        }
        else if (location.StartsWith ("\""))
        {
          location = location.Replace ("\\", "/");

          location = location.Replace ("\"", "\\\""); // required to escape the nested string.
        }

        string command = string.Format ("-interpreter-exec console \"info line {0}\"", location);

        MiResultRecord resultRecord = GdbClient.SendCommand (command);

        MiResultRecord.RequireOk (resultRecord, command);

        string pattern = "Line (?<line>[0-9]+) of \"(?<file>[^\\\"]+?)\" starts at address (?<start>[^ ]+) [<]?(?<startsym>[^>]+)[>]? (but contains no code|and ends at (?<end>[^ ]+) [<]?(?<endsym>[^>.]+)[>]?)";

        Regex regExMatcher = new Regex (pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        foreach (MiStreamRecord record in resultRecord.Records)
        {
          if (!record.Stream.StartsWith ("Line"))
          {
            continue; // early rejection.
          }

          string unescapedStream = Regex.Unescape (record.Stream);

          Match regExLineMatch = regExMatcher.Match (unescapedStream);

          if (regExLineMatch.Success)
          {
            string line = regExLineMatch.Result ("${line}");

            string file = regExLineMatch.Result ("${file}");

            string start = regExLineMatch.Result ("${start}");

            string startsym = regExLineMatch.Result ("${startsym}");

            string end = regExLineMatch.Result ("${end}");

            string endsym = regExLineMatch.Result ("${endsym}");

            TEXT_POSITION [] documentPositions = new TEXT_POSITION [2];

            documentPositions [0].dwLine = uint.Parse (line) - 1;

            documentPositions [0].dwColumn = 0;

            documentPositions [1].dwLine = documentPositions [0].dwLine;

            documentPositions [1].dwColumn = uint.MaxValue;

            DebuggeeAddress startAddress = new DebuggeeAddress (start);

            DebuggeeDocumentContext documentContext = new DebuggeeDocumentContext (Engine, file, documentPositions [0], documentPositions [1], DebugEngineGuids.guidLanguageCpp, startAddress);

            return new DebuggeeCodeContext (Engine, documentContext, startAddress);
          }
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
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
