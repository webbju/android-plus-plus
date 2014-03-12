////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;

#if VS2010
using Microsoft.VisualStudio.Project.Contracts.VS2010ONLY;
using Microsoft.VisualStudio.Project.Framework;
using Microsoft.VisualStudio.Project.Utilities.DebuggerProviders;
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

    public static object StartWithoutDebugging (int launchOptionsFlags, Dictionary <string, string> projectProperties, Project startupProject)
    {
      LoggingUtils.PrintFunction ();

      throw new NotImplementedException ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static object StartWithDebugging (int launchOptionsFlags, Dictionary<string, string> projectProperties, Project startupProject)
    {
      LoggingUtils.PrintFunction ();

      DebugLaunchSettings debugLaunchSettings = new DebugLaunchSettings ((DebugLaunchOptions)launchOptionsFlags);

      // 
      // Retrieve standard project macro values, and determine the prefere debugger configuration.
      // 

      string projectTargetName = string.Empty;

      string projectProjectDir = string.Empty;

      string debuggerConfiguration = string.Empty;

      string debuggerTargetApk = string.Empty;

      string debuggerCustomLaunchActivity = string.Empty;

      string debuggerDebugMode = string.Empty;

      string debuggerOpenGlTrace = string.Empty;

      string debuggerKeepAppData = string.Empty;

      string debuggerGdbTool = string.Empty;

      string debuggerLibraryPaths = string.Empty;

      projectProperties.TryGetValue ("TargetName", out projectTargetName);

      projectProperties.TryGetValue ("ProjectDir", out projectProjectDir);

      if (!projectProperties.TryGetValue ("DebuggerConfiguration", out debuggerConfiguration))
      {
        debuggerConfiguration = "Custom";
      }

      projectProperties.TryGetValue ("DebuggerTargetApk", out debuggerTargetApk);

      projectProperties.TryGetValue ("DebuggerCustomLaunchActivity", out debuggerCustomLaunchActivity);

      projectProperties.TryGetValue ("DebuggerDebugMode", out debuggerDebugMode);

      projectProperties.TryGetValue ("DebuggerOpenGlTrace", out debuggerOpenGlTrace);

      projectProperties.TryGetValue ("DebuggerKeepAppData", out debuggerKeepAppData);

      projectProperties.TryGetValue ("DebuggerGdbTool", out debuggerGdbTool);

      projectProperties.TryGetValue ("DebuggerLibraryPaths", out debuggerLibraryPaths);

      if (!File.Exists (debuggerGdbTool))
      {
        throw new FileNotFoundException ("Could not locate an instance of GDB. Expected: " + debuggerGdbTool);
      }

      // 
      // Validate project properties.
      // 

      string androidSdkRoot = string.Empty;

      string androidSdkBuildToolsVersion = string.Empty;

      projectProperties.TryGetValue ("AndroidSdkRoot", out androidSdkRoot);

      projectProperties.TryGetValue ("AndroidSdkBuildToolsVersion", out androidSdkBuildToolsVersion);

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

      // 
      // Spawn a AAPT.exe instance to gain some extra information about the APK we are trying to load.
      // 

      string applicationPackageName = string.Empty;

      string applicationLaunchActivity = string.Empty;

      using (SyncRedirectProcess getApkDetails = new SyncRedirectProcess (Path.Combine (androidSdkBuildToolsPath, "aapt.exe"), "dump --values badging " + debuggerTargetApk))
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

#if FALSE
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
#endif

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

      launchConfig ["GdbTool"] = debuggerGdbTool;

      launchConfig ["LibraryPaths"] = debuggerLibraryPaths;

      debugLaunchSettings.Options = launchConfig.ToString ();

      if (shouldAttach)
      {
        debugLaunchSettings.Executable = applicationPackageName;

        debugLaunchSettings.LaunchOperation = DebugLaunchOperation.AlreadyRunning;
      }
      else
      {
        if (string.IsNullOrEmpty (debuggerTargetApk) || !File.Exists (debuggerTargetApk))
        {
          throw new FileNotFoundException ("Could not find required target .apk. Expected: " + debuggerTargetApk);
        }

        debugLaunchSettings.Executable = debuggerTargetApk;

        debugLaunchSettings.LaunchOperation = DebugLaunchOperation.Custom;
      }

      return debugLaunchSettings;
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

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

    public static VCConfiguration GetActiveConfiguration (Project project)
    {
      LoggingUtils.PrintFunction ();

      if (project == null)
      {
        throw new ArgumentNullException ();
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
