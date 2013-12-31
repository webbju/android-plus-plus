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

using Microsoft.Build.Framework;
using Microsoft.Win32;
using Microsoft.Build.Utilities;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.Common
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public abstract class TrackedOutOfDateToolTask : TrackedToolTask
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public TrackedOutOfDateToolTask (ResourceManager taskResources)
      : base (taskResources)
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ITaskItem [] OutOfDateSources { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override bool Setup ()
    {
      bool result = base.Setup ();

      OutOfDateSources = Sources;

      if (result && !SkippedExecution)
      {
        // 
        // Retrieve list of sources considered out-of-date due to either command line changes or tracker flagging.
        // TODO: Switch use of CanonicalTracked* helpers to TrackedFileManager.
        // 

        CanonicalTrackedOutputFiles trackedOutputFiles = new CanonicalTrackedOutputFiles (this, TLogWriteFiles);

        TrackedInputFiles = new CanonicalTrackedInputFiles (this, TLogReadFiles, Sources, ExcludedInputPaths, trackedOutputFiles, true, false);

        ITaskItem [] outOfDateSourcesFromTracking = TrackedInputFiles.ComputeSourcesNeedingCompilation ();

        ITaskItem [] outOfDateSourcesFromCommandLine = GetOutOfDateSourcesFromCmdLineChanges ();

        // 
        // Merge out-of-date lists from both sources and assign these for compilation.
        // 

        List<ITaskItem> mergedOutOfDateSources = new List<ITaskItem> (outOfDateSourcesFromTracking);

        foreach (ITaskItem item in outOfDateSourcesFromCommandLine)
        {
          if (!mergedOutOfDateSources.Contains (item))
          {
            mergedOutOfDateSources.Add (item);
          }
        }

        OutOfDateSources = mergedOutOfDateSources.ToArray ();

        if ((OutOfDateSources == null) || (OutOfDateSources.Length == 0))
        {
          SkippedExecution = true;
        }
        else
        {
          // 
          // Remove sources to compile from tracked file list.
          // 

          TrackedInputFiles.RemoveEntriesForSource (OutOfDateSources);

          trackedOutputFiles.RemoveEntriesForSource (OutOfDateSources);

          TrackedInputFiles.SaveTlog ();

          trackedOutputFiles.SaveTlog ();
        }
      }

      return result;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override int ExecuteTool (string pathToTool, string responseFileCommands, string commandLineCommands)
    {
      int retCode = -1;

      try
      {
        // 
        // Construct list of target output files for all valid sources.
        // 

        List<ITaskItem> outputFiles = new List<ITaskItem> ();

        foreach (ITaskItem source in OutOfDateSources)
        {
          if (!string.IsNullOrWhiteSpace (source.GetMetadata ("OutputFile")))
          {
            outputFiles.Add (new TaskItem (Path.GetFullPath (source.GetMetadata ("OutputFile"))));
          }

          if (!string.IsNullOrWhiteSpace (source.GetMetadata ("OutputFiles")))
          {
            string [] files = source.GetMetadata ("OutputFiles").Split (';');

            foreach (string file in files)
            {
              outputFiles.Add (new TaskItem (Path.GetFullPath (file)));
            }
          }
        }

        OutputFiles = outputFiles.ToArray ();
      }
      catch (Exception e)
      {
        Log.LogErrorFromException (e, true);
      }

      try
      {
        m_commandBuffer = GenerateCommandLineBuffer (OutOfDateSources);

        retCode = TrackedExecuteTool (pathToTool, responseFileCommands, commandLineCommands);
      }
      catch (Exception e)
      {
        Log.LogErrorFromException (e, true);

        retCode = -1;
      }
      finally
      {
        OutputWriteTLog (m_commandBuffer, OutOfDateSources);

        OutputReadTLog (m_commandBuffer, OutOfDateSources);

        OutputCommandTLog (m_commandBuffer);
      }

      return retCode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected Dictionary<string, List <ITaskItem>> GenerateCommandLinesFromTlog ()
    {
      // 
      // Extract command-line attributes for each saved file in the TLog.
      // 

      Dictionary<string, List<ITaskItem>> commandLineSourceDictionary = new Dictionary<string, List<ITaskItem>> ();

      if (TLogCommandFile != null)
      {
        string commandFileFullPath = (!string.IsNullOrEmpty (TLogCommandFile.GetMetadata ("FullPath"))) ? TLogCommandFile.GetMetadata ("FullPath") : Path.GetFullPath (TLogCommandFile.ItemSpec);

        if (File.Exists (commandFileFullPath))
        {
          using (StreamReader reader = File.OpenText (commandFileFullPath))
          {
            // 
            // Construct a dictionary for each unique tracked command, with an associated list of paired files (separated by '|').
            // 

            for (string line = reader.ReadLine (); !string.IsNullOrEmpty (line); line = reader.ReadLine ())
            {
              if (line.StartsWith ("^"))
              {
                string [] trackedFiles = line.Substring (1).Split ('|');

                string command = reader.ReadLine ();

                List <ITaskItem> sourceList = new List<ITaskItem> ();

                foreach (string file in trackedFiles)
                {
                  sourceList.Add (new TaskItem (file));
                }

                commandLineSourceDictionary.Add (command, sourceList);
              }
            }

            reader.Close ();
          }
        }
      }

      return commandLineSourceDictionary;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected ITaskItem [] GetOutOfDateSourcesFromCmdLineChanges ()
    {
      // 
      // Evaluate a list of the currently saved commands, and check whether these are considered out-of-date.
      // 

      Dictionary<string, List<ITaskItem>> commandLineDictionary = GenerateCommandLinesFromTlog ();

      List<ITaskItem> outOfDateSources = new List<ITaskItem> ();

      foreach (ITaskItem source in Sources)
      {
        // 
        // Identify if the source's command line is different from that previously built. This may require iterating through mutliple sources per command.
        // 

        string sourcePath = source.GetMetadata ("FullPath").ToUpperInvariant ();

        string commandLine = GenerateCommandLineFromProps (source);

        List<ITaskItem> commandLineCachedSources = null;

        bool outOfDate = true;

        if (commandLineDictionary.TryGetValue (commandLine, out commandLineCachedSources))
        {
          foreach (ITaskItem cachedSource in commandLineCachedSources)
          {
            string cachedSourcePath = Path.GetFullPath (cachedSource.ItemSpec).ToUpperInvariant ();

            if (cachedSourcePath.Equals (sourcePath))
            {
              outOfDate = false;

              break;
            }
          }
        }

        if (outOfDate)
        {
          outOfDateSources.Add (source);
        }
      }

      return outOfDateSources.ToArray ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void OutputCommandTLog (Dictionary<string, List<ITaskItem>> commandDictionary)
    {
      // 
      // Output a tracking file for each of the commands used in the previous build, and target sources to which they relate.
      // 
      // *Keeps existing entries in the command log. Updates entries for sources which have been compiled/modified.*
      // 

      if (TLogCommandFile != null)
      {
        string commandFilePath = TLogCommandFile.GetMetadata ("FullPath");

        // 
        // Merge existing and new dictionaries. This is quite expensive, but means we can utilise a more simple base export implementation.
        // 

        List<ITaskItem> mergedSources = new List<ITaskItem> ();

        Dictionary<string, List<ITaskItem>> cachedCommandLogDictionary = GenerateCommandLinesFromTlog ();

        Dictionary<string, List<ITaskItem>> mergedCommandLogDictionary = new Dictionary<string, List<ITaskItem>> ();

        // 
        // Add recently changed sources first, ensuring these take precedence.
        // 

        foreach (KeyValuePair<string, List<ITaskItem>> entry in commandDictionary)
        {
          foreach (ITaskItem source in entry.Value)
          {
            if (!mergedSources.Contains (source))
            {
              mergedSources.Add (source);

              List <ITaskItem> mergedLogDictionaryList = null;

              if (mergedCommandLogDictionary.TryGetValue (entry.Key, out mergedLogDictionaryList))
              {
                mergedLogDictionaryList.Add (source);
              }
              else
              {
                mergedCommandLogDictionary.Add (entry.Key, new List <ITaskItem> { source });
              }
            }
          }
        }

        // 
        // Continue by adding the remaining cached source commands, if they won't overwrite any existing entries.
        // 

        foreach (KeyValuePair<string, List<ITaskItem>> entry in cachedCommandLogDictionary)
        {
          foreach (ITaskItem source in entry.Value)
          {
            if (!mergedSources.Contains (source))
            {
              mergedSources.Add (source);

              List<ITaskItem> mergedLogDictionaryList = null;

              if (mergedCommandLogDictionary.TryGetValue (entry.Key, out mergedLogDictionaryList))
              {
                mergedLogDictionaryList.Add (source);
              }
              else
              {
                mergedCommandLogDictionary.Add (entry.Key, new List<ITaskItem> { source });
              }
            }
          }
        }

        base.OutputCommandTLog (mergedCommandLogDictionary);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /*protected override void OutputReadTLog (Dictionary<string, List<ITaskItem>> commandDictionary, CanonicalTrackedOutputFiles outputs, bool appendToOutputLog)
    {
      // 
      // Output a tracking file detailing which files were read (or are dependencies) for the source files built. Changes in these files will invoke recompilation.
      // 

      CanonicalTrackedInputFiles files = new CanonicalTrackedInputFiles (TLogReadFiles, Sources, outputs, true, false);

      foreach (KeyValuePair<string, List<ITaskItem>> keyPair in commandDictionary)
      {
        // 
        // Remove entries for sources which have been compiled/processed during this execution.
        // 

        files.RemoveEntriesForSource (keyPair.Value.ToArray ());
      }

      files.SaveTlog ();

      // 
      // Append the built sources to the existing (built source stripped) log.
      // 

      base.OutputReadTLog (commandDictionary, outputs, appendToOutputLog);
    }*/

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /*protected override CanonicalTrackedOutputFiles OutputWriteTLog (ITaskItem [] inputs)
    {
      // 
      // Should be able to use the default implementation - since OutputFiles, dependencies (and delta updates) are taken into account anyway.
      // 

      return base.OutputWriteTLog (inputs);
    }*/

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
