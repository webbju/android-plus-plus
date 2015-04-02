////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.Common
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public sealed class GdbSetup : IDisposable
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public GdbSetup (AndroidProcess process, string gdbToolPath)
    {
      LoggingUtils.PrintFunction ();

      Process = process;

      Host = "localhost";

      Port = 5039;

      if (!Process.HostDevice.IsOverWiFi)
      {
        Socket = "debug-socket";
      }

      string deviceDir = Process.HostDevice.ID.Replace (':', '-');

      CacheDirectory = string.Format (@"{0}\Android++\Cache\{1}\{2}", Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), deviceDir, Process.Name);

      Directory.CreateDirectory (CacheDirectory);

      CacheSysRoot = Path.Combine (CacheDirectory, "sysroot");

      Directory.CreateDirectory (CacheSysRoot);

      SymbolDirectories = new HashSet<string> ();

      GdbToolPath = gdbToolPath;

      GdbToolArguments = "--interpreter=mi ";

      if (!File.Exists (gdbToolPath))
      {
        throw new FileNotFoundException ("Could not find requested GDB instance. Expected: " + gdbToolPath);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Dispose ()
    {
      LoggingUtils.PrintFunction ();
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

    public HashSet<string> SymbolDirectories { get; set; }

    public string GdbToolPath { get; set; }

    public string GdbToolArguments { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupPortForwarding ()
    {
      // 
      // Setup network redirection.
      // 

      LoggingUtils.PrintFunction ();

      StringBuilder forwardArgsBuilder = new StringBuilder ();

      forwardArgsBuilder.AppendFormat ("tcp:{0} ", Port);

      if (!string.IsNullOrWhiteSpace (Socket))
      {
        forwardArgsBuilder.AppendFormat ("localfilesystem:{0}/{1}", Process.DataDirectory, Socket);
      }
      else
      {
        forwardArgsBuilder.AppendFormat ("tcp:{0} ", Port);
      }

      using (SyncRedirectProcess adbPortForward = AndroidAdb.AdbCommand (Process.HostDevice, "forward", forwardArgsBuilder.ToString ()))
      {
        adbPortForward.StartAndWaitForExit ();
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ClearPortForwarding ()
    {
      // 
      // Clear network redirection.
      // 

      LoggingUtils.PrintFunction ();

      StringBuilder forwardArgsBuilder = new StringBuilder ();

      forwardArgsBuilder.AppendFormat ("--remove tcp:{0}", Port);

      using (SyncRedirectProcess adbPortForward = AndroidAdb.AdbCommand (Process.HostDevice, "forward", forwardArgsBuilder.ToString ()))
      {
        adbPortForward.StartAndWaitForExit (1000);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string [] CacheSystemBinaries (bool only64bit)
    {
      // 
      // Evaluate remote binaries required for debugging, which must be cached on the host device.
      // 

      LoggingUtils.PrintFunction ();

      List<string> deviceBinaries = new List<string> ();

      if (only64bit && (Process.HostDevice.SdkVersion >= AndroidSettings.VersionCode.LOLLIPOP))
      {
        deviceBinaries.AddRange (new string []
        {
          "/system/bin/app_process64",
          "/system/bin/linker64",
        });
      }
      else
      {
        deviceBinaries.AddRange (new string []
        {
          "/system/bin/app_process",
          "/system/bin/app_process32",
          "/system/bin/linker",
        });
      }

      // 
      // Pull the required binaries from the device.
      // 

      List<string> hostBinaries = new List<string> ();

      foreach (string binary in deviceBinaries)
      {
        string cachedBinary = Path.Combine (CacheSysRoot, binary.Substring (1));

        string cachedBinaryDir = Path.GetDirectoryName (cachedBinary);

        string cachedBinaryFullPath = Path.Combine (cachedBinaryDir, Path.GetFileName (cachedBinary));

        Directory.CreateDirectory (cachedBinaryDir);

        FileInfo cachedBinaryFileInfo = new FileInfo (cachedBinaryFullPath);

        bool usedCached = false;

        if (cachedBinaryFileInfo.Exists && (DateTime.UtcNow - cachedBinaryFileInfo.CreationTimeUtc) < TimeSpan.FromDays (1))
        {
          LoggingUtils.Print (string.Format ("[GdbSetup] Using cached {0}.", binary));

          hostBinaries.Add (cachedBinaryFullPath);

          usedCached = true;
        }

        if (!usedCached)
        {
          try
          {
            Process.HostDevice.Pull (binary, cachedBinary);

            hostBinaries.Add (cachedBinaryFullPath);

            LoggingUtils.Print (string.Format ("[GdbSetup] Pulled {0} from device/emulator.", binary));
          }
          catch (Exception e)
          {
            LoggingUtils.HandleException (e);
          }
        }
      }

      return hostBinaries.ToArray ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string [] CacheSystemLibraries (bool only64bit)
    {
      // 
      // Evaluate the remote libraries required for debugging on the host device.
      // 

      LoggingUtils.PrintFunction ();

      List<string> deviceLibraries = new List<string> ();

      if (only64bit && (Process.HostDevice.SdkVersion >= AndroidSettings.VersionCode.LOLLIPOP))
      {
        deviceLibraries.AddRange (new string []
        {
          "/system/lib64/libandroid.so",
          "/system/lib64/libandroid_runtime.so",
          //"/system/lib64/libart.so",
          "/system/lib64/libbinder.so",
          "/system/lib64/libc.so",
          //"/system/lib64/libdvm.so",
          "/system/lib64/libEGL.so",
          "/system/lib64/libGLESv1_CM.so",
          "/system/lib64/libGLESv2.so",
          "/system/lib64/libGLESv3.so",
          "/system/lib64/libutils.so",
        });
      }
      else
      {
        deviceLibraries.AddRange (new string []
        {
          "/system/lib/libandroid.so",
          "/system/lib/libandroid_runtime.so",
          //"/system/lib/libart.so",
          "/system/lib/libbinder.so",
          "/system/lib/libc.so",
          //"/system/lib/libdvm.so",
          "/system/lib/libEGL.so",
          "/system/lib/libGLESv1_CM.so",
          "/system/lib/libGLESv2.so",
          "/system/lib/libGLESv3.so",
          "/system/lib/libutils.so",
        });
      }

      // 
      // Pull the required libraries from the device.
      // 

      List<string> hostBinaries = new List<string> ();

      foreach (string binary in deviceLibraries)
      {
        string cachedBinary = Path.Combine (CacheSysRoot, binary.Substring (1));

        string cachedBinaryDir = Path.GetDirectoryName (cachedBinary);

        string cachedBinaryFullPath = Path.Combine (Path.GetDirectoryName (cachedBinary), Path.GetFileName (cachedBinary));

        Directory.CreateDirectory (cachedBinaryDir);

        FileInfo cachedBinaryFileInfo = new FileInfo (cachedBinaryFullPath);

        bool usedCached = false;

        if (cachedBinaryFileInfo.Exists && (DateTime.UtcNow - cachedBinaryFileInfo.CreationTimeUtc) < TimeSpan.FromDays (1))
        {
          LoggingUtils.Print (string.Format ("[GdbSetup] Using cached {0}.", binary));

          hostBinaries.Add (cachedBinaryFullPath);

          usedCached = true;
        }

        if (!usedCached)
        {
          try
          {
            Process.HostDevice.Pull (binary, cachedBinary);

            hostBinaries.Add (cachedBinaryFullPath);

            LoggingUtils.Print (string.Format ("[GdbSetup] Pulled {0} from device/emulator.", binary));
          }
          catch (Exception e)
          {
            LoggingUtils.HandleException (e);
          }
        }
      }

      return hostBinaries.ToArray ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string [] CacheApplicationLibraries ()
    {
      // 
      // Application binaries (those under /lib/ of an installed application).
      // TODO: Consider improving this. Pulling libraries ensures consistency, but takes time (ADB is a slow protocol).
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        string libraryCachePath = Path.Combine (CacheSysRoot, Process.NativeLibraryPath.Substring (1));

        Directory.CreateDirectory (libraryCachePath);

#if true
        if (Process.HostDevice.SdkVersion >= AndroidSettings.VersionCode.LOLLIPOP)
        {
          // 
          // On Android L, Google have broken pull permissions to 'app-lib' (and '/data/app/XXX/lib/') content so we use cp to avoid this.
          // 

          string [] libraries = Process.HostDevice.Shell ("ls", Process.NativeLibraryPath).Replace ("\r", "").Split (new char [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

          foreach (string lib in libraries)
          {
            string remoteLib = Process.NativeLibraryPath + "/" + lib;

            string temporaryStorage = "/data/local/tmp/" + lib;

            Process.HostDevice.Shell ("cp", string.Format ("-fH {0} {1}", remoteLib, temporaryStorage));

            Process.HostDevice.Pull (temporaryStorage, libraryCachePath);

            Process.HostDevice.Shell ("rm", temporaryStorage);
          }
        }
        else
#endif
        {
          Process.HostDevice.Pull (Process.NativeLibraryPath, libraryCachePath);
        }

        LoggingUtils.Print (string.Format ("[GdbSetup] Pulled application libraries from device/emulator."));

        string [] additionalLibraries = Directory.GetFiles (libraryCachePath, "lib*.so", SearchOption.AllDirectories);

        return additionalLibraries;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }

      return new string [] {};
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string [] CreateGdbExecutionScript ()
    {
      LoggingUtils.PrintFunction ();

      List<string> gdbExecutionCommands = new List<string> ();

      gdbExecutionCommands.Add ("set target-async on");

      //gdbExecutionCommands.Add ("set mi-async on"); // as above, from GDB 7.7

      gdbExecutionCommands.Add ("set breakpoint pending on");

      gdbExecutionCommands.Add ("set logging file " + PathUtils.SantiseWindowsPath (Path.Combine (CacheDirectory, "gdb.log")));

      gdbExecutionCommands.Add ("set logging overwrite on");

      gdbExecutionCommands.Add ("set logging on");

#if DEBUG && false
      gdbExecutionCommands.Add ("set debug remote 1");

      gdbExecutionCommands.Add ("set debug infrun 1");

      gdbExecutionCommands.Add ("set verbose on");
#endif

      // 
      // Include a script copied from 'platform/development' (Android Git) which allows JVM stack traces on via Python.
      // - It also define a special mode for controlling debugging behaviour on ART.
      // 

      string androidPlusPlusRoot = Environment.GetEnvironmentVariable ("ANDROID_PLUS_PLUS");

      string dalkvikGdbScriptPath = Path.Combine (androidPlusPlusRoot, "contrib", "gdb", "scripts", "dalvik.gdb");

      if (File.Exists (dalkvikGdbScriptPath))
      {
        gdbExecutionCommands.Add ("source " + PathUtils.SantiseWindowsPath (dalkvikGdbScriptPath));

        if (Process.HostDevice.SdkVersion >= AndroidSettings.VersionCode.LOLLIPOP)
        {
          gdbExecutionCommands.Add ("art-on");

          gdbExecutionCommands.Add ("handle SIGSEGV print stop");
        }
      }

      return gdbExecutionCommands.ToArray ();
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
