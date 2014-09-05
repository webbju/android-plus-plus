////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.Common
{
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class AndroidDevice
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private Hashtable m_deviceProperties = new Hashtable ();

    private Dictionary<string, List<uint>> m_deviceProcessesPidsByName = new Dictionary<string, List<uint>> ();

    private Dictionary<uint, AndroidProcess> m_deviceProcessesByPid = new Dictionary<uint, AndroidProcess> ();

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidDevice (string deviceId)
    {
      ID = deviceId;

      PopulateProperties ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool IsOverWiFi
    {
      get
      {
        return ID.Contains (".");
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Refresh ()
    {
      LoggingUtils.PrintFunction ();

      PopulateProcesses ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string GetProperty (string property)
    {
      return (string)m_deviceProperties [property];
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidProcess GetProcessFromPid (uint processId)
    {
      AndroidProcess process;

      if (m_deviceProcessesByPid.TryGetValue (processId, out process))
      {
        return process;
      }

      return null;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidProcess [] GetProcessesFromName (string processName)
    {
      List <uint> processPidList;

      List<AndroidProcess> processList = new List<AndroidProcess> ();

      if (m_deviceProcessesPidsByName.TryGetValue (processName, out processPidList))
      {
        foreach (uint pid in processPidList)
        {
          processList.Add (GetProcessFromPid (pid));
        }
      }

      return processList.ToArray ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidProcess [] GetAllProcesses ()
    {
      AndroidProcess [] processes = new AndroidProcess [m_deviceProcessesByPid.Count];

      m_deviceProcessesByPid.Values.CopyTo (processes, 0);

      return processes;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string Shell (string command, string arguments, int timeout = 30000)
    {
      using (SyncRedirectProcess adbShellCommand = AndroidAdb.AdbCommand (this, "shell", string.Format ("{0} {1}", command, arguments)))
      {
        int exitCode = -1;
        
        try
        {
          exitCode = adbShellCommand.StartAndWaitForExit (timeout);

          if (exitCode != 0)
          {
            throw new InvalidOperationException ("Shell request failed: " + adbShellCommand.StandardError);
          }

          LoggingUtils.Print ("[AndroidDevice] Shell: " + command + " (" + arguments + "): " + adbShellCommand.StandardOutput);
        }
        catch (TimeoutException e)
        {
          LoggingUtils.HandleException (e);

          throw new InvalidOperationException ("Shell request failed: Timed out.");
        }

        return adbShellCommand.StandardOutput;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string Pull (string remotePath, string localPath)
    {
      using (SyncRedirectProcess adbPullCommand = AndroidAdb.AdbCommand (this, "pull", string.Format ("{0} {1}", remotePath, PathUtils.SantiseWindowsPath (localPath))))
      {
        int exitCode = -1;

        try
        {
          exitCode = adbPullCommand.StartAndWaitForExit ();

          if (exitCode != 0)
          {
            throw new InvalidOperationException ("Pull request failed: " + adbPullCommand.StandardError);
          }

          LoggingUtils.Print (string.Format ("[AndroidDevice] Pull: '{0}' => '{1}' - {2}", remotePath, localPath, adbPullCommand.StandardOutput));
        }
        catch (TimeoutException e)
        {
          LoggingUtils.HandleException (e);

          throw new InvalidOperationException ("Pull request failed: Timed out.");
        }

        return adbPullCommand.StandardOutput;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Install (string filename, bool reinstall, string installer)
    {
      // 
      // Install applications to internal storage (-l). Apps in /mnt/asec/ and other locations cause 'run-as' to fail regarding permissions.
      // 

      LoggingUtils.PrintFunction ();

      int exitCode = -1;

      string temporaryStorage = "/data/local/tmp";

      string temporaryStoredFile = temporaryStorage + "/" + Path.GetFileName (filename);

      try
      {
        // 
        // Push APK to temporary device/emulator storage.
        // 

        using (SyncRedirectProcess adbPushToTemporaryCommand = AndroidAdb.AdbCommand (this, "push", string.Format ("{0} {1}", PathUtils.SantiseWindowsPath (filename), temporaryStorage)))
        {
          try
          {
            exitCode = adbPushToTemporaryCommand.StartAndWaitForExit ();
          }
          catch (TimeoutException)
          {
            throw new InvalidOperationException ("Installation failed: Transfer timed out.");
          }

          if (exitCode != 0)
          {
            throw new InvalidOperationException ("Installation failed: Could not transfer to temporary storage.");
          }
        }

        // 
        // Run a custom (package managed based) install request, bypassing /mnt/app-asec/ install locations.
        // 

        StringBuilder installArgumentsBuilder = new StringBuilder ("pm install ");

        installArgumentsBuilder.Append ("-f "); // install package on internal flash. (required for debugging)

        if (reinstall)
        {
          installArgumentsBuilder.Append ("-r "); // reinstall an existing app, keeping its data.
        }

        if (!string.IsNullOrWhiteSpace (installer))
        {
          installArgumentsBuilder.Append (string.Format ("-i {0} ", installer));
        }

        installArgumentsBuilder.Append (temporaryStoredFile);

        using (SyncRedirectProcess adbInstallCommand = AndroidAdb.AdbCommand (this, "shell", installArgumentsBuilder.ToString ()))
        {
          try
          {
            exitCode = adbInstallCommand.StartAndWaitForExit ();
          }
          catch (TimeoutException)
          {
            throw new InvalidOperationException ("Installation failed: Install timed out.");
          }

          if ((exitCode != 0) || !adbInstallCommand.StandardOutput.ToLower ().Contains ("success"))
          {
            throw new InvalidOperationException ("Installation failed: " + adbInstallCommand.StandardOutput);
          }
        }

        LoggingUtils.Print ("[AndroidDevice] " + filename + " installed successfully.");

        return;
      }
      catch (Exception e)
      {
        LoggingUtils.Print ("[AndroidDevice] " + filename + " installation failed.");

        LoggingUtils.HandleException (e);

        throw;
      }
      finally
      {

        // 
        // Clear uploaded APK from temporary storage.
        // 

        using (SyncRedirectProcess adbClearTemporary = AndroidAdb.AdbCommand (this, "shell", "rm " + temporaryStoredFile))
        {
          try
          {
            exitCode = adbClearTemporary.StartAndWaitForExit ();
          }
          catch (TimeoutException)
          {
            throw new InvalidOperationException ("Installation failed: Clearing cache timed out.");
          }

          if (exitCode != 0)
          {
            throw new InvalidOperationException ("Installation failed: Failed clearing cached files.");
          }
        }

        LoggingUtils.Print ("[AndroidDevice] Removed temporary file: " + temporaryStoredFile);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Uninstall (string package, bool keepCache)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        using (SyncRedirectProcess adbUninstallCommand = AndroidAdb.AdbCommand (this, "install", ((keepCache) ? "-k " : "") + package))
        {
          int exitCode = -1;

          try
          {
            exitCode = adbUninstallCommand.StartAndWaitForExit (30000);
          }
          catch (TimeoutException)
          {
            throw new InvalidOperationException ("Uninstall failed: Clearing app timed out.");
          }

          if ((exitCode != 0) || adbUninstallCommand.StandardOutput.ToLower ().Contains ("success"))
          {
            throw new InvalidOperationException ("Uninstall failed: " + adbUninstallCommand.StandardOutput);
          }

          LoggingUtils.Print ("[AndroidDevice] " + package + " uninstalled successfully.");

          return;
        }
      }
      catch (Exception e)
      {
        LoggingUtils.Print ("[AndroidDevice] " + package + " uninstall failed.");

        LoggingUtils.HandleException (e);

        throw;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AsyncRedirectProcess Logcat (AsyncRedirectProcess.EventListener listener, bool clearCache)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        if (listener == null)
        {
          throw new ArgumentNullException ("listener");
        }

        if (clearCache)
        {
          using (SyncRedirectProcess adbLogcatClearCommand = AndroidAdb.AdbCommand ("logcat", "-c"))
          {
            adbLogcatClearCommand.StartAndWaitForExit ();
          }
        }

        AsyncRedirectProcess adbLogcatCommand = AndroidAdb.AdbCommandAsync (this, "logcat", "");

        adbLogcatCommand.Start (listener);

        return adbLogcatCommand;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }

      return null;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void PopulateProperties ()
    {
      LoggingUtils.PrintFunction ();

      string getPropOutput = Shell ("getprop", "");

      if (!String.IsNullOrEmpty (getPropOutput))
      {
        string pattern = @"^\[(?<key>[^\]:]+)\]:[ ]+\[(?<value>[^\]$]+)";

        Regex regExMatcher = new Regex (pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        string [] properties = getPropOutput.Replace ("\r", "").Split (new char [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < properties.Length; ++i)
        {
          if (!properties [i].StartsWith ("["))
          {
            continue; // early rejection.
          }

          string unescapedStream = Regex.Unescape (properties [i]);

          Match regExLineMatch = regExMatcher.Match (unescapedStream);

          if (regExLineMatch.Success)
          {
            string key = regExLineMatch.Result ("${key}");

            string value = regExLineMatch.Result ("${value}");

            if (!string.IsNullOrWhiteSpace (key))
            {
              m_deviceProperties [key] = value;
            }
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void PopulateProcesses ()
    {
      // 
      // Skip the first line, and read in tab-seperated process data.
      // 

      LoggingUtils.PrintFunction ();

      string deviceProcessList = Shell ("ps", "");

      if (!String.IsNullOrEmpty (deviceProcessList))
      {
        string [] processesOutputLines = deviceProcessList.Replace ("\r", "").Split (new char [] { '\n' });

        string processesRegExPattern = @"(?<user>[^ ]+)[ ]*(?<pid>[0-9]+)[ ]*(?<ppid>[0-9]+)[ ]*(?<vsize>[0-9]+)[ ]*(?<rss>[0-9]+)[ ]*(?<wchan>[A-Za-z0-9]+)[ ]*(?<pc>[A-Za-z0-9]+)[ ]*(?<s>[^ ]+)[ ]*(?<name>[^\r\n]+)";

        Regex regExMatcher = new Regex (processesRegExPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        m_deviceProcessesByPid.Clear ();

        m_deviceProcessesPidsByName.Clear ();

        for (uint i = 1; i < processesOutputLines.Length; ++i)
        {
          if (!String.IsNullOrEmpty (processesOutputLines [i]))
          {
            Match regExLineMatches = regExMatcher.Match (processesOutputLines [i]);

            string processUser = regExLineMatches.Result ("${user}");

            uint processPid = uint.Parse (regExLineMatches.Result ("${pid}"));

            uint processPpid = uint.Parse (regExLineMatches.Result ("${ppid}"));

            uint processVsize = uint.Parse (regExLineMatches.Result ("${vsize}"));

            uint processRss = uint.Parse (regExLineMatches.Result ("${rss}"));

            uint processWchan = Convert.ToUInt32 (regExLineMatches.Result ("${wchan}"), 16);

            uint processPc = Convert.ToUInt32 (regExLineMatches.Result ("${pc}"), 16);

            string processPcS = regExLineMatches.Result ("${s}");

            string processName = regExLineMatches.Result ("${name}");

            m_deviceProcessesByPid [processPid] = new AndroidProcess (this, processName, processPid, processPpid, processUser);

            List<uint> processPidsList;

            if (!m_deviceProcessesPidsByName.TryGetValue (processName, out processPidsList))
            {
              processPidsList = new List<uint> ();
            }

            processPidsList.Add (processPid);

            m_deviceProcessesPidsByName [processName] = processPidsList;
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string ID { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidSettings.VersionCode SdkVersion 
    {
      get
      {
        // 
        // Query device's current SDK level. If it's not an integer (like some custom ROMs) fall-back to ICS.
        // 

        try
        {
          int sdkLevel = int.Parse (GetProperty ("ro.build.version.sdk"));

          return (AndroidSettings.VersionCode) sdkLevel;
        }
        catch (Exception e)
        {
          LoggingUtils.HandleException (e);

          return AndroidSettings.VersionCode.GINGERBREAD;
        }
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
