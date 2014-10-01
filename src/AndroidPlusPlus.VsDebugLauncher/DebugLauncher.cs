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
using System.Windows.Forms;

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
using System.Threading;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugLauncher
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class DebugLauncher : IDebugLauncher
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly IServiceProvider m_serviceProvider;

    private readonly IUiDebugLaunchService m_debugLaunchService;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebugLauncher (IServiceProvider serviceProvider)
    {
      m_serviceProvider = serviceProvider;

      m_debugLaunchService = m_serviceProvider.GetService (typeof (IUiDebugLaunchService)) as IUiDebugLaunchService;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool CanLaunch (int launchOptionsFlags)
    {
      // 
      // Requirements to satisfy before launching a requested debug session.
      // 

      LoggingUtils.PrintFunction ();

      return true;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void PrepareLaunch ()
    {
      DateTime logTime = DateTime.Now;

      LoggingUtils.RequireOk (m_debugLaunchService.LaunchDialogShow ());

      LoggingUtils.RequireOk (m_debugLaunchService.LaunchDialogUpdate (string.Format ("Configuring Android++ ({0:D2}-{1:D2}-{2:D4} {3:D2}:{4:D2}.{5:D2})...", logTime.Day, logTime.Month, logTime.Year, logTime.Hour, logTime.Minute, logTime.Second), false));
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public object StartWithoutDebugging (int launchOptionsFlags, LaunchConfiguration launchConfig, IDictionary<string, string> projectProperties)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        DebugLaunchSettings nonDebuglaunchSettings = (DebugLaunchSettings) StartWithDebugging (launchOptionsFlags, launchConfig, projectProperties);

        nonDebuglaunchSettings.LaunchOptions |= DebugLaunchOptions.NoDebug;

        launchConfig.FromString (nonDebuglaunchSettings.Options);

        launchConfig ["DebugMode"] = "false"; // launch without waiting for a JDB instance.

        nonDebuglaunchSettings.Options = launchConfig.ToString ();

        return nonDebuglaunchSettings;
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

    public object StartWithDebugging (int launchOptionsFlags, LaunchConfiguration launchConfig, IDictionary<string, string> projectProperties)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        // 
        // Check for any currently connected devices, and determine whether the target application is already installed/running.
        // 

        AndroidAdb.Refresh ();

        AndroidDevice [] connectedDevices = AndroidAdb.GetConnectedDevices ();

        if (connectedDevices.Length == 0)
        {
          throw new InvalidOperationException ("No device/emulator found or connected. Check status via 'adb devices'.");
        }

        bool shouldAttach = false;

        AndroidDevice debuggingDevice = connectedDevices [0];

#if false
        AndroidProcess [] debuggingDeviceProcesses = debuggingDevice.GetProcesses ();

        foreach (AndroidProcess process in debuggingDeviceProcesses)
        {
          if (process.Name.Equals (applicationPackageName))
          {
            shouldAttach = true;

            break;
          }
        }
#endif

        DebugLaunchSettings debugLaunchSettings = new DebugLaunchSettings ((DebugLaunchOptions) launchOptionsFlags);

        debugLaunchSettings.LaunchDebugEngineGuid = new Guid ("8310DAF9-1043-4C8E-85A0-FF68896E1922");

        debugLaunchSettings.PortSupplierGuid = new Guid ("3AEE417F-E5F9-4B89-BC31-20534C99B7F5");

        debugLaunchSettings.PortName = "adb://" + debuggingDevice.ID;

        debugLaunchSettings.LaunchOptions = ((DebugLaunchOptions) launchOptionsFlags) | DebugLaunchOptions.Silent;

        debugLaunchSettings.Options = launchConfig.ToString ();

        if (shouldAttach)
        {
          debugLaunchSettings.Executable = launchConfig ["PackageName"];

          debugLaunchSettings.LaunchOperation = DebugLaunchOperation.AlreadyRunning;
        }
        else
        {
          LoggingUtils.RequireOk (m_debugLaunchService.LaunchDialogUpdate (string.Format ("Installing '{0}' to '{1}'...", launchConfig ["PackageName"], debuggingDevice.ID), false));

          InstallApplicationAsync (debuggingDevice, launchConfig);

          LoggingUtils.RequireOk (m_debugLaunchService.LaunchDialogUpdate (string.Format ("Installation completed successfully."), false));

          debugLaunchSettings.Executable = launchConfig ["TargetApk"];

          debugLaunchSettings.LaunchOperation = DebugLaunchOperation.Custom;
        }

        return debugLaunchSettings;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        string description = string.Format ("[Exception] {0}", e.Message);

        LoggingUtils.RequireOk (m_debugLaunchService.LaunchDialogUpdate (description, true));

        throw;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void InstallApplicationAsync (AndroidDevice debuggingDevice, LaunchConfiguration launchConfig)
    {
      // 
      // Asynchronous installation process, so the UI can be updated appropriately.
      // 

      ManualResetEvent installCompleteEvent = new ManualResetEvent (false);

      Exception installFailedException = null;

      System.Threading.Thread asyncInstallApplicationThread = new System.Threading.Thread (delegate ()
      {
        try
        {
          string targetApk = launchConfig ["TargetApk"];

          bool keepData = launchConfig ["KeepAppData"].Equals ("true");

          debuggingDevice.Install (targetApk, keepData, ""/*"com.android.vending"*/); // TODO: Installer needs to be customisable
        }
        catch (Exception e)
        {
          LoggingUtils.HandleException (e);

          installFailedException = e;
        }
        finally
        {
          installCompleteEvent.Set ();
        }
      });

      asyncInstallApplicationThread.Start ();

      while (!installCompleteEvent.WaitOne (0))
      {
        Application.DoEvents ();

        System.Threading.Thread.Sleep (100);
      }

      if (installFailedException != null)
      {
        throw installFailedException;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if VS2010
    public LaunchConfiguration GetLaunchConfigurationFromProjectProperties (IDictionary<string, string> projectProperties, Project startupProject)
#else
    public LaunchConfiguration GetLaunchConfigurationFromProjectProperties (IDictionary<string, string> projectProperties)
#endif
    {
      LoggingUtils.PrintFunction ();

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

      if (string.IsNullOrEmpty (debuggerConfiguration))
      {
        debuggerConfiguration = "Custom";
      }

#if DEBUG
      LoggingUtils.Print ("ConfigurationGeneral.TargetName: " + projectTargetName);

      LoggingUtils.Print ("ConfigurationGeneral.ProjectDir: " + projectProjectDir);

      LoggingUtils.Print ("AndroidPlusPlusDebugger.DebuggerConfiguration: " + debuggerConfiguration);

      LoggingUtils.Print ("AndroidPlusPlusDebugger.DebuggerTargetApk: " + debuggerTargetApk);

      LoggingUtils.Print ("AndroidPlusPlusDebugger.DebuggerCustomLaunchActivity: " + debuggerCustomLaunchActivity);

      LoggingUtils.Print ("AndroidPlusPlusDebugger.DebuggerDebugMode: " + debuggerDebugMode);

      LoggingUtils.Print ("AndroidPlusPlusDebugger.DebuggerOpenGlTrace: " + debuggerOpenGlTrace);

      LoggingUtils.Print ("AndroidPlusPlusDebugger.DebuggerKeepAppData: " + debuggerKeepAppData);
#endif

      // 
      // Support for vs-android. Rather hacky for VS2010.
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
      // Ensure the provided target APK is found and absolute.
      // 

      if (string.IsNullOrEmpty (debuggerTargetApk))
      {
        throw new FileNotFoundException ("Could not locate target application. Empty path provided.");
      }
      else if (!Path.IsPathRooted (debuggerTargetApk) && !string.IsNullOrWhiteSpace (projectProjectDir))
      {
        debuggerTargetApk = Path.Combine (projectProjectDir, debuggerTargetApk);
      }

      if (!Path.IsPathRooted (debuggerTargetApk))
      {
        throw new InvalidOperationException ("Could not evaluate an absolute path to the target application. Tried: " + debuggerTargetApk);
      }

      debuggerTargetApk = Path.GetFullPath (debuggerTargetApk); // normalises relative paths.

      if (!File.Exists (debuggerTargetApk))
      {
        throw new FileNotFoundException ("Could not find required target application. Expected: " + debuggerTargetApk);
      }

      // 
      // Find the selected Android SDK (and associated build-tools) deployment.
      // 

      string androidSdkRoot = EvaluateProjectProperty (projectProperties, "ConfigurationGeneral", "AndroidSdkRoot");

      if (string.IsNullOrWhiteSpace (androidSdkRoot))
      {
        throw new DirectoryNotFoundException ("Could not locate Android SDK. 'AndroidSdkRoot' property is empty.");
      }
      else if (!Directory.Exists (androidSdkRoot))
      {
        throw new DirectoryNotFoundException ("Could not locate Android SDK. 'AndroidSdkRoot' property references a directory which does not exist. Expected: " + androidSdkRoot);
      }

      string androidSdkBuildToolsVersion = EvaluateProjectProperty (projectProperties, "ConfigurationGeneral", "AndroidSdkBuildToolsVersion");

      string androidSdkBuildToolsPath = Path.Combine (androidSdkRoot, "build-tools", androidSdkBuildToolsVersion);

      if (!Directory.Exists (androidSdkBuildToolsPath))
      {
        throw new DirectoryNotFoundException (string.Format ("Could not locate Android SDK build-tools (v{0}). Expected: {1}", androidSdkBuildToolsVersion, androidSdkBuildToolsPath));
      }

      // 
      // Spawn a AAPT.exe instance to gain some extra information about the APK we are trying to load.
      // 

      string applicationPackageName = string.Empty;

      string applicationLaunchActivity = string.Empty;

      string aaptToolPath = Path.Combine (androidSdkBuildToolsPath, "aapt.exe");

      if (!File.Exists (aaptToolPath))
      {
        throw new FileNotFoundException ("Could not locate AAPT tool (under Android SDK build-tools).", aaptToolPath);
      }

      using (SyncRedirectProcess getApkDetails = new SyncRedirectProcess (aaptToolPath, "dump --values badging " + PathUtils.SantiseWindowsPath (debuggerTargetApk)))
      {
        int exitCode = getApkDetails.StartAndWaitForExit ();

        if (exitCode != 0)
        {
          throw new InvalidOperationException ("AAPT failed to dump required application badging information. Exit-code: " + exitCode);
        }

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

      LaunchConfiguration launchConfig = new LaunchConfiguration ();

      launchConfig ["TargetApk"] = debuggerTargetApk;

      launchConfig ["PackageName"] = applicationPackageName;

      launchConfig ["LaunchActivity"] = debuggerCustomLaunchActivity;

      launchConfig ["KeepAppData"] = debuggerKeepAppData;

      launchConfig ["DebugMode"] = debuggerDebugMode;

      launchConfig ["OpenGlTrace"] = debuggerOpenGlTrace;

      return launchConfig;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static string EvaluateProjectProperty (IDictionary <string, string> projectProperties, string schema, string property)
    {
      // 
      // VS2010 provides a pre-processed list of project properties. We have to evaluate these manually for VS2012+.
      // - In order to avoid duplicates from similar properties under different schemas, they are prefixed in the list.
      // 

      LoggingUtils.PrintFunction ();

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
    private static VCConfiguration GetActiveConfiguration (Project project)
    {
      LoggingUtils.PrintFunction ();

      if (project == null)
      {
        throw new ArgumentNullException ("project");
      }

      VCProject vcProject = project.Object as VCProject;

      VCConfiguration [] vcProjectConfigurations = (VCConfiguration []) vcProject.Configurations;

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

  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
