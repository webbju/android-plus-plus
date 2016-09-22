////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
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

  public class AndroidProcess
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private string m_apkPath;

    private string m_codePath;

    private string m_dataDir;

    private string m_resourcePath;

    private string m_nativeLibraryPath;

    private ICollection<string> m_nativeLibraryAbiPaths = new List<string> ();

    private string m_legacyNativeLibraryDir;

    private ICollection<string> m_processSupportedCpuAbis = new List<string> ();

    private string m_firstInstallTime;

    private string m_lastUpdateTime;

    private ICollection<string> m_pkgFlags;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidProcess (AndroidDevice device, string name, uint pid, uint ppid, string user)
    {
      if (device == null)
      {
        throw new ArgumentNullException ("device");
      }

      if (string.IsNullOrEmpty (name))
      {
        throw new ArgumentNullException ("name");
      }

      if (string.IsNullOrEmpty (user))
      {
        throw new ArgumentNullException ("user");
      }

      HostDevice = device;

      Name = name;

      Pid = pid;

      ParentPid = ppid;

      User = user;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidDevice HostDevice { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private const string CODE_PATH_EXPRESSION = "      codePath=";

    private const string RESOURCE_PATH_EXPRESSION = "      resourcePath=";

    private const string DATA_DIR_EXPRESSION = "      dataDir=";

    private const string NATIVE_LIBRARY_PATH_EXPRESSION = "      nativeLibraryPath=";

    private const string LEGACY_NATIVE_LIBRARY_DIR_EXPRESSION = "      legacyNativeLibraryDir=";

    private const string PRIMARY_CPU_ABI_EXPRESSION = "      primaryCpuAbi=";

    private const string SECONDARY_CPU_ABI_EXPRESSION = "      secondaryCpuAbi=";

    private const string FIRST_INSTALL_TIME_EXPRESSION = "      firstInstallTime=";

    private const string LAST_UPDATE_TIME_EXPRESSION = "      lastUpdateTime=";

    private const string PKG_FLAGS_EXPRESSION = "      pkgFlags=";

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void RefreshPackageInfo ()
    {
      LoggingUtils.PrintFunction ();

      StringBuilder builder = new StringBuilder (256);

      // 
      // Retrieves the install specific (coded) remote APK path.
      //   i.e: /data/app/com.example.hellogdbserver-2.apk
      // 

      builder.Length = 0;

      builder.Append (HostDevice.Shell ("pm", string.Format ("path {0}", Name)));

      builder.Replace ("\r", "");

      builder.Replace ("\n", "");

      string remoteAppPath = builder.ToString ();

      if (remoteAppPath.StartsWith ("package:"))
      {
        m_apkPath = remoteAppPath.Substring ("package:".Length);
      }

      // 
      // Retrieves the data directory associated with an installed application.
      //   i.e: /data/data/com.example.hellogdbserver/
      // 

      builder.Length = 0;

      builder.Append (HostDevice.Shell ("run-as", string.Format ("{0} /system/bin/sh -c pwd", Name)));

      builder.Replace ("\r", "");

      builder.Replace ("\n", "");

      string remoteDataDirectory = builder.ToString ();

      if (remoteDataDirectory.StartsWith ("/data/"))
      {
        m_dataDir = remoteDataDirectory;
      }

      // 
      // Perform an 'adb shell pm dump <package>' request, and parse output for relevant data.
      // - This isn't available on older devices; it's a fairly recent addition. JB+ possibly?
      // - TODO: This is extremely sub-optimal, but will have to do for now.
      // 

      if (HostDevice.SdkVersion >= AndroidSettings.VersionCode.JELLY_BEAN_MR1)
      {
        builder.Length = 0;

        builder.Append (HostDevice.Shell ("pm", string.Format ("dump {0}", Name)));

        builder.Replace ("\r", "");

        string [] packageDumpReport = builder.ToString ().Split (new char [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < packageDumpReport.Length; ++i)
        {
          string line = packageDumpReport [i];

          if (line.StartsWith (CODE_PATH_EXPRESSION))
          {
            string codePath = line.Substring (CODE_PATH_EXPRESSION.Length);

            if (!string.IsNullOrWhiteSpace (codePath))
            {
              m_codePath = codePath;
            }
          }
          else if (line.StartsWith (DATA_DIR_EXPRESSION))
          {
            string dataDir = line.Substring (DATA_DIR_EXPRESSION.Length);

            if (!string.IsNullOrWhiteSpace (dataDir))
            {
              m_dataDir = dataDir;
            }
          }
          else if (line.StartsWith (RESOURCE_PATH_EXPRESSION))
          {
            string resourcePath = line.Substring (RESOURCE_PATH_EXPRESSION.Length);

            if (!string.IsNullOrWhiteSpace (resourcePath))
            {
              m_resourcePath = resourcePath;
            }
          }
          else if (line.StartsWith (NATIVE_LIBRARY_PATH_EXPRESSION))
          {
            string nativeLibraryPath = line.Substring (NATIVE_LIBRARY_PATH_EXPRESSION.Length);

            if (!string.IsNullOrWhiteSpace (nativeLibraryPath))
            {
              m_nativeLibraryPath = nativeLibraryPath;
            }
          }
          else if (line.StartsWith (LEGACY_NATIVE_LIBRARY_DIR_EXPRESSION))
          {
            string legacyNativeLibraryDir = line.Substring (LEGACY_NATIVE_LIBRARY_DIR_EXPRESSION.Length);

            if (!string.IsNullOrWhiteSpace (legacyNativeLibraryDir))
            {
              m_legacyNativeLibraryDir = legacyNativeLibraryDir;
            }
          }
          else if (line.StartsWith (PRIMARY_CPU_ABI_EXPRESSION))
          {
            string primaryCpiAbi = line.Substring (PRIMARY_CPU_ABI_EXPRESSION.Length);

            if (!string.IsNullOrWhiteSpace (primaryCpiAbi) && !primaryCpiAbi.Equals ("null"))
            {
              m_processSupportedCpuAbis.Add (primaryCpiAbi);
            }
          }
          else if (line.StartsWith (SECONDARY_CPU_ABI_EXPRESSION))
          {
            string secondaryAbi = line.Substring (SECONDARY_CPU_ABI_EXPRESSION.Length);

            if (!string.IsNullOrWhiteSpace (secondaryAbi) && !secondaryAbi.Equals ("null"))
            {
              m_processSupportedCpuAbis.Add (secondaryAbi);
            }
          }
          else if (line.StartsWith (FIRST_INSTALL_TIME_EXPRESSION))
          {
            string firstInstallTime = line.Substring (FIRST_INSTALL_TIME_EXPRESSION.Length);

            if (!string.IsNullOrWhiteSpace (firstInstallTime))
            {
              m_firstInstallTime = firstInstallTime;
            }
          }
          else if (line.StartsWith (LAST_UPDATE_TIME_EXPRESSION))
          {
            string lastUpdateTime = line.Substring (LAST_UPDATE_TIME_EXPRESSION.Length);

            if (!string.IsNullOrWhiteSpace (lastUpdateTime))
            {
              m_lastUpdateTime = lastUpdateTime;
            }
          }
          else if (line.StartsWith (PKG_FLAGS_EXPRESSION))
          {
            string pkgFlags = line.Substring (PKG_FLAGS_EXPRESSION.Length);
            
            string [] pkgFlagsArray = pkgFlags.Trim (new char [] { '[', ']' }).Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            m_pkgFlags = pkgFlagsArray;
          }
        }
      }

      // 
      // Clean up some variables which may be empty or undefined.
      // 

      if (string.IsNullOrWhiteSpace (m_dataDir))
      {
        m_dataDir = string.Concat ("/data/data/", Name);
      }

      if (string.IsNullOrWhiteSpace (m_codePath))
      {
        if (!string.IsNullOrWhiteSpace (m_dataDir))
        {
          m_codePath = m_dataDir;
        }
        else
        {
          m_codePath = string.Concat ("/data/data/", Name);
        }
      }

      foreach (string abi in HostDevice.SupportedCpuAbis)
      {
        // 
        // Add each of the additional supported ABIs; but keep those already identified via dump output as primary/secondary.
        // 

        if (!m_processSupportedCpuAbis.Contains (abi))
        {
          m_processSupportedCpuAbis.Add (abi);
        }
      }

      if (string.IsNullOrWhiteSpace (m_legacyNativeLibraryDir))
      {
        if (HostDevice.SdkVersion >= AndroidSettings.VersionCode.JELLY_BEAN_MR1)
        {
          string bundleId = Path.GetFileNameWithoutExtension (m_apkPath);

          m_legacyNativeLibraryDir = string.Concat ("/data/app-lib/", bundleId);
        }
        else
        {
          m_legacyNativeLibraryDir = string.Concat (m_codePath, "/lib");
        }
      }

      if (string.IsNullOrWhiteSpace (m_nativeLibraryPath))
      {
        m_nativeLibraryPath = m_legacyNativeLibraryDir;
      }

      if (HostDevice.SdkVersion >= AndroidSettings.VersionCode.JELLY_BEAN_MR1)
      {
        ICollection<string> nativeLibraryAbiPaths = new List<string> (m_processSupportedCpuAbis.Count);

        foreach (string abi in m_processSupportedCpuAbis)
        {
          switch (abi)
          {
            case "armeabi":
            case "armeabi-v7a":
            {
              nativeLibraryAbiPaths.Add (string.Concat (m_nativeLibraryPath, "/", "arm"));

              break;
            }

            case "arm64-v8a":
            {
              nativeLibraryAbiPaths.Add (string.Concat (m_nativeLibraryPath, "/", "arm64"));

              break;
            }

            case "x86":
            case "x86_64":
            case "mips":
            case "mips64":
            default:
            {
              nativeLibraryAbiPaths.Add (string.Concat (m_nativeLibraryPath, "/", abi));

              break;
            }
          }
        }

        m_nativeLibraryAbiPaths = nativeLibraryAbiPaths;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string RemoteApkPath
    {
      get
      {
        return m_apkPath;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string DataDirectory
    {
      get
      {
        return m_dataDir;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string NativeLibraryPath
    {
      get
      {
        return m_nativeLibraryPath;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ICollection<string> NativeLibraryAbiPaths
    {
      get
      {
        return m_nativeLibraryAbiPaths;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ICollection<string> ProcessSupportedCpuAbis
    {
      get
      {
        return m_processSupportedCpuAbis;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool IsUserProcess 
    {
      get
      {
        // 
        // Android 4.1+ applications are designated a user-based app-id (e.g. u0_a60), older SDKs prefer 'app' - assume any without these are a system process.
        // 

        return (User.StartsWith ("u") && User.Contains ("_")) || User.StartsWith ("app");
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string User { get; protected set; }

    public uint Pid { get; protected set; }

    public uint ParentPid { get; protected set; }

    //public uint Vsize { get; protected set; }

    //public uint Rss { get; protected set; }

    //public uint Wchan { get; protected set; }

    //public uint Pc { get; protected set; }

    public string Name { get; protected set; }

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
