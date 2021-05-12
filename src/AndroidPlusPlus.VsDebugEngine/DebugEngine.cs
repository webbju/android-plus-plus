////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using AndroidPlusPlus.Common;
using AndroidPlusPlus.VsDebugCommon;
using CliWrap.Buffered;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

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

    private AutoResetEvent m_broadcastHandleLock;

    private DebugBreakpointManager m_breakpointManager;

    private Dictionary<string, object> m_metrics;

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

    public DebugBreakpointManager BreakpointManager => m_breakpointManager;

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
      Broadcast (m_sdmCallback, debugEvent, program, thread);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Broadcast (IDebugEventCallback2 callback, IDebugEvent2 debugEvent, IDebugProgram2 program, IDebugThread2 thread)
    {
      LoggingUtils.PrintFunction ();

      Guid eventGuid = ComUtils.GuidOf (debugEvent);

      LoggingUtils.RequireOk(debugEvent.GetAttributes(out uint eventAttributes));

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
#if false
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
#endif
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

        Program = rgpPrograms[0] as DebuggeeProgram;

        Program.AttachedEngine = this;

        ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
          await AndroidAdb.Refresh();

          //
          // Run a couple of tests which prevent the run-as tool from functioning properly:
          //
          // 1) Test if this device/emulator is susceptible to a (usually 4.3 specific) run-as permissions bug.
          //      https://code.google.com/p/android/issues/detail?id=58373
          // 2) Test if the installed package is not declared 'debuggable'.
          //

          AndroidDevice debuggingDevice = Program.DebugProcess.NativeProcess.HostDevice;

          try
          {
            await AndroidAdb.AdbCommand().WithArguments($"-s {debuggingDevice.ID} shell run-as {Program.DebugProcess.NativeProcess.Name} ls -l").ExecuteAsync();
          }
          catch (Exception e)
          {
            throw new InvalidOperationException("Can not debug native code on this device/emulator.\nMore info: https://code.google.com/p/android/issues/detail?id=58373", e);
          }
        });

        var gdbSetup = new GdbSetup(Program.DebugProcess.NativeProcess, Path.Combine(AndroidSettings.NdkRoot, @"prebuilt\windows-x86_64\bin\gdb.exe"));

        var jdbSetup = new JdbSetup(Program.DebugProcess.NativeProcess);

        gdbSetup.DebugFileDirectories.Add(@"C:\webbju\android-plus-plus\msbuild\samples\hello-gdbserver\I686\Debug");

        NativeDebugger = new CLangDebugger (this, Program, gdbSetup);

        JavaDebugger = new JavaLangDebugger (this, Program, jdbSetup);

        var cLangCallback = new CLangDebuggerCallback(NativeDebugger);

        var javaLangCallback = new JavaLangDebuggerCallback(JavaDebugger);

        m_sdmCallback = new DebugEngineCallback(this, cLangCallback, javaLangCallback, ad7Callback);

        LoggingUtils.RequireOk(Program.Attach(m_sdmCallback), "Failed to attach to target application.");

        CLangDebuggeeThread currentThread = null;

        NativeDebugger.RunInterruptOperation(async (CLangDebugger debugger) =>
        {
          await debugger.NativeProgram.RefreshAllThreadsAsync();

          uint currentThreadId = debugger.NativeProgram.CurrentThreadId;

          // Lack of current thread is usually a good indication that connection/attaching failed.
          currentThread = debugger.NativeProgram.GetThread(currentThreadId) ?? throw new InvalidOperationException($"Failed to retrieve program's main thread (tid: {currentThreadId}).");
        });

        Func<DebuggeeProgram, DebuggeeThread, Task<int>> debugEngineBindingHandler = async (DebuggeeProgram program, DebuggeeThread thread) =>
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
            Broadcast(new DebugEngineEvent.EngineCreate(this), program, null);

            Broadcast(new DebugEngineEvent.ProgramCreate(), program, null);

            Broadcast(new DebugEngineEvent.LoadComplete(), program, thread);

            if (dwReason == enum_ATTACH_REASON.ATTACH_REASON_LAUNCH)
            {
              Broadcast(new DebugEngineEvent.EntryPoint(), program, thread);
            }

            Broadcast(new DebugEngineEvent.AttachComplete(), program, null);

            //Broadcast(new DebugEngineEvent.DebuggerLogcatEvent(program.DebugProcess.NativeProcess.HostDevice), Program, null);

            Serilog.Log.Information($"Attached successfully to {program.DebugProcess.NativeProcess.Name}'");
          }
          catch (Exception e)
          {
            LoggingUtils.HandleException(e);

            Broadcast(ad7Callback, new DebugEngineEvent.Error(e.Message, true), program, null);

            Broadcast(new DebugEngineEvent.ProgramDestroy(0), program, null);
          }

          return Constants.S_OK;
        };

        ThreadPool.QueueUserWorkItem((object state) =>
        {
          debugEngineBindingHandler(Program, currentThread);
        }, null);

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

      LoggingUtils.Print($"ContinueFromSynchronousEvent: {eventObject.GetType()}");

      try
      {
        if (ComUtils.GuidOf(eventObject).Equals(ComUtils.GuidOf(typeof(DebugEngineEvent.ProgramDestroy))))
        {
          if (Program.AttachedEngine?.NativeDebugger != null)
          {
            Program.AttachedEngine.NativeDebugger.Kill();

            Program.AttachedEngine.NativeDebugger.Dispose();

            Program.AttachedEngine.NativeDebugger = null;
          }

          if (Program.AttachedEngine?.JavaDebugger != null)
          {
            Program.AttachedEngine.JavaDebugger.Kill();

            Program.AttachedEngine.JavaDebugger.Dispose();

            Program.AttachedEngine.JavaDebugger = null;
          }

          Program.AttachedEngine = null;
        }

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
        ppEnum = new DebuggeeProgram.Enumerator (new IDebugProgram2[] { Program });

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
        LoggingUtils.RequireOk(Program.EnumModules(out IEnumDebugModules2 ppEnum));

        LoggingUtils.RequireOk(ppEnum.GetCount(out uint count));

        var debugModules = new DebuggeeModule[count];

        LoggingUtils.RequireOk(ppEnum.Next(count, debugModules, ref count));

        foreach (var module in debugModules)
        {
          LoggingUtils.RequireOk(module.LoadSymbols());
        }

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

        var filtedExceptions = new List<EXCEPTION_INFO> (pException.Length);

        for (int i = 0; i < pException.Length; ++i)
        {
          if (DebugEngineGuids.guidDebugEngineID.Equals (pException [i].guidType))
          {
            filtedExceptions.Add (pException [i]);
          }
        }

        if (filtedExceptions.Count > 0)
        {
          NativeDebugger.RunInterruptOperation (async (CLangDebugger debugger) =>
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

      LoggingUtils.Print ($"SetEngineGuid: {guidEngine}");

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

      m_metrics ??= new Dictionary<string, object>();

      m_metrics[pszMetric] = varValue;

      return Constants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int SetRegistryRoot (string pszRegistryRoot)
    {
      //
      // Sets the registry root for the Engine. Different VS installations can change where their registry data is stored.
      //

      LoggingUtils.Print ($"SetRegistryRoot: {pszRegistryRoot}");

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
        if (string.IsNullOrWhiteSpace (szSymbolSearchPath))
        {
          return Constants.S_OK; // Nothing to do.
        }

        var symbolSearchPaths = new List<string> ();

        var symbolsPaths = szSymbolSearchPath.Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string path in symbolsPaths)
        {
          symbolSearchPaths.Add (PathUtils.SantiseWindowsPath (path));
        }

        NativeDebugger.RunInterruptOperation (async (CLangDebugger debugger) =>
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

        //
        // Evaluate options; including current debugger target application.
        //

        var launchConfiguration = JsonConvert.DeserializeObject<Dictionary<string, string>> (options);

        string packageName = launchConfiguration["PackageName"];

        //
        // Cache any LaunchSuspended specific parameters.
        //

        launchConfiguration["LaunchSuspendedExe"] = exe;

        launchConfiguration["LaunchSuspendedDir"] = dir;

        launchConfiguration["LaunchSuspendedEnv"] = env;

        //
        // Prevent blocking the main VS thread when launching a suspended application.
        //

        Func<DebuggeePort, Task> launchAppViaIntent = async (DebuggeePort port) =>
        {
          //
          // Launch application on device in a 'suspended' state.
          //

          AndroidDevice device = port.PortDevice;

          Serilog.Log.Information($"[{GetType().Name}] Starting {packageName} on {device.ID} ...");

          var launchArgumentsBuilder = new StringBuilder();

          launchArgumentsBuilder.Append("start ");

          if ((launchFlags & enum_LAUNCH_FLAGS.LAUNCH_NODEBUG) != 0)
          {
            launchArgumentsBuilder.Append("-W "); // wait
          }
          else
          {
            launchArgumentsBuilder.Append("-D "); // debug

            launchArgumentsBuilder.Append("-N "); // enable native debugging
          }

          launchArgumentsBuilder.Append("-S "); // force stop the target app before starting the activity

          if (launchConfiguration.TryGetValue("LaunchActivity", out string launchActivity) && !string.IsNullOrWhiteSpace(launchActivity))
          {
            launchArgumentsBuilder.Append(packageName + "/" + launchActivity);
          }
          else
          {
            var resolveActivity = await AndroidAdb.AdbCommand().WithArguments($"-s {device.ID} shell \"cmd package resolve-activity --brief {packageName} | tail -1\"").ExecuteBufferedAsync();

            using var reader = new StringReader(resolveActivity.StandardOutput);

            launchArgumentsBuilder.Append(await reader.ReadLineAsync());
          }

          await AndroidAdb.AdbCommand().WithArguments($"-s {device.ID} shell am {launchArgumentsBuilder}").ExecuteBufferedAsync();
        };

        Func<DebuggeePort, Task<DebuggeeProcess>> waitForRunningStatus = async (DebuggeePort port) =>
        {
          try
          {
            //
            // Query whether the target application is already running. (Double-check)
            //

            int maxLaunchAttempts = 25;

            for (int launchAttempt = 1; launchAttempt <= maxLaunchAttempts; ++launchAttempt)
            {
              Serilog.Log.Information($"Waiting for {packageName} to launch (attempt {launchAttempt} of {maxLaunchAttempts}) ...");

              //
              // Validate that the process is running and was spawned by one of the zygote processes.
              //

              await port.RefreshProcessesAsync();

              foreach (var portProcess in port.PortProcesses.Where(port => port.Value.NativeProcess.IsUserProcess))
              {
                PROCESS_INFO[] processInfo = new PROCESS_INFO[1];

                LoggingUtils.RequireOk(portProcess.Value.GetInfo(enum_PROCESS_INFO_FIELDS.PIF_ALL, processInfo));

                if (string.Equals(packageName, processInfo[0].bstrTitle))
                {
                  Serilog.Log.Information($"Found {packageName} (pid: {processInfo[0].ProcessId})");

                  return portProcess.Value;
                }
              }

              await System.Threading.Tasks.Task.Delay(500);
            }
          }
          catch (Exception e)
          {
            LoggingUtils.HandleException (e);

            Broadcast (ad7Callback, new DebugEngineEvent.Error ($"[Exception] {e.Message}\n{e.StackTrace}", true), null, null);
          }

          return null;
        };

        process = ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
          await launchAppViaIntent(port as DebuggeePort);

          return await waitForRunningStatus(port as DebuggeePort);
        });

        if (process == null)
        {
          throw new TimeoutException($"{packageName} failed to launch. Could not continue.");
        }

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

        DebuggeeProcess debugProcess = process as DebuggeeProcess;

        LoggingUtils.RequireOk (debugProcess.GetPort (out IDebugPort2 port));

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

        Broadcast(new DebugEngineEvent.ProgramDestroy(1), debugProcess.DebuggeeProgram, null);

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

}
