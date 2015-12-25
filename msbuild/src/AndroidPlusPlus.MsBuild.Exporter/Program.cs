////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.Exporter
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  class Program
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static bool s_uninstall = false;

    private static bool s_killMsBuildInstances = false;

    private static HashSet<string> s_templateDirs = new HashSet<string> ();

    private static string s_versionDescriptorFile = string.Empty;

    private static Dictionary<string, string> s_vsVersionMsBuildDirs = new Dictionary<string, string> ();

    private static HashSet<string> s_exportDirectories = new HashSet<string> ();

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    static int Main (string [] args)
    {
      try
      {
        ProcessArguments (args);

        if (s_killMsBuildInstances)
        {
          KillMsBuildInstances ();
        }

        Dictionary<string, string> textSubstitution = new Dictionary<string, string> ();

        textSubstitution.Add ("{master}", "Android++");

        textSubstitution.Add ("{master-verbose}", "AndroidPlusPlus");

        // 
        // Decide whether to validate MSBuild installed directories if the user specified particular VS version(s).
        // 

        bool validateMsBuildInstallations = true;

        if (s_vsVersionMsBuildDirs.ContainsKey ("all"))
        {
          validateMsBuildInstallations = false;
        }

        // 
        // Accumulate additional export locations for each requested MSBuild directory/version.
        // 

        foreach (KeyValuePair<string, string> keyPair in s_vsVersionMsBuildDirs)
        {
          s_exportDirectories.Clear ();

          string version = keyPair.Key;

          string dir = keyPair.Value;

          if (Directory.Exists (dir))
          {
            if (!s_exportDirectories.Contains (dir))
            {
              s_exportDirectories.Add (dir);
            }
          }
          else if (validateMsBuildInstallations)
          {
            throw new DirectoryNotFoundException (string.Format ("Could not locate required MSBuild platforms directory. This should have been installed with Visual Studio {0}. Tried: {1}", version, dir));
          }

          // 
          // Install/Uninstall scripts for each specified VS version.
          // 

          UninstallMsBuildTemplates (version, ref textSubstitution, ref s_exportDirectories);

          if (!s_uninstall)
          {
            ExportMsBuildTemplateForVersion (version, ref textSubstitution, ref s_exportDirectories, ref s_templateDirs);
          }
        }
      }
      catch (Exception e)
      {
        string exception = string.Format ("[AndroidPlusPlus.MsBuild.Exporter] {0}: {1}\nStack trace:\n{2}", e.GetType ().Name, e.Message, e.StackTrace);

        Console.WriteLine (exception);

        Trace.WriteLine (exception);

        PrintArgumentUsage ();

        return 1;
      }

      Console.WriteLine ("[AndroidPlusPlus.MsBuild.Exporter] Success.");

      Trace.WriteLine ("[AndroidPlusPlus.MsBuild.Exporter] Success.");

      return 0;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void ProcessArguments (string [] args)
    {
      if ((args == null) || (args.Length == 0))
      {
        throw new ArgumentException ("No arguments specified");
      }

      for (int i = 0; i < args.Length; ++i)
      {
        switch (args [i])
        {
          case "--uninstall":
          {
            s_uninstall = true;

            break;
          }

          case "--kill-msbuild":
          {
            s_killMsBuildInstances = true;

            break;
          }

          case "--template-dir":
          {
            string template = args [++i];

            if (!Directory.Exists (template))
            {
              throw new DirectoryNotFoundException ("--template-dir references non-existent directory. Tried: " + template);
            }

            s_templateDirs.Add (template);

            break;
          }

          case "--export-dir":
          {
            string exportDir = args [++i].Replace ("\"", "");

            if (!Directory.Exists (exportDir))
            {
              Directory.CreateDirectory (exportDir);
            }

            if (!s_exportDirectories.Contains (exportDir))
            {
              s_exportDirectories.Add (exportDir);
            }

            break;
          }

          case "--version-file":
          {
            string descriptorFile = args [++i];

            if (!File.Exists (descriptorFile))
            {
              throw new DirectoryNotFoundException ("--version-file references non-existent file. Tried: " + descriptorFile);
            }

            s_versionDescriptorFile = descriptorFile;

            break;
          }

          case "--vs-version":
          {
            string [] versions = args [++i].Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string version in versions)
            {
              switch (version)
              {
                case "2010":
                {
                  if (!s_vsVersionMsBuildDirs.ContainsKey ("2010"))
                  {
                    string dir = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles) + @"\MSBuild\Microsoft.Cpp\v4.0\";

                    if (!Directory.Exists (dir))
                    {
                      dir = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFilesX86) + @"\MSBuild\Microsoft.Cpp\v4.0\";
                    }

                    s_vsVersionMsBuildDirs.Add ("2010", dir);
                  }

                  break;
                }

                case "2012":
                {
                  if (!s_vsVersionMsBuildDirs.ContainsKey ("2012"))
                  {
                    string dir = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles) + @"\MSBuild\Microsoft.Cpp\v4.0\V110\";

                    if (!Directory.Exists (dir))
                    {
                      dir = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFilesX86) + @"\MSBuild\Microsoft.Cpp\v4.0\V110\";
                    }

                    s_vsVersionMsBuildDirs.Add ("2012", dir);
                  }

                  break;
                }

                case "2013":
                {
                  if (!s_vsVersionMsBuildDirs.ContainsKey ("2013"))
                  {
                    string dir = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles) + @"\MSBuild\Microsoft.Cpp\v4.0\V120\";

                    if (!Directory.Exists (dir))
                    {
                      dir = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFilesX86) + @"\MSBuild\Microsoft.Cpp\v4.0\V120\";
                    }

                    s_vsVersionMsBuildDirs.Add ("2013", dir);
                  }

                  break;
                }

                case "2015":
                {
                  if (!s_vsVersionMsBuildDirs.ContainsKey ("2015"))
                  {
                    string dir = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles) + @"\MSBuild\Microsoft.Cpp\v4.0\V140\";

                    if (!Directory.Exists (dir))
                    {
                      dir = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFilesX86) + @"\MSBuild\Microsoft.Cpp\v4.0\V140\";
                    }

                    s_vsVersionMsBuildDirs.Add ("2015", dir);
                  }

                  break;
                }

                case "all":
                {
                  ProcessArguments ("--vs-version 2010;2012;2013;2015".Split (' '));

                  break;
                }

                default:
                {
                  throw new ArgumentException ("--vs-version references invalid version. Tried: " + version);
                }
              }
            }

            break;
          }
        }
      }

      // 
      // Validate the tool executed with appropriate arguments.
      // 

      if (!s_uninstall)
      {
        if (s_templateDirs.Count () == 0)
        {
          throw new ArgumentException ("--template-dir not specified.");
        }

        if (s_templateDirs.Count () > 1)
        {
          throw new ArgumentException ("Please only specify a single target --template-dir.");
        }
      }

      if (s_vsVersionMsBuildDirs.Count () == 0)
      {
        throw new ArgumentException ("--vs-version not defined correctly. Expected: 'all', '2013' or '2015'.");
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void PrintArgumentUsage ()
    {
      // TODO
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void KillMsBuildInstances ()
    {
      try
      {
        Process [] activeMsBuildProcesses = Process.GetProcessesByName ("MSBuild");

        foreach (Process process in activeMsBuildProcesses)
        {
          bool killed = false;

          try
          {
            process.Kill ();

            killed = true;
          }
          catch (Exception)
          {
            using (Process taskkill = Process.Start ("taskkill", "/pid " + process.Id + " /f"))
            {
              taskkill.WaitForExit ();

              killed = (taskkill.ExitCode == 0);
            }
          }
          finally
          {
            if (!killed)
            {
              Console.WriteLine (string.Format ("[AndroidPlusPlus.MsBuild.Exporter] Couldn't kill a running MSBuild instance."));
            }
          }
        }
      }
      catch (Exception)
      {
        Console.WriteLine (string.Format ("[AndroidPlusPlus.MsBuild.Exporter] Failed to terminate running MSBuild instance(s)."));
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void UninstallMsBuildTemplates (string version, ref Dictionary<string, string> textSubstitution, ref HashSet <string> exportDirectories)
    {
      foreach (string exportDir in exportDirectories)
      {
        // 
        // Clean 'BuildCustomizations' files and sub-directories.
        // 

        Console.WriteLine (string.Format ("[AndroidPlusPlus.MsBuild.Exporter] Uninstalling scripts from {0}", exportDir));

        if (Directory.Exists (Path.Combine (exportDir, "Application Type")))
        {
          string [] installedCustomisationFiles = Directory.GetFiles (Path.Combine (exportDir, "Application Type"));

          string [] installedCustomisationDirectories = Directory.GetDirectories (Path.Combine (exportDir, "Application Type"));

          foreach (string file in installedCustomisationFiles)
          {
            if (file.Contains (textSubstitution ["{master}"]))
            {
              File.Delete (file);
            }
          }

          foreach (string directory in installedCustomisationDirectories)
          {
            if (directory.Contains (textSubstitution ["{master}"]))
            {
              Directory.Delete (directory, true);
            }
          }
        }

        if (Directory.Exists (Path.Combine (exportDir, "BuildCustomizations")))
        {
          string [] installedCustomisationFiles = Directory.GetFiles (Path.Combine (exportDir, "BuildCustomizations"));

          string [] installedCustomisationDirectories = Directory.GetDirectories (Path.Combine (exportDir, "BuildCustomizations"));

          foreach (string file in installedCustomisationFiles)
          {
            if (file.Contains (textSubstitution ["{master}"]))
            {
              File.Delete (file);
            }
          }

          foreach (string directory in installedCustomisationDirectories)
          {
            if (directory.Contains (textSubstitution ["{master}"]))
            {
              Directory.Delete (directory, true);
            }
          }
        }

        // 
        // Clean 'Platforms' files and sub-directories.
        // 

        if (Directory.Exists (Path.Combine (exportDir, "Platforms")))
        {
          string [] installedPlatformFiles = Directory.GetFiles (Path.Combine (exportDir, "Platforms"));

          string [] installedPlatformDirectories = Directory.GetDirectories (Path.Combine (exportDir, "Platforms"));

          foreach (string file in installedPlatformFiles)
          {
            if (file.Contains (textSubstitution ["{master}"]))
            {
              File.Delete (file);
            }
          }

          foreach (string directory in installedPlatformDirectories)
          {
            if (directory.Contains (textSubstitution ["{master}"]))
            {
              Directory.Delete (directory, true);
            }
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void ExportMsBuildTemplateForVersion (string version, ref Dictionary <string, string> textSubstitution, ref HashSet <string> exportDirectories, ref HashSet <string> templateDirectories)
    {
      foreach (string exportDir in exportDirectories)
      {
        // 
        // Copy each directory of the template directories and apply pattern processing.
        // 

        foreach (string templateDir in templateDirectories)
        {
          Console.WriteLine (string.Format ("[AndroidPlusPlus.MsBuild.Exporter] Copying {0} to {1}", templateDir, exportDir));

          CopyFoldersAndFiles (templateDir, exportDir, true, ref textSubstitution);
        }

        // 
        // Copy specified version descriptor file to the root of 'Platforms'. Useful for tracking install versions.
        // 

        if (!string.IsNullOrEmpty (s_versionDescriptorFile))
        {
          string destinationVersionFile = Path.Combine (exportDir, "Application Type", textSubstitution ["{master}"], Path.GetFileName (s_versionDescriptorFile));

          File.Copy (s_versionDescriptorFile, destinationVersionFile, true);
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void CopyFoldersAndFiles (string sourcePath, string destinationPath, bool recursive, ref Dictionary<string, string> textSub)
    {
      if (Directory.Exists (sourcePath))
      {
        Directory.CreateDirectory (ApplyTextSubstitution (destinationPath.Replace (sourcePath, destinationPath), ref textSub));

        foreach (string directory in Directory.GetDirectories (sourcePath, "*", (recursive) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
        {
          Directory.CreateDirectory (ApplyTextSubstitution (directory.Replace (sourcePath, destinationPath), ref textSub));
        }

        foreach (string file in Directory.GetFiles (sourcePath, "*.*", (recursive) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
        {
          string newFileName = ApplyTextSubstitution (file.Replace (sourcePath, destinationPath), ref textSub);

          File.Copy (file, newFileName, true);

          // 
          // Process file contents with same text substitution settings too.
          // 

          switch (Path.GetExtension (newFileName))
          {
            default:
            {
              break;
            }

            case ".xml":
            case ".xaml":
            case ".props":
            case ".targets":
            {
              StringBuilder fileContents = new StringBuilder (File.ReadAllText (newFileName));

              foreach (KeyValuePair<string, string> keyPair in textSub)
              {
                fileContents.Replace (keyPair.Key, keyPair.Value);
              }

              File.WriteAllText (newFileName, fileContents.ToString ());

              break;
            }
          }

          // 
          // Validate the written file exists, and is proper.
          // 

          try
          {
            switch (Path.GetExtension (newFileName))
            {
              case ".xml":
              case ".xaml":
              case ".props":
              case ".targets":
              {
                XmlDocument xmlDocument = new XmlDocument ();

                xmlDocument.Load (newFileName);

                break;
              }
            }
          }
          catch (Exception e)
          {
            throw new InvalidOperationException ("File validation failed: " + newFileName + ". Exception: " + e.Message);
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static string ApplyTextSubstitution (string original, ref Dictionary<string, string> textSub)
    {
      StringBuilder substitutedString = new StringBuilder (original);

      foreach (KeyValuePair<string, string> keyPair in textSub)
      {
        substitutedString.Replace (keyPair.Key, keyPair.Value);
      }

      return substitutedString.ToString ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}
