////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;

#if VS2010
using EnvDTE;
using Microsoft.VisualStudio.Project.Contracts.VS2010ONLY;
using Microsoft.VisualStudio.Project.Framework;
using Microsoft.VisualStudio.Project.Utilities.DebuggerProviders;
#elif VS2012
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Project.Debuggers;
using Microsoft.VisualStudio.Project.Utilities;
using Microsoft.VisualStudio.Project.Utilities.DebuggerProviders;
using Microsoft.VisualStudio.Project.VS.Debuggers;
#elif VS2013
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Debuggers;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.Utilities.DebuggerProviders;
using Microsoft.VisualStudio.ProjectSystem.VS.Debuggers;
#endif

using AndroidPlusPlus.Common;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugLauncher
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class DebugLauncher
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static bool CanLaunch (int launchOptionsFlags)
    {
      // 
      // Requirements to satisfy before launching a requested debug session.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        AndroidAdb.Refresh();

        AndroidDevice[] connectedDevices = AndroidAdb.GetConnectedDevices();

        return (connectedDevices.Length > 0);
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException(e);
      }

      return false;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if VS2010
    public static object StartWithoutDebugging (int launchOptionsFlags, Dictionary <string, string> projectProperties, Project startupProject)
#else
    public static object StartWithoutDebugging (int launchOptionsFlags, Dictionary <string, string> projectProperties)
