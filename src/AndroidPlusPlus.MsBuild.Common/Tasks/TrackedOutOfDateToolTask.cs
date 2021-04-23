////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.Common
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class TrackedOutOfDateToolTask : TrackedToolTask
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

    protected bool ForcedRebuildRequired()
    {
      //
      // If we can't find a cached 'TLog Command' file, presume we have to force rebuild all sources.
      //

      if (TLogCommandFiles == null || TLogCommandFiles.Length == 0)
      {
        return true;
      }

      return !File.Exists(TLogCommandFiles[0]?.GetMetadata("FullPath"));
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override bool SkipTaskExecution()
    {
      //
      // Check there are actually sources to build, otherwise we can skip execution.
      //

      bool shouldSkipTaskExecution = base.SkipTaskExecution();

      if (MinimalRebuildFromTracking && !ForcedRebuildRequired())
      {
        TrackedOutputFiles = new CanonicalTrackedOutputFiles(this, TLogWriteFiles);

        TrackedInputFiles = new CanonicalTrackedInputFiles(this, TLogReadFiles, InputFiles, ExcludedInputPaths, TrackedOutputFiles, true, false);

        var outOfDateSourcesFromTracking = TrackedInputFiles.ComputeSourcesNeedingCompilation(true);

        var outOfDateSourcesFromCommandLine = GetOutOfDateSourcesFromCmdLineChanges(GenerateUnionFileCommands(), InputFiles);

#if DEBUG
        Log.LogMessageFromText($"[{GetType().Name}] Out of date sources (from tracking): {outOfDateSourcesFromTracking.Length}", MessageImportance.Low);

        Log.LogMessageFromText($"[{GetType().Name}] Out of date sources (command line differs): {outOfDateSourcesFromCommandLine.Length}", MessageImportance.Low);
#endif

        // 
        // Merge out-of-date lists from both sources and assign these for compilation.
        // 

        var mergedOutOfDateSources = new HashSet<ITaskItem>(outOfDateSourcesFromTracking);

        mergedOutOfDateSources.UnionWith(outOfDateSourcesFromCommandLine);

        OutOfDateInputFiles = mergedOutOfDateSources.ToArray();

        SkippedExecution = OutOfDateInputFiles == null || OutOfDateInputFiles.Length == 0;

        if (SkippedExecution)
        {
          return SkippedExecution;
        }

        // 
        // Remove out-of-date files from tracked file list. Thus they are missing and need re-processing.
        // 

        TrackedInputFiles.RemoveEntriesForSource(OutOfDateInputFiles);

        TrackedInputFiles.SaveTlog();

        TrackedOutputFiles.RemoveEntriesForSource(OutOfDateInputFiles);

        TrackedOutputFiles.SaveTlog();

        SkippedExecution = false;

        return SkippedExecution;
      }

      // 
      // Consider nothing out of date. Process everything.
      // 

      OutOfDateInputFiles = InputFiles;

      SkippedExecution = false;

      return SkippedExecution;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected Dictionary<string, List <ITaskItem>> GenerateCommandLinesFromTlogs (IEnumerable<ITaskItem> tlogs)
    {
      // 
      // Extract command-line attributes for each saved file in the TLog.
      // 

      if (tlogs == null)
      {
        throw new ArgumentNullException (nameof(tlogs));
      }

      var commandLineSourceDictionary = new Dictionary<string, List<ITaskItem>>();

      foreach (var tlog in tlogs)
      {
        string tlogFullPath = tlog.GetMetadata("FullPath");

        if (string.IsNullOrEmpty(tlogFullPath))
        {
          tlogFullPath = Path.GetFullPath(tlog.ItemSpec);
        }

        if (!File.Exists(tlogFullPath))
        {
          continue; // Don't error as sometimes this is expected; full rebuilds for example.
        }

        using StreamReader reader = File.OpenText(tlogFullPath);

        // 
        // Construct a dictionary for each unique tracked command, with an associated list of paired files (separated by '|').
        // 

        for (string line = reader.ReadLine(); !string.IsNullOrEmpty(line); line = reader.ReadLine())
        {
          if (line.StartsWith("^"))
          {
            var trackedFiles = line.Substring(1).Split('|').Select(f => new TaskItem(f));

            string command = reader.ReadLine();

            if (!commandLineSourceDictionary.TryGetValue(command, out List<ITaskItem> sourceList))
            {
              sourceList = new List<ITaskItem>();
            }

            foreach (string file in trackedFiles)
            {
              sourceList.Add(new TaskItem(file));
            }

            commandLineSourceDictionary[command] = sourceList;
          }
        }
      }

      return commandLineSourceDictionary;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected ITaskItem [] GetOutOfDateSourcesFromCmdLineChanges (string expectedCommandLine, ITaskItem [] uncheckedSources)
    {
      // 
      // Evaluate which (if any) of the `uncheckedSources` are already present in the command TLog.
      // If cached entries are found, identify whether this source should be considered "out of date".
      // 

      if (TLogCommandFiles?.Length == 0)
      {
        return Array.Empty<ITaskItem>();
      }

      Dictionary<string, ITaskItem> uncheckedSourcesRooted = new Dictionary<string, ITaskItem>();

      foreach (var uncheckedSource in uncheckedSources)
      {
        uncheckedSourcesRooted.Add(TrackedFileManager.ConvertToTrackerFormat(uncheckedSource), uncheckedSource);
      }

      var outOfDateSources = new HashSet<ITaskItem>();

      using StreamReader reader = File.OpenText(TLogCommandFiles[0].ItemSpec);

      for (string line = reader.ReadLine(); !string.IsNullOrEmpty(line); line = reader.ReadLine())
      {
        if (line.StartsWith("^"))
        {
          var trackedFiles = line.Substring(1).Split('|');

          string trackedCommandLine = reader.ReadLine();

          foreach (string trackedFile in trackedFiles)
          {
            if (uncheckedSourcesRooted.TryGetValue(trackedFile, out ITaskItem match) && !string.Equals(trackedCommandLine, expectedCommandLine))
            {
#if DEBUG
              Log.LogMessageFromText($"[{GetType().Name}] Out of date source identified: {trackedFile}. Command lines differed.", MessageImportance.Low);

              Log.LogMessageFromText($"[{GetType().Name}] Out of date source identified: {trackedFile}. Expected: {expectedCommandLine} ", MessageImportance.Low);

              Log.LogMessageFromText($"[{GetType().Name}] Out of date source identified: {trackedFile}. Cached: {trackedCommandLine} ", MessageImportance.Low);
#endif

              outOfDateSources.Add(match);
            }
          }
        }
      }

      return outOfDateSources.ToArray();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if false
    protected override void OutputCommandTLog (ITaskItem commandFile, string responseFileCommands, string commandLineCommands)
    {
      // 
      // Output a tracking file for each of the commands used in the previous build, and target sources to which they relate.
      // 
      // *Keeps existing entries in the command log. Updates entries for sources which have been compiled/modified.*
      // 

      if (TLogCommandFiles?.Length == 0)
      {
        throw new InvalidOperationException ("TLogCommandFile is invalid");
      }

      // 
      // Merge existing and new dictionaries. This is quite expensive, but means we can utilise a more simple base export implementation.
      // 

      Dictionary<string, List<ITaskItem>> cachedCommandLogDictionary = GenerateCommandLinesFromTlogs (TLogCommandFiles);

      Dictionary<string, List<ITaskItem>> mergedCommandLogDictionary = new Dictionary<string, List<ITaskItem>> ();

      // 
      // Add recently changed sources first, ensuring these take precedence.
      // 

      foreach (KeyValuePair<string, List<ITaskItem>> entry in commandDictionary)
      {
        HashSet<string> mergedLogDictionaryListAsFullPaths = new HashSet<string> ();

        if (mergedCommandLogDictionary.TryGetValue (entry.Key, out List<ITaskItem> mergedLogDictionaryList))
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
        HashSet<string> mergedLogDictionaryListAsFullPaths = new HashSet<string> ();

        if (mergedCommandLogDictionary.TryGetValue (entry.Key, out List<ITaskItem> mergedLogDictionaryList))
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
#endif

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
