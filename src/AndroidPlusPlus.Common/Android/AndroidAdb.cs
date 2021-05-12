////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using CliWrap;
using CliWrap.Buffered;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.Common
{

  public static class AndroidAdb
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public interface IStateListener
    {
      void DeviceConnected (AndroidDevice device);

      void DeviceDisconnected (AndroidDevice device);

      void DevicePervasive (AndroidDevice device);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static ConcurrentDictionary<string, AndroidDevice> m_connectedDevices = new ConcurrentDictionary<string, AndroidDevice> ();

    private static List<IStateListener> m_registeredDeviceStateListeners = new List<IStateListener> ();

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async static Task<ConcurrentDictionary<string, AndroidDevice>> Refresh (CancellationToken cancellationToken = default)
    {
      //
      // Start an ADB instance, if required.
      //

      await AdbCommand().WithArguments("start-server").WithValidation(CommandResultValidation.None).ExecuteAsync(cancellationToken);

      //
      // Parse 'devices' output, skipping headers and potential 'start-server' output.
      //

      var adbDevices = await AdbCommand().WithArguments("devices").ExecuteBufferedAsync(cancellationToken);

      var pattern = new Regex(@"^([^ ]+)\t([^ ]+)$", RegexOptions.Compiled);

      var currentAdbDevices = new Dictionary<string, string>();

      using (var reader = new StringReader(adbDevices.StandardOutput))
      {
        for (string line = await reader.ReadLineAsync (); !string.IsNullOrEmpty(line); line = await reader.ReadLineAsync())
        {
          Match match = pattern.Match(line);

          if (!match.Success)
          {
            continue;
          }

          string deviceName = match.Groups[1].Value;

          string deviceType = match.Groups[2].Value;

          LoggingUtils.Print($"[AndroidAdb] Device: {deviceName} ({deviceType})");

          currentAdbDevices.Add(deviceName, deviceType);
        }
      }

      //
      // First identify any previously tracked devices which aren't in 'devices' output.
      //

      var disconnectedDevices = new HashSet<string>();

      foreach (string key in m_connectedDevices.Keys)
      {
        string deviceName = (string)key;

        if (!currentAdbDevices.ContainsKey(deviceName))
        {
          disconnectedDevices.Add(deviceName);
        }
      }

      //
      // Identify whether any devices have changed state; connected/persisted/disconnected.
      //

      foreach (KeyValuePair<string, string> devicePair in currentAdbDevices)
      {
        string deviceName = devicePair.Key;

        string deviceType = devicePair.Value;

        if (deviceType.Equals("offline", StringComparison.InvariantCultureIgnoreCase))
        {
          disconnectedDevices.Add(deviceName);
        }
        else if (deviceType.Equals("unauthorized", StringComparison.InvariantCultureIgnoreCase))
        {
          // User needs to allow USB debugging.
        }
        else if (m_connectedDevices.TryGetValue(deviceName, out AndroidDevice connectedDevice))
        {
          //
          // Device is pervasive. Refresh internal properties.
          //

          LoggingUtils.Print($"[AndroidAdb] Device pervaded: {deviceName} - {deviceType}");

          foreach (IStateListener deviceListener in m_registeredDeviceStateListeners)
          {
            deviceListener.DevicePervasive(connectedDevice);
          }
        }
        else
        {
          //
          // Device connected.
          //

          LoggingUtils.Print($"[AndroidAdb] Device connected: {deviceName} - {deviceType}");

          connectedDevice = new AndroidDevice(deviceName);

          m_connectedDevices.TryAdd(deviceName, connectedDevice);

          foreach (IStateListener deviceListener in m_registeredDeviceStateListeners)
          {
            deviceListener.DeviceConnected(connectedDevice);
          }
        }
      }

      //
      // Finally, handle device disconnection.
      //

      foreach (string deviceName in disconnectedDevices)
      {
        LoggingUtils.Print($"[AndroidAdb] Device disconnected: {deviceName}");

        if (m_connectedDevices.TryRemove(deviceName, out AndroidDevice disconnectedDevice))
        {
          foreach (IStateListener deviceListener in m_registeredDeviceStateListeners)
          {
            deviceListener.DeviceDisconnected(disconnectedDevice);
          }
        }
      }

      return m_connectedDevices;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async static Task Push(AndroidDevice device, string localPath, string remotePath, CancellationToken cancellationToken = default)
    {
      try
      {
        await AdbCommand().WithArguments($"-s {device.ID} push {PathUtils.QuoteIfNeeded(localPath)} {remotePath}").ExecuteAsync(cancellationToken);
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

    public async static Task Pull(AndroidDevice device, string remotePath, string localPath, CancellationToken cancellationToken = default)
    {
      try
      {
        //
        // Check if the remote path is a symbolic link, and adjust the target file.
        // (ADB Pull doesn't follow these links)
        //

        var readlink = await AdbCommand().WithArguments($"-s {device.ID} shell readlink {remotePath}").WithValidation(CommandResultValidation.None).ExecuteBufferedAsync(cancellationToken);

        if (readlink.ExitCode == 0)
        {
          using var reader = new StringReader(readlink.StandardOutput);

          for (string line = await reader.ReadLineAsync(); !string.IsNullOrEmpty(line); line = await reader.ReadLineAsync())
          {
            if (line.StartsWith("/"))  // absolute path link
            {
              remotePath = line;
            }
            else // relative path link
            {
              int i = remotePath.LastIndexOf('/');

              if (i != -1)
              {
                string parentPath = remotePath.Substring(0, i);

                string file = remotePath.Substring(i + 1);

                remotePath = parentPath + '/' + file;
              }
            }
          }
        }

        await AdbCommand().WithArguments($"-s {device.ID} pull {remotePath} {PathUtils.QuoteIfNeeded(localPath)}").ExecuteAsync(cancellationToken);
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

    public async static Task<Dictionary<uint, AndroidProcess>> ProcessesSnapshot(AndroidDevice device, string args, CancellationToken cancellationToken = default)
    {
      var processList = new Dictionary<uint, AndroidProcess>();

      try
      {
        var command = await AdbCommand().WithArguments($"-s {device.ID} shell ps {args}").ExecuteBufferedAsync(cancellationToken);

        var processesRegEx = new Regex(@"(?<user>[^ ]+)[ ]*(?<pid>[0-9]+)[ ]*(?<ppid>[0-9]+)[ ]*(?<vsize>[0-9]+)[ ]*(?<rss>[0-9]+)[ ]*(?<wchan>[^ ]+)[ ]*(?<pc>[A-Za-z0-9]+)[ ]*(?<s>[^ ]+)[ ]*(?<name>[^\r\n]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        using var reader = new StringReader(command.StandardOutput);

        for (string line = await reader.ReadLineAsync(); !string.IsNullOrEmpty(line); line = await reader.ReadLineAsync())
        {
          Match match = processesRegEx.Match(line);

          if (!match.Success)
          {
            continue;
          }

          string user = match.Result("${user}");

          uint pid = uint.Parse(match.Result("${pid}"));

          uint ppid = uint.Parse(match.Result("${ppid}"));

          string name = match.Result("${name}");

          processList.Add(pid, new AndroidProcess(device, name, pid, ppid, user));
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException(e);
      }

      return processList;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async static Task<Dictionary<string, string>> GetProp(AndroidDevice device, string args, CancellationToken cancellationToken = default)
    {
      try
      {
        var command = AdbCommand().WithArguments($"-s {device.ID} shell getprop {args}").ExecuteBufferedAsync(cancellationToken);

        var regExMatcher = new Regex(@"^\[(?<key>[^\]:]+)\]:[ ]+\[(?<value>[^\]$]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        var deviceProperties = new Dictionary<string, string>();
        
        using var reader = new StringReader((await command).StandardOutput);

        for (string line = await reader.ReadLineAsync(); !string.IsNullOrEmpty(line); line = await reader.ReadLineAsync())
        {
          Match match = regExMatcher.Match(line);

          if (!match.Success)
          {
            continue;
          }

          string key = match.Result("${key}");

          string value = match.Result("${value}");

          deviceProperties[key] = value;
        }

        return deviceProperties;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException(e);

        return new Dictionary<string, string>();
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static AndroidDevice GetConnectedDeviceById (string id)
    {
      return m_connectedDevices.TryGetValue(id, out AndroidDevice device) ? device : null;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string AdbExe = Path.Combine(AndroidSettings.SdkRoot, "platform-tools", "adb.exe");

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static Command AdbCommand ()
    {
      return Cli.Wrap(AdbExe);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static bool IsDeviceConnected (AndroidDevice queryDevice)
    {
      return m_connectedDevices.ContainsKey(queryDevice.ID);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void RegisterDeviceStateListener (IStateListener listener)
    {
      m_registeredDeviceStateListeners.Add(listener);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void UnregisterDeviceStateListener (IStateListener listener)
    {
      m_registeredDeviceStateListeners.Remove(listener);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

}
