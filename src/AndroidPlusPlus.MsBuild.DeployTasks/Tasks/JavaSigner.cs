﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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

using Microsoft.Build.Framework;
using Microsoft.Win32;
using Microsoft.Build.Utilities;

using AndroidPlusPlus.MsBuild.Common;
using AndroidPlusPlus.Common;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.MSBuild.DeployTasks
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class JavaSigner : TrackedToolTask, ITask
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public JavaSigner ()
      : base (new ResourceManager ("AndroidPlusPlus.MsBuild.DeployTasks.Properties.Resources", Assembly.GetExecutingAssembly ()))
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [Required]
    public ITaskItem SignedOutputFile { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override int TrackedExecuteTool (string pathToTool, string responseFileCommands, string commandLineCommands)
    {
      int retCode = -1;

      try
      {
        retCode = base.TrackedExecuteTool (pathToTool, responseFileCommands, commandLineCommands);

        if (retCode == 0)
        {
          // 
          // Construct a simple dependency file for tracking purposes.
          // 

          string signedOutputPath = SignedOutputFile.GetMetadata ("FullPath");

          using (StreamWriter writer = new StreamWriter (signedOutputPath + ".d", false, Encoding.Unicode))
          {
            writer.WriteLine (string.Format ("{0}: \\", GccUtilities.DependencyParser.ConvertPathWindowsToDependencyFormat (signedOutputPath)));

            foreach (ITaskItem source in Sources)
            {
              string sourcePath = source.GetMetadata ("FullPath");

              writer.WriteLine (string.Format ("  {0} \\", GccUtilities.DependencyParser.ConvertPathWindowsToDependencyFormat (sourcePath)));
            }
          }
        }
      }
      catch (Exception e)
      {
        Log.LogErrorFromException (e, true);

        retCode = -1;
      }

      return retCode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override string GenerateCommandLineCommands ()
    {
      // 
      // Build a command-line based on parsing switches from the registered property sheet, and any additional flags.
      // 

      StringBuilder builder = new StringBuilder (PathUtils.CommandLineLength);

      builder.Append (m_parsedProperties.Parse (Sources [0]));

      return builder.ToString ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override bool ValidateParameters ()
    {
      // 
      // Check for existence of a valid keystore file before attempting signing.
      // 

      string keystorePath = Sources [0].GetMetadata ("Keystore");

      if (string.IsNullOrWhiteSpace (keystorePath) || !File.Exists (keystorePath))
      {
        Log.LogError (string.Format ("Could not find specified .keystore file. Expected: {0}", keystorePath), MessageImportance.High);

        return false;
      }

      // 
      // This tool expects a single ZIP-compatible archive as input.
      // 

      if (Sources.Length == 0)
      {
        Log.LogError ("No inputs specified - Please provide a single ZIP-compatible archive.");

        return false;
      }
      else if (Sources.Length > 1)
      {
        Log.LogError ("Multiple inputs specified - Please provide a single ZIP-compatible archive.");

        return false;
      }
      else
      {
        string sourcePath = Sources [0].GetMetadata ("FullPath");

        string sourceExtension = Path.GetExtension (sourcePath);

        switch (sourceExtension)
        {
          case ".jar":
          case ".apk":
          case ".zip":
          {
            break;
          }

          default:
          {
            Log.LogError (string.Format ("{0} is not a ZIP-compatible archive. Must be .jar, .apk, or .zip.", sourcePath));

            return false;
          }
        }
      }

      return base.ValidateParameters ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void AddTaskSpecificOutputFiles (ref TrackedFileManager trackedFileManager, ITaskItem [] sources)
    {
      trackedFileManager.AddDependencyForSources (new ITaskItem [] { SignedOutputFile }, sources);
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

    protected override string ToolName
    {
      get
      {
        return "JavaSigner";
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