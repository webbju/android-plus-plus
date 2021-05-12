////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using CliWrap.Buffered;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.Common
{

  public sealed class GdbSetup
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public GdbSetup(AndroidProcess process, string gdbToolPath)
    {
      LoggingUtils.PrintFunction();

      Process = process;

      Host = "localhost";

      Port = RandomAvailablePort();

      if (!Process.HostDevice.IsOverWiFi)
      {
        Socket = "debug-socket";
      }

      string sanitisedDeviceId = Process.HostDevice.ID.Replace(':', '-');

      CacheDirectory = string.Format(@"{0}\Android++\Cache\{1}\{2}", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), sanitisedDeviceId, Process.Name);

      CacheSysRoot = Path.Combine(CacheDirectory, "sysroot");

      DebugFileDirectories = new HashSet<string>();

      GdbToolPath = gdbToolPath;

      GdbToolArguments = "--interpreter=mi ";

      if (!File.Exists(gdbToolPath))
      {
        throw new FileNotFoundException("Could not find requested GDB instance. Expected: " + gdbToolPath);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidProcess Process { get; set; }

    public string Socket { get; set; }

    public string Host { get; set; }

    public uint Port { get; set; }

    public string CacheDirectory { get; set; }

    public string CacheSysRoot { get; set; }

    public HashSet<string> DebugFileDirectories { get; set; }

    public string GdbToolPath { get; set; }

    public string GdbToolArguments { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async Task SetupPortForwarding (CancellationToken cancellationToken = default)
    {
      LoggingUtils.PrintFunction();

      var builder = new StringBuilder ();

      builder.Append($"--no-rebind ");

      builder.Append ($"tcp:{Port} ");

      if (!string.IsNullOrWhiteSpace (Socket))
      {
        builder.Append ($"localfilesystem:{Process.GetDataDirectory()}/{Socket}");
      }
      else
      {
        builder.Append ($"tcp:{Port}");
      }

      await AndroidAdb.AdbCommand().WithArguments($"-s {Process.HostDevice.ID} forward {builder}").ExecuteAsync(cancellationToken);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async Task ClearPortForwarding (CancellationToken cancellationToken = default)
    {
      LoggingUtils.PrintFunction();

      await AndroidAdb.AdbCommand().WithArguments($"-s {Process.HostDevice.ID} forward --remove tcp:{Port}").WithValidation(CliWrap.CommandResultValidation.None).ExecuteAsync(cancellationToken);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static uint RandomAvailablePort(uint defaultPort = 5039)
    {
      uint availablePort = defaultPort;

      try
      {
        var listener = new TcpListener(IPAddress.Loopback, 0);

        listener.Start();

        availablePort = (uint)((IPEndPoint)listener.LocalEndpoint).Port;

        listener.Stop();
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException(e);
      }

      return availablePort;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async Task<ICollection<string>> CacheSystemBinaries (CancellationToken cancellationToken = default)
    {
      //
      // Evaluate remote binaries required for debugging, which must be cached on the host device.
      //

      LoggingUtils.PrintFunction ();

      var deviceBinaries = new List<string>
      {
        "/system/bin/app_process",
        "/system/bin/app_process32",
        "/system/bin/linker",
      };

      if (string.Equals("zygote64", Process.HostDevice.GetProperty("ro.zygote")))
      {
        deviceBinaries.AddRange(new string[] { "/system/bin/app_process64", "/system/bin/linker64" });
      }

      //
      // Pull the required binaries from the device.
      //

      var hostBinaries = new List<string> ();

      foreach (string binary in deviceBinaries)
      {
        try
        {
          string cachedBinary = Path.Combine(CacheSysRoot, binary.Substring(1));

          Directory.CreateDirectory(Path.GetDirectoryName(cachedBinary));

          FileInfo cachedBinaryFileInfo = new FileInfo(cachedBinary);

          if (cachedBinaryFileInfo.Exists && (DateTime.UtcNow - cachedBinaryFileInfo.CreationTimeUtc) < TimeSpan.FromDays(1))
          {
            hostBinaries.Add(cachedBinaryFileInfo.FullName);

            LoggingUtils.Print($"[{GetType().Name}] Using cached {binary}");

            continue;
          }

          await AndroidAdb.Pull(Process.HostDevice, binary, cachedBinary);

          hostBinaries.Add(cachedBinaryFileInfo.FullName);

          LoggingUtils.Print($"[{GetType().Name}] Pulled {binary} from device/emulator.");
        }
        catch (Exception e)
        {
          LoggingUtils.HandleException(e);
        }
      }

      return hostBinaries;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async Task<ICollection<string>> CacheSystemLibraries (CancellationToken cancellationToken = default)
    {
      //
      // Evaluate the remote libraries required for debugging on the host device.
      //

      LoggingUtils.PrintFunction ();

      AndroidDevice device = Process.HostDevice;

      string systemLibDir = string.Equals("zygote64", device.GetProperty("ro.zygote")) ? "/system/lib64" : "/system/lib";

      var cacheableSystemLibraries = new List<string>
      {
        $"{systemLibDir}/libc.so",
        $"{systemLibDir}/libdl.so",
        $"{systemLibDir}/libm.so",
      };

      //
      // Pull the required libraries from the device.
      //

      var hostBinaries = new List<string> ();

      foreach (string binary in cacheableSystemLibraries)
      {
        try
        {
          string cachedBinary = Path.Combine(CacheSysRoot, binary.Substring(1));

          Directory.CreateDirectory(Path.GetDirectoryName(cachedBinary));

          var cachedBinaryFileInfo = new FileInfo(cachedBinary);

          if (cachedBinaryFileInfo.Exists && (DateTime.UtcNow - cachedBinaryFileInfo.CreationTimeUtc) < TimeSpan.FromDays(1))
          {
            LoggingUtils.Print($"[GdbSetup] Using cached {binary}.");

            hostBinaries.Add(cachedBinaryFileInfo.FullName);

            continue;
          }

          await AndroidAdb.Pull(device, binary, cachedBinary);

          hostBinaries.Add(cachedBinaryFileInfo.FullName);

          LoggingUtils.Print($"[GdbSetup] Pulled {binary} from device/emulator.");
        }
        catch (Exception e)
        {
          LoggingUtils.HandleException(e);
        }
      }

      return hostBinaries;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async Task<ICollection<string>> CacheApplicationLibraries (CancellationToken cancellationToken = default)
    {
      //
      // Application binaries (those under /lib/ of an installed application).
      // TODO: Consider improving this. Pulling libraries ensures consistency, but takes time (ADB is a slow protocol).
      //

      LoggingUtils.PrintFunction ();

      AndroidDevice device = Process.HostDevice;

      var cachedLibaries = new HashSet<string>();

      foreach (string path in Process.GetNativeLibraryAbiPaths())
      {
        try
        {
          var command = await AndroidAdb.AdbCommand().WithArguments($"-s {device.ID} shell ls {path}/*.so").ExecuteBufferedAsync(cancellationToken);

          using var reader = new StringReader(command.StandardOutput);

          for (string line = await reader.ReadLineAsync(); !string.IsNullOrEmpty(line); line = await reader.ReadLineAsync())
          {
            string binary = line;

            string cachePath = Path.Combine(CacheSysRoot, binary.Substring(1).Replace('/', '\\'));

            Directory.CreateDirectory(Path.GetDirectoryName(cachePath));

            try
            {
              await AndroidAdb.Pull(device, binary, cachePath);

              cachedLibaries.Add(binary);

              LoggingUtils.Print($"[GdbSetup] Pulled {binary} from device/emulator.");

              continue;
            }
            catch (Exception e)
            {
              LoggingUtils.HandleException(e);
            }

            //
            // On Android L, Google have broken pull permissions to 'app-lib' (and '/data/app/XXX/lib/') content so we use cp to attempt this instead.
            //

            try
            {
              string temporaryStorage = "/data/local/tmp/" + Path.GetFileName(cachePath);

              await AndroidAdb.AdbCommand().WithArguments($"-s {device.ID} shell cp -fH {binary} {temporaryStorage}").ExecuteAsync(cancellationToken);

              await AndroidAdb.Pull(device, temporaryStorage, cachePath);

              await AndroidAdb.AdbCommand().WithArguments($"-s {device.ID} shell rm -f {temporaryStorage}").ExecuteAsync(cancellationToken);

              LoggingUtils.Print($"[GdbSetup] Pulled {binary} from device/emulator.");
            }
            catch (Exception e)
            {
              LoggingUtils.HandleException(e);
            }
          }
        }
        catch (Exception e)
        {
          LoggingUtils.HandleException(e);
        }
      }

      return cachedLibaries;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

}
