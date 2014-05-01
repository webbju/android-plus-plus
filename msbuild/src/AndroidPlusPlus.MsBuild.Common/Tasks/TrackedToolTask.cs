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

    public override bool Execute ()
    {
      if (Setup ())
      {
        return base.Execute ();
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

        List<ITaskItem> outputFiles = new List<ITaskItem> ();

        foreach (ITaskItem source in Sources)
        {
          if (!string.IsNullOrWhiteSpace (source.GetMetadata ("OutputFile")))
          {
            outputFiles.Add (new TaskItem (Path.GetFullPath (source.GetMetadata ("OutputFile"))));
          }

          if (!string.IsNullOrWhiteSpace (source.GetMetadata ("ObjectFileName")))
          {
            outputFiles.Add (new TaskItem (Path.GetFullPath (source.GetMetadata ("ObjectFileName"))));
          }

          if (!string.IsNullOrWhiteSpace (source.GetMetadata ("OutputFiles")))
          {
            string [] files = source.GetMetadata ("OutputFiles").Split (';');

            foreach (string file in files)
            {
              outputFiles.Add (new TaskItem (Path.GetFullPath (file)));
            }
          }

          AddTaskSpecificOutputFiles (ref outputFiles);
        }

        OutputFiles = outputFiles.ToArray ();
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

        if (TrackFileAccess)
        {
          OutputWriteTLog (m_commandBuffer, Sources);

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
        Log.LogError (string.Format ("[{0}] Couldn't locate target tool: {1}", ToolName, pathToTool), MessageImportance.High);

        return -1;
      }

      int returnCode = -1;

      foreach (KeyValuePair<string, List<ITaskItem>> commandKeyPair in m_commandBuffer)
      {
        // 
        // Append source files to each command in the buffer. Clear any matching response file commands so they can setup via a new process.
        // 

        StringBuilder bufferedCommandWithFiles = new StringBuilder ();

        try
        {
          bufferedCommandWithFiles.Append (commandKeyPair.Key);

          if (!string.IsNullOrWhiteSpace (responseFileCommands))
          {
            bufferedCommandWithFiles.Replace (responseFileCommands, "");
          }

          foreach (ITaskItem threadSource in commandKeyPair.Value)
          {
            //Log.LogMessageFromText (string.Format ("[{0}] {1}", ToolName, Path.GetFileName (threadSource.GetMetadata ("Identity") ?? threadSource.ToString ())), MessageImportance.High);

            if (AppendSourcesToCommandLine)
            {
              string threadSourceFilePath = Path.GetFullPath (threadSource.GetMetadata ("FullPath") ?? threadSource.ToString ());

              bufferedCommandWithFiles.Append (" " + GccUtilities.ConvertPathWindowsToPosix (threadSourceFilePath));
            }
          }

          if (OutputCommandLine)
          {
            Log.LogMessageFromText (string.Format ("[{0}] Tool: {1}", ToolName, pathToTool), MessageImportance.High);

            Log.LogMessageFromText (string.Format ("[{0}] Command line: {1}", ToolName, bufferedCommandWithFiles.ToString ()), MessageImportance.High);

            Log.LogMessageFromText (string.Format ("[{0}] Response file commands: {1}", ToolName, responseFileCommands), MessageImportance.High);
          }

          using (Process trackedProcess = new Process ())
          {
            trackedProcess.StartInfo = base.GetProcessStartInfo (pathToTool, bufferedCommandWithFiles.ToString (), responseFileCommands ?? string.Empty);

            trackedProcess.StartInfo.UseShellExecute = false;

            trackedProcess.StartInfo.RedirectStandardOutput = true;

            trackedProcess.StartInfo.RedirectStandardError = true;

            trackedProcess.OutputDataReceived += (sender, e) =>
            {
              if (!string.IsNullOrWhiteSpace (e.Data))
              {
                TrackedExecuteToolOutput (commandKeyPair, e.Data);
              }
            };

            trackedProcess.ErrorDataReceived += (sender, e) =>
            {
              if (!string.IsNullOrWhiteSpace (e.Data))
              {
                TrackedExecuteToolOutput (commandKeyPair, e.Data);
              }
            };

            if (trackedProcess.Start ())
            {
              trackedProcess.BeginOutputReadLine ();

              trackedProcess.BeginErrorReadLine ();

              trackedProcess.WaitForExit ();

              returnCode = trackedProcess.ExitCode;
            }
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

      return returnCode;

      /*long numberOfThreads = (MultiProcessorCompilation && ProcessorNumber > 1) ? ProcessorNumber : 1;

      long numberOfActiveThreads = 0;

      Log.LogMessageFromText (string.Format ("[{0}] Preparing to execute with {1} threads.", ToolName, numberOfThreads), MessageImportance.Low);

      // 
      // Overloaded default tool invokation to better support concurrency and multi-processor builds.
      // 
      // Multiple calls to base method usually results in a 'ObjectDisposedException' ("Safe handle has been closed")
      // 

      Dictionary<string, int> threadJobQueue = new Dictionary<string, int> ();

      foreach (KeyValuePair<string, List<ITaskItem>> entry in m_commandBuffer)
      {
        if (ToolCanceled.WaitOne (0))
        {
          break;
        }

        lock (threadJobQueue)
        {
          threadJobQueue.Add (entry.Key, int.MinValue);
        }

        while (Interlocked.Read (ref numberOfActiveThreads) >= numberOfThreads)
        {
          // 
          // If we don't have a thread to currently satisfy this job, wait.
          // 

          Thread.Sleep (10);
        }

        Interlocked.Increment (ref numberOfActiveThreads);

        Thread jobThread = new Thread (delegate (object arg)
        {
          try
          {
            int returnCode = -1;

            KeyValuePair<string, List<ITaskItem>> threadEntry = (KeyValuePair<string, List<ITaskItem>>)arg;

            //
            // Construct required entire command line from entry key (props derived command line) and registered file paths.
            // 

            StringBuilder commandLineWithFiles = new StringBuilder ();

            commandLineWithFiles.Append (threadEntry.Key);

            foreach (ITaskItem threadSource in threadEntry.Value)
            {
              string threadSourceFilePath = Path.GetFullPath (threadSource.GetMetadata ("FullPath") ?? threadSource.ToString ());

              if (AppendSourcesToCommandLine)
              {
                commandLineWithFiles.Append (" " + GccUtilities.ConvertPathWindowsToPosix (threadSourceFilePath));
              }
            }

            if (OutputCommandLine)
            {
              Log.LogCommandLine (MessageImportance.High, pathToTool + " " + commandLineWithFiles.ToString ());
            }

            using (Process compilerProcess = new Process ())
            {
              //string mockResponseFile = Path.GetTempPath () + Guid.NewGuid () + ".rsp";

              compilerProcess.StartInfo = base.GetProcessStartInfo (pathToTool, string.Empty, commandLineWithFiles.ToString ());

              //File.WriteAllText (mockResponseFile, commandLineWithFiles.ToString (), Encoding.ASCII);

              //compilerProcess.StartInfo.Arguments = "@" + mockResponseFile;

              compilerProcess.StartInfo.RedirectStandardOutput = true;

              compilerProcess.StartInfo.RedirectStandardError = true;

              compilerProcess.OutputDataReceived += (sender, e) =>
              {
                if (!string.IsNullOrWhiteSpace (e.Data))
                {
                  LogEventsFromTextOutput (e.Data, MessageImportance.High);
                }
              };

              compilerProcess.ErrorDataReceived += (sender, e) =>
              {
                if (!string.IsNullOrWhiteSpace (e.Data))
                {
                  LogEventsFromTextOutput (e.Data, MessageImportance.High);
                }
              };

              if (compilerProcess.Start ())
              {
                compilerProcess.BeginOutputReadLine ();

                compilerProcess.BeginErrorReadLine ();

                compilerProcess.WaitForExit ();

                returnCode = compilerProcess.ExitCode;
              }

              //File.Delete (mockResponseFile);
            }

            Log.LogMessageFromText (string.Format ("[{0}] ExecuteTool returned {1}.", ToolName, returnCode), MessageImportance.Low);

            lock (threadJobQueue)
            {
              threadJobQueue [threadEntry.Key] = returnCode;
            }
          }
          catch (Exception e)
          {
            Log.LogError (string.Format ("[{0}] ExecuteTool encountered exception. {1}", ToolName, e), MessageImportance.High);
          }
          finally
          {
            Interlocked.Decrement (ref numberOfActiveThreads);
          }
        });

        jobThread.Start (entry);
      }

      // 
      // Wait for active threads to complete, or a cancel signal.
      // 

      while (Interlocked.Read (ref numberOfActiveThreads) > 0)
      {
        Thread.Sleep (10);

        if (ToolCanceled.WaitOne (0))
        {
          return -1;
        }
      }

      // 
      // Return an error code, if we had one, otherwise success (0).
      // 

      foreach (KeyValuePair<string, int> job in threadJobQueue)
      {
        if (job.Value != 0)
        {
          return job.Value;
        }
      }

      return 0;*/
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
        Log.LogMessageFromText (string.Format ("[{0}] Sources: [{1}] {2}", ToolName, i, Sources [i].ToString ()), MessageImportance.Low);

        foreach (string metadataName in Sources [i].MetadataNames)
        {
          Log.LogMessageFromText (string.Format ("[{0}] --> Metadata: '{1}' = '{2}' ", ToolName, metadataName, Sources [i].GetMetadata (metadataName)), MessageImportance.Low);
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

    protected virtual Dictionary <string, List <ITaskItem>> GenerateCommandLineBuffer (ITaskItem [] input)
    {
      if (input == null)
      {
        throw new ArgumentNullException ();
      }

      if (input.Length == 0)
      {
        throw new ArgumentException ();
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
          commandLineBuilder.Append (commandLineCommands);
        }

        if (!string.IsNullOrWhiteSpace (responseFileCommands))
        {
          if (commandLineBuilder.Length > 0)
          {
            commandLineBuilder.Append (" ");
          }

          commandLineBuilder.Append (responseFileCommands);
        }

        commandBuffer.Add (commandLineBuilder.ToString (), new List<ITaskItem> (input));
      }
      else
      {
        // 
        // Group together provided sources based on their required command line. Sources with identical command lines can be handled at the same time.
        // 

        foreach (ITaskItem source in input)
        {
          string commandLineFromProps = GenerateCommandLineFromProps (source);

          if (!string.IsNullOrWhiteSpace (commandLineFromProps))
          {
            List<ITaskItem> bufferTasks = null;

            ITaskItem fullPathSourceItem = new TaskItem (source.GetMetadata ("FullPath"));

            if (!commandBuffer.TryGetValue (commandLineFromProps, out bufferTasks))
            {
              bufferTasks = new List<ITaskItem> ();

              commandBuffer.Add (commandLineFromProps, bufferTasks);
            }

            bufferTasks.Add (fullPathSourceItem);

            commandBuffer [commandLineFromProps] = bufferTasks;
          }
        }
      }

      if (commandBuffer.Count == 0)
      {
        throw new InvalidOperationException ("Command buffer is empty");
      }

      return commandBuffer;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual string GenerateCommandLineFromProps (ITaskItem source)
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
        throw new ArgumentNullException ();
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

    protected virtual void OutputReadTLog (Dictionary<string, List<ITaskItem>> commandDictionary, ITaskItem [] inputs)
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
          throw new ArgumentNullException ();
        }

        if ((TLogReadFiles == null) || (TLogReadFiles.Length != 1))
        {
          throw new InvalidOperationException ("TLogReadFiles is missing or does not have a length of 1");
        }

        //System.Diagnostics.Debugger.Break ();

        TrackedFileManager trackedFileManager = new TrackedFileManager ();

        if (trackedFileManager != null)
        {
          // 
          // Clear any old entries to sources which have just been processed.
          // 

          trackedFileManager.ImportFromExistingTLog (TLogReadFiles [0]);

          trackedFileManager.RemoveSourcesFromTable (inputs);

          trackedFileManager.AddSourcesToTable (inputs);

          // 
          // Add any explicit inputs registered by parent task.
          // 

          AddTaskSpecificDependencies (ref trackedFileManager);

          // 
          // Create dependency mappings for 'global' task outputs. Assume these relate to all processed sources.
          // 

          if (OutputFiles != null)
          {
            foreach (KeyValuePair<string, List<ITaskItem>> keyPair in commandDictionary)
            {
              foreach (ITaskItem output in OutputFiles)
              {
                string outputFullPath = output.GetMetadata ("FullPath");

                string [] potentialDependencyFilePaths =
                {
                  // C style: 'SRC.d'
                  Path.ChangeExtension (outputFullPath, ".d"), 

                  // Java (or Unix) style: 'SRC.java.d' 'FILE.EXT.d'
                  outputFullPath + ".d" 
                };

                List <string> sourcesAsFullPaths = keyPair.Value.ConvertAll<string> (element => element.GetMetadata ("FullPath"));

                foreach (string dependencyFilePath in potentialDependencyFilePaths)
                {
                  if (File.Exists (dependencyFilePath))
                  {
                    try
                    {
                      GccUtilities.DependencyParser parser = new GccUtilities.DependencyParser (dependencyFilePath);

#if DEBUG
                      Log.LogMessageFromText (string.Format ("[{0}] --> Dependencies (Read) : {1} ({2})", ToolName, dependencyFilePath, parser.Dependencies.Count), MessageImportance.Low);

                      for (int i = 0; i < parser.Dependencies.Count; ++i)
                      {
                        Log.LogMessageFromText (string.Format ("[{0}] --> Dependencies (Read) : [{1}] '{2}'", ToolName, i, parser.Dependencies [i]), MessageImportance.Low);
                      }
#endif

                      foreach (ITaskItem file in parser.Dependencies)
                      {
                        string dependencyFileFullPath = file.GetMetadata ("FullPath");

                        if (!sourcesAsFullPaths.Contains (dependencyFileFullPath))
                        {
                          if (!dependencyFileFullPath.Equals (dependencyFilePath) && !dependencyFileFullPath.Equals (outputFullPath))
                          {
                            if (!File.Exists (dependencyFileFullPath))
                            {
                              Log.LogMessage (MessageImportance.High, "Could not find dependency: " + dependencyFileFullPath);
                            }

                            trackedFileManager.AddDependencyForSources (dependencyFileFullPath, keyPair.Value.ToArray ());
                          }
                        }
                      }
                    }
                    catch (Exception)
                    {
                      Log.LogMessage (MessageImportance.High, "Could not parse dependency file: " + dependencyFilePath);
                    }
                  }
                }
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

    protected virtual void OutputWriteTLog (Dictionary<string, List<ITaskItem>> commandDictionary, ITaskItem [] outputs)
    {
      try
      {
        if (!TrackFileAccess)
        {
          throw new InvalidOperationException ("'TrackFileAccess' is not set. Should not be attempting to output write TLog.");
        }

        if (commandDictionary == null)
        {
          throw new ArgumentNullException ();
        }

        if (outputs == null)
        {
          throw new ArgumentNullException ();
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

          trackedFileManager.RemoveSourcesFromTable (outputs);

          trackedFileManager.AddSourcesToTable (outputs);

          // 
          // Create dependency mappings between source and explicit output file (object-file type relationship).
          // 

          foreach (KeyValuePair<string, List<ITaskItem>> keyPair in commandDictionary)
          {
            foreach (ITaskItem source in keyPair.Value)
            {
              string dependantOutput = source.GetMetadata ("OutputFile");

              if (!string.IsNullOrWhiteSpace (dependantOutput))
              {
                trackedFileManager.AddDependencyForSources (Path.GetFullPath (dependantOutput), new ITaskItem [] { source });
              }
            }
          }

          // 
          // Create dependency mappings for 'global' task outputs. Assume these relate to all processed sources.
          // 

          if (OutputFiles != null)
          {
            foreach (KeyValuePair<string, List<ITaskItem>> keyPair in commandDictionary)
            {
              foreach (ITaskItem output in OutputFiles)
              {
                string outputFullPath = output.GetMetadata ("FullPath");

                trackedFileManager.AddDependencyForSources (outputFullPath, keyPair.Value.ToArray ());

                string [] potentialDependencyFilePaths =
                {
                  // C style: 'SRC.d'
                  Path.ChangeExtension (outputFullPath, ".d"), 

                  // Java (or Unix) style: 'SRC.java.d' 'FILE.EXT.d'
                  outputFullPath + ".d" 
                };
                
                foreach (string dependencyFilePath in potentialDependencyFilePaths)
                {
                  if (File.Exists (dependencyFilePath))
                  {
                    try
                    {
                      GccUtilities.DependencyParser parser = new GccUtilities.DependencyParser (dependencyFilePath);

#if DEBUG
                      Log.LogMessageFromText (string.Format ("[{0}] --> Dependencies (Write) : {1}", ToolName, dependencyFilePath), MessageImportance.Low);

                      Log.LogMessageFromText (string.Format ("[{0}] --> Dependencies (Write) : [{1}] '{2}'", ToolName, 0, parser.OutputFile), MessageImportance.Low);
#endif

                      trackedFileManager.AddDependencyForSources (parser.OutputFile.GetMetadata ("FullPath"), keyPair.Value.ToArray ());
                    }
                    catch (Exception)
                    {
                      Log.LogMessage (MessageImportance.High, "Could not parse dependency file: " + dependencyFilePath);

                      throw;
                    }
                  }
                }
              }
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

    protected virtual void AddTaskSpecificDependencies (ref TrackedFileManager trackedFileManager)
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual void AddTaskSpecificOutputFiles (ref List<ITaskItem> outputFiles)
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override bool ValidateParameters ()
    {
      m_parsedProperties = new XamlParser (PropertiesFile);

      return base.ValidateParameters ();
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
        return ToolName + ".command.tlog";
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual string [] ReadTLogNames
    {
      get
      {
        return new string [] { ToolName + ".read.tlog" };
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual string [] WriteTLogNames
    {
      get
      {
        return new string [] { ToolName + ".write.tlog" };
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
