////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using AndroidPlusPlus.Common;
using AndroidPlusPlus.MsBuild.Common.Attributes;
using Microsoft.Build.Framework;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.Common
{

  public class TrackedToolTask : ToolTask
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public TrackedToolTask()
      : base()
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public TrackedToolTask(ResourceManager taskResources)
      : base(taskResources)
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ITaskItem[] InputFiles { get; set; }

    public ITaskItem[] OutOfDateInputFiles { get; set; }

    [Required]
    [SwitchString(Subtype = "folder", Description = "Tracker log directory.", IsRequired = true, IncludeInCommandLine = false)]
    public ITaskItem TrackerLogDirectory { get; set; }

    [Output]
    public ITaskItem[] OutputFiles { get; set; }

    [Output]
    public bool SkippedExecution { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool OutputCommandLine { get; set; } = false;

    public bool MinimalRebuildFromTracking { get; set; }

    public bool TrackFileAccess { get; set; } = true;

    public string PropertiesFile { get; set; }

    public bool BuildingInIDE { get; set; }

    protected Dictionary<string, SwitchBaseAttribute> SwitchAttributes { get; set; }

    protected CanonicalTrackedInputFiles TrackedInputFiles { get; set; }

    protected CanonicalTrackedOutputFiles TrackedOutputFiles { get; set; }

    protected override MessageImportance StandardOutputLoggingImportance => MessageImportance.Normal;

    protected override Encoding StandardOutputEncoding => Encoding.UTF8;

    protected override Encoding ResponseFileEncoding => new UTF8Encoding(false);

    protected override string ToolName => GetType().Name;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override bool ValidateParameters()
    {
      bool validParameters = base.ValidateParameters();

      if (validParameters)
      {
        try
        {
          if (InputFiles == null || InputFiles.Length == 0)
          {
            Log.LogError($"Required parameter {nameof(InputFiles)} is {(InputFiles == null ? "(null)" : "empty")}.");

            validParameters = false;
          }

#if DEBUG
          for (int i = 0; i < InputFiles?.Length; ++i)
          {
            Log.LogMessageFromText($"[{GetType().Name}] {nameof(InputFiles)}: [{i}] {InputFiles[i]}", MessageImportance.Low);

            foreach (string metadataName in InputFiles[i].MetadataNames)
            {
              Log.LogMessageFromText($"[{GetType().Name}] -- Metadata: '{metadataName}' = '{InputFiles[i].GetMetadata(metadataName)}' ", MessageImportance.Low);
            }
          }
#endif

          OutOfDateInputFiles = InputFiles;

          OutputFiles = Array.Empty<ITaskItem>();
        }
        catch (Exception e)
        {
          Log.LogWarningFromException(e, true);

          return false;
        }
      }

      return validParameters && InitializeSwitches();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected bool InitializeSwitches()
    {
      // 
      // Collate and initialise any bound switch attributes.
      // 

      try
      {
        SwitchAttributes ??= new Dictionary<string, SwitchBaseAttribute>();

        var properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var propInfo in properties)
        {
          var basePropertyAttributes = (SwitchBaseAttribute[])Attribute.GetCustomAttributes(propInfo, typeof(SwitchBaseAttribute), true);

          foreach (var attribute in basePropertyAttributes)
          {
            attribute.Initialize(this, propInfo);

            SwitchAttributes.Add(attribute.Name, attribute);
          }
        }

#if DEBUG
        foreach (var attribute in SwitchAttributes)
        {
          Log.LogMessage($"[{ToolName}] Initialize property from {GetType().Name}: {attribute.Key} | {attribute.Value.GetType().Name}");
        }
#endif
      }
      catch (Exception e)
      {
        Log.LogErrorFromException(e, true);
      }

      return true;
    }

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

    protected override int ExecuteTool (string pathToTool, string responseFileCommands, string commandLineCommands)
    {
      int exitCode = -1;

      try
      {
        exitCode = TrackedExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
      }
      catch (Exception e)
      {
        Log.LogErrorFromException (e, true);
      }
      finally
      {
        exitCode = PostExecuteTool(exitCode, responseFileCommands, commandLineCommands);
      }

      return exitCode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual int PostExecuteTool(int exitCode, string responseFileCommands, string commandLineCommands)
    {
      TrackedOutputFiles = new CanonicalTrackedOutputFiles(this, TLogWriteFiles);

      TrackedInputFiles = new CanonicalTrackedInputFiles(this, TLogReadFiles, OutOfDateInputFiles, ExcludedInputPaths, TrackedOutputFiles, true, false);

      var trackedCommands = TrackerUtilities.ReadCommandTLog(TLogCommandFiles[0], ResponseFileEncoding);

      switch (exitCode)
      {
        case 0:
        {
          //
          // Successful build. Begin by collating known output files. (We have a manual workaround for instances where TrackedOutputFiles can't find rooted output.)
          //

          string commandLine = (commandLineCommands.Length > 0) ? commandLineCommands + " " + responseFileCommands : responseFileCommands;

          foreach (var inputFile in OutOfDateInputFiles)
          {
            trackedCommands[FileTracker.FormatRootingMarker(inputFile)] = commandLine;
          }

          OutputFiles = TrackedOutputFiles.OutputsForSource(OutOfDateInputFiles);

          if (OutputFiles != null)
          {
            foreach (var outfile in OutputFiles)
            {
              outfile.ItemSpec = PathUtils.GetExactPathName(outfile.ItemSpec);
            }
          }

          //
          // Remove any instances where "input files" (sources which existed before this build) are shown as read/touched/written.
          //

          TrackedOutputFiles.RemoveDependenciesFromEntryIfMissing(OutOfDateInputFiles);

          TrackedInputFiles.RemoveDependenciesFromEntryIfMissing(OutOfDateInputFiles);

          //
          // Crunch the logs. This will erradicate un-rooted tracking data.
          //

          TrackedOutputFiles.SaveTlog();

          TrackedInputFiles.SaveTlog();

          TrackerUtilities.OutputCommandTLog(TLogCommandFiles[0], trackedCommands, ResponseFileEncoding);

          break;
        }

        default:
        {
          //
          // Task failed. Remove any potentially processed file outputs to refresh clean state.
          //

          foreach (var file in OutOfDateInputFiles)
          {
            trackedCommands.Remove(FileTracker.FormatRootingMarker(file));
          }

          TrackedOutputFiles.RemoveEntriesForSource(OutOfDateInputFiles);

          TrackedInputFiles.RemoveEntriesForSource(OutOfDateInputFiles);

          TrackedOutputFiles.SaveTlog();

          TrackedInputFiles.SaveTlog();

          TrackerUtilities.OutputCommandTLog(TLogCommandFiles[0], trackedCommands, ResponseFileEncoding);

          break;
        }
      }

      return exitCode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual int TrackedExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
    {
      //
      // Thread body. Generate required command line and launch tool.
      //

      int exitCode = -1;

      try
      {
        //
        // If response files arguments are used/supported, migrate shared flags to a file and strip them from the command line.
        //

        var commandLineSwitchesBuffer = new StringBuilder(commandLineCommands);

        var responseFileSwitchesBuffer = new StringBuilder(responseFileCommands);

        if (responseFileSwitchesBuffer.Length > 0)
        {
          commandLineSwitchesBuffer.Replace(responseFileSwitchesBuffer.ToString(), "");
        }

#if DEBUG
        Log.LogMessageFromText($"[{ToolName}] Tool: {pathToTool}", MessageImportance.High);

        Log.LogMessageFromText($"[{ToolName}] Command line: {commandLineSwitchesBuffer}", MessageImportance.High);

        Log.LogMessageFromText($"[{ToolName}] Response file commands: {responseFileSwitchesBuffer}", MessageImportance.High);
#endif

        var trackerToolPath = FileTracker.GetTrackerPath(ExecutableType.Native64Bit); // @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Tracker.exe";

        var trackerCommandLineSwitches = FileTracker.TrackerCommandArguments(pathToTool, commandLineSwitchesBuffer.ToString());

        if (!string.IsNullOrEmpty(trackerToolPath))
        {
          var trackerRootingMarker = OutOfDateInputFiles.Length > 0 ? FileTracker.FormatRootingMarker(OutOfDateInputFiles) : null;

          var trackerResponseFileArguments = FileTracker.TrackerResponseFileArguments(FileTracker.GetFileTrackerPath(ExecutableType.Native64Bit), TrackerIntermediateDirectory.GetMetadata("FullPath"), trackerRootingMarker, null);

          var trackerResponseFile = Path.GetTempFileName();

          File.WriteAllText(trackerResponseFile, trackerResponseFileArguments, ResponseFileEncoding);

          // /a : Enable extended tracking: GetFileAttributes, GetFileAttributesEx
          // /e : Enable extended tracking: GetFileAttributes, GetFileAttributesEx, RemoveDirectory, CreateDirectory
          // /k : Keep the full tool chain in tlog filenames.
          // /t : Track command lines (will expand response files specified with the '@filename' syntax)
          // (specifying /t will export *.command.*.tlog files)

          trackerCommandLineSwitches = $"{PathUtils.QuoteIfNeeded("@" + trackerResponseFile)} /k {trackerCommandLineSwitches}";
        }

#if DEBUG
        Log.LogMessageFromText($"[{ToolName}] Tracker tool: {trackerToolPath}", MessageImportance.High);

        Log.LogMessageFromText($"[{ToolName}] Tracker command line: {trackerCommandLineSwitches}", MessageImportance.High);
#endif

        //
        //
        //

        responseFileSwitchesBuffer.Replace("\\", "\\\\");

        responseFileSwitchesBuffer.Replace("\\\\\\\\ ", "\\\\ ");

        if (true)
        {
          exitCode = base.ExecuteTool(trackerToolPath, responseFileSwitchesBuffer.ToString(), trackerCommandLineSwitches);
        }
#if false
        else
        {
          string responseFileSwitch = string.Empty;

          if (responseFileSwitchesBuffer.Length > 0)
          {
            string responseFilePath = Path.GetTempFileName();

            File.WriteAllText(responseFilePath, responseFileSwitchesBuffer.ToString(), ResponseFileEncoding);

            responseFileSwitch = GetResponseFileSwitch(responseFilePath);
          }

          using var trackedProcess = new Process();

          trackedProcess.StartInfo = base.GetProcessStartInfo(trackerToolPath, trackerCommandLineSwitches, responseFileSwitch);

          trackedProcess.StartInfo.CreateNoWindow = true;

          trackedProcess.StartInfo.UseShellExecute = false;

          trackedProcess.StartInfo.ErrorDialog = false;

          trackedProcess.StartInfo.RedirectStandardOutput = true;

          trackedProcess.StartInfo.RedirectStandardError = true;

          trackedProcess.OutputDataReceived += (object sender, DataReceivedEventArgs args) => LogEventsFromTextOutput(args.Data, MessageImportance.Low);

          trackedProcess.ErrorDataReceived += (object sender, DataReceivedEventArgs args) => LogEventsFromTextOutput(args.Data, MessageImportance.Low);

          trackedProcess.EnableRaisingEvents = true;

          if (OutputCommandLine)
          {
            Log.LogMessageFromText($"[{ToolName}] Process started: {trackedProcess.StartInfo.FileName} {trackedProcess.StartInfo.Arguments}", MessageImportance.High);
          }

          if (!trackedProcess.Start())
          {
            throw new InvalidOperationException("Could not start tracked child process.");
          }

          trackedProcess.BeginOutputReadLine();

          trackedProcess.BeginErrorReadLine();

          trackedProcess.WaitForExit();

          exitCode = trackedProcess.ExitCode;
        }
#endif
      }
      catch (Exception e)
      {
        Log.LogErrorFromException(e, true);

        exitCode = -1;
      }

      return exitCode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
    {
      base.LogEventsFromTextOutput(singleLine, messageImportance);

      switch (messageImportance)
      {
        case MessageImportance.High:
        case MessageImportance.Normal:
          Serilog.Log.Information(singleLine);
          break;
        case MessageImportance.Low:
          Serilog.Log.Verbose(singleLine);
          break;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual void SetupTrackerLogPaths ()
    {
      //
      // Create tracker tasks for each of the target output files; command, read and write logs.
      //

      try
      {
        var trackerLogDirectory = TrackerLogDirectory.GetMetadata("FullPath");

        Directory.CreateDirectory(trackerLogDirectory);

        if (TLogCommandFiles == null)
        {
          TLogCommandFiles = TLogCommandNames.Select(log => new TaskItem(Path.Combine(trackerLogDirectory, log))).ToArray();
        }

        if (TLogReadFiles == null)
        {
          TLogReadFiles = TLogReadNames.Select(log => new TaskItem(Path.Combine(trackerLogDirectory, log))).ToArray();
        }

        if (TLogWriteFiles == null)
        {
          TLogWriteFiles = TLogWriteNames.Select(log => new TaskItem(Path.Combine(trackerLogDirectory, log))).ToArray();
        }
      }
      catch (Exception e)
      {
        Log.LogWarningFromException(e, true);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected bool ForcedRebuildRequired()
    {
      //
      // If we can't find a cached "command" file, presume we have to force rebuild all sources.
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
      // (MSBuild docs: Returns true if task execution is not necessary. Executed after ValidateParameters)
      //

      SetupTrackerLogPaths();

      if (MinimalRebuildFromTracking && !ForcedRebuildRequired())
      {
        //
        // Check there are actually sources to build, otherwise we can skip execution.
        //

        TrackedOutputFiles = new CanonicalTrackedOutputFiles(this, TLogWriteFiles);

        TrackedInputFiles = new CanonicalTrackedInputFiles(this, TLogReadFiles, InputFiles, ExcludedInputPaths, TrackedOutputFiles, true, false);

        var outOfDateSourcesFromTracking = TrackedInputFiles.ComputeSourcesNeedingCompilation(true);

        var outOfDateSourcesFromCommandLine = GetOutOfDateSourcesFromCmdLineChanges(GenerateUnionFileCommands(), InputFiles);

#if DEBUG
        Log.LogMessageFromText($"[{GetType().Name}] Out of date sources (from tracking): {outOfDateSourcesFromTracking.Length}", MessageImportance.Low);

        Log.LogMessageFromText($"[{GetType().Name}] Out of date sources (command line differs): {outOfDateSourcesFromCommandLine.Count}", MessageImportance.Low);
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

    protected ICollection<ITaskItem> GetOutOfDateSourcesFromCmdLineChanges(string expectedCommandLine, ITaskItem[] uncheckedSources)
    {
      // 
      // Evaluate which (if any) of the `uncheckedSources` are already present in the command TLog.
      // If cached entries are found, identify whether this source should be considered "out of date".
      // 

      if (TLogCommandFiles?.Length == 0)
      {
        return Array.Empty<ITaskItem>();
      }

      var uncheckedSourcesRooted = new Dictionary<string, ITaskItem>();

      foreach (var uncheckedSource in uncheckedSources)
      {
        uncheckedSourcesRooted.Add(FileTracker.FormatRootingMarker(uncheckedSource), uncheckedSource);
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

      return outOfDateSources;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly HashSet<string> _propertiesToIgnore = new HashSet<string>();

    private CommandLineBuilder _commandLineSwitchesBuilder;

    private CommandLineBuilder _responseFileSwitchesBuilder;

    protected void GenerateToolSwitches()
    {
      //
      // Evaluate command line and response file switches, caching them for lookup.
      //

      if (_commandLineSwitchesBuilder == null)
      {
        _commandLineSwitchesBuilder = new CommandLineBuilder();

        GenerateCommandLineSwitches(_commandLineSwitchesBuilder, _propertiesToIgnore);
      }

      if (_responseFileSwitchesBuilder == null)
      {
        _responseFileSwitchesBuilder = new CommandLineBuilder();

        GenerateResponseFileSwitches(_responseFileSwitchesBuilder, _propertiesToIgnore);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override string GenerateCommandLineCommands()
    {
      //
      // Check if there are no response file flags, or if the derived class explicitly turned them off.
      // If true, we need to condense all intended response file switches and return them from this function instead.
      //

      try
      {
        GenerateToolSwitches();

        if (GenerateResponseFileCommands() == string.Empty)
        {
          return GenerateUnionFileCommands();
        }

        return _commandLineSwitchesBuilder.ToString();
      }
      catch (Exception e)
      {
        Log.LogWarningFromException(e, true);
      }

      return string.Empty;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override string GenerateResponseFileCommands()
    {
      GenerateToolSwitches();

      return _responseFileSwitchesBuilder.ToString();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected string GenerateUnionFileCommands()
    {
      GenerateToolSwitches();

      return string.Join(" ", new string[] { _commandLineSwitchesBuilder.ToString(), _responseFileSwitchesBuilder.ToString() }.Where(str => !string.IsNullOrEmpty(str)));
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual string GenerateCommandLineSwitches(CommandLineBuilder builder = default, HashSet<string> propertiesToIgnore = null)
    {
      builder ??= new CommandLineBuilder();

      return builder?.ToString() ?? string.Empty;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual string GenerateResponseFileSwitches(CommandLineBuilder builder = default, HashSet<string> propertiesToIgnore = null)
    {
      builder ??= new CommandLineBuilder();

      propertiesToIgnore ??= new HashSet<string>();

      //
      // Generate a XAML rule based on [Switch...] attributes and any specified properties file.
      //

      Rule rule = new Rule();

      var parser = new CommandLineGenerator();

      if (SwitchAttributes?.Count > 0)
      {
        var attributeBasedProperties = SwitchAttributes.Select(attr => attr.Value.GetProperty());

        foreach (var property in attributeBasedProperties)
        {
          rule.Properties.Add(property);

#if DEBUG
          Log.LogMessage($"[{ToolName}] Added property from {GetType().Name}: {property.Name} | {property.GetType().Name}");
#endif
        }
      }

      if (PropertiesFile != null)
      {
        var xamlFileRule = CommandLineGenerator.LoadXamlRule(PropertiesFile);

        rule.Properties.AddRange(xamlFileRule.Properties);
      }

      //
      // Evaluate parameter types and values for each of those attributed with a [ToolSwitch...] variant.
      //

      var propertyValues = new Dictionary<string, object>();

      try
      {
        foreach (var attribute in SwitchAttributes)
        {
          string name = attribute.Key;

          BaseProperty property = attribute.Value.GetProperty();

          object value = attribute.Value.GetValue();

          propertyValues.Add(name, value);

          Log.LogMessage($"[{ToolName}] Property from {GetType().Name}: {name} | {property.GetType().Name} | {(value == null ? "(null)" : $"{value} ({value.GetType()})")}");
        }

        if (OutOfDateInputFiles?.Length > 0)
        {
          GenerateSwitchesForTaskItem(ref propertyValues, OutOfDateInputFiles[0], rule, propertiesToIgnore);
        }

        //
        // Validate required parameters are present.
        //

        foreach (var prop in rule.Properties)
        {
          if (prop.IsRequired && !propertyValues.ContainsKey(prop.Name))
          {
            Log.LogError($"[{GetType().Name}] Required parameter {prop.Name} has no assigned property value.");
          }
        }

        string commandLine = parser.GenerateCommandLine(rule, propertyValues, propertiesToIgnore);

        builder.AppendTextUnquoted(commandLine);
      }
      catch (Exception e)
      {
        Log.LogErrorFromException(e, true);
      }

      return builder?.ToString() ?? string.Empty;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual void GenerateSwitchesForTaskItem(ref Dictionary<string, object> propertyValues, ITaskItem item, Rule rule, HashSet<string> propertiesToIgnore = null)
    {
      //
      // Per-item switches. Most of these should be informed by property values and the XAML property sheet rules.
      //

      var metadata = item.CloneCustomMetadata();

      if (metadata.Count == 0)
      {
        return;
      }

      foreach (var prop in rule.Properties)
      {
        if (!metadata.Contains(prop.Name))
        {
          continue;
        }

        object value = metadata[prop.Name];

        if (value == null)
        {
          continue; // Early abort is metadata is missing.
        }
        else if (prop is BoolProperty)
        {
          value = CommandLineGenerator.ConvertToObject<bool?>(value);
        }
        else if (prop is IntProperty)
        {
          value = CommandLineGenerator.ConvertToObject<int?>(value);
        }
        else if (prop is EnumProperty)
        {
          value = CommandLineGenerator.ConvertToObject<string>(value);
        }
        else if (prop is StringProperty stringProperty)
        {
          if (string.Equals(stringProperty.Subtype, "file", StringComparison.OrdinalIgnoreCase) || string.Equals(stringProperty.Subtype, "folder"))
          {
            value = CommandLineGenerator.ConvertToObject<ITaskItem>(value);
          }
          else
          {
            value = CommandLineGenerator.ConvertToObject<string>(value);
          }
        }
        else if (prop is StringListProperty stringList)
        {
          var strValue = CommandLineGenerator.ConvertToObject<string>(value);

          if (strValue != null)
          {
            var values = strValue.Trim(new char[] { Path.PathSeparator, ' ' }).Split(new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);

            if (string.Equals(stringList.Subtype, "file", StringComparison.OrdinalIgnoreCase) || string.Equals(stringList.Subtype, "folder") || (string.Equals(stringList.DataSource?.ItemType, "Item", StringComparison.OrdinalIgnoreCase)))
            {
              value = values.Select(str => CommandLineGenerator.ConvertToObject<ITaskItem>(str)).ToArray();
            }
            else if (values != null)
            {
              value = values.ToArray();
            }
          }
          else
          {
            value = strValue;
          }
        }
        else
        {
          throw new NotImplementedException($"{prop.Name} | {prop.GetType().Name} | {(value == null ? "(null)" : value)} ({value?.GetType()})");
        }

#if DEBUG
        Log.LogMessage($"[{ToolName}] Property from metadata: {prop.Name} | {prop.GetType().Name} | {(value == null ? "(null)" : $"{value} ({value.GetType()})")}");

        if (propertyValues.TryGetValue(prop.Name, out object existingValue) && existingValue != null && !Equals(value, existingValue))
        {
          Log.LogWarning($"[{ToolName}] Property {prop.Name} already has an assigned value: {(existingValue == null ? "(null)" : $"{existingValue} ({existingValue.GetType()})")}");

          Log.LogWarning($"[{ToolName}] Property {prop.Name} will be overwritten with source item metadata value: {(value == null ? "(null)" : $"{value} ({value.GetType()})")}");
        }
#endif

        propertyValues[prop.Name] = value;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override string GenerateFullPathToTool ()
    {
      //
      // Gets the fully qualified tool name. Should return ToolExe if ToolTask should search for the tool in the system path. If ToolPath is set, this is ignored.
      //

      if (ToolPath == null)
      {
        return ToolExe; // go fish.
      }

      return Path.Combine (ToolPath, ToolExe);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ITaskItem[] TLogCommandFiles { get; set; }

    public ITaskItem[] TLogReadFiles { get; set; }

    public ITaskItem[] TLogWriteFiles { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual ITaskItem TrackerIntermediateDirectory
    {
      get => TrackerLogDirectory;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual string [] TLogCommandNames
    {
      get
      {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(ToolExe);
        return new string[] { $"{fileNameWithoutExtension}.command.1.tlog" };
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual string [] TLogReadNames
    {
      get
      {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(ToolExe);
        return new string[]
        {
          $"{fileNameWithoutExtension}.read.*.tlog",
          $"{fileNameWithoutExtension}.*.read.*.tlog",
          $"{fileNameWithoutExtension}-*.read.*.tlog",
        };
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual string [] TLogWriteNames
    {
      get
      {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(ToolExe);
        return new string[]
        {
          $"{fileNameWithoutExtension}.write.*.tlog",
          $"{fileNameWithoutExtension}.*.write.*.tlog",
          $"{fileNameWithoutExtension}-*.write.*.tlog",
        };
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual string[] TLogDeleteNames
    {
      get
      {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(ToolExe);
        return new string[]
        {
          $"{fileNameWithoutExtension}.delete.*.tlog",
          $"{fileNameWithoutExtension}.*.delete.*.tlog",
          $"{fileNameWithoutExtension}-*.delete.*.tlog",
        };
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ITaskItem[] ExcludedInputPaths
    {
      get
      {
        return new ITaskItem[]
        {
          new TaskItem(TrackerIntermediateDirectory),
          new TaskItem(Environment.GetFolderPath(Environment.SpecialFolder.System)),
          new TaskItem(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86)),
          new TaskItem(Environment.GetFolderPath(Environment.SpecialFolder.Windows)),
          new TaskItem(Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\\GLOBALIZATION\\SORTING")
        };
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

}
