////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using AndroidPlusPlus.Common;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.DeployTasks
{

  public class AndroidNdkDepends : Task
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidNdkDepends ()
      : base (new ResourceManager ("AndroidPlusPlus.MsBuild.DeployTasks.Properties.Resources", Assembly.GetExecutingAssembly ()))
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [Required]
    public string NdkDependsTool { get; set; }

    [Required]
    public ITaskItem [] TargetElfFiles { get; set; }

    [Required]
    public ITaskItem [] LibrarySearchPaths { get; set; }

    [Required]
    public bool Verbose { get; set; }

    [Output]
    public ITaskItem [] DependentLibraries { get; set; }

    [Output]
    public ITaskItem [] DependentSystemLibraries { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override bool Execute ()
    {
      try
      {
        if (!File.Exists (NdkDependsTool))
        {
          throw new ArgumentNullException ("Failed to find 'ndk-depends.exe'. Tried: " + NdkDependsTool);
        }

        // 
        // --help|-h|-?    Print this message.
        // --verbose       Increase verbosity.
        // --print-direct  Only print direct dependencies.
        // -L<path>        Append <path> to the library search path.
        // --host-libs     Append host library search path.
        // --print-paths   Print full paths of all libraries.
        // --print-java    Print Java library load sequence.
        // --print-dot     Print the dependency graph as a Graphviz .dot file.
        // 

        StringBuilder commandLineBuilder = new StringBuilder ();

        commandLineBuilder.Append ("--print-direct ");

        commandLineBuilder.Append ("--print-paths ");

        commandLineBuilder.Append ("--host-libs ");

        if (Verbose)
        {
          commandLineBuilder.Append ("--verbose ");
        }

        foreach (ITaskItem libraryPath in LibrarySearchPaths)
        {
          commandLineBuilder.Append ("-L" + PathUtils.SantiseWindowsPath (libraryPath.GetMetadata ("FullPath")) + " ");
        }

        foreach (ITaskItem targetElf in TargetElfFiles)
        {
          commandLineBuilder.Append (PathUtils.SantiseWindowsPath (targetElf.GetMetadata ("FullPath")) + " ");
        }

        // 
        // Skim tool output to extract required dependency paths.
        // 

        int returnCode = -1;

        bool receivedErrorOutput = false;

        List<ITaskItem> depedentLibraries = new List<ITaskItem> ();

        List<ITaskItem> dependentSystemLibraries = new List<ITaskItem> ();

        using (Process trackedProcess = new Process ())
        {
          trackedProcess.StartInfo = new ProcessStartInfo (NdkDependsTool, commandLineBuilder.ToString ());

          trackedProcess.StartInfo.UseShellExecute = false;

          trackedProcess.StartInfo.RedirectStandardOutput = true;

          trackedProcess.StartInfo.RedirectStandardError = true;

          trackedProcess.OutputDataReceived += (sender, e) =>
          {
            if (!string.IsNullOrWhiteSpace (e.Data))
            {
              //
              // libSDL2.so -> L:\dev\projects\android-plus-plus\msbuild\samples\Android++\Debug\armeabi-v7a/libSDL2.so
              // libstdc++.so -> $ /system/lib/libstdc++.so
              // libSDL2.so -> !! Could not find library
              // 

              if (Verbose)
              {
                Log.LogMessageFromText (string.Format ("[{0}] {1}", "AndroidNdkDepends", e.Data), MessageImportance.High);
              }

              if (e.Data.Contains (" -> "))
              {
                string [] splitDependencyEntry = e.Data.Split (new string [] {" -> "}, StringSplitOptions.None);

                if (splitDependencyEntry [1].StartsWith ("!!"))
                {
                  Log.LogError ("Could not evaluate path for dependency: '" + splitDependencyEntry [0] + "'.");
                }
                else if (splitDependencyEntry [1].StartsWith ("$")) // Android system library.
                {
                  string dependency = splitDependencyEntry [1].Substring (2);

                  dependentSystemLibraries.Add (new TaskItem (dependency));
                }
                else
                {
                  string dependency = splitDependencyEntry [1];

                  depedentLibraries.Add (new TaskItem (dependency));
                }
              }
            }
          };

          trackedProcess.ErrorDataReceived += (sender, e) =>
          {
            if (!string.IsNullOrWhiteSpace (e.Data))
            {
              Log.LogError (e.Data);

              receivedErrorOutput = true;
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

        DependentLibraries = depedentLibraries.ToArray ();

        DependentSystemLibraries = dependentSystemLibraries.ToArray ();

        return (returnCode == 0) && (!receivedErrorOutput);
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

  }

}
