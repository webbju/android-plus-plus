////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.IO;
using System.Reflection;
using System.Resources;

using Microsoft.Build.Framework;
using Microsoft.Win32;
using Microsoft.Build.Utilities;

using AndroidPlusPlus.MsBuild.Common;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.CppTasks
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class GccLink : TrackedOutOfDateToolTask, ITask
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public GccLink ()
      : base (new ResourceManager ("AndroidPlusPlus.MsBuild.CppTasks.Properties.Resources", Assembly.GetExecutingAssembly ()))
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override int TrackedExecuteTool (string pathToTool, string responseFileCommands, string commandLineCommands)
    {
      int retCode = base.TrackedExecuteTool (pathToTool, responseFileCommands, commandLineCommands);

      if (retCode == 0)
      {
        // 
        // Construct a simple dependency file for tracking purposes.
        // 

        try
        {
          List<ITaskItem> resolvedLibraryItems = new List<ITaskItem> ();

          List<ITaskItem> resolvedWholeLibraryItems = new List<ITaskItem> ();

          if (!GetSourceResolvedLibraryDependencies (Sources [0], ref resolvedLibraryItems, ref resolvedWholeLibraryItems))
          {
            throw new InvalidOperationException ("Failed evaluating static library dependencies.");
          }

          foreach (ITaskItem outputFile in OutputFiles)
          {
            string outputDependencyFile = outputFile.GetMetadata ("FullPath") + ".d";

            using (StreamWriter writer = new StreamWriter (outputDependencyFile, false, Encoding.Unicode))
            {
              writer.WriteLine (string.Format ("{0}: \\", GccUtilities.DependencyParser.ConvertPathWindowsToDependencyFormat (outputFile.GetMetadata ("FullPath"))));

              foreach (ITaskItem source in Sources)
              {
                writer.WriteLine (string.Format ("  {0} \\", GccUtilities.DependencyParser.ConvertPathWindowsToDependencyFormat (source.GetMetadata ("FullPath"))));
              }

              foreach (ITaskItem source in resolvedLibraryItems)
              {
                writer.WriteLine (string.Format ("  {0} \\", GccUtilities.DependencyParser.ConvertPathWindowsToDependencyFormat (source.GetMetadata ("FullPath"))));
              }

              foreach (ITaskItem source in resolvedWholeLibraryItems)
              {
                writer.WriteLine (string.Format ("  {0} \\", GccUtilities.DependencyParser.ConvertPathWindowsToDependencyFormat (source.GetMetadata ("FullPath"))));
              }
            }
          }
        }
        catch (Exception e)
        {
          Log.LogErrorFromException (e, true);

          retCode = -1;
        }
      }

      return retCode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override string GenerateResponseFileCommands ()
    {
      // 
      // In *theory* linker settings are the same for all sources, so just query them from the first file.
      // 

      StringBuilder responseFileCommands = new StringBuilder (PathUtils.CommandLineLength);

      StringBuilder sourceLibraryDependencies = new StringBuilder ();

      // 
      // We require GCC to behave more like Visual Studio in terms of library dependencies.
      // - Here we filter object file and specified libraries so that they are contained with in -Wl,--start-group/--end-group
      // - This tends to fix cyclic dependencies as undefined symbols are continually re-evaluated for each library in turn.
      // - Also accommodate use of -Wl,--whole-archive/-Wl,--no-whole-archive to ensure these are batched properly.
      // 

      try
      {
#if true
        string derivedSourceProperties = m_parsedProperties.Parse (Sources [0]);

        string [] responseFileArguments = derivedSourceProperties.Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string arg in responseFileArguments)
        {
          if (arg.StartsWith ("-l"))
          {
            sourceLibraryDependencies.Append (arg + " ");
          }
          else if (arg.EndsWith (".a"))
          {
            sourceLibraryDependencies.Append (arg + " ");
          }
          else if (arg.Equals ("-Wl,--whole-archive") || arg.Equals ("-Wl,--no-whole-archive"))
          {
            sourceLibraryDependencies.Append (arg + " ");
          }
          else if (arg.Equals ("-Wl,--start-group") || arg.Equals ("-Wl,--end-group"))
          {
            // Skip these duplicate group being/end markers. We're grouping everything anyway.
          }
          else if (!arg.StartsWith ("-"))
          {
            if (Path.GetExtension (arg) == String.Empty)
            {
              // Support for libraries specified without -l (Win32 style); i.e. "SDL" -> "libSDL.a"

              sourceLibraryDependencies.Append ("-l" + arg + " ");
            }
            else
            {
              responseFileCommands.Append (arg + " ");
            }
          }
          else
          {
            responseFileCommands.Append (arg + " ");
          }
        }

        responseFileCommands.Append (" -Wl,--start-group ");

        foreach (ITaskItem source in Sources)
        {
          responseFileCommands.Append (PathUtils.SantiseWindowsPath (source.GetMetadata ("Identity")) + " ");
        }

        responseFileCommands.Append (sourceLibraryDependencies.ToString ());

        responseFileCommands.Append (" -Wl,--end-group ");
#else
        List<ITaskItem> resolvedLibraryItems = new List<ITaskItem> ();

        List<ITaskItem> resolvedWholeLibraryItems = new List<ITaskItem> ();

        if (!GetSourceResolvedLibraryDependencies (Sources [0], ref resolvedLibraryItems, ref resolvedWholeLibraryItems))
        {
          throw new InvalidOperationException ("Failed evaluating static library dependencies.");
        }

        responseFileCommands.Append (" -Wl,--start-group ");

        foreach (ITaskItem libraryItem in resolvedLibraryItems)
        {
          responseFileCommands.Append (PathUtils.SantiseWindowsPath (libraryItem.GetMetadata ("FullPath")) + " ");
        }

        responseFileCommands.Append (" -Wl,--whole-archive ");

        foreach (ITaskItem libraryItem in resolvedWholeLibraryItems)
        {
          responseFileCommands.Append (PathUtils.SantiseWindowsPath (libraryItem.GetMetadata ("FullPath")) + " ");
        }

        responseFileCommands.Append (" -Wl,--no-whole-archive ");

        foreach (ITaskItem source in Sources)
        {
          responseFileCommands.Append (PathUtils.SantiseWindowsPath (source.GetMetadata ("FullPath")) + " ");
        }

        responseFileCommands.Append (" -Wl,--end-group ");
#endif
      }
      catch (Exception e)
      {
        Log.LogErrorFromException (e, true);
      }

      return responseFileCommands.ToString ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override string GetResponseFileSwitch (string responseFilePath)
    {
      return '@' + PathUtils.SantiseWindowsPath (responseFilePath);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void TrackedExecuteToolOutput (KeyValuePair<string, List<ITaskItem>> commandAndSourceFiles, string singleLine)
    {
      if (ToolExe.StartsWith ("clang"))
      {
        LogEventsFromTextOutput (singleLine, MessageImportance.High);
      }
      else
      {
        // 
        // GCC output differs from a Visual Studio's "jump to line" format, we transform that output here.
        // 

        LogEventsFromTextOutput (GccUtilities.ConvertGccOutputToVS (singleLine), MessageImportance.High);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private bool GetSourceResolvedLibraryDependencies (ITaskItem source, ref List <ITaskItem> resolvedLibraryItems, ref List <ITaskItem> resolvedWholeLibraryItems)
    {
      try
      {
        // 
        // Probe source command-line for referenced static libraries and linker directories.
        // 

        string derivedSourceProperties = m_parsedProperties.Parse (source);

        string [] responseFileArguments = derivedSourceProperties.Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        List<string> libraryDirectories = new List<string> ();

        List<string> libraryFiles = new List<string> ();

        HashSet<string> libraryWholeFiles = new HashSet<string> ();

        bool wholeLibraryGroup = false;

        for (int i = 0; i < responseFileArguments.Length; ++i)
        {
          string arg = responseFileArguments [i];

          if (arg.StartsWith ("-L"))
          {
            libraryDirectories.Add (arg.Substring (2));
          }
          else if (arg.StartsWith ("-l"))
          {
            // GCC also accepts definitions with whitespace; "-l <lib>"
            if (arg.Length == 2)
            {
              arg = responseFileArguments [++i];
            }

            if (wholeLibraryGroup)
            {
              libraryWholeFiles.Add ("lib" + arg.Substring (2));
            }

            libraryFiles.Add ("lib" + arg.Substring (2));
          }
          else if (arg.EndsWith (".a"))
          {
            // Support for libraries specified by filename (without path).

            if (wholeLibraryGroup)
            {
              libraryWholeFiles.Add (arg);
            }

            libraryFiles.Add (arg);
          }
          else if (arg.Equals ("-Wl,--whole-archive"))
          {
            wholeLibraryGroup = true;
          }
          else if (arg.Equals ("-Wl,--no-whole-archive"))
          {
            wholeLibraryGroup = false;
          }
          else if (!arg.StartsWith ("-"))
          {
            // Support for libraries specified without -l (Win32 style); i.e. "SDL" -> "libSDL.a"

            if (Path.GetExtension (arg) == String.Empty)
            {
              if (wholeLibraryGroup)
              {
                libraryWholeFiles.Add ("lib" + arg);
              }

              libraryFiles.Add ("lib" + arg);
            }
          }
        }

        // 
        // Iterate registered linker directories searching for a matching absolute path.
        // 

        for (int i = 0; i < libraryFiles.Count; ++i)
        {
          string libraryPath = libraryFiles [i];

          bool wholeLibrary = libraryWholeFiles.Contains (libraryPath);

          bool absoluteLibraryPath = Path.IsPathRooted (libraryPath);

          if (!absoluteLibraryPath)
          {
            List<string> librarySearchPatterns = new List<string> ();

            if (Path.GetExtension (libraryPath) == String.Empty)
            {
              librarySearchPatterns.Add (libraryPath + ".a");

              librarySearchPatterns.Add (libraryPath + ".so");
            }
            else
            {
              librarySearchPatterns.Add (libraryPath);
            }

            foreach (string pattern in librarySearchPatterns)
            {
              foreach (string dir in libraryDirectories)
              {
                if (File.Exists (Path.Combine (dir, pattern)))
                {
                  libraryPath = Path.Combine (dir, pattern);

                  absoluteLibraryPath = true;

                  break;
                }
                else
                {
                  string evaluatedFullPath = Path.GetFullPath (pattern);

                  if (File.Exists (evaluatedFullPath))
                  {
                    libraryPath = evaluatedFullPath;

                    absoluteLibraryPath = true;

                    break;
                  }
                }
              }

              if (absoluteLibraryPath)
              {
                break;
              }
            }
          }

          if (!absoluteLibraryPath)
          {
            Log.LogError ("Could not evaluate absolute path for library: " + libraryPath);
          }

          if (wholeLibrary)
          {
            resolvedWholeLibraryItems.Add (new TaskItem (libraryPath));
          }
          else
          {
            resolvedLibraryItems.Add (new TaskItem (libraryPath));
          }
        }

        return true;
      }
      catch (Exception e)
      {
        Log.LogErrorFromException (e, true);
      }

      return false;
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
        return "GccLink";
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
