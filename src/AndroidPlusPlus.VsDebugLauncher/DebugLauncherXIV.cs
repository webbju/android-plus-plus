////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using AndroidPlusPlus.Common;
using AndroidPlusPlus.VsDebugCommon;
using CliWrap.Buffered;
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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

        var debuggingDevice = await GetPrioritisedConnectedDeviceAsync();

        /*var launchProps = GetLaunchPropsFromProjectProperties (projectProperties);

foreach (var prop in launchProps)
{
  await AndroidAdb.AdbCommand(debuggingDevice, "setprop", $"{prop.Item1} {prop.Item2}").ExecuteAsync();
}*/

        var launchConfig = await GetLaunchConfigurationFromProjectPropertiesAsync(projectProperties);

        if (launchOptions.HasFlag (DebugLaunchOptions.NoDebug))
        {
          debugLaunchSettings = await StartWithoutDebuggingAsync (launchOptions, debuggingDevice, launchConfig, projectProperties);
        }
        else
        {
          debugLaunchSettings = await StartWithDebuggingAsync (launchOptions, debuggingDevice, launchConfig, projectProperties);
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

    public async Task<DebugLaunchSettings> StartWithoutDebuggingAsync (DebugLaunchOptions launchOptions, AndroidDevice debuggingDevice, IDictionary<string, string> launchConfig, IDictionary<string, string> projectProperties)
    {
      LoggingUtils.PrintFunction ();

      if (launchConfig == null)
      {
        throw new ArgumentNullException (nameof(launchConfig));
      }

      //
      // Construct VS launch settings to debug or attach to the specified target application.
      //

      var nonDebuglaunchSettings = new DebugLaunchSettings (launchOptions | DebugLaunchOptions.Silent);

      nonDebuglaunchSettings.LaunchDebugEngineGuid = DebugEngineGuids.guidDebugEngineID;

      nonDebuglaunchSettings.PortSupplierGuid = DebugEngineGuids.guidDebugPortSupplierID;

      nonDebuglaunchSettings.PortName = debuggingDevice?.ID ?? throw new ArgumentException(nameof(debuggingDevice));

      nonDebuglaunchSettings.Executable = launchConfig ["TargetApk"];

      nonDebuglaunchSettings.Options = JsonConvert.SerializeObject(launchConfig, Formatting.Indented);

      nonDebuglaunchSettings.LaunchOperation = DebugLaunchOperation.Custom;

      return nonDebuglaunchSettings;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async Task<DebugLaunchSettings> StartWithDebuggingAsync (DebugLaunchOptions launchOptions, AndroidDevice debuggingDevice, IDictionary<string, string> launchConfig, IDictionary<string, string> projectProperties, CancellationToken cancellationToken = default)
    {
      LoggingUtils.PrintFunction ();

      if (launchConfig == null)
      {
        throw new ArgumentNullException (nameof(launchConfig));
      }

      if (projectProperties == null)
      {
        throw new ArgumentNullException (nameof(projectProperties));
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

      debugLaunchSettings.Executable = launchConfig["PackageName"];

      debugLaunchSettings.LaunchOperation = DebugLaunchOperation.AlreadyRunning;

      if (shouldAttach)
      {
        return debugLaunchSettings;
      }

      //
      // Determine whether the application is currently installed, and if it is;
      // check last modified date to ensure we don't re-installed unchanged binaries.
      //

      bool appIsOutOfDate = true;

      try
      {
        //
        // Get the target device/emulator's UTC current time.
        //
        //   This is done by specifying the '-u' argument to 'date'. Despite this though,
        //   the returned string will always claim to be in GMT:
        //
        //   i.e: "Fri Jan  9 14:35:23 GMT 2015"
        //

        var deviceUtcTimestampCmd = await AndroidAdb.AdbCommand().WithArguments($"-s {debuggingDevice.ID} shell date -u").ExecuteBufferedAsync(cancellationToken);

        DateTime debuggingDeviceUtcTime = default;

        using (var reader = new StringReader(deviceUtcTimestampCmd.StandardOutput))
        {
          for (string line = await reader.ReadLineAsync(); !string.IsNullOrEmpty(line); line = await reader.ReadLineAsync())
          {
            var debuggingDeviceUtcTimestampComponents = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            debuggingDeviceUtcTimestampComponents[4] = "-00:00";

            debuggingDeviceUtcTime = DateTime.ParseExact(string.Join(" ", debuggingDeviceUtcTimestampComponents), "ddd MMM  d HH:mm:ss zzz yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);

            debuggingDeviceUtcTime = debuggingDeviceUtcTime.ToUniversalTime();
          }
        }

        //
        // Convert current device/emulator time to UTC, and probe the working machine's time too.
        //

        DateTime thisMachineUtcTime = DateTime.UtcNow;

        TimeSpan thisMachineUtcVersusDeviceUtc = debuggingDeviceUtcTime - thisMachineUtcTime;

        Serilog.Log.Information($"Current UTC time on {Environment.MachineName}: {thisMachineUtcTime}");

        Serilog.Log.Information($"Current UTC time on {debuggingDevice.ID}: {debuggingDeviceUtcTime}");

        Serilog.Log.Information($"Difference in UTC time between {Environment.MachineName} and {debuggingDevice.ID}: {thisMachineUtcVersusDeviceUtc}");

        //
        // Check the last modified date; ls output currently uses this format:
        //
        // -rw-r--r-- system   system   11533274 2015-01-09 13:47 com.example.native_activity-2.apk
        //

        DateTime lastModifiedTimestampDeviceLocalTime = default;

        var installedCheckCmd = await AndroidAdb.AdbCommand().WithArguments($"-s {debuggingDevice.ID} shell pm path {launchConfig["PackageName"]}").ExecuteBufferedAsync(cancellationToken);

        using (var reader = new StringReader(installedCheckCmd.StandardOutput))
        {
          for (string line = await reader.ReadLineAsync(); !string.IsNullOrEmpty(line); line = await reader.ReadLineAsync())
          {
            string installedPath = line.Substring("package:".Length);

            var lastModifiedTimestampCmd = await AndroidAdb.AdbCommand().WithArguments($"-s {debuggingDevice.ID} shell ls -l {installedPath}").ExecuteBufferedAsync(cancellationToken);

            using (var timestampReader = new StringReader(lastModifiedTimestampCmd.StandardOutput))
            {
              var regExMatcher = new Regex(@"(?<datetime>\d{4}-\d{2}-\d{2} \d{2}:\d{2})", RegexOptions.Compiled);

              for (string timestamp = await timestampReader.ReadLineAsync(); !string.IsNullOrEmpty(timestamp); timestamp = await timestampReader.ReadLineAsync())
              {
                var regExMatch = regExMatcher.Match(timestamp);

                string datetime = regExMatch.Success ? regExMatch.Result("${datetime}") : string.Empty;

                lastModifiedTimestampDeviceLocalTime = DateTime.ParseExact(datetime, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
              }
            }
          }
        }

        //
        // Calculate how long ago the APK was changed, according to the device's local time.
        //

        FileInfo targetApkFileInfo = new FileInfo(launchConfig["TargetApk"]);

        TimeSpan timeSinceLastModification = debuggingDeviceUtcTime - lastModifiedTimestampDeviceLocalTime;

        DateTime debuggingDeviceUtcTimeAtLastModification = debuggingDeviceUtcTime - timeSinceLastModification;

        DateTime thisMachineUtcTimeAtLastModification = thisMachineUtcTime - timeSinceLastModification;

        Serilog.Log.Information($"{launchConfig["PackageName"]} was last modified on '{debuggingDevice.ID}' at: {debuggingDeviceUtcTimeAtLastModification}.");

        Serilog.Log.Information($"{debuggingDeviceUtcTimeAtLastModification} (on {debuggingDevice.ID}) was around {thisMachineUtcTimeAtLastModification} (on {Environment.MachineName}).");

        Serilog.Log.Information($"{Path.GetFileName(targetApkFileInfo.FullName)} was last modified on '{Environment.MachineName}' at: {targetApkFileInfo.LastWriteTime}.");

        appIsOutOfDate = (targetApkFileInfo.LastWriteTime + thisMachineUtcVersusDeviceUtc) > thisMachineUtcTimeAtLastModification;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException(e);
      }

      if (appIsOutOfDate)
      {
        Serilog.Log.Information($"Installing {launchConfig["PackageName"]} to {debuggingDevice.ID} ...");

        await InstallApplicationAsync (debuggingDevice, launchConfig);

        Serilog.Log.Information($"{launchConfig["PackageName"]} installed successfully.");
      }
      else
      {
        Serilog.Log.Information($"{launchConfig["PackageName"]} on {debuggingDevice.ID} is up-to-date. Skipping installation ...");
      }

      debugLaunchSettings.Executable = launchConfig ["TargetApk"];

      debugLaunchSettings.LaunchOperation = DebugLaunchOperation.Custom;

      return debugLaunchSettings;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async Task<AndroidDevice> GetPrioritisedConnectedDeviceAsync ()
    {
      //
      // Refresh ADB service and evaluate a list of connected devices or emulators.
      //
      // We want to prioritise devices over emulators here, which makes the logic a little dodgy.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        var connectedDevices = await AndroidAdb.Refresh();

        var debuggingDevice = connectedDevices.Values.OrderByDescending(device => device.IsEmulator ? 0 : 1).FirstOrDefault();

        if (debuggingDevice == null)
        {
          throw new InvalidOperationException("No device/emulator found or connected. Check status using \"adb devices\".");
        }

        return debuggingDevice;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException(e);

        throw;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private async Task InstallApplicationAsync (AndroidDevice debuggingDevice, IDictionary<string, string> launchConfig)
    {
      //
      // Asynchronous installation process, so the UI can be updated appropriately.
      //

      LoggingUtils.PrintFunction ();

      Exception installFailedException = null;

      using var asyncInstallApplicationTask = Task.Run(async () =>
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

          await Task.Yield();

          await AndroidAdb.Push(debuggingDevice, targetLocalApk, targetRemoteTemporaryPath);

          await Task.Yield();

          await AndroidAdb.AdbCommand().WithArguments($"-s {debuggingDevice.ID} shell pm install {installArgsBuilder}").ExecuteAsync();

          await Task.Yield();

          await AndroidAdb.AdbCommand().WithArguments($"-s {debuggingDevice.ID} shell rm -f {targetRemoteTemporaryFile}").ExecuteAsync();
        }
        catch (Exception e)
        {
          LoggingUtils.HandleException (e);

          installFailedException = e;
        }
      });

      await asyncInstallApplicationTask;

      if (installFailedException != null)
      {
        throw installFailedException;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async Task<IDictionary<string, string>> GetLaunchConfigurationFromProjectPropertiesAsync (IDictionary<string, string> projectProperties)
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

      using (SyncRedirectProcess getApkDetails = new SyncRedirectProcess(Path.Combine(AndroidSettings.SdkBuildToolsRoot, "aapt.exe"), "dump --values badging " + PathUtils.SantiseWindowsPath(debuggerTargetApk)))
      {
        var (exitCode, _, _) = getApkDetails.StartAndWaitForExit();

        if (exitCode != 0)
        {
          throw new InvalidOperationException("AAPT failed to dump required application badging information. Exit-code: " + exitCode);
        }

        using var reader = new StringReader(getApkDetails.StandardOutput);

        for (string line = await reader.ReadLineAsync(); !string.IsNullOrEmpty(line); line = await reader.ReadLineAsync())
        {
          if (line.StartsWith("package: ", StringComparison.OrdinalIgnoreCase))
          {
            //
            // Retrieve package name from format: "package: name='com.example.hellogdbserver' versionCode='1' versionName='1.0'"
            //

            var packageData = line.Substring("package: ".Length).Split(' ');

            foreach (string data in packageData)
            {
              if (data.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
              {
                applicationPackageName = data.Substring("name=".Length).Trim('\'');

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
