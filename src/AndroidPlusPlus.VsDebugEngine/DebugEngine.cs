////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Windows.Forms;

using AndroidPlusPlus.Common;
using AndroidPlusPlus.VsDebugCommon;

using Microsoft.VisualStudio.Debugger.Interop;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugEngine
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  [ComVisible (true)]

  [Guid (DebugEngineGuids.guidDebugEngineStringCLSID)]

  [ClassInterface (ClassInterfaceType.None)]

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class DebugEngine : IDebugEngine3, IDebugEngineLaunch2, IDebugEngineProgram2, IDisposable
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private DebugEngineCallback m_sdmCallback = null;

    private CLangDebuggerCallback m_cLangCallback = null;

    private JavaLangDebuggerCallback m_javaLangCallback = null;

    private AutoResetEvent m_broadcastHandleLock;

    private DebugBreakpointManager m_breakpointManager;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebugEngine ()
    {
      m_broadcastHandleLock = new AutoResetEvent (false);

      m_breakpointManager = new DebugBreakpointManager (this);

      Program = null;

      NativeDebugger = null;

      JavaDebugger = null;
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
        if (m_broadcastHandleLock != null)
        {
          m_broadcastHandleLock.Dispose ();

          m_broadcastHandleLock = null;
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebuggeeProgram Program { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebugger NativeDebugger { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public JavaLangDebugger JavaDebugger { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebugBreakpointManager BreakpointManager
    {
      get
      {
        return m_breakpointManager;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Broadcast (IDebugEvent2 debugEvent, IDebugProgram3 program, IDebugThread3 thread)
    {
      Broadcast (debugEvent, program as IDebugProgram2, thread as IDebugThread2);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Broadcast (IDebugEvent2 debugEvent, IDebugProgram2 program, IDebugThread2 thread)
    {
      Guid eventGuid = ComUtils.GuidOf (debugEvent);

      if ((m_cLangCallback != null) && (m_cLangCallback.IsRegistered (ref eventGuid)))
      {
        Broadcast (m_cLangCallback, debugEvent, program, thread);
      }
      else if ((m_javaLangCallback != null) && (m_javaLangCallback.IsRegistered (ref eventGuid)))
      {
        Broadcast (m_javaLangCallback, debugEvent, program, thread);
      }
      else
      {
        Broadcast (m_sdmCallback, debugEvent, program, thread);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Broadcast (IDebugEventCallback2 callback, IDebugEvent2 debugEvent, IDebugProgram2 program, IDebugThread2 thread)
    {
      LoggingUtils.PrintFunction ();

      Guid eventGuid = ComUtils.GuidOf (debugEvent);

      uint eventAttributes = 0;

      LoggingUtils.RequireOk (debugEvent.GetAttributes (out eventAttributes));

      if (((eventAttributes & (uint) enum_EVENTATTRIBUTES.EVENT_STOPPING) != 0) && (thread == null))
      {
        throw new ArgumentNullException (nameof(thread), "For stopping events, this parameter cannot be a null value as the stack frame is obtained from this parameter.");
      }

      try
      {
        int handle = callback.Event (this, null, program, thread, debugEvent, ref eventGuid, eventAttributes);

        if (handle != Constants.E_NOTIMPL)
        {
          LoggingUtils.RequireOk (handle);
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw;
      }
      finally
      {
        if ((eventAttributes & (uint) enum_EVENTATTRIBUTES.EVENT_SYNCHRONOUS) != 0)
        {
          while (!m_broadcastHandleLock.WaitOne (0))
          {
            Thread.Sleep (100);
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Detach (DebuggeeProgram program)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        if ((program.AttachedEngine != null) && (program.AttachedEngine.NativeDebugger != null))
        {
          program.AttachedEngine.NativeDebugger.Kill ();

          program.AttachedEngine.NativeDebugger.Dispose ();

          program.AttachedEngine.NativeDebugger = null;
        }

        if ((program.AttachedEngine != null) && (program.AttachedEngine.JavaDebugger != null))
        {
          program.AttachedEngine.JavaDebugger.Kill ();

          program.AttachedEngine.JavaDebugger.Dispose ();

          program.AttachedEngine.JavaDebugger = null;
        }

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_FAIL;
      }
      finally
      {
        if ((program != null) && (program.AttachedEngine != null))
        {
          Broadcast (new DebugEngineEvent.ProgramDestroy (0), program, null);

          program.AttachedEngine = null;
        }

        m_cLangCallback = null;

        m_javaLangCallback = null;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugEngine2 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Attach (IDebugProgram2 [] rgpPrograms, IDebugProgramNode2 [] rgpProgramNodes, uint celtPrograms, IDebugEventCallback2 ad7Callback, enum_ATTACH_REASON dwReason)
    {
      //
      // Attach the debug engine to a program.
      //

      LoggingUtils.PrintFunction ();

      m_sdmCallback = new DebugEngineCallback (this, ad7Callback);

      m_cLangCallback = new CLangDebuggerCallback (this);

      m_javaLangCallback = new JavaLangDebuggerCallback (this);

      try
      {
        if ((rgpPrograms == null) || (rgpPrograms.Length == 0))
        {
          throw new ApplicationException ("Attach failed. No target process specified.");
        }

        if (celtPrograms > 1)
        {
          throw new ApplicationException ("Attach failed. Can not debug multiple target processes concurrently.");
        }

        if (Program != null)
        {
          throw new ApplicationException ("Attach failed. Already attached to " + Program.DebugProcess.NativeProcess.Name);
        }

        AndroidAdb.Refresh ();

        Program = rgpPrograms [0] as DebuggeeProgram;

        Program.AttachedEngine = this;

        Program.DebugProcess.NativeProcess.RefreshPackageInfo ();

        Broadcast (new DebugEngineEvent.DebuggerConnectionEvent (DebugEngineEvent.DebuggerConnectionEvent.EventType.LogStatus, string.Format ("Starting GDB client...")), null, null);

        NativeDebugger = new CLangDebugger (this, new LaunchConfiguration(), Program);

        Broadcast (new DebugEngineEvent.DebuggerConnectionEvent (DebugEngineEvent.DebuggerConnectionEvent.EventType.LogStatus, string.Format ("Starting JDB client...")), null, null);

        JavaDebugger = new JavaLangDebugger (this, Program);

        ThreadPool.QueueUserWorkItem (delegate (object obj)
        {
          //
          // When this method is called, the DE needs to send these events in sequence:
          // 1. IDebugEngineCreate2
          // 2. IDebugProgramCreateEvent2
          // 3. IDebugLoadCompleteEvent2
          // 4. (if enum_ATTACH_REASON.ATTACH_REASON_LAUNCH), IDebugEntryPointEvent2
          //

          try
          {
            Broadcast (new DebugEngineEvent.EngineCreate (this), Program, null);

            //
            // Run a couple of tests which prevent the run-as tool from functioning properly:
            //
            // 1) Test if this device/emulator is susceptible to a (usually 4.3 specific) run-as permissions bug.
            //      https://code.google.com/p/android/issues/detail?id=58373
            // 2) Test if the installed package is not declared 'debuggable'.
            //

            AndroidDevice debuggingDevice = Program.DebugProcess.NativeProcess.HostDevice;

            string runasPackageFileList = debuggingDevice.Shell (string.Format ("run-as {0}", Program.DebugProcess.NativeProcess.Name), "ls -l");

            if (runasPackageFileList.Contains (string.Format ("run-as: Package '{0}' is unknown", Program.DebugProcess.NativeProcess.Name)))
            {
              throw new InvalidOperationException ("Can not debug native code on this device/emulator.\nMore info: https://code.google.com/p/android/issues/detail?id=58373");
            }
            else if (runasPackageFileList.Contains (string.Format ("run-as: Package '{0}' is not debuggable", Program.DebugProcess.NativeProcess.Name)))
            {
              throw new InvalidOperationException (string.Format ("Package '{0}' is not debuggable.\nPlease ensure you're trying to connect to a 'Debug' application.\nAlternatively, completely uninstall the current app and try again.", Program.DebugProcess.NativeProcess.Name));
            }

            Broadcast (new DebugEngineEvent.DebuggerConnectionEvent (DebugEngineEvent.DebuggerConnectionEvent.EventType.LogStatus, string.Format ("Attaching to '{0}'...", Program.DebugProcess.NativeProcess.Name)), null, null);

            LoggingUtils.RequireOk (Program.Attach (m_sdmCallback), "Failed to attach to target application.");

            CLangDebuggeeThread currentThread = null;

            NativeDebugger.RunInterruptOperation ((CLangDebugger debugger) =>
            {
              debugger.NativeProgram.RefreshAllThreads ();

              // Lack of current thread is usually a good indication that connection/attaching failed.
              currentThread = debugger.NativeProgram.GetThread (debugger.NativeProgram.CurrentThreadId) ?? throw new InvalidOperationException (string.Format ("Failed to retrieve program's main thread (tid: {0}).", debugger.NativeProgram.CurrentThreadId));
            });

            Broadcast (new DebugEngineEvent.ProgramCreate (), Program, null);

            Broadcast (new DebugEngineEvent.LoadComplete (), Program, currentThread);

            if (dwReason == enum_ATTACH_REASON.ATTACH_REASON_LAUNCH)
            {
              Broadcast (new DebugEngineEvent.EntryPoint (), Program, currentThread);
            }

            Broadcast (new DebugEngineEvent.AttachComplete (), Program, null);

            Broadcast (new DebugEngineEvent.DebuggerLogcatEvent (debuggingDevice), Program, null);

            Broadcast (new DebugEngineEvent.DebuggerConnectionEvent (DebugEngineEvent.DebuggerConnectionEvent.EventType.LogStatus, string.Format ("Attached successfully to '{0}'.", Program.DebugProcess.NativeProcess.Name)), null, null);

            Broadcast (new DebugEngineEvent.DebuggerConnectionEvent (DebugEngineEvent.DebuggerConnectionEvent.EventType.CloseDialog, string.Empty), null, null);
          }
          catch (Exception e)
          {
            LoggingUtils.HandleException (e);

            Broadcast (ad7Callback, new DebugEngineEvent.Error (e.Message, true), Program, null);

            Detach (Program);
          }
        });

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        Broadcast (ad7Callback, new DebugEngineEvent.Error (e.Message, true), Program, null);

        Detach (Program);

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int CauseBreak ()
    {
      //
      // Requests all programs being debugged by this DebugEngine instance stop execution (next time one of their threads attempts to run).
      // Normally called in response to user clicking the PAUSE button in the debugger.
      // When the break is complete, an AsyncBreakComplete event will be sent back to the debugger.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        LoggingUtils.RequireOk (Program.DebugProcess.CauseBreak ());

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int ContinueFromSynchronousEvent (IDebugEvent2 eventObject)
    {
      //
      // Called by the Session Debug Manager (SDM) to indicate that a synchronous event, previously sent by the DebugEngine to the SDM,
      // was received and processed. An example of this is 'Program Destroy', which triggers a shutdown of the DebugEngine.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        m_broadcastHandleLock.Set ();

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int CreatePendingBreakpoint (IDebugBreakpointRequest2 pBPRequest, out IDebugPendingBreakpoint2 ppPendingBP)
    {
      //
      // Creates a pending breakpoint for this DebugEngine.
      // A 'PendingBreakpoint' contains all required data to bind a breakpoint to a location in the debuggee.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        LoggingUtils.RequireOk (BreakpointManager.CreatePendingBreakpoint (pBPRequest, out ppPendingBP));

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        ppPendingBP = null;

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int DestroyProgram (IDebugProgram2 pProgram)
    {
      //
      // Informs a DebugEngine that the program specified has been atypically terminated, and that the DebugEngine should
      // clean up all references to the program and send a 'program destroy' event.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int EnumPrograms (out IEnumDebugPrograms2 ppEnum)
    {
      //
      // Retrieves a list of all programs being debugged by a debug engine (DE).
      //

      LoggingUtils.PrintFunction ();

      try
      {
        IDebugProgram2 [] programs = new IDebugProgram2 [] { Program };

        ppEnum = new DebuggeeProgram.Enumerator (programs);

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        ppEnum = null;

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetEngineId (out Guid guidEngine)
    {
      //
      // Gets the GUID of the DebugEngine.
      //

      LoggingUtils.PrintFunction ();

      guidEngine = DebugEngineGuids.guidDebugEngineID;

      return Constants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int LoadSymbols ()
    {
      //
      // Loads (as necessary) symbols for all modules being debugged by this debugging engine.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int RemoveAllSetExceptions (ref Guid guidType)
    {
      //
      // Removes the list of exceptions the IDE has set for a particular run-time architecture or language.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int RemoveSetException (EXCEPTION_INFO [] pException)
    {
      //
      // Removes the specified exception so it is no longer handled by the DebugEngine.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int SetAllExceptions (enum_EXCEPTION_STATE dwState)
    {
      //
      // This method sets the state of all outstanding exceptions.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int SetException (EXCEPTION_INFO [] pException)
    {
      //
      // Specifies how the DebugEngine should handle a given exception.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        //
        // Filter exceptions to those targeted at this debug engine.
        //

        List<EXCEPTION_INFO> filtedExceptions = new List<EXCEPTION_INFO> (pException.Length);

        for (int i = 0; i < pException.Length; ++i)
        {
          if (DebugEngineGuids.guidDebugEngineID.Equals (pException [i].guidType))
          {
            filtedExceptions.Add (pException [i]);
          }
        }

        if (filtedExceptions.Count > 0)
        {
          NativeDebugger.RunInterruptOperation (delegate (CLangDebugger debugger)
          {
            for (int i = 0; i < pException.Length; ++i)
            {
              string exceptionName = filtedExceptions [i].bstrExceptionName;

              exceptionName = exceptionName.Substring (0, exceptionName.IndexOf (' ')); // pick out 'SIG*' identifier.

              GdbClient.Signal signal = debugger.GdbClient.GetClientSignal (exceptionName);

              bool shouldStop = ((filtedExceptions [i].dwState & enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE) != 0);

              signal.SetShouldStop (shouldStop);
            }
          });
        }

        return Constants.S_OK;
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int SetEngineGuid (ref Guid guidEngine)
    {
      //
      // This method sets the debug engine's (DE) GUID.
      //

      LoggingUtils.PrintFunction ();

      return Constants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int SetJustMyCodeState (int fUpdate, uint dwModules, JMC_CODE_SPEC [] rgJMCSpec)
    {
      //
      // This method tells the debug engine about the JustMyCode state information.
      //

      LoggingUtils.PrintFunction ();

      return Constants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int SetLocale (ushort wLangID)
    {
      //
      // Called by the Session Debug Manager (SDM) to propagate the locale settings of the IDE.
      //

      LoggingUtils.PrintFunction ();

      return Constants.S_OK; // Not localised.
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int SetMetric (string pszMetric, object varValue)
    {
      //
      // A metric is a registry value used to change Engine behaviour or advertise supported functionality.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int SetRegistryRoot (string pszRegistryRoot)
    {
      //
      // Sets the registry root for the Engine. Different VS installations can change where their registry data is stored.
      //

      LoggingUtils.PrintFunction ();

      return Constants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int SetSymbolPath (string szSymbolSearchPath, string szSymbolCachePath, uint Flags)
    {
      //
      // Sets the path or paths that are searched for debugging symbols.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        List<string> symbolSearchPaths = new List<string> ();

        if (!string.IsNullOrWhiteSpace (szSymbolSearchPath))
        {
          var symbolsPaths = szSymbolSearchPath.Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries);

          foreach (string path in symbolsPaths)
          {
            symbolSearchPaths.Add (PathUtils.SantiseWindowsPath (path));
          }
        }

        NativeDebugger.RunInterruptOperation ((CLangDebugger debugger) =>
        {
          debugger.GdbClient.SetSetting ("solib-search-path", string.Join<string> (";", symbolSearchPaths), true);

          debugger.GdbClient.SetSetting ("debug-file-directory", string.Join<string> (";", symbolSearchPaths), true);

          BreakpointManager.SetDirty (true);

          BreakpointManager.RefreshBreakpoints ();
        });

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugEngineLaunch2 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int CanTerminateProcess (IDebugProcess2 process)
    {
      //
      // Determines if a process can be terminated.
      //

      LoggingUtils.PrintFunction ();

      return Constants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int LaunchSuspended (string pszServer, IDebugPort2 port, string exe, string args, string dir, string env, string options, enum_LAUNCH_FLAGS launchFlags, uint hStdInput, uint hStdOutput, uint hStdError, IDebugEventCallback2 ad7Callback, out IDebugProcess2 process)
    {
      //
      // Normally, VS launches a program using the IDebugPortEx2::LaunchSuspended method, and the attaches the debugger to the suspended program.
      // However, there are circumstances in which the DebugEngine may need to launch a program or other dependencies (e.g. tools or interpreters) in which case this method is used.
      // IDebugEngineLaunch2::ResumeProcess method is called to start the process after the program has been launched in a suspended state.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        if (port == null)
        {
          throw new ArgumentNullException (nameof(port));
        }

        if (string.IsNullOrEmpty (exe))
        {
          throw new ArgumentNullException (nameof(exe));
        }

        if (!File.Exists (exe))
        {
          throw new FileNotFoundException ("Failed to find target application: " + exe);
        }

        m_sdmCallback = new DebugEngineCallback (this, ad7Callback);

        DebuggeePort debuggeePort = port as DebuggeePort;

        DebuggeeProcess debugProcess = null;

        //
        // Evaluate options; including current debugger target application.
        //

        var launchConfiguration = LaunchConfiguration.FromString (options);

        string packageName = launchConfiguration["PackageName"];

        string launchActivity = launchConfiguration["LaunchActivity"];

        bool debugMode = launchConfiguration["DebugMode"].Equals ("true");

        bool openGlTrace = launchConfiguration["OpenGlTrace"].Equals ("true");

        bool appIsRunning = false;

        //
        // Cache any LaunchSuspended specific parameters.
        //

        launchConfiguration["LaunchSuspendedExe"] = exe;

        launchConfiguration["LaunchSuspendedDir"] = dir;

        launchConfiguration["LaunchSuspendedEnv"] = env;

        //
        // Prevent blocking the main VS thread when launching a suspended application.
        //

        Broadcast (new DebugEngineEvent.DebuggerConnectionEvent (DebugEngineEvent.DebuggerConnectionEvent.EventType.ShowDialog, string.Empty), null, null);

        ManualResetEvent launchSuspendedMutex = new ManualResetEvent (false);

        Thread asyncLaunchSuspendedThread = new Thread (delegate ()
        {
          try
          {
            //
            // Launch application on device in a 'suspended' state.
            //

            Broadcast (new DebugEngineEvent.DebuggerConnectionEvent (DebugEngineEvent.DebuggerConnectionEvent.EventType.LogStatus, string.Format ("Starting '{0}'...", packageName)), null, null);

            if (!appIsRunning)
            {
              StringBuilder launchArgumentsBuilder = new StringBuilder ();

              launchArgumentsBuilder.Append ("start ");

              if (debugMode)
              {
                launchArgumentsBuilder.Append ("-D "); // debug
              }
              else
              {
                launchArgumentsBuilder.Append ("-W "); // wait
              }

              launchArgumentsBuilder.Append ("-S "); // force stop the target app before starting the activity

              if (openGlTrace)
              {
                launchArgumentsBuilder.Append ("--opengl-trace ");
              }

              launchArgumentsBuilder.Append (packageName + "/" + launchActivity);

              Broadcast (new DebugEngineEvent.DebuggerConnectionEvent (DebugEngineEvent.DebuggerConnectionEvent.EventType.LogStatus, string.Format ("[adb:shell:am] {0}", launchArgumentsBuilder)), null, null);

              string launchResponse = debuggeePort.PortDevice.Shell ("am", launchArgumentsBuilder.ToString ());

              if (string.IsNullOrEmpty (launchResponse) || launchResponse.Contains ("Error:"))
              {
                throw new InvalidOperationException ("Launch intent failed:\n" + launchResponse);
              }
            }

            //
            // Query whether the target application is already running. (Double-check)
            //

            int launchAttempt = 1;

            int maxLaunchAttempts = 20;

            while (!appIsRunning)
            {
              Broadcast(new DebugEngineEvent.DebuggerConnectionEvent(DebugEngineEvent.DebuggerConnectionEvent.EventType.LogStatus, string.Format("Waiting for '{0}' to launch (Attempt {1} of {2})...", packageName, launchAttempt, maxLaunchAttempts)), null, null);

              LoggingUtils.RequireOk (debuggeePort.RefreshProcesses ());

              //
              // Validate that the process is running and was spawned by one of the zygote processes.
              //

              uint [] zygotePids = debuggeePort.PortDevice.GetPidsFromName ("zygote");

              uint [] zygote64Pids = debuggeePort.PortDevice.GetPidsFromName ("zygote64");

              uint [] packagePids = debuggeePort.PortDevice.GetPidsFromName (packageName);

              for (int i = packagePids.Length - 1; i >= 0; --i)
              {
                uint pid = packagePids [i];

                AndroidProcess packageProcess = debuggeePort.PortDevice.GetProcessFromPid (pid);

                bool spawnedByZygote = false;

                if ((zygotePids.Length > 0) && (packageProcess.ParentPid == zygotePids [0]))
                {
                  spawnedByZygote = true;
                }
                else if ((zygote64Pids.Length > 0) && (packageProcess.ParentPid == zygote64Pids [0]))
                {
                  spawnedByZygote = true;
                }

                if (spawnedByZygote)
                {
                  debugProcess = debuggeePort.GetProcessForPid (pid);

                  appIsRunning = (debugProcess != null);

                  break;
                }
              }

              if (!appIsRunning)
              {
                if (++launchAttempt > maxLaunchAttempts)
                {
                  throw new TimeoutException (string.Format ("'{0}' failed to launch. Please ensure device is unlocked.", packageName));
                }

                Application.DoEvents ();

                Thread.Sleep (100);
              }
            }

            launchSuspendedMutex.Set ();
          }
          catch (Exception e)
          {
            LoggingUtils.HandleException (e);

            string error = string.Format ("[Exception] {0}\n{1}", e.Message, e.StackTrace);

            Broadcast (ad7Callback, new DebugEngineEvent.Error (error, true), null, null);

            launchSuspendedMutex.Set ();
          }
        });

        asyncLaunchSuspendedThread.Start ();

        while (!launchSuspendedMutex.WaitOne (0))
        {
          Application.DoEvents ();

          Thread.Sleep (100);
        }

        //
        // Attach to launched process.
        //

        process = debugProcess ?? throw new InvalidOperationException ($"{packageName} failed to launch. Could not continue.");

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        process = null;

        try
        {
          string error = string.Format ("[Exception] {0}\n{1}", e.Message, e.StackTrace);

          Broadcast (ad7Callback, new DebugEngineEvent.Error (error, true), null, null);
        }
        catch
        {
          LoggingUtils.HandleException (e);
        }

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int ResumeProcess (IDebugProcess2 process)
    {
      //
      // Resume a process launched by IDebugEngineLaunch2.LaunchSuspended
      //

      LoggingUtils.PrintFunction ();

      try
      {
        //
        // Send a program node to the SDM.
        // This will cause the SDM to turn around and call IDebugEngine2.Attach which will complete the hookup with AD7
        //

        IDebugPort2 port;

        DebuggeeProcess debugProcess = process as DebuggeeProcess;

        LoggingUtils.RequireOk (debugProcess.GetPort (out port));

        DebuggeePort debugPort = port as DebuggeePort;

        LoggingUtils.RequireOk (debugPort.AddProgramNode (debugProcess.DebuggeeProgram));

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int TerminateProcess (IDebugProcess2 process)
    {
      //
      // Terminate a process launched by IDebugEngineLaunch2.LaunchSuspended.
      //
      // The debugger will call IDebugEngineLaunch2.CanTerminateProcess before calling this method.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        DebuggeeProcess debugProcess = (process as DebuggeeProcess);

        LoggingUtils.RequireOk (debugProcess.Terminate ());

        Detach (debugProcess.DebuggeeProgram);

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugEngineProgram2 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Stop ()
    {
      //
      // Stops all threads running in this program.
      // This method is called when this program is being debugged in a multi-program environment.
      // This DebugEngine only supports debugging native applications and therefore only has one program per-process.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int WatchForExpressionEvaluationOnThread (IDebugProgram2 pOriginatingProgram, uint dwTid, uint dwEvalFlags, IDebugEventCallback2 pExprCallback, int fWatch)
    {
      //
      // WatchForExpressionEvaluationOnThread is used to cooperate between two different engines debugging the same process.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int WatchForThreadStep (IDebugProgram2 pOriginatingProgram, uint dwTid, int fWatch, uint dwFrame)
    {
      //
      // WatchForThreadStep is used to cooperate between two different engines debugging the same process.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #endregion

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
