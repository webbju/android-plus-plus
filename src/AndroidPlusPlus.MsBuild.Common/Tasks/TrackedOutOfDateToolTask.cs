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

        TrackedInputFiles = new CanonicalTrackedInputFiles (this, TLogReadFiles, Sources, ExcludedInputPaths, trackedOutputFiles, true, false);//true);

        ITaskItem [] outOfDateSourcesFromTracking = TrackedInputFiles.ComputeSourcesNeedingCompilation ();// (true);

        ITaskItem [] outOfDateSourcesFromCommandLine = GetOutOfDateSourcesFromCmdLineChanges (Sources);

#if DEBUG
        Log.LogMessageFromText (string.Format ("[{0}] --> No. out-of-date sources (from tracking): {1}", ToolName, outOfDateSourcesFromTracking.Length), MessageImportance.Low);

        Log.LogMessageFromText (string.Format ("[{0}] --> No. out-of-date sources (command line differs): {1}", ToolName, outOfDateSourcesFromCommandLine.Length), MessageImportance.Low);
#endif

        // 
        // Merge out-of-date lists from both sources and assign these for compilation.
        // 

        HashSet<ITaskItem> mergedOutOfDateSources = new HashSet<ITaskItem> (outOfDateSourcesFromTracking);

        foreach (ITaskItem item in outOfDateSourcesFromCommandLine)
        {
          if (!mergedOutOfDateSources.Contains (item))
          {
            mergedOutOfDateSources.Add (item);
          }
        }

        OutOfDateSources = new ITaskItem [mergedOutOfDateSources.Count];

        mergedOutOfDateSources.CopyTo (OutOfDateSources);

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

#if DEBUG
      Log.LogMessageFromText (string.Format ("[{0}] --> Skipped execution: {1}", ToolName, SkippedExecution), MessageImportance.Low);

      for (int i = 0; i < OutOfDateSources.Length; ++i)
      {
        Log.LogMessageFromText (string.Format ("[{0}] --> Out-of-date Sources: [{1}] {2}", ToolName, i, OutOfDateSources [i].ToString ()), MessageImportance.Low);
      }
#endif

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

        HashSet<string> outputFiles = new HashSet<string> ();

        foreach (ITaskItem source in OutOfDateSources)
        {
          if (!string.IsNullOrWhiteSpace (source.GetMetadata ("OutputFile")))
          {
            if (!outputFiles.Contains (source.GetMetadata ("OutputFile")))
            {
              outputFiles.Add (source.GetMetadata ("OutputFile"));
            }
          }

          if (!string.IsNullOrWhiteSpace (source.GetMetadata ("ObjectFileName")))
          {
            if (!outputFiles.Contains (source.GetMetadata ("ObjectFileName")))
            {
              outputFiles.Add (source.GetMetadata ("ObjectFileName"));
            }
          }

          if (!string.IsNullOrWhiteSpace (source.GetMetadata ("OutputFiles")))
          {
            string [] files = source.GetMetadata ("OutputFiles").Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string file in files)
            {
              if (!outputFiles.Contains (file))
              {
                outputFiles.Add (file);
              }
            }
          }
        }

        // 
        // Convert all output file paths to exportable items.
        // 

        List<ITaskItem> outputFileItems = new List<ITaskItem> ();

        foreach (string outputFile in outputFiles)
        {
          outputFileItems.Add (new TaskItem (Path.GetFullPath (outputFile)));
        }

        OutputFiles = outputFileItems.ToArray ();
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
#if DEBUG
        foreach (ITaskItem outputFile in OutputFiles)
        {
          Log.LogMessageFromText (string.Format ("[{0}] --> Outputs: '{1}'", ToolName, outputFile), MessageImportance.Low);
        }
