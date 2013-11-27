////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Xml;

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.VisualStudio.Project.Contracts.VS2010ONLY;
using Microsoft.VisualStudio.Project.Framework;
using Microsoft.VisualStudio.Project.Utilities.DebuggerProviders;

using AndroidPlusPlus.Common;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugLauncherX
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  [DebuggerScope("Android++Debugger")]

  [ProjectScope(ProjectScopeRequired.ConfiguredProject)]

  [Export(typeof(IDebugLaunchProvider))]

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class DebugLauncher : IDebugLaunchProvider
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // 
    // Gets the set of debuggers already in the Visual C++ project system.
    // This allows us to find the debugger that we intend to wrap.
    // 

    [ImportMany]
    public List<Lazy<IDebugLaunchProvider, IDictionary<string, object>>> DebugProviders { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // 
    // Gets the service provider used by the top-level Visual C++ project engine.
    // 

    [Import]
    public IServiceProvider ServiceProvider { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Gets whether the debugger can launch in the current configuration.
    /// </summary>
    /// <param name="launchOptions">
    /// The launch options that would be passed to a subsequent call to <see cref="PrepareLaunch"/>.
    /// </param>
    /// <param name="projectProperties">Evaluated project and user properties.</param>
    /// <remarks>
    /// This method may be called at any time and the implementation should be fast enough to
    /// perform well if called every time the UI is updated (potentially several times per second).
    /// Implementers SHOULD NOT rely on this method being called directly before a call to
    /// <see cref="PrepareLaunch"/>.  No state should be saved within this method to be used
    /// by the <see cref="PrepareLaunch"/> method.
    /// </remarks>
    public bool CanLaunch (DebugLaunchOptions launchOptions, IDictionary <string, string> projectProperties)
    {
      AndroidAdb.Refresh ();

      AndroidDevice [] connectedDevices = AndroidAdb.GetConnectedDevices ();

      return (connectedDevices.Length > 0);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Prepares to launch a debugger by generating one or more initialization objects
    /// that describe how to launch the debugger and any supporting processes.
    /// </summary>
    /// <param name="options">
    /// A set of default launch options that can be used to initialize each <see cref="IDebugLaunchSettings"/> instance.
    /// These options may be customized by the particular debug launch implementation.
    /// </param>
    /// <param name="projectProperties">Evaluated project and user properties.</param>
    /// <returns>
    /// An enumeration of no more than 2 debugger process launch instructions.  
    /// The enumeration MUST NOT contain null elements.
    /// The enumeration may itself be null or empty if no debugger or process should be launched and no error displayed to the user.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if the debugger cannot start.</exception>
    /// <remarks>
    /// <para>The <see cref="CanLaunch"/> <i>might</i> not be called prior to calling
    /// this method, and thus this method may be called even if <see cref="CanLaunch"/> 
    /// would currently return false.  An implementation should check whether it is sane 
    /// to launch the debugger and throw an <see cref="InvalidOperationException"/> if it is not.</para>
    /// <para>The caller MIGHT NOT actually launch the debugger after calling this method.</para>
    /// <para>Implementations SHOULD NOT launch any processes within this method associated with
    /// launching the debuggee or debugger, but should instead return an enumeration to allow the host
    /// to launch these processes.</para>
    /// </remarks>
    public IEnumerable<IDebugLaunchSettings> PrepareLaunch (DebugLaunchOptions options, IDictionary<string, string> projectProperties)
    {
      Trace.WriteLine ("[DebugLauncher] PrepareLaunch: " + options);

#if FALSE
      // 
      // Print verbose details of the project being launched.
      // 

      foreach (KeyValuePair <string, string> projectPropsPair in projectProperties)
      {
        Trace.WriteLine ("[DebugLauncher] Project property: '" + projectPropsPair.Key + "': '" + projectPropsPair.Value + "'");
      }
#endif

      List<IDebugLaunchSettings> settings = new List<IDebugLaunchSettings> ();

      try
      {
        if (ServiceProvider != null)
        {
          DebugLaunchSettings debugLaunchSettings;

          Project startupProject = GetStartupSolutionProject (ref projectProperties);

          if (startupProject == null)
          {
            throw new InvalidOperationException ("Could not find solution startup project.");
          }

          Trace.WriteLine ("Launcher startup project: " + startupProject.Name + " (" + startupProject.FullName + ")");

          if (options.HasFlag (DebugLaunchOptions.NoDebug))
          {
            debugLaunchSettings = StartWithoutDebugging (options, projectProperties, startupProject);
          }
          else
          {
            debugLaunchSettings = StartWithDebugging (options, projectProperties, startupProject);
          }

          settings.Add (debugLaunchSettings);
        }
      }
      catch (Exception e)
      {
        ShowErrorDialog (string.Format ("Debugging failed to launch, encountered exception: {0}", e.Message));
      }

      return settings;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private DebugLaunchSettings StartWithoutDebugging (DebugLaunchOptions options, IDictionary<string, string> projectProperties, Project startupProject)
    {
      DebugLaunchSettings debugLaunchSettings = new DebugLaunchSettings (options);

      throw new NotImplementedException ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private DebugLaunchSettings StartWithDebugging (DebugLaunchOptions options, IDictionary<string, string> projectProperties, Project startupProject)
    {
      DebugLaunchSettings debugLaunchSettings = new DebugLaunchSettings (options);

      try
      {
        // 
        // Retrieve standard project macro values, and determine the prefere debugger configuration.
        // 

        string projectTargetName = string.Empty;

        string projectProjectDir = string.Empty;

        string debuggerConfiguration = string.Empty;

        string debuggerTargetApk = string.Empty;

        string debuggerAndroidManifest = string.Empty;

        string debuggerCustomLaunchActivity = string.Empty;

        string debuggerDebugMode = string.Empty;

        string debuggerOpenGlTrace = string.Empty;

        string debuggerKeepAppData = string.Empty;

        string debuggerLibraryPaths = string.Empty;

        projectProperties.TryGetValue ("TargetName", out projectTargetName);

        projectProperties.TryGetValue ("ProjectDir", out projectProjectDir);

        if (!projectProperties.TryGetValue ("DebuggerConfiguration", out debuggerConfiguration))
        {
          debuggerConfiguration = "Custom";
        }

        projectProperties.TryGetValue ("DebuggerTargetApk", out debuggerTargetApk);

        projectProperties.TryGetValue ("DebuggerAndroidManifest", out debuggerAndroidManifest);

        projectProperties.TryGetValue ("DebuggerCustomLaunchActivity", out debuggerCustomLaunchActivity);

        projectProperties.TryGetValue ("DebuggerDebugMode", out debuggerDebugMode);

        projectProperties.TryGetValue ("DebuggerOpenGlTrace", out debuggerOpenGlTrace);

        projectProperties.TryGetValue ("DebuggerKeepAppData", out debuggerKeepAppData);

        projectProperties.TryGetValue ("DebuggerLibraryPaths", out debuggerLibraryPaths);

        // 
        // Override provided debugger properties with those evaluated from active build configurations.
        // 

        VCConfiguration activeConfiguration = GetActiveConfiguration (startupProject);

        if (debuggerConfiguration.Equals ("vs-android"))
        {
          IVCRulePropertyStorage rulesAntBuild = activeConfiguration.Rules.Item ("AntBuild");

          string antBuildPath = rulesAntBuild.GetEvaluatedPropertyValue ("AntBuildPath");

          string antBuildType = rulesAntBuild.GetEvaluatedPropertyValue ("AntBuildType");

          string antBuildXml = Path.Combine (antBuildPath, "build.xml");

          XmlDocument buildXmlDocument = new XmlDocument ();

          buildXmlDocument.Load (antBuildXml);

          string antBuildXmlProjectName = buildXmlDocument.DocumentElement.GetAttribute ("name");

          debuggerTargetApk = Path.Combine (antBuildPath, "bin", antBuildXmlProjectName + "-" + antBuildType.ToLower () + ".apk");

          debuggerAndroidManifest = Path.Combine (antBuildPath, "AndroidManifest.xml");
        }

        // 
        // Attempt loading associated AndroidManifest.xml, and override any settings with defaults.
        // 

        AndroidManifest applicationManifest = new AndroidManifest (Path.Combine (projectProjectDir, debuggerAndroidManifest));

        if (string.IsNullOrEmpty (debuggerCustomLaunchActivity))
        {
          debuggerCustomLaunchActivity = applicationManifest.LauncherActivityName;
        }

        // 
        // Check for any currently connected devices, and determine whether the target application is already installed/running.
        // 

        AndroidAdb.Refresh ();

        AndroidDevice [] connectedDevices = AndroidAdb.GetConnectedDevices ();

        if (connectedDevices.Length == 0)
        {
          throw new InvalidOperationException ("No device(s) connected.");
        }

        AndroidDevice debuggingDevice = connectedDevices [0];

        AndroidProcess [] debuggingDeviceProcesses = debuggingDevice.GetProcesses ();

        bool shouldAttach = false;

        foreach (AndroidProcess process in debuggingDeviceProcesses)
        {
          if (process.Name.Equals (applicationManifest.PackageName))
          {
            shouldAttach = true;

            break;
          }
        }

        debugLaunchSettings.LaunchDebugEngineGuid = new Guid ("8310DAF9-1043-4C8E-85A0-FF68896E1922");

        debugLaunchSettings.PortSupplierGuid = new Guid ("3AEE417F-E5F9-4B89-BC31-20534C99B7F5");

        debugLaunchSettings.PortName = "adb://" + connectedDevices [0].ID;

        debugLaunchSettings.LaunchOptions = options | DebugLaunchOptions.Silent;

        if (shouldAttach)
        {
          debugLaunchSettings.Executable = applicationManifest.PackageName;

          debugLaunchSettings.LaunchOperation = DebugLaunchOperation.AlreadyRunning;
        }
        else
        {
          if (string.IsNullOrEmpty (debuggerTargetApk) || !File.Exists (debuggerTargetApk))
          {
            throw new FileNotFoundException ("Could not find required target .apk. Tried: " + debuggerTargetApk);
          }

          debugLaunchSettings.Executable = debuggerTargetApk;

          debugLaunchSettings.LaunchOperation = DebugLaunchOperation.Custom;

          LaunchConfiguration launchConfig = new LaunchConfiguration ();

          launchConfig ["TargetApk"] = debuggerTargetApk;

          launchConfig ["AndroidManifest"] = debuggerAndroidManifest;

          launchConfig ["PackageName"] = applicationManifest.PackageName;

          launchConfig ["LaunchActivity"] = debuggerCustomLaunchActivity;

          launchConfig ["KeepAppData"] = debuggerKeepAppData;

          launchConfig ["DebugMode"] = debuggerDebugMode;

          launchConfig ["OpenGlTrace"] = debuggerOpenGlTrace;

          launchConfig ["LibraryPaths"] = StringUtils.ConvertPathWindowsToPosix (debuggerLibraryPaths);

          debugLaunchSettings.Options = launchConfig.ToString ();
        }
      }
      catch (Exception e)
      {
        ShowErrorDialog (string.Format ("Debugging failed to launch, encountered exception: {0}", e.Message));
      }

      return debugLaunchSettings;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void ShowErrorDialog (string message)
    {
      Debug.WriteLine (message);

      VsShellUtilities.ShowMessageBox (ServiceProvider, message, "Android++ Debugger", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private Project GetStartupSolutionProject (ref IDictionary <string, string> projectProperties)
    {
      DTE dteService = ServiceProvider.GetService (typeof (SDTE)) as DTE;

      Solution solution = dteService.Solution;

      SolutionBuild solutionBuild = solution.SolutionBuild;

      object [] startupProjects = (object [])solutionBuild.StartupProjects;

      Project startupProject = null;

      string startupProjectName = string.Empty;

      if (projectProperties.TryGetValue ("ProjectName", out startupProjectName))
      {
        foreach (Project project in solution.Projects)
        {
          if (project.Name.Equals (startupProjectName))
          {
            startupProject = project;
          }
        }
      }
      else if (startupProjects.Length > 0)
      {
        startupProject = startupProjects [0] as Project;

        startupProjectName = startupProject.Name;
      }

      return startupProject;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private VCConfiguration GetActiveConfiguration (Project project)
    {
      if (project == null)
      {
        throw new ArgumentNullException ();
      }

      VCProject vcProject = project.Object as VCProject;

      Configuration activeConfiguration = project.ConfigurationManager.ActiveConfiguration;

      foreach (VCConfiguration config in vcProject.Configurations)
      {
        if (config.Name.StartsWith (activeConfiguration.ConfigurationName))
        {
          return config;
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
