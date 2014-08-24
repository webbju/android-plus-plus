////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace app_javac_dependencies
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  class Program
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static string s_jdkHomePath = string.Empty;

    private static List<string> s_javacToolArguments = new List<string> ();

    private static HashSet<string> s_sourcePathList = new HashSet<string> ();

    private static HashSet<string> s_sourceFileList = new HashSet<string> ();

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    static int Main (string [] args)
    {
      int returnCode = -1;

      try
      {
        ProcessArguments (args);

        ValidateArguments ();

        HashSet<string> filesRead = new HashSet<string> ();

        HashSet<string> filesWritten = new HashSet<string> ();

        using (Process trackedProcess = new Process ())
        {
          StringBuilder argumentsBuilder = new StringBuilder ();

          argumentsBuilder.Append (string.Join (" ", s_javacToolArguments));

          argumentsBuilder.Append (" ");

          foreach (string sourceFile in s_sourceFileList)
          {
            argumentsBuilder.Append (QuotePathIfNeeded (sourceFile) + " ");
          }

          trackedProcess.StartInfo = new ProcessStartInfo (Path.Combine (s_jdkHomePath, "bin", "javac.exe"), argumentsBuilder.ToString ());

          trackedProcess.StartInfo.UseShellExecute = false;

          trackedProcess.StartInfo.RedirectStandardOutput = true;

          trackedProcess.StartInfo.RedirectStandardError = true;

          trackedProcess.OutputDataReceived += (sender, e) =>
          {
            if (e.Data != null)
            {
              ProcessJavacOutput (e.Data, ref filesRead, ref filesWritten);
            }
          };

          trackedProcess.ErrorDataReceived += (sender, e) =>
          {
            if (e.Data != null)
            {
              ProcessJavacOutput (e.Data, ref filesRead, ref filesWritten);
            }
          };

          if (trackedProcess.Start ())
          {
            trackedProcess.BeginOutputReadLine ();

            trackedProcess.BeginErrorReadLine ();

            trackedProcess.WaitForExit ();

            if (returnCode != 0)
            {
              returnCode = trackedProcess.ExitCode;
            }
          }
        }

        // 
        // Dump a dependency file alongside any of the tool's output classes.
        // 

        if (returnCode == 0)
        {
          foreach (string file in filesWritten)
          {
            using (StreamWriter writer = new StreamWriter (file + ".d", false, Encoding.Unicode))
            {
              writer.WriteLine (string.Format ("{0}: \\", ConvertPathWindowsToGccDependency (file)));

              foreach (string dependency in filesRead)
              {
                writer.WriteLine (string.Format ("  {0} \\", ConvertPathWindowsToGccDependency (dependency)));
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        Debug.WriteLine ("[app-javac-depedencies] Encountered exception: " + e.Message + "\nStack trace: " + e.StackTrace);

        Console.WriteLine ("[app-javac-depedencies] Encountered exception: " + e.Message + "\nStack trace: " + e.StackTrace);

        returnCode = -1;
      }

      return returnCode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void ProcessArguments (string [] args)
    {
      // 
      // All arguments which are not '--jdk-home' or a source-file reference, should be passed on to javac.exe invocation.
      // 

      for (int i = 0; i < args.Length; ++i)
      {
        switch (args [i])
        {
          case "--jdk-home":
          {
            s_jdkHomePath = args [++i];

            break;
          }

          case "-bootclasspath":
          case "-classpath":
          case "-d":
          case "-encoding":
          case "-endorseddirs":
          case "-extdirs":
          case "-processor":
          case "-processorpath":
          case "-s":
          case "-source":
          case "-target":
          {
            if (args [i+1].StartsWith ("-"))
            {
              throw new ArgumentException ("Expected data for argument (" + args [i] + "). Got: " + args [i+1]);
            }

            s_javacToolArguments.Add (string.Format ("{0} {1}", args [i], QuotePathIfNeeded (args [++i])));

            break;
          }

          case "-sourcepath":
          {
            string [] specifiedSourcePaths = args [++i].Split (';');

            foreach (string sourcePath in specifiedSourcePaths)
            {
              if (!s_sourcePathList.Contains (sourcePath))
              {
                s_sourcePathList.Add (sourcePath);
              }
            }

            break;
          }

          default:
          {
            if (args [i].EndsWith (".java"))
            {
              s_sourceFileList.Add (args [i]);
            }
            else
            {
              s_javacToolArguments.Add (args [i]);
            }

            break;
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void ValidateArguments ()
    {
      if (string.IsNullOrWhiteSpace (s_jdkHomePath))
      {
        throw new ArgumentException ("--jdk-home argument not specified. Please reference an existing JDK installation.");
      }

      if (!Directory.Exists (s_jdkHomePath))
      {
        throw new ArgumentException ("--jdk-home references an invalid or non-existent directory.");
      }

      if (!File.Exists (Path.Combine (s_jdkHomePath, "bin", "javac.exe")))
      {
        throw new ArgumentException ("--jdk-home references an invalid JDK installation. Can not find required 'bin\\javac.exe' tool.");
      }

      if (s_sourceFileList.Count == 0)
      {
        throw new ArgumentException ("No .java source files provided.");
      }

      if (s_sourcePathList.Count > 0)
      {
        s_javacToolArguments.Add ("-sourcepath " + string.Join (";", s_sourcePathList.ToArray ()));
      }

      // 
      // This pass-through tool required verbose mode in order to identify source dependencies, turn it on if it isn't already.
      // 

      if (!s_javacToolArguments.Contains ("-verbose"))
      {
        s_javacToolArguments.Add ("-verbose");
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void ProcessJavacOutput (string singleLine, ref HashSet<string> filesRead, ref HashSet<string> filesWritten)
    {
      Debug.WriteLine (singleLine);

      Console.WriteLine (singleLine);

      if (string.IsNullOrWhiteSpace (singleLine))
      {
        return;
      }
      else if (singleLine.StartsWith ("["))
      {
        string sanitisedOutput = singleLine.Trim (new char [] { ' ', '[', ']' });

        if (sanitisedOutput.StartsWith ("parsing started "))
        {
          // 
          // Handle parsing of a source file provided through the command-line.
          // 

          string fileLoaded = sanitisedOutput.Substring ("parsing started ".Length);

          fileLoaded = StripFileObjectDescriptor (fileLoaded);

          if (!filesRead.Contains (fileLoaded))
          {
            filesRead.Add (fileLoaded);
          }
        }
        else if (sanitisedOutput.StartsWith ("search path for class files: "))
        {
          // 
          // Only flag archives (.jar) as valid dependencies from the class path. We can track .class file loading via '[loading ...]' output.
          // 

          string [] classPath = sanitisedOutput.Substring ("search path for class files: ".Length).Split (',');

          foreach (string file in classPath)
          {
            if (file.EndsWith (".jar") && !file.Contains (s_jdkHomePath))
            {
              if (!filesRead.Contains (file))
              {
                filesRead.Add (file);
              }
            }
          }
        }
        else if (sanitisedOutput.StartsWith ("loading "))
        {
          // 
          // Tracking of per-file classes referenced through the class path.
          // 

          string fileLoaded = sanitisedOutput.Substring ("loading ".Length);

          fileLoaded = StripFileObjectDescriptor (fileLoaded);

          if (fileLoaded.EndsWith (".class") || fileLoaded.EndsWith (".java"))
          {
            if (!filesRead.Contains (fileLoaded))
            {
              filesRead.Add (fileLoaded);
            }
          }
        }
        else if (sanitisedOutput.StartsWith ("wrote "))
        {
          string fileWritten = sanitisedOutput.Substring ("wrote ".Length);

          fileWritten = StripFileObjectDescriptor (fileWritten);

          if (!filesWritten.Contains (fileWritten))
          {
            filesWritten.Add (fileWritten);
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static string StripFileObjectDescriptor (string fileObjectDescription)
    {
      // 
      // Convert from JDK 7-style verbose file output to the raw filename.
      // 
      // e.g: [wrote RegularFileObject[..\..\build\obj\bin\classes\com\google\android\gms\R$attr.class]]
      // 

      int filenameStart = fileObjectDescription.LastIndexOf ('[');

      if (filenameStart != -1)
      {
        fileObjectDescription = fileObjectDescription.Substring (filenameStart).Trim (new char [] { '[', ']' });
      }

      return fileObjectDescription;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static string ConvertPathWindowsToGccDependency (string path)
    {
      string rtn = path.Replace ('\\', '/');

      return Escape (rtn);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static string Escape (string input)
    {
      StringBuilder escapedStringBuilder = new StringBuilder (input);

      escapedStringBuilder.Replace (@"\", @"\\");

      escapedStringBuilder.Replace (@" ", @"\ ");

      return escapedStringBuilder.ToString ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static string QuotePathIfNeeded (string arg)
    {
      // 
      // Add quotes around a string, if they are needed.
      // 

      var match = arg.IndexOfAny (new char [] { ' ', '\t', ';', '&' }) != -1;

      if (!match)
      {
        return arg;
      }

      return "\"" + arg + "\"";
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
