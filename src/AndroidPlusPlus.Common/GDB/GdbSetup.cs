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

    public ICollection<string> CacheSystemBinaries ()
    {
      // 
      // Evaluate remote binaries required for debugging, which must be cached on the host device.
      // 

      LoggingUtils.PrintFunction ();

      string [] deviceBinaries = new string []
      {
        "/system/bin/app_process",
        "/system/bin/app_process32",
        "/system/bin/app_process64",
        "/system/bin/linker",
        "/system/bin/linker64",
      };

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

    public ICollection<string> CacheSystemLibraries ()
    {
      // 
      // Evaluate the remote libraries required for debugging on the host device.
      // 

      LoggingUtils.PrintFunction ();

      List<string> systemLibraries = new List<string> ();

      systemLibraries.AddRange(new string []
      {
        "/system/lib/libandroid.so",
        "/system/lib/libandroid_runtime.so",
        "/system/lib/libbinder.so",
        "/system/lib/libc.so",
        "/system/lib/libEGL.so",
        "/system/lib/libGLESv1_CM.so",
        "/system/lib/libGLESv2.so",
        "/system/lib/libutils.so",
      });

      try
      {
        string dir = "/system/lib64";

        string ls = Process.HostDevice.Shell ("ls", dir);

        if (ls.ToLowerInvariant ().Contains ("no such file"))
        {
          throw new DirectoryNotFoundException (dir);
        }

        systemLibraries.AddRange (new string []
        {
          "/system/lib64/libandroid.so",
          "/system/lib64/libandroid_runtime.so",
          "/system/lib64/libbinder.so",
          "/system/lib64/libc.so",
          "/system/lib64/libEGL.so",
          "/system/lib64/libGLESv1_CM.so",
          "/system/lib64/libGLESv2.so",
          "/system/lib64/libutils.so",
        });
      }
      catch (Exception)
      {
        // Ignore. No lib64 directory?
      }

      // 
      // Pull the required libraries from the device.
      // 

      List<string> hostBinaries = new List<string> ();

      foreach (string binary in systemLibraries)
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

    public ICollection<string> CacheApplicationLibraries ()
    {
      // 
      // Application binaries (those under /lib/ of an installed application).
      // TODO: Consider improving this. Pulling libraries ensures consistency, but takes time (ADB is a slow protocol).
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        HashSet<string> deviceBinaries = new HashSet<string> ();

        foreach (string path in Process.NativeLibraryAbiPaths)
        {
          string ls = string.Empty;

          try
          {
            ls = Process.HostDevice.Shell ("ls", path);

            if (ls.ToLowerInvariant ().Contains ("no such file"))
            {
              throw new DirectoryNotFoundException (path);
            }
          }
          catch (Exception)
          {
          }
          finally
          {
            string [] libraries = ls.Replace ("\r", "").Split (new char [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string file in libraries)
            {
              string lib = path + '/' + file;

              deviceBinaries.Add (lib);
            }
          }
        }

        // 
        // On Android L, Google have broken pull permissions to 'app-lib' (and '/data/app/XXX/lib/') content so we use cp to avoid this.
        // 

        List<string> applicationLibraries = new List<string> (deviceBinaries.Count);

        foreach (string binary in deviceBinaries)
        {
          string cachePath = Path.Combine (CacheSysRoot, binary.Substring (1).Replace ('/', '\\'));

          Directory.CreateDirectory (Path.GetDirectoryName (cachePath));

          try
          {
            if (Process.HostDevice.SdkVersion >= AndroidSettings.VersionCode.LOLLIPOP)
            {
              string temporaryStorage = "/data/local/tmp/" + Path.GetFileName (cachePath);

              Process.HostDevice.Shell ("cp", string.Format ("-fH {0} {1}", binary, temporaryStorage));

              Process.HostDevice.Pull (temporaryStorage, cachePath);

              Process.HostDevice.Shell ("rm", temporaryStorage);

              LoggingUtils.Print (string.Format ("[GdbSetup] Pulled {0} from device/emulator.", binary));
            }
            else
            {
              Process.HostDevice.Pull (binary, cachePath);
            }

            LoggingUtils.Print (string.Format ("[GdbSetup] Pulled {0} from device/emulator.", binary));

            applicationLibraries.Add (binary);
          }
          catch (Exception e)
          {
            LoggingUtils.HandleException (string.Format ("[GdbSetup] Failed pulling {0} from device/emulator.", binary), e);
          }
        }

        return applicationLibraries;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }

      return new List<string>();
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