#endif
    {
      LoggingUtils.PrintFunction ();

      throw new NotImplementedException ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if VS2010
    public static object StartWithDebugging (int launchOptionsFlags, Dictionary<string, string> projectProperties, Project startupProject)
#else
    public static object StartWithDebugging (int launchOptionsFlags, Dictionary<string, string> projectProperties)
#endif
    {
      LoggingUtils.PrintFunction ();

      DebugLaunchSettings debugLaunchSettings = new DebugLaunchSettings ((DebugLaunchOptions)launchOptionsFlags);

      // 
      // Retrieve standard project macro values, and determine the preferred debugger configuration.
      // 

      string projectTargetName = EvaluateProjectProperty (projectProperties, "ConfigurationGeneral", "TargetName");

      string projectProjectDir = EvaluateProjectProperty (projectProperties, "ConfigurationGeneral", "ProjectDir");

      string debuggerConfiguration = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerConfiguration");

      string debuggerTargetApk = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerTargetApk");

      string debuggerCustomLaunchActivity = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerCustomLaunchActivity");

      string debuggerDebugMode = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerDebugMode");

      string debuggerOpenGlTrace = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerOpenGlTrace");

      string debuggerKeepAppData = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerKeepAppData");

      if (string.IsNullOrEmpty (debuggerTargetApk))
      {
        debuggerConfiguration = "Custom";
      }

      // 
      // Additional support for vs-android. Rather hacky for VS2010.
      // 

      if (debuggerConfiguration.Equals ("vs-android"))
      {
#if VS2010
        VCConfiguration activeConfiguration = GetActiveConfiguration (startupProject);

        IVCRulePropertyStorage rulesAntBuild = activeConfiguration.Rules.Item ("AntBuild");

        string antBuildPath = rulesAntBuild.GetEvaluatedPropertyValue ("AntBuildPath");

        string antBuildType = rulesAntBuild.GetEvaluatedPropertyValue ("AntBuildType");
#else
        string antBuildPath = EvaluateProjectProperty (projectProperties, "AntBuild", "AntBuildPath");

        string antBuildType = EvaluateProjectProperty (projectProperties, "AntBuild", "AntBuildType");
#endif

        string antBuildXml = Path.Combine (antBuildPath, "build.xml");

        XmlDocument buildXmlDocument = new XmlDocument ();

        buildXmlDocument.Load (antBuildXml);

        string antBuildXmlProjectName = buildXmlDocument.DocumentElement.GetAttribute ("name");

        debuggerTargetApk = Path.Combine (antBuildPath, "bin", antBuildXmlProjectName + "-" + antBuildType.ToLower () + ".apk");
      }

      // 
      // Validate project properties.
      // 

      string androidSdkRoot = EvaluateProjectProperty (projectProperties, "ConfigurationGeneral", "AndroidSdkRoot");

      string androidSdkBuildToolsVersion = EvaluateProjectProperty (projectProperties, "ConfigurationGeneral", "AndroidSdkBuildToolsVersion");

      if (string.IsNullOrWhiteSpace (androidSdkRoot))
      {
        throw new DirectoryNotFoundException ("'AndroidSdkRoot' property is empty.");
      }

      if (!Directory.Exists (androidSdkRoot))
      {
        throw new DirectoryNotFoundException ("'AndroidSdkRoot' property references a directory which does not exist. (" + androidSdkRoot + ")");
      }

      string androidSdkBuildToolsPath = Path.Combine (androidSdkRoot, "build-tools", androidSdkBuildToolsVersion);

      if (!Directory.Exists (androidSdkBuildToolsPath))
      {
        throw new DirectoryNotFoundException ("Could not locate Android SDK build-tools. Expected " + androidSdkBuildToolsPath);
      }

      if (string.IsNullOrEmpty (debuggerTargetApk))
      {
        throw new FileNotFoundException ("No target APK path provided.");
      }
      else
      {
        if (!Path.IsPathRooted (debuggerTargetApk))
        {
          debuggerTargetApk = Path.Combine (projectProjectDir, debuggerTargetApk);
        }

        if (!Path.IsPathRooted(debuggerTargetApk))
        {
          debuggerTargetApk = Path.GetFullPath (debuggerTargetApk);
        }

        if (!File.Exists (debuggerTargetApk))
        {
          throw new FileNotFoundException ("Could not find required target .apk. Expected: " + debuggerTargetApk);
        }
      }

      // 
      // Spawn a AAPT.exe instance to gain some extra information about the APK we are trying to load.
      // 

      string applicationPackageName = string.Empty;

      string applicationLaunchActivity = string.Empty;

      using (SyncRedirectProcess getApkDetails = new SyncRedirectProcess (Path.Combine (androidSdkBuildToolsPath, "aapt.exe"), "dump --values badging " + StringUtils.ConvertPathWindowsToPosix (debuggerTargetApk)))
      {
        getApkDetails.StartAndWaitForExit (5000);

        string [] apkDetails = getApkDetails.StandardOutput.Replace ("\r", "").Split (new char [] { '\n' });

        foreach (string singleLine in apkDetails)
        {
          if (singleLine.StartsWith ("package: "))
          {
            // 
            // Retrieve package name from format: "package: name='com.example.hellogdbserver' versionCode='1' versionName='1.0'"
            // 

            string [] packageData = singleLine.Substring ("package: ".Length).Split (' ');

            foreach (string data in packageData)
            {
              if (data.StartsWith ("name="))
              {
                applicationPackageName = data.Substring ("name=".Length).Trim ('\'');
              }
            }
          }
          else if (singleLine.StartsWith ("launchable-activity: "))
          {
            string [] launchActivityData = singleLine.Substring ("launchable-activity: ".Length).Split (' ');

            foreach (string data in launchActivityData)
            {
              if (data.StartsWith ("name="))
              {
                applicationLaunchActivity = data.Substring ("name=".Length).Trim ('\'');
              }
            }
          }
        }
      }

      // 
      // If a specific launch activity was not requested, ensure that the default one is referenced.
      // 

      if (string.IsNullOrEmpty (debuggerCustomLaunchActivity))
      {
        debuggerCustomLaunchActivity = applicationLaunchActivity;
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
        if (process.Name.Equals (applicationPackageName))
        {
          shouldAttach = true;

          break;
        }
      }

      debugLaunchSettings.LaunchDebugEngineGuid = new Guid ("8310DAF9-1043-4C8E-85A0-FF68896E1922");

      debugLaunchSettings.PortSupplierGuid = new Guid ("3AEE417F-E5F9-4B89-BC31-20534C99B7F5");

      debugLaunchSettings.PortName = "adb://" + connectedDevices [0].ID;

      debugLaunchSettings.LaunchOptions = ((DebugLaunchOptions)launchOptionsFlags) | DebugLaunchOptions.Silent;

      LaunchConfiguration launchConfig = new LaunchConfiguration ();

      launchConfig ["TargetApk"] = debuggerTargetApk;

      launchConfig ["PackageName"] = applicationPackageName;

      launchConfig ["LaunchActivity"] = debuggerCustomLaunchActivity;

      launchConfig ["KeepAppData"] = debuggerKeepAppData;

      launchConfig ["DebugMode"] = debuggerDebugMode;

      launchConfig ["OpenGlTrace"] = debuggerOpenGlTrace;

      debugLaunchSettings.Options = launchConfig.ToString ();

      if (shouldAttach)
      {
        debugLaunchSettings.Executable = applicationPackageName;

        debugLaunchSettings.LaunchOperation = DebugLaunchOperation.AlreadyRunning;
      }
      else
      {
        debugLaunchSettings.Executable = debuggerTargetApk;

        debugLaunchSettings.LaunchOperation = DebugLaunchOperation.Custom;
      }

      return debugLaunchSettings;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static string EvaluateProjectProperty (Dictionary <string, string> projectProperties, string schema, string property)
    {
      // 
      // VS2010 provides a pre-processed list of project properties. We have to evaluate these manually for VS2012+.
      // - In order to avoid duplicates from similar properties under different schemas, they are prefixed in the list.
      // 

      string evaluatedProperty;

#if VS2012 || VS2013
      string schemaGroupedKey = schema + "." + property;

      if (projectProperties.TryGetValue (schemaGroupedKey, out evaluatedProperty))
      {
        return evaluatedProperty;
      }
#endif

      if (projectProperties.TryGetValue (property, out evaluatedProperty))
      {
        return evaluatedProperty;
      }

      return string.Empty;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if VS2010
    public static Project GetStartupSolutionProject (IServiceProvider serviceProvider, Dictionary<string, string> projectProperties)
    {
      LoggingUtils.PrintFunction ();

      DTE dteService = serviceProvider.GetService (typeof (SDTE)) as DTE;

      Solution solution = dteService.Solution;

      SolutionBuild solutionBuild = solution.SolutionBuild;

      object [] startupProjects = (object []) solutionBuild.StartupProjects;

      Project startupProject = null;

      string startupProjectName = string.Empty;

      if (projectProperties.TryGetValue ("ProjectName", out startupProjectName))
      {
        // 
        // Construct a listing of all the sub-projects in this solution.
        // 

        List <Project> solutionProjects = new List <Project> ();

        foreach (Project project in solution.Projects)
        {
          solutionProjects.Add (project);

          GetProjectSubprojects (project, ref solutionProjects);
        }

        foreach (Project project in solutionProjects)
        {
          if (project.Name.Equals (startupProjectName))
          {
            startupProject = project;

            break;
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
#endif

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if VS2010
    public static void GetProjectSubprojects (Project project, ref List <Project> projectListing)
    {
      foreach (ProjectItem item in project.ProjectItems)
      {
        if (item.SubProject != null)
        {
          projectListing.Add (item.SubProject);

          GetProjectSubprojects (item.SubProject, ref projectListing);
        }
      }
    }
#endif

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if VS2010
    public static VCConfiguration GetActiveConfiguration (Project project)
    {
      LoggingUtils.PrintFunction ();

      if (project == null)
      {
        throw new ArgumentNullException ("project");
      }

      VCProject vcProject = project.Object as VCProject;

      VCConfiguration [] vcProjectConfigurations = (VCConfiguration [])vcProject.Configurations;

      Configuration activeConfiguration = project.ConfigurationManager.ActiveConfiguration;

      foreach (VCConfiguration config in vcProjectConfigurations)
      {
        if (config.Name.StartsWith (activeConfiguration.ConfigurationName))
        {
          return config;
        }
      }

      return null;
    }
#endif

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void ShowErrorDialog (IServiceProvider serviceProvider, string message)
    {
      VsShellUtilities.ShowMessageBox (serviceProvider, message, "Android++ Debugger", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
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
