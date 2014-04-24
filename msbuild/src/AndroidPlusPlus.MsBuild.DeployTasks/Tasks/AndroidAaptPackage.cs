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

using AndroidPlusPlus.MsBuild.Common;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.DeployTasks
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class AndroidAaptPackage : TrackedOutOfDateToolTask, ITask
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidAaptPackage ()
      : base (new ResourceManager ("AndroidPlusPlus.MsBuild.DeployTasks.Properties.Resources", Assembly.GetExecutingAssembly ()))
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

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

            if (retCode == 0)
            {
              AndroidManifestDocument sourceManifest = new AndroidManifestDocument ();

              sourceManifest.Load (sourcePath);

              // 
              // Determine if this manifest requested APK generation, add to output list.
              // 

              if (!string.IsNullOrWhiteSpace (source.GetMetadata ("ApkOutputFile")))
              {
                string apkOutputFile = source.GetMetadata ("ApkOutputFile");

                if (!File.Exists (apkOutputFile))
                {
                  throw new FileNotFoundException ("Requested APK output file does not exist. Error during packging?");
                }

                ITaskItem outputApkItem = new TaskItem (Path.GetFullPath (apkOutputFile));

                outputApkFileList.Add (outputApkItem);

                outputFilesList.Add (outputApkItem);
              }


              if (source.GetMetadata ("GenerateDependencies") == "true")
              {
                // 
                // Evaluate which resource constant source files have been exported. 'ExtraPackages' lists additional package names that also have resource ids.
                // 

                List<string> resourceConstantSourceFiles = new List<string> ();

                string resourcesDirectory = source.GetMetadata ("ResourceConstantsOutputDirectory");

                if (File.Exists (Path.Combine (resourcesDirectory, "R.java")))
                {
                  resourceConstantSourceFiles.Add (Path.Combine (resourcesDirectory, "R.java"));
                }

                if (File.Exists (Path.Combine (resourcesDirectory, sourceManifest.PackageName.Replace ('.', '\\'), "R.java")))
                {
                  resourceConstantSourceFiles.Add (Path.Combine (resourcesDirectory, sourceManifest.PackageName.Replace ('.', '\\'), "R.java"));
                }

                if (!string.IsNullOrWhiteSpace (source.GetMetadata ("ExtraPackages")))
                {
                  string [] extraPackages = source.GetMetadata ("ExtraPackages").Split (':');

                  foreach (string package in extraPackages)
                  {
                    if (File.Exists (Path.Combine (resourcesDirectory, package.Replace ('.', '\\'), "R.java")))
                    {
                      resourceConstantSourceFiles.Add (Path.Combine (resourcesDirectory, package.Replace ('.', '\\'), "R.java"));
                    }
                  }
                }

                // 
                // When exporting an APK the R.java.d file will not properly list dependencies. Copy from ??.apk.d to solve this.
                // 

                string dependencyFileSource = Path.Combine (resourcesDirectory, "R.java.d");

                if (!string.IsNullOrWhiteSpace (source.GetMetadata ("ApkOutputFile")))
                {
                  string apkDependencyFileSource = source.GetMetadata ("ApkOutputFile") + ".d";

                  File.Copy (apkDependencyFileSource, dependencyFileSource, true); // overwrite invalid R.java.d export
                }

                // 
                // R.java.d dependency files are not placed alongside its master file if exported using package directories. Fix this.
                // 

                foreach (string master in resourceConstantSourceFiles)
                {
                  string masterDependencyFile = master + ".d";

                  Directory.CreateDirectory (Path.GetDirectoryName (masterDependencyFile));

                  if (File.Exists (masterDependencyFile))
                  {
                    File.Delete (masterDependencyFile);
                  }

                  File.Copy (dependencyFileSource, masterDependencyFile);
                }

                // 
                // Finally ensure all resource constant export files are added to the output list.
                // 

                foreach (string file in resourceConstantSourceFiles)
                {
                  ITaskItem outputResourceFileItem = new TaskItem (Path.GetFullPath (file));

                  outputFilesList.Add (outputResourceFileItem);
                }
              }

              if (retCode != 0)
              {
                break;
              }
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
        OutputApk = outputApkFileList.ToArray ();

        OutputFiles = outputFilesList.ToArray ();
      }

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

        builder.Append ("package ");

        builder.Append (" -M " + GccUtilities.QuoteIfNeeded (source.GetMetadata ("FullPath")) + " ");

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
            AndroidManifestDocument sourceManifest = new AndroidManifestDocument ();

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