#endif

        if (/*(retCode == 0) &&*/ TrackFileAccess)
        {
          OutputWriteTLog (m_commandBuffer, OutOfDateSources);

          OutputReadTLog (m_commandBuffer, OutOfDateSources);

          OutputCommandTLog (m_commandBuffer);
        }
      }

      return retCode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected static Dictionary<string, List <ITaskItem>> GenerateCommandLinesFromTlog (ITaskItem tlog)
    {
      // 
      // Extract command-line attributes for each saved file in the TLog.
      // 

      if (tlog == null)
      {
        throw new ArgumentNullException ("tlog");
      }

      string tlogFullPath = (!string.IsNullOrEmpty (tlog.GetMetadata ("FullPath")) ? tlog.GetMetadata ("FullPath") : Path.GetFullPath (tlog.ItemSpec));

      if (string.IsNullOrEmpty (tlogFullPath))
      {
        throw new ArgumentException ("Could not evaluate full path for TLog: " + tlog);
      }

      Dictionary<string, List<ITaskItem>> commandLineSourceDictionary = new Dictionary<string, List<ITaskItem>> ();

      if (!File.Exists (tlogFullPath))
      {
        return commandLineSourceDictionary; // Don't error as sometimes this is expected; full rebuilds for example.
      }

      using (StreamReader reader = File.OpenText (tlogFullPath))
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

            List<ITaskItem> sourceList;

            if (!commandLineSourceDictionary.TryGetValue (command, out sourceList))
            {
              sourceList = new List<ITaskItem> ();
            }

            foreach (string file in trackedFiles)
            {
              sourceList.Add (new TaskItem (file));
            }

            commandLineSourceDictionary [command] = sourceList;
          }
        }
      }

      return commandLineSourceDictionary;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected ITaskItem [] GetOutOfDateSourcesFromCmdLineChanges (ITaskItem [] uncheckedSources)
    {
      // 
      // Evaluate a list of the currently saved commands, and check whether these are considered out-of-date.
      // 

      if (TLogCommandFile == null)
      {
        return new ITaskItem [] { };
      }

      Dictionary<string, List<ITaskItem>> commandLineFromTLog = GenerateCommandLinesFromTlog (TLogCommandFile);

      Dictionary<string, List<ITaskItem>> commandLineFromSources = GenerateCommandLineBuffer (uncheckedSources);

      List<ITaskItem> outOfDateSources = new List<ITaskItem> ();

      // 
      // Identify the command line to be used for a specific source file for the *current* build.
      // 

      foreach (ITaskItem source in uncheckedSources)
      {
        string sourcePath = source.GetMetadata ("FullPath").ToUpperInvariant ();

        bool foundMatchingSource = false;

        foreach (KeyValuePair<string, List<ITaskItem>> keyPair in commandLineFromSources)
        {
          List<ITaskItem> cachedSourcesUsingCommand = keyPair.Value;

          foreach (ITaskItem cachedSource in cachedSourcesUsingCommand)
          {
            string cachedSourcePath = Path.GetFullPath (cachedSource.ItemSpec).ToUpperInvariant ();

            if (cachedSourcePath.Equals (sourcePath))
            {
              // 
              // Found a matching source file. Check if this command is already cached in the TLog. 
              // If not, it should be considered out-of-date.
              // 

              foundMatchingSource = true;

              List <ITaskItem> matchingCachedTLogSources = null;

              if (!commandLineFromTLog.TryGetValue (keyPair.Key, out matchingCachedTLogSources))
              {
                outOfDateSources.Add (source);
              }

              break;
            }
          }

          if (foundMatchingSource)
          {
            break;
          }
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

      if (TLogCommandFile == null)
      {
        throw new InvalidOperationException ("TLogCommandFile is invalid");
      }

      // 
      // Merge existing and new dictionaries. This is quite expensive, but means we can utilise a more simple base export implementation.
      // 

      Dictionary<string, List<ITaskItem>> cachedCommandLogDictionary = GenerateCommandLinesFromTlog (TLogCommandFile);

      Dictionary<string, List<ITaskItem>> mergedCommandLogDictionary = new Dictionary<string, List<ITaskItem>> ();

      // 
      // Add recently changed sources first, ensuring these take precedence.
      // 

      foreach (KeyValuePair<string, List<ITaskItem>> entry in commandDictionary)
      {
        List<ITaskItem> mergedLogDictionaryList;

        HashSet<string> mergedLogDictionaryListAsFullPaths = new HashSet<string> ();

        if (mergedCommandLogDictionary.TryGetValue (entry.Key, out mergedLogDictionaryList))
        {
          foreach (ITaskItem source in mergedLogDictionaryList)
          {
            string trackerFormat = TrackedFileManager.ConvertToTrackerFormat (source.GetMetadata ("FullPath"));

            if (!mergedLogDictionaryListAsFullPaths.Contains (trackerFormat))
            {
              mergedLogDictionaryListAsFullPaths.Add (trackerFormat);
            }
          }
        }
        else
        {
          mergedLogDictionaryList = new List<ITaskItem> ();
        }

        foreach (ITaskItem source in entry.Value)
        {
          string trackerFormat = TrackedFileManager.ConvertToTrackerFormat (source.GetMetadata ("FullPath"));

          if (!mergedLogDictionaryListAsFullPaths.Contains (trackerFormat))
          {
            mergedLogDictionaryList.Add (source);

            mergedLogDictionaryListAsFullPaths.Add (trackerFormat);
          }
        }

        mergedCommandLogDictionary [entry.Key] = mergedLogDictionaryList;
      }

      // 
      // Continue by adding the remaining cached source commands, if they won't overwrite any existing entries.
      // 

      foreach (KeyValuePair<string, List<ITaskItem>> entry in cachedCommandLogDictionary)
      {
        List<ITaskItem> mergedLogDictionaryList;

        HashSet<string> mergedLogDictionaryListAsFullPaths = new HashSet<string> ();

        if (mergedCommandLogDictionary.TryGetValue (entry.Key, out mergedLogDictionaryList))
        {
          foreach (ITaskItem source in mergedLogDictionaryList)
          {
            string trackerFormat = TrackedFileManager.ConvertToTrackerFormat (source.GetMetadata ("FullPath"));

            if (!mergedLogDictionaryListAsFullPaths.Contains (trackerFormat))
            {
              mergedLogDictionaryListAsFullPaths.Add (trackerFormat);
            }
          }
        }
        else
        {
          mergedLogDictionaryList = new List<ITaskItem> ();
        }

        foreach (ITaskItem source in entry.Value)
        {
          string trackerFormat = TrackedFileManager.ConvertToTrackerFormat (source.GetMetadata ("FullPath"));

          if (!mergedLogDictionaryListAsFullPaths.Contains (trackerFormat))
          {
            mergedLogDictionaryList.Add (source);

            mergedLogDictionaryListAsFullPaths.Add (trackerFormat);
          }
        }

        mergedCommandLogDictionary [entry.Key] = mergedLogDictionaryList;
      }

      base.OutputCommandTLog (mergedCommandLogDictionary);
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
