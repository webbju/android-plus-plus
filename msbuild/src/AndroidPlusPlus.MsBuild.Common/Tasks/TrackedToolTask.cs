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

namespace AndroidPlusPlus.MsBuild.Common
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public abstract class TrackedToolTask : ToolTask
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected XamlParser m_parsedProperties;

    protected Dictionary<string, List<ITaskItem>> m_commandBuffer = new Dictionary<string, List<ITaskItem>> ();

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public TrackedToolTask (ResourceManager taskResources)
      : base (taskResources)
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [Required]
    public ITaskItem [] Sources { get; set; }

    [Required]
    public bool OutputCommandLine { get; set; }

    [Required]
    public string PropertiesFile { get; set; }

    [Required]
    public string TrackerLogDirectory { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [Output]
    public ITaskItem [] OutputFiles { get; set; }

    [Output]
    public bool SkippedExecution { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool BuildingInIDE { get; set; }

    public int ProcessorNumber { get; set; }

    public bool MultiProcessorCompilation { get; set; }

    public ITaskItem [] ExcludedInputPaths { get; set; }

    public ITaskItem TLogCommandFile { get; set; }

    public ITaskItem [] TLogReadFiles { get; set; }

    public ITaskItem [] TLogWriteFiles { get; set; }

    protected CanonicalTrackedInputFiles TrackedInputFiles { get; set; }

    public bool TrackFileAccess { get; set; }

    public bool MinimalRebuildFromTracking { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void Cancel ()
    {
      base.Cancel ();

      try
      {
        ToolCanceled.Set ();
      }
      catch (Exception e)
      {
        Log.LogWarningFromException (e, true);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override bool Execute ()
    {
      try
      {
        if (Setup ())
        {
          return base.Execute ();
        }
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

    protected override int ExecuteTool (string pathToTool, string responseFileCommands, string commandLineCommands)
    {
      int retCode = -1;

      try
      {
        // 
        // Construct list of target output files for all valid sources.
        // 

        HashSet<string> outputFiles = new HashSet<string> ();

        foreach (ITaskItem source in Sources)
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
        m_commandBuffer = GenerateCommandLineBuffer (Sources);

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

        if ((retCode == 0) && TrackFileAccess)
        {
          OutputWriteTLog (m_commandBuffer, OutputFiles);

          OutputReadTLog (m_commandBuffer, Sources);

          OutputCommandTLog (m_commandBuffer);
        }
      }

      return retCode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual int TrackedExecuteTool (string pathToTool, string responseFileCommands, string commandLineCommands)
    {
      if (!File.Exists (pathToTool))
      {
        Log.LogError (string.Format ("[{0}] Couldn't locate target tool: {1}", ToolName, pathToTool));

        return -1;
      }

      int returnCode = 0;

      long numberOfThreads = (MultiProcessorCompilation && ProcessorNumber > 1) ? ProcessorNumber : 1;

      long numberOfActiveThreads = 0;

      Log.LogMessageFromText (string.Format ("[{0}] --> Preparing to execute with {1} thread(s).", ToolName, numberOfThreads), MessageImportance.Low);

      // 
      // Concurrency of multiple compilation jobs is achieved by creating an OS semaphore for restricting instances (across multiple MSBuild agents).
      // 

      Semaphore threadJobSemaphore;

      try
      {
        threadJobSemaphore = Semaphore.OpenExisting (ToolName);
      }
      catch (WaitHandleCannotBeOpenedException)
      {
        threadJobSemaphore = new Semaphore ((int) numberOfThreads, (int) numberOfThreads, ToolName);
      }

      Dictionary<string, int> threadJobQueue = new Dictionary<string, int> ();

      foreach (KeyValuePair<string, List<ITaskItem>> commandKeyPair in m_commandBuffer)
      {
        bool gotSemaphore = false;

        try
        {
          while (!gotSemaphore)
          {
            if (ToolCanceled.WaitOne (0))
            {
              returnCode = -1;

              break;
            }

            gotSemaphore = threadJobSemaphore.WaitOne (0);

            if (!gotSemaphore)
            {
              Thread.Yield ();
            }
          }

          if (gotSemaphore)
          {
            lock (threadJobQueue)
            {
              threadJobQueue.Add (commandKeyPair.Key, int.MinValue);
            }

            Interlocked.Increment (ref numberOfActiveThreads);

            new Thread (delegate (object arg)
            {
              // 
              // Thread body. Generate required command line and launch tool.
              // 

              int threadExitCode = -1;

              KeyValuePair<string, List<ITaskItem>> threadKeyPair = (KeyValuePair<string, List<ITaskItem>>) arg;

              try
              {
                // 
                // Append source files to each command in the buffer. Clear any matching response file commands so they can setup via a new process.
                // 

                StringBuilder bufferedCommandWithFiles = new StringBuilder ();

                bufferedCommandWithFiles.Append (threadKeyPair.Key);

                if (!string.IsNullOrWhiteSpace (responseFileCommands))
                {
                  bufferedCommandWithFiles.Replace (responseFileCommands, "");
                }

                foreach (ITaskItem threadSource in threadKeyPair.Value)
                {
                  Log.LogMessageFromText (string.Format ("[{0}] {1}", ToolName, Path.GetFileName (threadSource.GetMetadata ("Identity") ?? threadSource.ToString ())), MessageImportance.High);

                  if (AppendSourcesToCommandLine)
                  {
                    string threadSourceFilePath = Path.GetFullPath (threadSource.GetMetadata ("FullPath") ?? threadSource.ToString ());

                    bufferedCommandWithFiles.Append (" " + PathUtils.SantiseWindowsPath (threadSourceFilePath));
                  }
                }

                if (OutputCommandLine)
                {
                  Log.LogMessageFromText (string.Format ("[{0}] Tool: {1}", ToolName, pathToTool), MessageImportance.High);

                  Log.LogMessageFromText (string.Format ("[{0}] Command line: {1}", ToolName, bufferedCommandWithFiles.ToString ()), MessageImportance.High);

                  Log.LogMessageFromText (string.Format ("[{0}] Response file commands: {1}", ToolName, responseFileCommands), MessageImportance.High);
                }
                
                // 
                // Create per-file response file cache. Use a customisable switch (e.g. '@').
                // 
                
                if (!string.IsNullOrWhiteSpace (responseFileCommands))
                {
                  string responseFile = Path.Combine (TrackerLogDirectory, string.Format ("{0}_{1}.rcf", ToolName, Guid.NewGuid ().ToString ()));

                  string responseFileSwitch = GetResponseFileSwitch (responseFile);

                  if (!string.IsNullOrWhiteSpace (responseFileSwitch))
                  {
                    using (StreamWriter writer = new StreamWriter (responseFile, false, Encoding.ASCII))
                    {
                      writer.WriteLine (responseFileCommands);
                    }

                    responseFileCommands = responseFileSwitch;
                  }
                }

                if (OutputCommandLine)
                {
                  Log.LogMessageFromText (string.Format ("[{0}] Response file switch: {1}", ToolName, responseFileCommands), MessageImportance.High);
                }

                using (Process trackedProcess = new Process ())
                {
                  trackedProcess.StartInfo = base.GetProcessStartInfo (pathToTool, bufferedCommandWithFiles.ToString (), responseFileCommands ?? string.Empty);

                  trackedProcess.StartInfo.CreateNoWindow = true;

                  trackedProcess.StartInfo.UseShellExecute = false;

                  trackedProcess.StartInfo.ErrorDialog = false;

                  trackedProcess.StartInfo.RedirectStandardOutput = true;

                  trackedProcess.StartInfo.RedirectStandardError = true;

                  trackedProcess.OutputDataReceived += delegate (object sender, DataReceivedEventArgs args)
                  {
                    try
                    {
                      if (!String.IsNullOrWhiteSpace (args.Data))
                      {
                        TrackedExecuteToolOutput (threadKeyPair, args.Data);
                      }
                    }
                    catch (Exception e)
                    {
                      Log.LogErrorFromException (e, true);
                    }
                  };

                  trackedProcess.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs args)
                  {
                    try
                    {
                      if (!String.IsNullOrWhiteSpace (args.Data))
                      {
                        TrackedExecuteToolOutput (threadKeyPair, args.Data);
                      }
                    }
                    catch (Exception e)
                    {
                      Log.LogErrorFromException (e, true);
                    }
                  };

                  trackedProcess.EnableRaisingEvents = true;

                  if (!trackedProcess.Start ())
                  {
                    throw new InvalidOperationException ("Could not start tracked child process.");
                  }

                  trackedProcess.BeginOutputReadLine ();

                  trackedProcess.BeginErrorReadLine ();

                  trackedProcess.WaitForExit ();

                  threadExitCode = trackedProcess.ExitCode;
                }
              }
              catch (Exception e)
              {
                Log.LogErrorFromException (e, true);

                threadExitCode = -1;
              }
              finally
              {
                try
                {
                  lock (threadJobQueue)
                  {
                    threadJobQueue [threadKeyPair.Key] = threadExitCode;
                  }

                  Interlocked.Decrement (ref numberOfActiveThreads);

                  threadJobSemaphore.Release ();
                }
                catch (Exception e)
                {
                  Log.LogErrorFromException (e, true);
                }
              }
            }).Start (commandKeyPair);
          }
        }
        catch (Exception e)
        {
          Log.LogErrorFromException (e, true);

          returnCode = -1;
        }

        if (returnCode != 0)
        {
          break;
        }
      }

      //
      // Wait for active threads to complete, if the task wasn't terminated.
      //

      try
      {
        if (returnCode == 0)
        {
          while (Interlocked.Read (ref numberOfActiveThreads) > 0)
          {
            if (ToolCanceled.WaitOne (0))
            {
              returnCode = -1;

              break;
            }

            Thread.Yield ();
          }
        }

        if (returnCode == 0)
        {
          foreach (KeyValuePair<string, int> job in threadJobQueue)
          {
            if (job.Value != 0)
            {
              returnCode = job.Value;
            }
          }
        }
      }
      catch (System.Exception ex)
      {
        Log.LogErrorFromException (ex, true);

        returnCode = -1;
      }

      return returnCode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual void TrackedExecuteToolOutput (KeyValuePair<string, List<ITaskItem>> commandAndSourceFiles, string singleLine)
    {
      LogEventsFromTextOutput (string.Format ("[{0}] {1}", ToolName, singleLine), MessageImportance.High);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual bool Setup ()
    {
      SkippedExecution = false;

      if (!ValidateParameters ())
      {
        return false;
      }

      if (TrackFileAccess || MinimalRebuildFromTracking)
      {
        SetupTrackerLogPaths ();
      }

      if (ForcedRebuildRequired () || !MinimalRebuildFromTracking)
      {
        // 
        // Check there are actually sources to build, otherwise we can skip execution.
        // 

        if ((Sources == null) || Sources.Length == 0)
        {
          SkippedExecution = true;
        }
      }

#if DEBUG
      for (int i = 0; i < Sources.Length; ++i)
      {
        Log.LogMessageFromText (string.Format ("[{0}] --> Sources: [{1}] {2}", ToolName, i, Sources [i].ToString ()), MessageImportance.Low);

        foreach (string metadataName in Sources [i].MetadataNames)
        {
          Log.LogMessageFromText (string.Format ("[{0}] ----> Metadata: '{1}' = '{2}' ", ToolName, metadataName, Sources [i].GetMetadata (metadataName)), MessageImportance.Low);
        }
      }
#endif

      return true;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override bool SkipTaskExecution ()
    {
      return SkippedExecution;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected bool ForcedRebuildRequired ()
    {
      // 
      // If we can't find a cached 'TLog Command' file, presume we have to force rebuild all sources.
      // 

      if (TLogCommandFile != null)
      {
        string tLogCommandFilePath = TLogCommandFile.GetMetadata ("FullPath");

        if (!string.IsNullOrEmpty (tLogCommandFilePath))
        {
          return File.Exists (tLogCommandFilePath);
        }
      }

      return false;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual void SetupTrackerLogPaths ()
    {
      // 
      // Create tracker tasks for each of the target output files; command, read and write logs.
      // 

      if (TLogCommandFile == null)
      {
        TLogCommandFile = new TaskItem (Path.Combine (TrackerLogDirectory, CommandTLogName));
      }

      if (TLogReadFiles == null)
      {
        TLogReadFiles = new ITaskItem [ReadTLogNames.Length];

        for (int i = 0; i < ReadTLogNames.Length; ++i)
        {
          TLogReadFiles [i] = new TaskItem (Path.Combine (TrackerLogDirectory, ReadTLogNames [i]));
        }
      }

      if (TLogWriteFiles == null)
      {
        TLogWriteFiles = new ITaskItem [WriteTLogNames.Length];

        for (int i = 0; i < WriteTLogNames.Length; ++i)
        {
          TLogWriteFiles [i] = new TaskItem (Path.Combine (TrackerLogDirectory, WriteTLogNames [i]));
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual Dictionary <string, List <ITaskItem>> GenerateCommandLineBuffer (ITaskItem [] inputSources)
    {
      if (inputSources == null)
      {
        throw new ArgumentNullException ("inputSources");
      }

      Dictionary<string, List<ITaskItem>> commandBuffer = new Dictionary<string, List<ITaskItem>> ();

      // 
      // Prefer command line and response file switches, prefixed to allow for appropriate processing.
      // 

      string commandLineCommands = GenerateCommandLineCommands ();

      string responseFileCommands = GenerateResponseFileCommands ();

      if (!string.IsNullOrWhiteSpace (commandLineCommands) || !string.IsNullOrWhiteSpace (responseFileCommands))
      {
        StringBuilder commandLineBuilder = new StringBuilder ();

        if (!string.IsNullOrWhiteSpace (commandLineCommands))
        {
          commandLineBuilder.Append (commandLineCommands + " ");
        }

        if (!string.IsNullOrWhiteSpace (responseFileCommands))
        {
          commandLineBuilder.Append (responseFileCommands + " ");
        }

        commandBuffer.Add (commandLineBuilder.ToString (), new List<ITaskItem> (inputSources));
      }
      else
      {
        // 
        // Group together provided sources based on their required command line. Sources with identical command lines can be handled at the same time.
        // 

        foreach (ITaskItem source in inputSources)
        {
          string commandLineFromProps = GenerateCommandLineFromProps (source);

          if (!string.IsNullOrWhiteSpace (commandLineFromProps))
          {
            List<ITaskItem> bufferTasks = null;

            if (!commandBuffer.TryGetValue (commandLineFromProps, out bufferTasks))
            {
              bufferTasks = new List<ITaskItem> ();

              commandBuffer.Add (commandLineFromProps, bufferTasks);
            }

            bufferTasks.Add (source);

            commandBuffer [commandLineFromProps] = bufferTasks;
          }
        }
      }

      return commandBuffer;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual string GenerateCommandLineFromProps (ITaskItem source)
    {
      // 
      // Build a command-line based on parsing switches from the registered property sheet, and any additional flags.
      // 

      StringBuilder builder = new StringBuilder (PathUtils.CommandLineLength);

      try
      {
        if (source == null)
        {
          throw new ArgumentNullException ("source");
        }

        builder.Append (m_parsedProperties.Parse (source));
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

    protected virtual void OutputCommandTLog (Dictionary <string, List<ITaskItem>> commandDictionary)
    {
      // 
      // Output a tracking file for each of the commands used in the previous build, and target sources to which they relate.
      // 

      if (!TrackFileAccess)
      {
        throw new InvalidOperationException ("'TrackFileAccess' is not set. Should not be attempting to output command TLog.");
      }

      if (commandDictionary == null)
      {
        throw new ArgumentNullException ("commandDictionary");
      }

      if (TLogCommandFile == null)
      {
        throw new InvalidOperationException ("TLogCommandFile is missing");
      }

      // 
      // Export the current command execution command buffer in the style of a TLog. See 'TrackedFileManager' for more explaination.
      // 

      string commandFileFullPath = (!string.IsNullOrEmpty (TLogCommandFile.GetMetadata ("FullPath"))) ? TLogCommandFile.GetMetadata ("FullPath") : Path.GetFullPath (TLogCommandFile.ItemSpec);

      using (StreamWriter writer = new StreamWriter (commandFileFullPath, false, Encoding.Unicode))
      {
        StringBuilder sourceFileList = new StringBuilder ();

        foreach (KeyValuePair<string, List<ITaskItem>> keyPair in commandDictionary)
        {
          sourceFileList.Length = 0;

          foreach (ITaskItem source in keyPair.Value)
          {
            sourceFileList.Append ("|" + source.GetMetadata ("FullPath").ToUpperInvariant ());
          }

          sourceFileList.Replace ('|', '^', 0, 1);

          writer.WriteLine (sourceFileList.ToString ());

          writer.WriteLine (keyPair.Key);
        }

        writer.Close ();
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual void OutputReadTLog (Dictionary<string, List<ITaskItem>> commandDictionary, ITaskItem [] sources)
    {
      // 
      // Output a tracking file detailing which files were read (or are dependencies) for the source files built. Changes in these files will invoke recompilation.
      // 

      try
      {
        if (!TrackFileAccess)
        {
          throw new InvalidOperationException ("'TrackFileAccess' is not set. Should not be attempting to output read TLog.");
        }

        if (commandDictionary == null)
        {
          throw new ArgumentNullException ("commandDictionary");
        }

        if (sources == null)
        {
          throw new ArgumentNullException ("sources");
        }

        if ((TLogReadFiles == null) || (TLogReadFiles.Length != 1))
        {
          throw new InvalidOperationException ("TLogReadFiles is missing or does not have a length of 1");
        }

        TrackedFileManager trackedFileManager = new TrackedFileManager ();

        if (trackedFileManager != null)
        {
          // 
          // Clear any old entries to sources which have just been processed.
          // 

          trackedFileManager.ImportFromExistingTLog (TLogReadFiles [0]);

          trackedFileManager.RemoveSourcesFromTable (sources);

          trackedFileManager.AddSourcesToTable (sources);

          // 
          // Add any explicit inputs registered by parent task.
          // 

          AddTaskSpecificDependencies (ref trackedFileManager, sources);

          // 
          // Create dependency mappings for 'global' task outputs. Assume these relate to all processed sources.
          // 

          Dictionary<string, List<ITaskItem>> cachedOutputDependencies = new Dictionary<string, List<ITaskItem>> ();

          Dictionary<string, string> dependencyFilePermutations = new Dictionary<string, string> ();

          foreach (KeyValuePair<string, List<ITaskItem>> commandKeyPair in commandDictionary)
          {
            foreach (ITaskItem source in commandKeyPair.Value)
            {
              string dependantOutputFile = source.GetMetadata ("OutputFile");

              string dependantObjectFileName = source.GetMetadata ("ObjectFileName");

              // 
              // Evaluate potential exported dependency files (each use slightly different conventions).
              // 
              // - C style: 'SRC.d'
              // - Java (or Unix) style: 'SRC.java.d' 'FILE.EXT.d'
              // 

              dependencyFilePermutations.Clear ();

              if (!string.IsNullOrWhiteSpace (dependantOutputFile))
              {
                dependencyFilePermutations.Add (Path.ChangeExtension (dependantOutputFile, ".d"), dependantOutputFile);

                dependencyFilePermutations.Add (dependantOutputFile + ".d", dependantOutputFile);
              }

              if (!string.IsNullOrWhiteSpace (dependantObjectFileName))
              {
                dependencyFilePermutations.Add (Path.ChangeExtension (dependantObjectFileName, ".d"), dependantObjectFileName);

                dependencyFilePermutations.Add (dependantObjectFileName + ".d", dependantObjectFileName);
              }

              // 
              // Iterate through each possible dependency file. Cache listings so that similar outputs aren't re-parsed (i.e. static/shared libraries from object files)
              // 

              foreach (KeyValuePair<string, string> dependencyKeyPair in dependencyFilePermutations)
              {
                string dependencyFile = dependencyKeyPair.Key;

                if (string.IsNullOrWhiteSpace (dependencyFile))
                {
                  continue;
                }
                else if (string.IsNullOrWhiteSpace (Path.GetFileNameWithoutExtension (dependencyFile)))
                {
                  continue;
                }
                else if (!File.Exists (dependencyFile))
                {
                  continue;
                }

                // 
                // Probe and cache each dependency file. Saves re-parsing identical file references each time.
                // 

                List<ITaskItem> dependencies;

                if (!cachedOutputDependencies.TryGetValue (dependencyFile, out dependencies))
                {
                  GccUtilities.DependencyParser parser = new GccUtilities.DependencyParser (dependencyFile);

#if DEBUG
                  Log.LogMessageFromText (string.Format ("[{0}] --> Dependencies (Read) : {1} (Entries: {2})", ToolName, dependencyFile, parser.Dependencies.Count), MessageImportance.Low);

                  for (int i = 0; i < parser.Dependencies.Count; ++i)
                  {
                    Log.LogMessageFromText (string.Format ("[{0}] --> Dependencies (Read) : [{1}] '{2}'", ToolName, i, parser.Dependencies [i]), MessageImportance.Low);
                  }
#endif

                  dependencies = parser.Dependencies;

                  cachedOutputDependencies.Add (dependencyFile, parser.Dependencies);
                }

                trackedFileManager.AddDependencyForSources (dependencies.ToArray (), new ITaskItem [] { source });
              }
            }
          }

          trackedFileManager.Save (TLogReadFiles [0]);
        }
      }
      catch (Exception e)
      {
        Log.LogErrorFromException (e, true, true, null);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual void OutputWriteTLog (Dictionary<string, List<ITaskItem>> commandDictionary, ITaskItem [] sources)
    {
      try
      {
        if (!TrackFileAccess)
        {
          throw new InvalidOperationException ("'TrackFileAccess' is not set. Should not be attempting to output write TLog.");
        }

        if (commandDictionary == null)
        {
          throw new ArgumentNullException ("commandDictionary");
        }

        if (sources == null)
        {
          throw new ArgumentNullException ("sources");
        }

        if ((TLogWriteFiles == null) || (TLogWriteFiles.Length != 1))
        {
          throw new InvalidOperationException ("TLogWriteFiles is missing or does not have a length of 1");
        }

        TrackedFileManager trackedFileManager = new TrackedFileManager ();

        if (trackedFileManager != null)
        {
          // 
          // Clear any old entries to sources which have just been processed.
          // 

          trackedFileManager.ImportFromExistingTLog (TLogWriteFiles [0]);

          trackedFileManager.RemoveSourcesFromTable (sources);

          trackedFileManager.AddSourcesToTable (sources);

          // 
          // Add any explicit outputs registered by parent task.
          // 

          AddTaskSpecificOutputFiles (ref trackedFileManager, sources);

          // 
          // Create dependency mappings between source and explicit output file (object-file type relationship).
          // 

          List<ITaskItem> dependantFiles = new List<ITaskItem> (2);

          foreach (KeyValuePair<string, List<ITaskItem>> keyPair in commandDictionary)
          {
            foreach (ITaskItem source in keyPair.Value)
            {
              dependantFiles.Clear ();

              if (!string.IsNullOrWhiteSpace (source.GetMetadata ("OutputFile")))
              {
                dependantFiles.Add (new TaskItem (Path.GetFullPath (source.GetMetadata ("OutputFile"))));
              }

              if (!string.IsNullOrWhiteSpace (source.GetMetadata ("ObjectFileName")))
              {
                dependantFiles.Add (new TaskItem (Path.GetFullPath (source.GetMetadata ("ObjectFileName"))));
              }

              if (!string.IsNullOrWhiteSpace (source.GetMetadata ("OutputFiles")))
              {
                string [] files = source.GetMetadata ("OutputFiles").Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string file in files)
                {
                  dependantFiles.Add (new TaskItem (Path.GetFullPath (file)));
                }
              }

              trackedFileManager.AddDependencyForSources (dependantFiles.ToArray (), new ITaskItem [] { source });
            }
          }

          trackedFileManager.Save (TLogWriteFiles [0]);
        }
      }
      catch (Exception e)
      {
        Log.LogErrorFromException (e, true, true, null);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual void AddTaskSpecificDependencies (ref TrackedFileManager trackedFileManager, ITaskItem [] sources)
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual void AddTaskSpecificOutputFiles (ref TrackedFileManager trackedFileManager, ITaskItem [] sources)
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override bool ValidateParameters ()
    {
      try
      {
        m_parsedProperties = new XamlParser (PropertiesFile);

        if (string.IsNullOrWhiteSpace (ToolPath))
        {
          throw new InvalidOperationException ("ToolPath is empty or invalid: " + ToolPath);
        }

        if (string.IsNullOrWhiteSpace (ToolExe))
        {
          throw new InvalidOperationException ("ToolExe is empty or invalid: " + ToolExe);
        }

        return base.ValidateParameters ();
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

    protected override string GenerateFullPathToTool ()
    {
      return Path.Combine (ToolPath, ToolExe);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual bool AppendSourcesToCommandLine
    {
      get
      {
        return true;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual string TrackerIntermediateDirectory
    {
      get
      {
        return string.Empty;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual string CommandTLogName
    {
      get
      {
        return ToolName + ".command.1.tlog";
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual string [] ReadTLogNames
    {
      get
      {
        return new string [] { ToolName + ".read.1.tlog" };
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual string [] WriteTLogNames
    {
      get
      {
        return new string [] { ToolName + ".write.1.tlog" };
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override MessageImportance StandardOutputLoggingImportance
    {
      get
      {
        // 
        // Override default StandardOutputLoggingImportance so that we see the stdout from the toolchain from within visual studio.
        // 

        return MessageImportance.Normal;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override Encoding StandardOutputEncoding
    {
      get
      {
        return Encoding.ASCII;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override Encoding ResponseFileEncoding
    {
      get
      {
        return Encoding.Unicode;
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
