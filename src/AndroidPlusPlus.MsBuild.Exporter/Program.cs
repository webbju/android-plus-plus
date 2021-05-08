////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using CommandLine;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.Exporter {

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  class Program
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    class Options
    {
      [Option("uninstall", Default = false)]
      public bool Uninstall { get; set; }

      [Option("kill-msbuild", Default = false)]
      public bool KillMsBuild { get; set; }

      [Option("copy-dlls", Default = true)]
      public bool CopyDlls { get; set; }

      [Option("template-dir", Required = true)]
      public IEnumerable<string> TemplateDirs { get; set; }

      [Option("export-dir", Required = true)]
      public IEnumerable<string> ExportDirs { get; set; }

      [Option("version-file")]
      public string VersionFile { get; set; }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    static int Main (string [] args)
    {
      Log.Logger = new LoggerConfiguration()
        .WriteTo.Console(Serilog.Events.LogEventLevel.Verbose)
        .WriteTo.Debug(Serilog.Events.LogEventLevel.Verbose)
        .CreateLogger();

      Parser.Default.ParseArguments<Options>(args)
        .WithParsed(RunOptions);

      return 0;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    static void RunOptions (Options options)
    {
      try
      {
        if (options.KillMsBuild)
        {
          KillMsBuildInstances ();
        }

        var textSubstitution = new Dictionary<string, string>
        {
          { "{master}", "Android++" },
          { "{master-version}", "1.0" },
          { "{master-verbose}", "AndroidPlusPlus" }
        };

        foreach (var exportDir in options.ExportDirs)
        {
          string canonicalExportDir = Path.GetFullPath(exportDir);

          UninstallMsBuildTemplates (canonicalExportDir, ref textSubstitution);
        }

        if (!options.Uninstall)
        {
          foreach (var exportDir in options.ExportDirs)
          {
            string canonicalExportDir = Path.GetFullPath(exportDir);

            ExportMsBuildTemplateForVersion (options, canonicalExportDir, ref textSubstitution);
          }
        }
      }
      catch (Exception e)
      {
        Log.Error(e, "");
      }
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
            using Process taskkill = Process.Start("taskkill", "/pid " + process.Id + " /f");

            taskkill.WaitForExit();

            killed = taskkill.ExitCode == 0;
          }
          finally
          {
            if (!killed)
            {
              Log.Information (string.Format ("[AndroidPlusPlus.MsBuild.Exporter] Couldn't kill a running MSBuild instance."));
            }
          }
        }
      }
      catch (Exception e)
      {
        Log.Error(e, "Failed to terminate running MSBuild instance(s).");
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void UninstallMsBuildTemplates (string exportDir, ref Dictionary<string, string> textSubstitution)
    {
      //
      // Clean 'Application Type' files and sub-directories.
      //

      Log.Information(string.Format ("[AndroidPlusPlus.MsBuild.Exporter] Uninstalling scripts from {0}", exportDir));

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

      //
      // Clean 'BuildCustomizations' files and sub-directories.
      //


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

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void ExportMsBuildTemplateForVersion (Options options, string exportDir, ref Dictionary<string, string> textSubstitution)
    {
      //
      // Copy each directory of the template directories and apply pattern processing.
      //

      foreach (string templateDir in options.TemplateDirs)
      {
        string canonicalTemplateDir = Path.GetFullPath(templateDir);

        Log.Information(string.Format ("[AndroidPlusPlus.MsBuild.Exporter] Copying {0} to {1}", canonicalTemplateDir, exportDir));

        CopyFoldersAndFiles (canonicalTemplateDir, exportDir, true, ref textSubstitution);
      }

      //
      // Copy assemblies.
      //

      foreach (string templateDir in options.TemplateDirs)
      {
        var assemblies = Directory.GetFiles(".", "*.dll");

        foreach (var assembly in assemblies)
        {
          if (Path.GetFileName(assembly).StartsWith("System."))
          {
            continue;
          }

          string canonicalAssemblyFile = Path.GetFullPath(assembly);

          string destinationAssemblyFile = Path.Combine(exportDir, "Application Type", textSubstitution["{master}"], textSubstitution ["{master-version}"], "Toolchain", "bin", Path.GetFileName(canonicalAssemblyFile));

          Log.Information(string.Format("[AndroidPlusPlus.MsBuild.Exporter] Copying {0} to {1}", canonicalAssemblyFile, destinationAssemblyFile));

          File.Copy(canonicalAssemblyFile, destinationAssemblyFile, true);
        }
      }

      //
      // Copy specified version descriptor file to the root of 'Platforms'. Useful for tracking install versions.
      //

      if (!string.IsNullOrEmpty (options.VersionFile))
      {
        string canonicalVersionFile = Path.GetFullPath(options.VersionFile);

        string destinationVersionFile = Path.Combine (exportDir, "Application Type", textSubstitution ["{master}"], textSubstitution ["{master-version}"], Path.GetFileName (canonicalVersionFile));

        Log.Information(string.Format("[AndroidPlusPlus.MsBuild.Exporter] Copying {0} to {1}", canonicalVersionFile, destinationVersionFile));

        File.Copy (canonicalVersionFile, destinationVersionFile, true);
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
            throw new InvalidOperationException ("File validation failed: " + newFileName, e);
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
