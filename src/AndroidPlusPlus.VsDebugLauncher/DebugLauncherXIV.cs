////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using AndroidPlusPlus.Common;
using AndroidPlusPlus.VsDebugCommon;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.Debug;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugLauncher
{

  [ComVisible (true)]

  [Guid ("A7A96E37-9D90-489A-84CB-14BBBE5686D2")]

  [ExportDebugger("AndroidPlusPlusDebugger")]

  [AppliesTo(ProjectCapabilities.VisualC)]

  public class DebugLauncher : DebugLaunchProviderBase
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [Import]
    private Rules.RuleProperties DebuggerProperties { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [ImportingConstructor] // NOTE: Took me two days to realise this line was missing. Launcher won't work otherwise.
    public DebugLauncher (ConfiguredProject configuredProject)
      : base (configuredProject)
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override async Task<bool> CanLaunchAsync (DebugLaunchOptions launchOptions)
    {
      LoggingUtils.PrintFunction ();

      return await Task.FromResult(true);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync (DebugLaunchOptions launchOptions)
    {
      LoggingUtils.PrintFunction ();

      var debugLaunchSettings = new DebugLaunchSettings (launchOptions);

      try
      {
        Serilog.Log.Information(string.Format (CultureInfo.InvariantCulture, "Session started: {0}", DateTime.Now.ToString ("F", CultureInfo.CurrentCulture)), false);

        var projectProperties = await DebuggerProperties.ProjectPropertiesToDictionaryAsync ();

        projectProperties.Add ("ConfigurationGeneral.ProjectDir", Path.GetDirectoryName (DebuggerProperties.GetConfiguredProject().UnconfiguredProject.FullPath));

        var launchConfig = GetLaunchConfigurationFromProjectProperties (projectProperties);

        var launchProps = GetLaunchPropsFromProjectProperties (projectProperties);

        if (launchOptions.HasFlag (DebugLaunchOptions.NoDebug))
        {
          debugLaunchSettings = await StartWithoutDebuggingAsync (launchOptions, launchConfig, launchProps, projectProperties);
        }
        else
        {
          debugLaunchSettings = await StartWithDebuggingAsync (launchOptions, launchConfig, launchProps, projectProperties);
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException(e);

        string description = string.Format(CultureInfo.InvariantCulture, "[{0}] {1}", e.GetType().Name, e.Message);

        VsShellUtilities.ShowMessageBox(ServiceProvider, description, "Android++", OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
      }

      return new IDebugLaunchSettings [] { debugLaunchSettings };
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public Task<DebugLaunchSettings> StartWithoutDebuggingAsync (DebugLaunchOptions launchOptions, IDictionary<string, string> launchConfig, ICollection<Tuple<string, string>> launchProps, IDictionary<string, string> projectProperties)
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

      var debuggingDevice = GetPrioritisedConnectedDevice () ?? throw new InvalidOperationException ("No device/emulator found or connected. Check status using \"adb devices\".");

      //
      // Construct VS launch settings to debug or attach to the specified target application.
      //

      var nonDebuglaunchSettings = new DebugLaunchSettings (launchOptions | DebugLaunchOptions.Silent);

      // MDD Android
      nonDebuglaunchSettings.LaunchDebugEngineGuid = DebugEngineGuids.guidDebugEngineID;

      nonDebuglaunchSettings.PortSupplierGuid = DebugEngineGuids.guidDebugPortSupplierID;

      nonDebuglaunchSettings.PortName = debuggingDevice.ID;

      nonDebuglaunchSettings.Options = JsonConvert.SerializeObject(launchConfig, Formatting.Indented);

      nonDebuglaunchSettings.Executable = launchConfig ["TargetApk"];

      nonDebuglaunchSettings.LaunchOperation = DebugLaunchOperation.Custom;

      return Task.FromResult(nonDebuglaunchSettings);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async Task<DebugLaunchSettings> StartWithDebuggingAsync (DebugLaunchOptions launchOptions, IDictionary<string, string> launchConfig, ICollection<Tuple<string, string>> launchProps, IDictionary<string, string> projectProperties)
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
      // Enforce required device/emulator properties.
      //

      var debuggingDevice = GetPrioritisedConnectedDevice() ?? throw new InvalidOperationException("No device/emulator found or connected. Check status using \"adb devices\".");

      foreach (var prop in launchProps)
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

      DebugLaunchSettings debugLaunchSettings = new DebugLaunchSettings (launchOptions | DebugLaunchOptions.Silent);

      // new Guid("EA6637C6-17DF-45B5-A183-0951C54243BC"); // MDD Android
      debugLaunchSettings.LaunchDebugEngineGuid = DebugEngineGuids.guidDebugEngineID;

      debugLaunchSettings.PortSupplierGuid = DebugEngineGuids.guidDebugPortSupplierID;

      debugLaunchSettings.PortName = debuggingDevice.ID;

      debugLaunchSettings.Options = JsonConvert.SerializeObject(launchConfig, Formatting.Indented);

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

        bool shouldUpToDateCheck = launchConfig ["UpToDateCheck"].Equals ("true");

        bool appIsInstalled = false;

        bool appIsOutOfDate = true;

        if (shouldUpToDateCheck)
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

                Serilog.Log.Information(string.Format (CultureInfo.InvariantCulture, "'{0}' already installed on target '{1}'.", launchConfig ["PackageName"], debuggingDevice.ID));

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

                Serilog.Log.Information(string.Format (CultureInfo.InvariantCulture, "Current UTC time on '{0}': {1}", debuggingDevice.ID, debuggingDeviceUtcTime.ToString ()), false);

                Serilog.Log.Information(string.Format (CultureInfo.InvariantCulture, "Current UTC time on '{0}': {1}", System.Environment.MachineName, thisMachineUtcTime.ToString ()), false);

                Serilog.Log.Information(string.Format (CultureInfo.InvariantCulture, "Difference in UTC time between '{0}' and '{1}': {2}", System.Environment.MachineName, debuggingDevice.ID, thisMachineUtcVersusDeviceUtc.ToString ()), false);

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

                Serilog.Log.Information(string.Format (CultureInfo.InvariantCulture, "'{0}' was last modified on '{1}' at: {2}.", launchConfig ["PackageName"], debuggingDevice.ID, debuggingDeviceUtcTimeAtLastModification.ToString ()), false);

                Serilog.Log.Information(string.Format (CultureInfo.InvariantCulture, "{0} (on {1}) was around {2} (on {3}).", debuggingDeviceUtcTimeAtLastModification.ToString (), debuggingDevice.ID, thisMachineUtcTimeAtLastModification.ToString (), System.Environment.MachineName), false);

                Serilog.Log.Information(string.Format (CultureInfo.InvariantCulture, "'{0}' was last modified on '{1}' at: {2}.", Path.GetFileName (targetApkFileInfo.FullName), System.Environment.MachineName, targetApkFileInfo.LastWriteTime.ToString ()), false);

                appIsOutOfDate = (targetApkFileInfo.LastWriteTime + thisMachineUtcVersusDeviceUtc) > thisMachineUtcTimeAtLastModification;

                break;
              }
            }
          }
          catch (Exception)
          {
            appIsInstalled = false;
          }
        }

        if (!appIsInstalled || appIsOutOfDate)
        {
          Serilog.Log.Information(string.Format (CultureInfo.InvariantCulture, "Installing '{0}' to '{1}'...", launchConfig ["PackageName"], debuggingDevice.ID), false);

          InstallApplicationAsync (debuggingDevice, launchConfig);

          Serilog.Log.Information(string.Format (CultureInfo.InvariantCulture, "'{0}' installed successfully.", launchConfig ["PackageName"]), false);
        }
        else
        {
          Serilog.Log.Information(string.Format (CultureInfo.InvariantCulture, "'{0}' on '{1}' is up-to-date. Skipping installation...", launchConfig ["PackageName"], debuggingDevice.ID), false);
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

      AndroidAdb.Refresh ();

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

    private void InstallApplicationAsync (AndroidDevice debuggingDevice, IDictionary<string, string> launchConfig)
    {
      //
      // Asynchronous installation process, so the UI can be updated appropriately.
      //

      LoggingUtils.PrintFunction ();

      ManualResetEvent installCompleteEvent = new ManualResetEvent (false);

      Exception installFailedException = null;

      var asyncInstallApplicationThread = new System.Threading.Thread (() =>
      {
        try
        {
          string targetLocalApk = launchConfig ["TargetApk"];

          string targetRemoteTemporaryPath = "/data/local/tmp";

          string targetRemoteTemporaryFile = targetRemoteTemporaryPath + '/' + Path.GetFileName (targetLocalApk);

          //
          // Construct 'am install' arguments for installing the application in a manner compatible with GDB.
          //
          // Note: Installations to /mnt/asec/ cause 'run-as' to fail regarding permissions.
          //

          var installArgsBuilder = new StringBuilder ();

          installArgsBuilder.Append ("-f "); // install package on internal flash. (required for debugging)

          if (launchConfig.TryGetValue("KeepAppData", out string keepAppData) && string.Equals (keepAppData, "true", StringComparison.OrdinalIgnoreCase))
          {
            installArgsBuilder.Append ("-r "); // reinstall an existing app, keeping its data.
          }

          if (launchConfig.TryGetValue("InstallerPackage", out string installerPackage) && !string.IsNullOrWhiteSpace (installerPackage))
          {
            installArgsBuilder.Append (string.Format (CultureInfo.InvariantCulture, "-i {0} ", launchConfig ["InstallerPackage"]));
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

          Serilog.Log.Information(string.Format (CultureInfo.InvariantCulture, "[adb:push] {0} {1}", targetLocalApk, targetRemoteTemporaryPath), false);

          debuggingDevice.Push (targetLocalApk, targetRemoteTemporaryPath);

          Serilog.Log.Information(string.Format (CultureInfo.InvariantCulture, "[adb:shell:pm] {0} {1}", "install", installArgsBuilder.ToString ()), false);

          string installReport = debuggingDevice.Shell ("pm", "install " + installArgsBuilder.ToString (), int.MaxValue);

          if (!installReport.Contains ("Success"))
          {
            string sanitisedFailure = installReport;

            throw new InvalidOperationException (string.Format (CultureInfo.InvariantCulture, "[adb:shell:pm] install failed: {0}", sanitisedFailure));
          }

          Serilog.Log.Information(string.Format (CultureInfo.InvariantCulture, "[adb:shell:rm] {0}", targetRemoteTemporaryFile), false);

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
        System.Windows.Forms.Application.DoEvents ();

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

    public IDictionary<string, string> GetLaunchConfigurationFromProjectProperties (IDictionary<string, string> projectProperties)
    {
      LoggingUtils.PrintFunction ();

      //
      // Retrieve standard project macro values, and determine the preferred debugger configuration.
      //

      projectProperties.TryGetValue("ConfigurationGeneral.TargetName", out string projectTargetName);

      projectProperties.TryGetValue("ConfigurationGeneral.ProjectDir", out string projectProjectDir);

      projectProperties.TryGetValue("AndroidPlusPlusDebugger.DebuggerConfigTargetApk", out string debuggerTargetApk);

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
      // Spawn an `aapt.exe` instance to gain some extra information about the APK we are trying to load.
      //

      string applicationPackageName = string.Empty;

      using (SyncRedirectProcess getApkDetails = new SyncRedirectProcess (Path.Combine (AndroidSettings.SdkBuildToolsRoot, "aapt.exe"), "dump --values badging " + PathUtils.SantiseWindowsPath (debuggerTargetApk)))
      {
        int exitCode = getApkDetails.StartAndWaitForExit ();

        if (exitCode != 0)
        {
          throw new InvalidOperationException ("AAPT failed to dump required application badging information. Exit-code: " + exitCode);
        }

        var apkDetails = getApkDetails.StandardOutput.Replace ("\r", "").Split (new char [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

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

                break;
              }
            }

            break;
          }
        }
      }

      //
      // If a specific launch activity was not requested, ensure that the default one is referenced.
      //

      var launchConfig = new Dictionary<string, string>();

      launchConfig["TargetApk"] = debuggerTargetApk;

      launchConfig["PackageName"] = applicationPackageName;

      if (projectProperties.TryGetValue("AndroidPlusPlusDebugger.DebuggerConfigLaunchActivity", out string debuggerLaunchActivity) && !string.IsNullOrWhiteSpace(debuggerLaunchActivity))
      {
        launchConfig["LaunchActivity"] = debuggerLaunchActivity;
      }

      if (projectProperties.TryGetValue("AndroidPlusPlusDebugger.DebuggerConfigUpToDateCheck", out string debuggerUpToDateCheck) && !string.IsNullOrWhiteSpace(debuggerUpToDateCheck))
      {
        launchConfig["UpToDateCheck"] = debuggerUpToDateCheck;
      }

      if (projectProperties.TryGetValue("AndroidPlusPlusDebugger.DebuggerConfigDebugMode", out string debuggerDebugMode) && !string.IsNullOrWhiteSpace(debuggerDebugMode))
      {
        launchConfig["DebugMode"] = debuggerDebugMode;
      }

      if (projectProperties.TryGetValue("AndroidPlusPlusDebugger.DebuggerConfigKeepAppData", out string debuggerKeepAppData) && !string.IsNullOrWhiteSpace(debuggerKeepAppData))
      {
        launchConfig["KeepAppData"] = debuggerKeepAppData;
      }

      if (projectProperties.TryGetValue("AndroidPlusPlusDebugger.DebuggerConfigInstallerPackage", out string debuggerInstallerPackage) && !string.IsNullOrWhiteSpace(debuggerInstallerPackage))
      {
        launchConfig["InstallerPackage"] = debuggerInstallerPackage;
      }

      return launchConfig;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ICollection<Tuple<string, string>> GetLaunchPropsFromProjectProperties (IDictionary<string, string> projectProperties)
    {
      projectProperties.TryGetValue("AndroidPlusPlusDebugger.DebuggerPropCheckJni", out string debuggerPropCheckJni);

      projectProperties.TryGetValue("AndroidPlusPlusDebugger.DebuggerPropEglCallstack", out string debuggerPropEglCallstack);

      var launchProps = new List<Tuple<string, string>>
      {
        new Tuple<string, string>("debug.checkjni", (string.Equals("true", debuggerPropCheckJni, StringComparison.OrdinalIgnoreCase)) ? "1" : "0"),

        new Tuple<string, string>("debug.egl.callstack", (string.Equals("true", debuggerPropEglCallstack, StringComparison.OrdinalIgnoreCase)) ? "1" : "0"),
      };

      return launchProps;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

}
