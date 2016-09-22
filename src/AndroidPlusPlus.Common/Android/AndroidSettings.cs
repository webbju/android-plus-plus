////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using Microsoft.Win32;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.Common
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class AndroidSettings
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public struct VersionCode
    {
      public const uint BASE = 1;
      public const uint BASE_1_1 = BASE + 1;
      public const uint CUPCAKE = BASE_1_1 + 1;
      public const uint DONUT = CUPCAKE + 1;
      public const uint ECLAIR = DONUT + 1;
      public const uint ECLAIR_0_1 = ECLAIR + 1;
      public const uint ECLAIR_MR1 = ECLAIR_0_1 + 1;
      public const uint FROYO = ECLAIR_MR1 + 1;
      public const uint GINGERBREAD = FROYO + 1;
      public const uint GINGERBREAD_MR1 = GINGERBREAD + 1;
      public const uint HONEYCOMB = GINGERBREAD_MR1 + 1;
      public const uint HONEYCOMB_MR1 = HONEYCOMB + 1;
      public const uint HONEYCOMB_MR2 = HONEYCOMB_MR1 + 1;
      public const uint ICE_CREAM_SANDWICH = HONEYCOMB_MR2 + 1;
      public const uint ICE_CREAM_SANDWICH_MR1 = ICE_CREAM_SANDWICH + 1;
      public const uint JELLY_BEAN = ICE_CREAM_SANDWICH_MR1 + 1;
      public const uint JELLY_BEAN_MR1 = JELLY_BEAN + 1;
      public const uint JELLY_BEAN_MR2 = JELLY_BEAN_MR1 + 1;
      public const uint KITKAT = JELLY_BEAN_MR2 + 1;
      public const uint KITKAT_WATCH = KITKAT + 1;
      public const uint L_PREVIEW = KITKAT_WATCH;
      public const uint LOLLIPOP = KITKAT_WATCH + 1;
      public const uint LOLLIPOP_MR1 = LOLLIPOP + 1;
      public const uint M = LOLLIPOP_MR1 + 1;
      public const uint N = M + 1;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string SdkRoot
    {
      get
      {
        // 
        // Probe for possible Android SDK installation directories.
        // 

        HashSet<string> androidSdkPossibleLocations = new HashSet<string> (); 

        try
        {
          string [] environmentPaths = new string [] 
          {
            Environment.GetEnvironmentVariable ("ANDROID_SDK"),
            Environment.GetEnvironmentVariable ("ANDROID_SDK_ROOT"),
            Environment.GetEnvironmentVariable ("ANDROID_HOME")
          };

          foreach (string possiblePath in environmentPaths)
          {
            if ((!string.IsNullOrEmpty (possiblePath)) && (!androidSdkPossibleLocations.Contains (possiblePath)))
            {
              androidSdkPossibleLocations.Add (possiblePath);
            }
          }
        }
        catch (SecurityException e)
        {
          LoggingUtils.Print (string.Format ("Failed retrieving ANDROID_SDK_* environment variables: {0}", e.Message));

          LoggingUtils.HandleException (e);
        }

        using (RegistryKey localMachineAndroidSdkTools = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Android SDK Tools\"))
        {
          if (localMachineAndroidSdkTools != null)
          {
            androidSdkPossibleLocations.Add (localMachineAndroidSdkTools.GetValue ("Path") as string);
          }
        }

        using (RegistryKey currentUserAndroidSdkTools = Registry.CurrentUser.OpenSubKey (@"SOFTWARE\Android SDK Tools\"))
        {
          if (currentUserAndroidSdkTools != null)
          {
            androidSdkPossibleLocations.Add (currentUserAndroidSdkTools.GetValue ("Path") as string);
          }
        }

        // 
        // Search specified path the default 'SDK Manager' executable.
        // 

        foreach (string location in androidSdkPossibleLocations)
        {
          if (location != null)
          {
            if (File.Exists (Path.Combine(location, "SDK Manager.exe")))
            {
              return location;
            }
          }
        }

        return null;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static ICollection <uint> SdkInstalledPlatforms
    {
      get
      {
        // 
        // Evaluate which 'platforms' are supported by this distribution. (This implementation is crude, should use Android.bat).
        // 

        List<uint> installedSdkPlatforms = new List<uint> ();

        string platformSrcPath = Path.Combine (SdkRoot, "platforms");

        if (File.Exists (platformSrcPath))
        {
          string [] platformDirs = Directory.GetDirectories (platformSrcPath);

          for (uint i = 0; i < platformDirs.Length; ++i)
          {
            if (platformDirs [i].StartsWith ("android-"))
            {
              installedSdkPlatforms.Add (uint.Parse (platformDirs [i].Substring ("android-".Length - 1)));
            }
          }
        }

        if (installedSdkPlatforms.Count == 0)
        {
          throw new InvalidOperationException ();
        }

        return installedSdkPlatforms;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string NdkRoot
    {
      get
      {
        // 
        // Probe for possible Android NDK installation directories.
        // 

        List <string> androidNdkPossibleLocations = new List <string> (2); 

        try
        {
          androidNdkPossibleLocations.Add (Environment.GetEnvironmentVariable ("ANDROID_NDK"));

          androidNdkPossibleLocations.Add (Environment.GetEnvironmentVariable ("ANDROID_NDK_ROOT"));

          androidNdkPossibleLocations.Add (Environment.GetEnvironmentVariable ("ANDROID_NDK_PATH"));
        }
        catch (SecurityException e)
        {
          LoggingUtils.Print (string.Format ("Failed retrieving ANDROID_NDK_* environment variables: {0}", e.Message));

          LoggingUtils.HandleException (e);
        }

        // 
        // Search specified path the default 'ndk-build' script.
        // 

        foreach (string location in androidNdkPossibleLocations)
        {
          if (location != null)
          {
            if (File.Exists (Path.Combine (location, "ndk-build")))
            {
              return location;
            }
          }
        }

        return null;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static ICollection<uint> NdkInstalledPlatforms
    {
      get
      {
        // 
        // Evaluate which 'platforms' are supported by this distribution. (This implemenation is crude, should use Android.bat).
        // 

        List<uint> installedNdkPlatforms = new List<uint> ();

        string platformSrcPath = Path.Combine (NdkRoot, "platforms");

        if (File.Exists (platformSrcPath))
        {
          string [] platformDirs = Directory.GetDirectories (platformSrcPath);

          for (uint i = 0; i < platformDirs.Length; ++i)
          {
            if (platformDirs [i].StartsWith ("android-"))
            {
              installedNdkPlatforms.Add (uint.Parse (platformDirs [i].Substring ("android-".Length - 1)));
            }
          }
        }

        if (installedNdkPlatforms.Count == 0)
        {
          throw new InvalidOperationException ();
        }

        return installedNdkPlatforms;
      }
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
