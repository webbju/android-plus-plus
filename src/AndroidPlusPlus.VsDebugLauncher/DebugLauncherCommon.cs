////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using System.Windows.Forms;

using AndroidPlusPlus.Common;
using AndroidPlusPlus.VsDebugCommon;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.Debug;
using System.Threading.Tasks;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugLauncher
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class DebugLauncherCommon : IDebugLauncher
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly IServiceProvider m_serviceProvider;

    private readonly IDebuggerConnectionService m_debugConnectionService;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebugLauncherCommon (IServiceProvider serviceProvider)
    {
      m_serviceProvider = serviceProvider ?? throw new ArgumentNullException (nameof(serviceProvider));

      m_debugConnectionService = m_serviceProvider.GetService (typeof (IDebuggerConnectionService)) as IDebuggerConnectionService;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public IDebuggerConnectionService GetConnectionService ()
    {
      return m_debugConnectionService;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public Task<bool> CanLaunch (int launchOptions)
    {
      //
      // Requirements to satisfy before launching a requested debug session.
      //

      return Task.FromResult(true);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async Task<int> PrepareLaunch ()
    {
      LoggingUtils.PrintFunction ();

      DateTime logTime = DateTime.Now;

      await m_debugConnectionService.LaunchDialogShow ();

      return await m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "Session started: {0}", logTime.ToString ("F", CultureInfo.CurrentCulture)), false);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public Task<DebugLaunchSettings> StartWithoutDebugging (int launchOptions, LaunchConfiguration launchConfig, ICollection<LaunchProps> launchProps, IDictionary<string, string> projectProperties)
    {
      LoggingUtils.PrintFunction ();

      if (launchConfig == null)
      {
        throw new ArgumentNullException (nameof(launchConfig));
      }

      if (launchProps == null)
      {
        throw new ArgumentNullException (nameof(launchProps));
      }

      if (projectProperties == null)
      {
        throw new ArgumentNullException (nameof(projectProperties));
      }

      //
      // Refresh ADB service and evaluate a list of connected devices or emulators.
      //

      AndroidAdb.Refresh ();

      AndroidDevice debuggingDevice = GetPrioritisedConnectedDevice ();

      if (debuggingDevice == null)
      {
        throw new InvalidOperationException ("No device/emulator found or connected. Check status using \"adb devices\".");
      }

      //
      // Construct VS launch settings to debug or attach to the specified target application.
      //

      launchOptions |= (int) DebugLaunchOptions.Silent;

      DebugLaunchSettings nonDebuglaunchSettings = new DebugLaunchSettings ((DebugLaunchOptions) launchOptions);

      nonDebuglaunchSettings.LaunchDebugEngineGuid = new Guid ("8310DAF9-1043-4C8E-85A0-FF68896E1922");

      nonDebuglaunchSettings.PortSupplierGuid = new Guid ("3AEE417F-E5F9-4B89-BC31-20534C99B7F5");

      nonDebuglaunchSettings.PortName = debuggingDevice.ID;

      nonDebuglaunchSettings.Options = launchConfig.ToString ();

      nonDebuglaunchSettings.Executable = launchConfig ["TargetApk"];

      nonDebuglaunchSettings.LaunchOperation = DebugLaunchOperation.Custom;

      return Task.FromResult(nonDebuglaunchSettings);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async Task<DebugLaunchSettings> StartWithDebugging (int launchOptions, LaunchConfiguration launchConfig, ICollection<LaunchProps> launchProps, IDictionary<string, string> projectProperties)
    {
      LoggingUtils.PrintFunction ();

      if (launchConfig == null)
      {
        throw new ArgumentNullException (nameof(launchConfig));
      }

      if (launchProps == null)
      {
        throw new ArgumentNullException (nameof(launchProps));
      }

      if (projectProperties == null)
      {
        throw new ArgumentNullException (nameof(projectProperties));
      }

      //
      // Refresh ADB service and evaluate a list of connected devices or emulators.
      //

      AndroidAdb.Refresh ();

      AndroidDevice debuggingDevice = GetPrioritisedConnectedDevice ();

      if (debuggingDevice == null)
      {
        throw new InvalidOperationException ("No device/emulator found or connected. Check status using \"adb devices\".");
      }

      //
      // Enforce required device/emulator properties.
      //

      foreach (LaunchProps prop in launchProps)
      {
        debuggingDevice.Shell ("setprop", string.Format (CultureInfo.InvariantCulture, "{0} {1}", prop.Item1, prop.Item2));
      }

      //
      // Construct VS launch settings to debug or attach to the specified target application.
      //

      bool shouldAttach = false;

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

      launchOptions |= (int) DebugLaunchOptions.Silent;

      DebugLaunchSettings debugLaunchSettings = new DebugLaunchSettings ((DebugLaunchOptions) launchOptions);

      debugLaunchSettings.LaunchDebugEngineGuid = new Guid ("8310DAF9-1043-4C8E-85A0-FF68896E1922");

      debugLaunchSettings.PortSupplierGuid = new Guid ("3AEE417F-E5F9-4B89-BC31-20534C99B7F5");

      debugLaunchSettings.PortName = debuggingDevice.ID;

      debugLaunchSettings.Options = launchConfig.ToString ();

      if (shouldAttach)
      {
        debugLaunchSettings.Executable = launchConfig ["PackageName"];

        debugLaunchSettings.LaunchOperation = DebugLaunchOperation.AlreadyRunning;
      }
      else
      {
        //
        // Determine whether the application is currently installed, and if it is;
        // check last modified date to ensure we don't re-installed unchanged binaries.
        //

        bool upToDateCheck = launchConfig ["UpToDateCheck"].Equals ("true");

        bool appIsInstalled = false;

        bool appIsOutOfDate = true;

        if (upToDateCheck)
        {
          FileInfo targetApkFileInfo = new FileInfo (launchConfig ["TargetApk"]);

          try
          {
            var adbPmPathOutput = debuggingDevice.Shell ("pm", "path " + launchConfig ["PackageName"]).Replace ("\r", "").Split (new char [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in adbPmPathOutput)
            {
              if (line.StartsWith ("package:", StringComparison.OrdinalIgnoreCase))
              {
                appIsInstalled = true;

                LoggingUtils.RequireOk (await m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "'{0}' already installed on target '{1}'.", launchConfig ["PackageName"], debuggingDevice.ID), false));

                string path = line.Substring ("package:".Length);

                //
                // Get the target device/emulator's UTC current time.
                //
                //   This is done by specifying the '-u' argument to 'date'. Despite this though,
                //   the returned string will always claim to be in GMT:
                //
                //   i.e: "Fri Jan  9 14:35:23 GMT 2015"
                //

                DateTime debuggingDeviceUtcTime;

                try
                {
                  var deviceDateOutput = debuggingDevice.Shell ("date", "-u").Replace ("\r", "").Split (new char [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                  string debuggingDeviceUtcTimestamp = deviceDateOutput [0];

                  var debuggingDeviceUtcTimestampComponents = debuggingDeviceUtcTimestamp.Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                  debuggingDeviceUtcTimestampComponents [4] = "-00:00";

                  if (!DateTime.TryParseExact (string.Join (" ", debuggingDeviceUtcTimestampComponents), "ddd MMM  d HH:mm:ss zzz yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out debuggingDeviceUtcTime))
                  {
                    break;
                  }

                  debuggingDeviceUtcTime = debuggingDeviceUtcTime.ToUniversalTime ();
                }
                catch (Exception e)
                {
                  throw new InvalidOperationException ("Failed to evaluate device local time.", e);
                }

                //
                // Convert current device/emulator time to UTC, and probe the working machine's time too.
                //

                DateTime thisMachineUtcTime = DateTime.UtcNow;

                TimeSpan thisMachineUtcVersusDeviceUtc = debuggingDeviceUtcTime - thisMachineUtcTime;

                await m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "Current UTC time on '{0}': {1}", debuggingDevice.ID, debuggingDeviceUtcTime.ToString ()), false);

                await m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "Current UTC time on '{0}': {1}", System.Environment.MachineName, thisMachineUtcTime.ToString ()), false);

                await m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "Difference in UTC time between '{0}' and '{1}': {2}", System.Environment.MachineName, debuggingDevice.ID, thisMachineUtcVersusDeviceUtc.ToString ()), false);

                //
                // Check the last modified date; ls output currently uses this format:
                //
                // -rw-r--r-- system   system   11533274 2015-01-09 13:47 com.example.native_activity-2.apk
                //

                DateTime lastModifiedTimestampDeviceLocalTime;

                try
                {
                  var extendedLsOutput = debuggingDevice.Shell ("ls -l", path).Replace ("\r", "").Split (new char [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                  var extendedLsOutputComponents = extendedLsOutput [0].Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                  string date = extendedLsOutputComponents [4];

                  string time = extendedLsOutputComponents [5];

                  if (!DateTime.TryParseExact (date + " " + time, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out lastModifiedTimestampDeviceLocalTime))
                  {
                    break;
                  }
                }
                catch (Exception e)
                {
                  throw new InvalidOperationException (string.Format (CultureInfo.InvariantCulture, "Failed to evaluate device local modified time of: {0}", path), e);
                }

                //
                // Calculate how long ago the APK was changed, according to the device's local time.
                //

                TimeSpan timeSinceLastModification = debuggingDeviceUtcTime - lastModifiedTimestampDeviceLocalTime;

                DateTime debuggingDeviceUtcTimeAtLastModification = debuggingDeviceUtcTime - timeSinceLastModification;

                DateTime thisMachineUtcTimeAtLastModification = thisMachineUtcTime - timeSinceLastModification;

                await m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "'{0}' was last modified on '{1}' at: {2}.", launchConfig ["PackageName"], debuggingDevice.ID, debuggingDeviceUtcTimeAtLastModification.ToString ()), false);

                await m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "{0} (on {1}) was around {2} (on {3}).", debuggingDeviceUtcTimeAtLastModification.ToString (), debuggingDevice.ID, thisMachineUtcTimeAtLastModification.ToString (), System.Environment.MachineName), false);

                await m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "'{0}' was last modified on '{1}' at: {2}.", Path.GetFileName (targetApkFileInfo.FullName), System.Environment.MachineName, targetApkFileInfo.LastWriteTime.ToString ()), false);

                if ((targetApkFileInfo.LastWriteTime + thisMachineUtcVersusDeviceUtc) > thisMachineUtcTimeAtLastModification)
                {
                  await m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "'{0}' was determined to be out-of-date. Reinstalling...", launchConfig ["PackageName"]), false);
                }
                else
                {
                  appIsOutOfDate = false;
                }

                break;
              }
            }
          }
          catch (Exception)
          {
            appIsInstalled = false;
          }
        }
        else
        {
          await m_debugConnectionService.LaunchDialogUpdate ("Skipping up-to-date check.", false);
        }

        if (!appIsInstalled || appIsOutOfDate)
        {
          await m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "Installing '{0}' to '{1}'...", launchConfig ["PackageName"], debuggingDevice.ID), false);

          InstallApplicationAsync (debuggingDevice, launchConfig);

          await m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "'{0}' installed successfully.", launchConfig ["PackageName"]), false);
        }
        else
        {
          await m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "'{0}' on '{1}' is up-to-date. Skipping installation...", launchConfig ["PackageName"], debuggingDevice.ID), false);
        }

        debugLaunchSettings.Executable = launchConfig ["TargetApk"];

        debugLaunchSettings.LaunchOperation = DebugLaunchOperation.Custom;
      }

      return debugLaunchSettings;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidDevice GetPrioritisedConnectedDevice ()
    {
      //
      // Refresh ADB service and evaluate a list of connected devices or emulators.
      //
      // We want to prioritise devices over emulators here, which makes the logic a little dodgy.
      //

      LoggingUtils.PrintFunction ();

      var connectedDevices = AndroidAdb.GetConnectedDevices ();

      foreach (var device in connectedDevices)
      {
        if (!device.IsEmulator)
        {
          return device;
        }
      }

      foreach (var device in connectedDevices)
      {
        if (device.IsEmulator)
        {
          return device;
        }
      }

      return  null;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void InstallApplicationAsync (AndroidDevice debuggingDevice, LaunchConfiguration launchConfig)
    {
      //
      // Asynchronous installation process, so the UI can be updated appropriately.
      //

      LoggingUtils.PrintFunction ();

      ManualResetEvent installCompleteEvent = new ManualResetEvent (false);

      Exception installFailedException = null;

      System.Threading.Thread asyncInstallApplicationThread = new System.Threading.Thread (delegate ()
      {
        try
        {
          string targetLocalApk = launchConfig ["TargetApk"];

          string targetRemoteTemporaryPath = "/data/local/tmp";

          string targetRemoteTemporaryFile = targetRemoteTemporaryPath + '/' + Path.GetFileName (targetLocalApk);

          bool keepData = launchConfig ["KeepAppData"].Equals ("true");

          string installerPackage = launchConfig ["InstallerPackage"];

          //
          // Construct 'am install' arguments for installing the application in a manner compatible with GDB.
          //
          // Note: Installations to /mnt/asec/ cause 'run-as' to fail regarding permissions.
          //

          StringBuilder installArgsBuilder = new StringBuilder ();

          installArgsBuilder.Append ("-f "); // install package on internal flash. (required for debugging)

          if (keepData)
          {
            installArgsBuilder.Append ("-r "); // reinstall an existing app, keeping its data.
          }

          if (!string.IsNullOrWhiteSpace (installerPackage))
          {
            installArgsBuilder.Append (string.Format (CultureInfo.InvariantCulture, "-i {0} ", installerPackage));
          }

          installArgsBuilder.Append (targetRemoteTemporaryFile);

          //
          // Explicitly install the target APK using 'pm' tool, as this allows more customisation.
          //
          //  1) APKs must already be on the device for this tool to work. We push these manually.
          //
          //  2) Installations can fail for various reasons; errors are reported thusly:
          //         pkg: /data/local/tmp/hello-gdbserver-Debug.apk
          //       Failure [INSTALL_FAILED_INVALID_URI]
          //

          m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "[adb:push] {0} {1}", targetLocalApk, targetRemoteTemporaryPath), false);

          debuggingDevice.Push (targetLocalApk, targetRemoteTemporaryPath);

          m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "[adb:shell:pm] {0} {1}", "install", installArgsBuilder.ToString ()), false);

          string installReport = debuggingDevice.Shell ("pm", "install " + installArgsBuilder.ToString (), int.MaxValue);

          if (!installReport.Contains ("Success"))
          {
            string sanitisedFailure = installReport;

            throw new InvalidOperationException (string.Format (CultureInfo.InvariantCulture, "[adb:shell:pm] install failed: {0}", sanitisedFailure));
          }

          m_debugConnectionService.LaunchDialogUpdate (string.Format (CultureInfo.InvariantCulture, "[adb:shell:rm] {0}", targetRemoteTemporaryFile), false);

          debuggingDevice.Shell ("rm", targetRemoteTemporaryFile);
        }
        catch (Exception e)
        {
          LoggingUtils.HandleException (e);

          installFailedException = e;

          throw;
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "AndroidSdkRoot")]
    public LaunchConfiguration GetLaunchConfigurationFromProjectProperties (IDictionary<string, string> projectProperties)
    {
      LoggingUtils.PrintFunction ();

      //
      // Retrieve standard project macro values, and determine the preferred debugger configuration.
      //

      string projectTargetName = EvaluateProjectProperty (projectProperties, "ConfigurationGeneral", "TargetName");

      string projectProjectDir = EvaluateProjectProperty (projectProperties, "ConfigurationGeneral", "ProjectDir");

      string debuggerMode = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerConfigMode");

      string debuggerTargetApk = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerConfigTargetApk");

      string debuggerUpToDateCheck = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerConfigUpToDateCheck");

      string debuggerLaunchActivity = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerConfigLaunchActivity");

      string debuggerDebugMode = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerConfigDebugMode");

      string debuggerOpenGlTrace = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerConfigOpenGlTrace");

      string debuggerKeepAppData = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerConfigKeepAppData");

      string debuggerInstallerPackage = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerConfigInstallerPackage");

      if (string.IsNullOrEmpty (debuggerMode))
      {
        debuggerMode = "Custom";
      }
      else if (debuggerMode.Equals ("vs-android"))
      {
        //
        // Support for vs-android.
        //

        string antBuildPath = EvaluateProjectProperty (projectProperties, "AntBuild", "AntBuildPath");

        string antBuildType = EvaluateProjectProperty (projectProperties, "AntBuild", "AntBuildType");

        string antBuildXml = Path.Combine (antBuildPath, "build.xml");

        XmlDocument buildXmlDocument = new XmlDocument ();

        buildXmlDocument.Load (antBuildXml);

        string antBuildXmlProjectName = buildXmlDocument.DocumentElement.GetAttribute ("name");

        debuggerTargetApk = Path.Combine (antBuildPath, "bin", string.Format (CultureInfo.InvariantCulture, "{0}-{1}.apk", antBuildXmlProjectName, antBuildType));
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
        throw new DirectoryNotFoundException ("Could not locate Android SDK. \"AndroidSdkRoot\" property is empty.");
      }
      else if (!Directory.Exists (androidSdkRoot))
      {
        throw new DirectoryNotFoundException ("Could not locate Android SDK. \"AndroidSdkRoot\" property references a directory which does not exist. Expected: " + androidSdkRoot);
      }

      string androidSdkBuildToolsVersion = EvaluateProjectProperty (projectProperties, "ConfigurationGeneral", "AndroidSdkBuildToolsVersion");

      string androidSdkBuildToolsPath = Path.Combine (androidSdkRoot, "build-tools", androidSdkBuildToolsVersion);

      if (!Directory.Exists (androidSdkBuildToolsPath))
      {
        throw new DirectoryNotFoundException (string.Format (CultureInfo.CurrentCulture, "Could not locate Android SDK build-tools (v{0}). Expected: {1}", androidSdkBuildToolsVersion, androidSdkBuildToolsPath));
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

        var apkDetails = getApkDetails.StandardOutput.Replace ("\r", "").Split (new char [] { '\n' });

        foreach (string singleLine in apkDetails)
        {
          if (singleLine.StartsWith ("package: ", StringComparison.OrdinalIgnoreCase))
          {
            //
            // Retrieve package name from format: "package: name='com.example.hellogdbserver' versionCode='1' versionName='1.0'"
            //

            var packageData = singleLine.Substring ("package: ".Length).Split (' ');

            foreach (string data in packageData)
            {
              if (data.StartsWith ("name=", StringComparison.OrdinalIgnoreCase))
              {
                applicationPackageName = data.Substring ("name=".Length).Trim ('\'');
              }
            }
          }
          else if (singleLine.StartsWith ("launchable-activity: ", StringComparison.OrdinalIgnoreCase))
          {
            var launchActivityData = singleLine.Substring ("launchable-activity: ".Length).Split (' ');

            foreach (string data in launchActivityData)
            {
              if (data.StartsWith ("name=", StringComparison.OrdinalIgnoreCase))
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

      if (string.IsNullOrEmpty (debuggerLaunchActivity))
      {
        debuggerLaunchActivity = applicationLaunchActivity;
      }

      LaunchConfiguration launchConfig = new LaunchConfiguration
      {
        ["TargetApk"] = debuggerTargetApk,

        ["UpToDateCheck"] = debuggerUpToDateCheck,

        ["PackageName"] = applicationPackageName,

        ["LaunchActivity"] = debuggerLaunchActivity,

        ["DebugMode"] = debuggerDebugMode,

        ["OpenGlTrace"] = debuggerOpenGlTrace,

        ["KeepAppData"] = debuggerKeepAppData,

        ["InstallerPackage"] = debuggerInstallerPackage
      };

      return launchConfig;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ICollection<LaunchProps> GetLaunchPropsFromProjectProperties (IDictionary<string, string> projectProperties)
    {
      LoggingUtils.PrintFunction ();

      bool debuggerPropCheckJni = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerPropCheckJni").Equals ("true");

      bool debuggerPropEglCallstack = EvaluateProjectProperty (projectProperties, "AndroidPlusPlusDebugger", "DebuggerPropEglCallstack").Equals ("true");

      List<LaunchProps> launchProps = new List<LaunchProps>
      {
        new LaunchProps("debug.checkjni", (debuggerPropCheckJni) ? "1" : "0"),

        new LaunchProps("debug.egl.callstack", (debuggerPropEglCallstack) ? "1" : "0")
      };

      return launchProps.ToArray ();
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

      string evaluatedProperty = string.Empty;

      string schemaGroupedKey = string.Concat (schema, ".", property);

      bool foundProperty = false;

#if VS2013 || VS2015
      if (projectProperties.TryGetValue (schemaGroupedKey, out evaluatedProperty))
      {
        foundProperty = true;
      }
#endif

      if (!foundProperty && projectProperties.TryGetValue (property, out evaluatedProperty))
      {
        foundProperty = true;
      }

#if DEBUG
      LoggingUtils.Print (string.Format ("{0}: {1}", schemaGroupedKey, evaluatedProperty));
#endif

      return evaluatedProperty;
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
