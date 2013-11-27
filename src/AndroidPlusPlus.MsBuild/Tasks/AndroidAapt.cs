////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;

using Microsoft.Build.Framework;
using Microsoft.Win32;
using Microsoft.Build.Utilities;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class AndroidAapt : TrackedToolTask, ITask
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidAapt ()
      : base (new ResourceManager ("AndroidPlusPlus.MsBuild.Properties.Resources", Assembly.GetExecutingAssembly ()))
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [Required]
    public string ToolArgs { get; set; }

    [Output]
    public ITaskItem [] OutputApk { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override int TrackedExecuteTool (string pathToTool, string responseFileCommands, string commandLineCommands)
    {
      // 
      // Use default implementation, but ensure that any generated sources and dependency files get flagged as output appropriately.
      // 

      int retCode = -1;

      Dictionary<string, ITaskItem> processedManifestFiles = new Dictionary<string, ITaskItem> ();

      List<ITaskItem> outputApkFileList = new List<ITaskItem> ();

      List<ITaskItem> outputFilesList = new List<ITaskItem> ();

      try
      {
        foreach (ITaskItem source in Sources)
        {
          string sourcePath = Path.GetFullPath (source.GetMetadata ("FullPath"));

          if (!processedManifestFiles.ContainsKey (sourcePath))
          {
            retCode = base.TrackedExecuteTool (pathToTool, responseFileCommands, commandLineCommands);

            processedManifestFiles.Add (sourcePath, source);

            // 
            // R.java.d dependency files are not placed alongside its master file if exported using package directories. Fix this.
            // 

            if ((retCode == 0) && (source.GetMetadata ("GenerateDependencies") == "true") && (source.GetMetadata ("CreatePackageDirectoriesUnderOutput") == "true"))
            {
              AndroidManifest sourceManifest = new AndroidManifest ();

              sourceManifest.Load (sourcePath);

              string resourcesDirectory = source.GetMetadata ("ResourceConstantsOutputDirectory");

              string packageDirectory = sourceManifest.Package.Replace ('.', '\\');

              string dependencyFileSource = Path.Combine (resourcesDirectory, "R.java.d");

              string dependencyFileTarget = Path.Combine (resourcesDirectory, packageDirectory, "R.java.d");

              Directory.CreateDirectory (Path.Combine (resourcesDirectory, packageDirectory));

              if (File.Exists (dependencyFileTarget))
              {
                File.Delete (dependencyFileTarget);
              }

              File.Copy (dependencyFileSource, dependencyFileTarget);
            }

            if (retCode != 0)
            {
              break;
            }
          }
        }
      }
      catch (Exception e)
      {
        Log.LogErrorFromException (e, true);

        retCode = -1;
      }
      finally
      {
        if (retCode == 0)
        {
          // 
          // For each successfully processed set of AndroidManifest-based resources, build cumlative exported file listings.
          // 

          try
          {
            foreach (KeyValuePair<string, ITaskItem> processedManifestKeyPair in processedManifestFiles)
            {
              string intermediatePackageFile = processedManifestKeyPair.Value.GetMetadata ("ApkOutputFile");

              if (!string.IsNullOrWhiteSpace (intermediatePackageFile) && File.Exists (intermediatePackageFile))
              {
                ITaskItem outputApk = new TaskItem (Path.GetFullPath (intermediatePackageFile));

                outputApkFileList.Add (outputApk);

                outputFilesList.Add (outputApk);
              }

              AndroidManifest sourceManifest = new AndroidManifest ();

              sourceManifest.Load (processedManifestKeyPair.Key);

              if (sourceManifest != null)
              {
                string resourcesDirectory = processedManifestKeyPair.Value.GetMetadata ("ResourceConstantsOutputDirectory");

                string resourcesFile = Path.Combine (resourcesDirectory, "R.java");

                if (processedManifestKeyPair.Value.GetMetadata ("CreatePackageDirectoriesUnderOutput") == "true")
                {
                  string packageDirectory = sourceManifest.Package.Replace ('.', '\\');

                  resourcesFile = Path.Combine (resourcesDirectory, packageDirectory, "R.java");
                }

                if (!File.Exists (resourcesFile))
                {
                  throw new InvalidOperationException ("Expected 'R.java' output located: " + resourcesFile);
                }

                outputFilesList.Add (new TaskItem (resourcesFile));
              }
            }
          }
          catch (Exception e)
          {
            Log.LogErrorFromException (e, true);
          }
        }
      }

      OutputApk = outputApkFileList.ToArray ();

      OutputFiles = outputFilesList.ToArray ();

      return retCode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override string GenerateCommandLineFromProps (ITaskItem source)
    {
      // 
      // Build a commandline based on parsing switches from the registered property sheet, and any additional flags.
      // 

      StringBuilder builder = new StringBuilder (GccUtilities.CommandLineLength);

      try
      {
        if (source == null)
        {
          throw new ArgumentNullException ();
        }

        builder.Append (ToolArgs + " ");

        builder.Append (m_parsedProperties.Parse (source) + " ");
      }
      catch (Exception e)
      {
        Log.LogErrorFromException (e, true);
      }

      return builder.ToString ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override bool ValidateParameters ()
    {
      // 
      // This tool should really only expect 'AndroidManifest.xml' input. But people might change the names of these, so we can't rely on that to validate.
      // 

      bool validParameters = true;

      if (Sources.Length != 1)
      {
        Log.LogError ("Expected single input, got: " + Sources.Length + "'.", MessageImportance.High);

        validParameters = false;
      }

      if (validParameters)
      {
        string sourcePath = Path.GetFullPath (Sources [0].GetMetadata ("FullPath"));

        if (string.IsNullOrEmpty (sourcePath) || !File.Exists (sourcePath))
        {
          Log.LogError ("Could locate expected input: '" + sourcePath + "'.", MessageImportance.High);

          validParameters = false;
        }

        try
        {
          if (validParameters)
          {
            AndroidManifest sourceManifest = new AndroidManifest ();

            sourceManifest.Load (sourcePath);
          }
        }
        catch (Exception e)
        {
          Log.LogError ("Could not successfully parse '" + sourcePath + "'. Check this is a valid AndroidManifest.xml document. Reason: " + e, MessageImportance.High);

          validParameters = false;
        }
      }

      return (validParameters && base.ValidateParameters ());
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override string ToolName
    {
      get
      {
        return "AndroidAapt";
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override bool AppendSourcesToCommandLine
    {
      get
      {
        return false;
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
