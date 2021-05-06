////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Security;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using AndroidPlusPlus.Common;
using AndroidPlusPlus.VsDebugCommon;
using AndroidPlusPlus.VsDebugEngine;
using System.Threading;
using System.Threading.Tasks;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsIntegratedPackage
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  [ComVisible(true)]

  [Guid(Guids.guidAndroidPlusPlusPackageStringCLSID)]

  //
  // Package registration
  // - Ensure the VSXI plugin is initialised on startup, not adhoc (on first use).
  // - Register the data needed to show the this package in the Help/About dialog of Visual Studio.
  //

  [ProvideObject(typeof(VsIntegratedPackage.AndroidPackage))]

  [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]

  [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]

  [ProvideService(typeof(IDebuggerConnectionService), IsAsyncQueryable = true)]

  [InstalledProductRegistration("Android++", "A native development and debugging solution for Visual Studio.", "0.8")]

  //
  // VsDebugLauncher registration.
  //

  [ProvideDebugLauncher("A7A96E37-9D90-489A-84CB-14BBBE5686D2",
    "AndroidPlusPlus.VsDebugLauncher.DebugLauncher",
    "$PackageFolder$\\AndroidPlusPlus.VsDebugLauncher.dll",
    "AndroidPlusPlus.VsDebugLauncher")]

  //[ProvideObject(typeof(VsIntegratedPackage.DebugLauncher))]

  //[ProvideExternObject(typeof(VsIntegratedPackage.DebugLauncher))]

  //
  // VsDebugEngine registration.
  //

  [ProvideExternObject(typeof(DebugEngine))]

  [ProvideExternObject(typeof(DebugPortSupplier))]

  [ProvideExternObject(typeof(DebugProgramProvider))]

  [ProvideDebugPortSupplier(DebugEngineGuids.guidDebugPortSupplierStringID, "Android++", typeof(DebugPortSupplier))]

  [ProvideDebugEngine(DebugEngineGuids.guidDebugEngineStringID, "Android++", typeof(DebugEngine),
    IncompatibleList = new string []
    {
      DebugEngineGuids.guidIncompatibleDebugEngineSilverlightStringID,
      DebugEngineGuids.guidIncompatibleDebugEngineTSql2000StringID,
      DebugEngineGuids.guidIncompatibleDebugEngineTSql2005StringID,
      DebugEngineGuids.guidIncompatibleDebugEngineNativeStringID,
      DebugEngineGuids.guidIncompatibleDebugEngineManagedStringID,
      DebugEngineGuids.guidIncompatibleDebugEngineManaged20StringID,
      DebugEngineGuids.guidIncompatibleDebugEngineManaged40StringID,
      DebugEngineGuids.guidIncompatibleDebugEngineWorkflowStringID,
      DebugEngineGuids.guidIncompatibleDebugEngineManagedAndNativeStringID,
      DebugEngineGuids.guidIncompatibleDebugEngineScriptStringID,
    },
    PortSupplier = new string []
    {
      DebugEngineGuids.guidDebugPortSupplierStringID,
      "708C1ECA-FF48-11D2-904F-00C04FA302A1"
    },
    //ProgramProvider = typeof(VsDebugEngine.DebugProgramProvider),
    Attach = true,
    Disassembly = true,
    RemoteDebugging = true,
    AlwaysLoadLocal = true,
    AutoSelectPriority = 4,
    AddressBP = true,
    SetNextStatement = true,
    Exceptions = true,
    DataBP = true)]

  [ProvideDebugExtension (DebugEngineGuids.guidDebugEngineStringID,
    "Android++",
    (uint) 0,
    (uint) (enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE | enum_EXCEPTION_STATE.EXCEPTION_STOP_SECOND_CHANCE),
    GroupExtensions = new string []
    {
      // 0x4002 (enum_EXCEPTION_STATE.EXCEPTION_STOP_SECOND_CHANCE | enum_EXCEPTION_STATE.EXCEPTION_JUST_MY_CODE_SUPPORTED)
      // 0x4020 (enum_EXCEPTION_STATE.EXCEPTION_JUST_MY_CODE_SUPPORTED | enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_UNCAUGHT))
      "SIGHUP (Hangup)|0x0001|0x0001",
#if false
      "SIGINT (Interrupt)|0x0002|0x0001",
#endif
      "SIGQUIT (Quit)|0x0003|0x0001",
      "SIGILL (Illegal instruction)|0x0004|0x0001",
      "SIGTRAP (Trace/breakpoint trap)|0x0005|0x0001",
      "SIGABRT (Aborted)|0x0006|0x0001",
      "SIGEMT (Emulation trap)|0x0007|0x0001",
      "SIGFPE (Erroneous arithmetic operation)|0x0008|0x0001",
      "SIGKILL (Killed)|0x0009|0x0001",
      "SIGBUS (Bus error)|0x000A|0x0001",
      "SIGSEGV (Segmentation fault)|0x000B|0x0001",
      "SIGSYS (Bad system call)|0x000C|0x0001",
      "SIGPIPE (Broken pipe)|0x000D|0x0001",
      "SIGALRM (Alarm clock)|0x000E|0x0001",
      "SIGTERM (Terminated)|0x000F|0x0001",

      "SIGUSR1 (User-defined signal 1)|0x0010|0x0001",
      "SIGUSR2 (User-defined signal 2)|0x0011|0x0001",
      "SIGCHLD (Child status changed)|0x0012|0x0001",
      "SIGTSTP (Terminal stop signal|0x0014|0x0001",
      "SIGURG (Urgent I/O condition)|0x0015|0x0001",
      "SIGPOLL (Pollable event)|0x0016|0x0001",
      "SIGSTOP (Stopped (signal))|0x0017|0x0001",
      "SIGSTP (Stopped (user))|0x0018|0x0001",
      "SIGCONT (Continued)|0x0019|0x0001",
      "SIGTTIN (Stopped (tty input))|0x001A|0x0001",
      "SIGTTOU (Stopped (tty output))|0x001B|0x0001",

#if false
      // TODO: These still need codes
      "SIGIO (I/O possible)|0x0000|0x0001",
      "SIGXCPU (CPU time limit exceeded)|0x0000|0x0001",
      "SIGXFSZ (File size limit exceeded)|0x0000|0x0001",
      "SIGVTALRM (Virtual timer expired)|0x0000|0x0001",
      "SIGPROF (Profiling timer expired)|0x0000|0x0001",
      "SIGWINCH (Window size changed)|0x0000|0x0001",
      "SIGLOST (Resource lost)|0x0000|0x0001",
      "SIGUSR1 (User defined signal 1)|0x0000|0x0001",
      "SIGUSR2 (User defined signal 2)|0x0000|0x0001",
      "SIGPWR (Power fail/restart)|0x0000|0x0001",
      "SIGPOLL (Pollable event occurred)|0x0000|0x0001",
      "SIGWIND (SIGWIND)|0x0000|0x0001",
      "SIGPHONE (SIGPHONE)|0x0000|0x0001",
      "SIGWAITING (Process's LWPs are blocked)|0x0000|0x0001",
      "SIGLWP (Signal LWP)|0x0000|0x0001",
      "SIGDANGER (Swap space dangerously low)|0x0000|0x0001",
      "SIGGRANT (Monitor mode granted)|0x0000|0x0001",
      "SIGRETRACT (Need to relinquish monitor mode)|0x0000|0x0001",
      "SIGMSG (Monitor mode data available)|0x0000|0x0001",
      "SIGSOUND (Sound completed)|0x0000|0x0001",
      "SIGSAK (Secure attention)|0x0000|0x0001",
      "SIGPRIO (SIGPRIO)|0x0000|0x0001",
      "SIGCANCEL (LWP internal signal)|0x0000|0x0001",
      "SIGINFO (Information request)|0x0000|0x0001",
      "EXC_BAD_ACCESS (Could not access memory)|0x0000|0x0001",
      "EXC_BAD_INSTRUCTION (Illegal instruction/operand)|0x0000|0x0001",
      "EXC_ARITHMETIC (Arithmetic exception)|0x0000|0x0001",
      "EXC_EMULATION (Emulation instruction)|0x0000|0x0001",
      "EXC_SOFTWARE (Software generated exception)|0x0000|0x0001",
      "EXC_BREAKPOINT (Breakpoint)|0x0000|0x0001",
      "SIG32 (Real-time event 32)|0x0000|0x0001",
      "SIG33 (Real-time event 33)|0x0000|0x0001",
      "SIG34 (Real-time event 34)|0x0000|0x0001",
      "SIG35 (Real-time event 35)|0x0000|0x0001",
      "SIG36 (Real-time event 36)|0x0000|0x0001",
      "SIG37 (Real-time event 37)|0x0000|0x0001",
      "SIG38 (Real-time event 38)|0x0000|0x0001",
      "SIG39 (Real-time event 39)|0x0000|0x0001",
      "SIG40 (Real-time event 40)|0x0000|0x0001",
      "SIG41 (Real-time event 41)|0x0000|0x0001",
      "SIG42 (Real-time event 42)|0x0000|0x0001",
      "SIG43 (Real-time event 43)|0x0000|0x0001",
      "SIG44 (Real-time event 44)|0x0000|0x0001",
      "SIG45 (Real-time event 45)|0x0000|0x0001",
      "SIG46 (Real-time event 46)|0x0000|0x0001",
      "SIG47 (Real-time event 47)|0x0000|0x0001",
      "SIG48 (Real-time event 48)|0x0000|0x0001",
      "SIG49 (Real-time event 49)|0x0000|0x0001",
      "SIG50 (Real-time event 50)|0x0000|0x0001",
      "SIG51 (Real-time event 51)|0x0000|0x0001",
      "SIG52 (Real-time event 52)|0x0000|0x0001",
      "SIG53 (Real-time event 53)|0x0000|0x0001",
      "SIG54 (Real-time event 54)|0x0000|0x0001",
      "SIG55 (Real-time event 55)|0x0000|0x0001",
      "SIG56 (Real-time event 56)|0x0000|0x0001",
      "SIG57 (Real-time event 57)|0x0000|0x0001",
      "SIG58 (Real-time event 58)|0x0000|0x0001",
      "SIG59 (Real-time event 59)|0x0000|0x0001",
      "SIG60 (Real-time event 60)|0x0000|0x0001",
      "SIG61 (Real-time event 61)|0x0000|0x0001",
      "SIG62 (Real-time event 62)|0x0000|0x0001",
      "SIG63 (Real-time event 63)|0x0000|0x0001",
      // ... up to SIG127
#endif
    }
  )]

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public sealed class AndroidPackage : AsyncPackage
    {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private AssemblyResolveListener m_assemblyResolveListener;

    private PropertyEventListener m_propertyEventListener;

    private DebuggerEventListener m_debuggerEventListener;

    private SolutionEventListener m_solutionEventListener;

    private TextWriterTraceListener m_traceWriterListener;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidPackage ()
    {
      LoggingUtils.PrintFunction ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region Microsoft.VisualStudio.Shell.Package Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override async System.Threading.Tasks.Task InitializeAsync (CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
      await base.InitializeAsync(cancellationToken, progress);

      LoggingUtils.PrintFunction ();

      InitialiseTraceListeners ();

      AddService(typeof(IDebuggerConnectionService), CreateDebuggerConnectionServiceAsync);


      //InitialiseEventListeners ();

      //
      // Sanity type checking.
      //
#if false
      try
      {
        /*if (typeof (AndroidPlusPlus.VsIntegratedPackage.Package) != System.Type.GetTypeFromCLSID (Guids.guidAndroidPlusPlusPackageCLSID))
        {
          throw new COMException ("AndroidPlusPlus.VsIntegratedPackage.Package not registered with COM");
        }*/

        /*if (typeof (AndroidPlusPlus.VsDebugEngine.DebugEngine) != System.Type.GetTypeFromCLSID (DebugEngineGuids.guidDebugEngineCLSID))
        {
          throw new COMException ("AndroidPlusPlus.VsDebugEngine.DebugEngine not registered with COM");
        }*/
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        VsShellUtilities.ShowMessageBox (this, e.Message, "Android++ Debugger", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
      }

      /*try
      {
        //
        // Evaluate the current target HKLM registry location for this version of VisualStudio.
        //

        RegistryKey visualStudioPlatformRoot = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Wow6432Node\Microsoft\VisualStudio\10.0");

        if (visualStudioPlatformRoot == null)
        {
          // Running on a native 32-bit OS.

          visualStudioPlatformRoot = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\VisualStudio\10.0");
        }

        //
        // Traverse custom RegisterAttributes explictly registering their data within HKLM due to limitiations with DE architecture.
        //

        IEnumerable<ProvideExternObjectAttribute> provideExternObjectAttributes = typeof (AndroidMTPackage).GetCustomAttributes (typeof (ProvideExternObjectAttribute), false).Cast<ProvideExternObjectAttribute> ();

        IEnumerable<DebugPortSupplierRegistrationAttribute> debugPortSupplierRegistrationAttributes = typeof (AndroidMTPackage).GetCustomAttributes (typeof (DebugPortSupplierRegistrationAttribute), false).Cast<DebugPortSupplierRegistrationAttribute> ();

        IEnumerable<DebugEngineRegistrationAttribute> debugEngineRegistrationAttributes = typeof (AndroidMTPackage).GetCustomAttributes (typeof (DebugEngineRegistrationAttribute), false).Cast<DebugEngineRegistrationAttribute> ();

        foreach (ProvideExternObjectAttribute attribute in provideExternObjectAttributes)
        {
          attribute.Register (visualStudioPlatformRoot);
        }

        foreach (DebugPortSupplierRegistrationAttribute attribute in debugPortSupplierRegistrationAttributes)
        {
          attribute.Register (visualStudioPlatformRoot);
        }

        foreach (DebugEngineRegistrationAttribute attribute in debugEngineRegistrationAttributes)
        {
          attribute.Register (visualStudioPlatformRoot);
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }*/
#endif
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void Dispose (bool disposing)
    {
      if (disposing)
      {
        DisposeTraceListeners();

        base.Dispose(disposing);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private async Task<object> CreateDebuggerConnectionServiceAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
    {
      //
      // Create a service to manage the 'attach' status dialog. As we need to access this via VsDebugLauncher/VsDebugEngine.
      //

      var launchService = new DebuggerConnectionService ();

      await launchService.InitializeAsync(this, cancellationToken);

      return launchService;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void InitialiseTraceListeners ()
    {
      LoggingUtils.PrintFunction ();

      DateTime logTime = DateTime.Now;

      string traceLog = string.Format (@"{0}\Android++\{1:D4}-{2:D2}-{3:D2}.log", Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), logTime.Year, logTime.Month, logTime.Day);

      LoggingUtils.Print ("[Package] Trace Log: " + traceLog);

      m_traceWriterListener = new TextWriterTraceListener (traceLog);

      Trace.Listeners.Add (m_traceWriterListener);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void DisposeTraceListeners ()
    {
      LoggingUtils.PrintFunction ();

      Trace.Flush ();

      if (m_traceWriterListener != null)
      {
        Trace.Listeners.Remove (m_traceWriterListener);

        m_traceWriterListener.Close ();

        m_traceWriterListener.Dispose ();
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void InitialiseEventListeners ()
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      //
      // Acquire VisualStudio service references.
      //

      LoggingUtils.PrintFunction ();

      EnvDTE.DTE dteService = GetService (typeof (SDTE)) as EnvDTE.DTE;

      IVsShell shellService = GetService (typeof (SVsShell)) as IVsShell;

      IVsDebugger debuggerService = GetService (typeof (IVsDebugger)) as IVsDebugger;

      IVsSolution2 solutionService = GetService (typeof (SVsSolution)) as IVsSolution2;

      IVsMonitorSelection monitorSelectionService = GetService (typeof (IVsMonitorSelection)) as IVsMonitorSelection;

      IDebuggerConnectionService debuggerConnectionService = GetService (typeof (IDebuggerConnectionService)) as IDebuggerConnectionService;

      //
      // Register service listeners.
      //

      if (dteService == null)
      {
        throw new InvalidOperationException ("Failed to acquire 'DTE' service");
      }

      if (shellService == null)
      {
        throw new InvalidOperationException ("Failed to acquire 'IVsShell' service");
      }

      if (debuggerService == null)
      {
        throw new InvalidOperationException ("Failed to acquire 'IVsDebugger' service");
      }

      if (solutionService == null)
      {
        throw new InvalidOperationException ("Failed to acquire 'SVsSolution' service");
      }

      if (monitorSelectionService == null)
      {
        throw new InvalidOperationException ("Failed to acquire 'IVsMonitorSelection' service");
      }

      m_propertyEventListener = new PropertyEventListener (shellService);

      m_debuggerEventListener = new DebuggerEventListener (dteService, debuggerService, debuggerConnectionService);

      m_solutionEventListener = new SolutionEventListener (dteService, solutionService);

      //
      // Register a new listener to assist finding assemblies placed within the package's current directory.
      //

      m_assemblyResolveListener = new AssemblyResolveListener ();
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
