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

      string sanitisedDeviceId = Process.HostDevice.ID.Replace (':', '-');

      CacheDirectory = string.Format (@"{0}\Android++\Cache\{1}\{2}", Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), sanitisedDeviceId, Process.Name);

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

      // 
      // Spawn an initial GDB instance to evaluate the client version.
      // 

      GdbToolVersionMajor = 1;

      GdbToolVersionMinor = 0;

      using (SyncRedirectProcess gdbProcess = new SyncRedirectProcess (GdbToolPath, "--version"))
      {
        gdbProcess.StartAndWaitForExit ();

        string [] versionDetails = gdbProcess.StandardOutput.Replace ("\r", "").Split (new char [] { '\n' });

        string versionPrefix = "GNU gdb (GDB) ";

        for (int i = 0; i < versionDetails.Length; ++i)
        {
          if (versionDetails [i].StartsWith (versionPrefix))
          {
            string gdbVersion = versionDetails [i].Substring (versionPrefix.Length); ;

            string [] gdbVersionComponents = gdbVersion.Split ('.');

            if (gdbVersionComponents.Length > 0)
            {
              GdbToolVersionMajor = int.Parse (gdbVersionComponents [0]);
            }

            if (gdbVersionComponents.Length > 1)
            {
              GdbToolVersionMinor = int.Parse (gdbVersionComponents [1]);
            }

            break;
          }
        }
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

    public int GdbToolVersionMajor { get; set; }

    public int GdbToolVersionMinor { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool IsVersionEqualOrAbove (int major, int minor)
    {
      if ((major < GdbToolVersionMajor)
        || ((major == GdbToolVersionMajor) && (minor < GdbToolVersionMinor)))
      {
        return false;
      }

      return true;
    }

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

      string libdir = (only64bit) ? "lib64" : "lib";

      deviceLibraries.AddRange (new string []
      {
        string.Format ("/system/{0}/libandroid.so", libdir),
        string.Format ("/system/{0}/libandroid_runtime.so", libdir),
      //string.Format ("/system/{0}/libart.so", libdir),
        string.Format ("/system/{0}/libbinder.so", libdir),
        string.Format ("/system/{0}/libc.so", libdir),
      //string.Format ("/system/{0}/libdvm.so", libdir),
        string.Format ("/system/{0}/libEGL.so", libdir),
        string.Format ("/system/{0}/libGLESv1_CM.so", libdir),
        string.Format ("/system/{0}/libGLESv2.so", libdir),
        string.Format ("/system/{0}/libutils.so", libdir),
      });

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
        List<string> additionalLibraries = new List<string> ();

        foreach (string nativeLibraryAbiPath in Process.NativeLibraryAbiPaths)
        {
          string nativeOatLibraryPath = nativeLibraryAbiPath.Replace ("/lib", "/oat");

          // 
          // On Android L, Google have broken pull permissions to 'app-lib' (and '/data/app/XXX/lib/') content so we use cp to avoid this.
          // 

          bool pulledLibraries = false;

          string libraryCachePath = Path.Combine (CacheSysRoot, nativeLibraryAbiPath.Substring (1));

          Directory.CreateDirectory (libraryCachePath);

          if (Process.HostDevice.SdkVersion >= AndroidSettings.VersionCode.LOLLIPOP)
          {
            string [] libraries = Process.HostDevice.Shell ("ls", nativeLibraryAbiPath).Replace ("\r", "").Split (new char [] { '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string file in libraries)
            {
              string remoteLib = nativeLibraryAbiPath + "/" + file;

              string temporaryStorage = "/data/local/tmp/" + file;

              try
              {
                Process.HostDevice.Shell ("cp", string.Format ("-fH {0} {1}", remoteLib, temporaryStorage));

                Process.HostDevice.Pull (temporaryStorage, libraryCachePath);

                Process.HostDevice.Shell ("rm", temporaryStorage);

                LoggingUtils.Print (string.Format ("[GdbSetup] Pulled {0} from device/emulator.", remoteLib));

                pulledLibraries = true;
              }
              catch (Exception e)
              {
                LoggingUtils.HandleException (string.Format ("[GdbSetup] Failed pulling {0} from device/emulator.", remoteLib), e);
              }
            }
          }

          // 
          // Also on Android L, Google's new oat format is an ELF which is readable by GDB. We want to include this in the sysroot.
          // 

          bool pulledOatLibraries = false;

          string libraryOatCachePath = Path.Combine (CacheSysRoot, nativeOatLibraryPath.Substring (1));

          Directory.CreateDirectory (libraryOatCachePath);

          if (Process.HostDevice.SdkVersion >= AndroidSettings.VersionCode.LOLLIPOP)
          {
            string [] oatLibraries = new string []
            {
              // Due to permissions these have to be directly referenced; ls won't work.
              "base.odex"
            };

            foreach (string file in oatLibraries)
            {
              string remoteLib = nativeOatLibraryPath + "/" + file;

              string temporaryStorage = "/data/local/tmp/" + file;

              try
              {
                Process.HostDevice.Shell ("cp", string.Format ("-fH {0} {1}", remoteLib, temporaryStorage));

                Process.HostDevice.Pull (temporaryStorage, libraryCachePath);

                Process.HostDevice.Shell ("rm", temporaryStorage);

                LoggingUtils.Print (string.Format ("[GdbSetup] Pulled {0} from device/emulator.", remoteLib));

                pulledOatLibraries = true;
              }
              catch (Exception e)
              {
                LoggingUtils.HandleException (string.Format ("[GdbSetup] Failed pulling {0} from device/emulator.", remoteLib), e);
              }
            }
          }

          try
          {
            if (!pulledLibraries)
            {
              Process.HostDevice.Pull (nativeLibraryAbiPath, libraryCachePath);

              LoggingUtils.Print (string.Format ("[GdbSetup] Pulled {0} from device/emulator.", nativeLibraryAbiPath));

              pulledLibraries = true;
            }

            if (!pulledOatLibraries)
            {
              Process.HostDevice.Pull (nativeOatLibraryPath, libraryOatCachePath);

              LoggingUtils.Print (string.Format ("[GdbSetup] Pulled {0} from device/emulator.", nativeOatLibraryPath));

              pulledOatLibraries = true;
            }
          }
          catch (Exception e)
          {
            LoggingUtils.HandleException (string.Format ("[GdbSetup] Failed pulling {0} from device/emulator.", nativeLibraryAbiPath), e);
          }

          additionalLibraries.AddRange (Directory.GetFiles (libraryCachePath, "lib*.so", SearchOption.AllDirectories));

          additionalLibraries.AddRange (Directory.GetFiles (libraryOatCachePath, "*.odex", SearchOption.AllDirectories));
        }

        return additionalLibraries.ToArray ();
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

      gdbExecutionCommands.Add ("set breakpoint pending on");

      gdbExecutionCommands.Add ("set logging file " + PathUtils.SantiseWindowsPath (Path.Combine (CacheDirectory, "gdb.log")));

      gdbExecutionCommands.Add ("set logging overwrite on");

      gdbExecutionCommands.Add ("set logging on");

    #if false
      if (IsVersionEqualOrAbove (7, 7))
      {
        gdbExecutionCommands.Add ("set mi-async on"); // as above, from GDB 7.7
      }
    #endif

    #if DEBUG && false
      gdbExecutionCommands.Add ("set debug remote 1");

      gdbExecutionCommands.Add ("set debug infrun 1");

      gdbExecutionCommands.Add ("set verbose on");
    #endif

      // 
      // Include a script copied from 'platform/development' (Android Git) which allows JVM stack traces on via Python.
      // - It also define a special mode for controlling debugging behaviour on ART.
      // 

    #if false
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
    #endif

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
