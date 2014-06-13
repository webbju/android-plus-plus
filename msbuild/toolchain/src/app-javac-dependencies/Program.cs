////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
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

    private static List<string> s_sourcePathList = new List<string> ();

    private static Dictionary <string, List <string>> s_sourceDependencyList = new Dictionary<string,List<string>> ();

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    static int Main (string [] args)
    {
      int returnCode = 0;

      try
      {
        ProcessArguments (args);

        ValidateArguments ();

        foreach (KeyValuePair<string, List<string>> sourceKeyPair in s_sourceDependencyList)
        {
          using (Process trackedProcess = new Process ())
          {
            string arguments = string.Join (" ", s_javacToolArguments.ToArray ()) + " " + QuotePathIfNeeded (sourceKeyPair.Key);

            trackedProcess.StartInfo = new ProcessStartInfo (Path.Combine (s_jdkHomePath, "bin", "javac.exe"), arguments);

            trackedProcess.StartInfo.UseShellExecute = false;

            trackedProcess.StartInfo.RedirectStandardOutput = true;

            trackedProcess.StartInfo.RedirectStandardError = true;

            trackedProcess.OutputDataReceived += (sender, e) =>
            {
              if (!string.IsNullOrWhiteSpace (e.Data))
              {
                ProcessJavacOutput (e.Data, sourceKeyPair);
              }
            };

            trackedProcess.ErrorDataReceived += (sender, e) =>
            {
              if (!string.IsNullOrWhiteSpace (e.Data))
              {
                ProcessJavacOutput (e.Data, sourceKeyPair);
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
      // All arguments which are not '--jdk-home' or a source-file reference, should be passed on to javac invocation.
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
              s_sourceDependencyList.Add (args [i], new List<string> ());
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

      // 
      // Iterate source files and evaluate common 'source paths'.
      // 

#if FALSE
      foreach (KeyValuePair<string, List<string>> sourceKeyPair in s_sourceDependencyList)
      {
        string sourcePath = sourceKeyPair.Key;

        using (StreamReader reader = new StreamReader (sourcePath))
        {
          string line = reader.ReadLine ();

          int packageStart = -1;

          int packageEnd = -1;

          while (line != null)
          {
            packageStart = line.IndexOf ("package ");

            if (packageStart != -1)
            {
              packageEnd = line.IndexOf (';', packageStart);
            }

            if (packageEnd != -1)
            {
              // 
              // Identified the source's package address, evaluate it's source-path 'root' directory.
              // 

              int substringIndexStart = packageStart + "package ".Length;

              int substringLength = packageEnd - substringIndexStart;

              string package = line.Substring (substringIndexStart, substringLength).Trim (new char [] { ' ', ';', '\n', '\r' });

              string sourceRootWithoutPackage = sourcePath.Replace (package.Replace ('.', '\\'), "");

              if (!s_sourcePathList.Contains (sourceRootWithoutPackage))
              {
                s_sourcePathList.Add (sourceRootWithoutPackage);
              }

              line = null;
            }
            else
            {
              line = reader.ReadLine ();
            }
          }
        }
      }
#endif

      s_javacToolArguments.Add ("-sourcepath " + string.Join (";", s_sourcePathList.ToArray ()));

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

    private static void ProcessJavacOutput (string singleLine, KeyValuePair<string, List<string>> sourceKeyPair)
    {
      Debug.WriteLine (singleLine);

      Console.WriteLine (singleLine);

      if (singleLine.StartsWith ("["))
      {
        string sanitisedOutput = singleLine.Trim (new char [] { ' ', '[', ']' });

        if (sanitisedOutput.StartsWith ("parsing started "))
        {
          // 
          // Handle parsing of a source file provided through the command-line.
          // 

          string classLoaded = sanitisedOutput.Substring ("parsing started ".Length);

          if (!sourceKeyPair.Value.Contains (classLoaded))
          {
            sourceKeyPair.Value.Add (classLoaded);
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
              if (!sourceKeyPair.Value.Contains (file))
              {
                sourceKeyPair.Value.Add (file);
              }
            }
          }
        }
        else if (sanitisedOutput.StartsWith ("loading "))
        {
          // 
          // Tracking of per-file classes referenced through the class path.
          // 

          string classLoaded = sanitisedOutput.Substring ("loading ".Length);

          if ((classLoaded.EndsWith (".class") || classLoaded.EndsWith (".java")) && !sourceKeyPair.Value.Contains (classLoaded))
          {
            sourceKeyPair.Value.Add (classLoaded);
          }
        }
        else if (sanitisedOutput.StartsWith ("wrote "))
        {
          // 
          // Dump a dependency file alongside any of the tool's output classes.
          // 

          string classFileWritten = Path.GetFullPath (sanitisedOutput.Substring ("wrote ".Length));

          using (StreamWriter writer = new StreamWriter (classFileWritten + ".d", false, Encoding.Unicode))
          {
            writer.WriteLine (string.Format ("{0}: \\", ConvertPathWindowsToGccDependency (classFileWritten)));

            foreach (string dependency in sourceKeyPair.Value)
            {
              writer.WriteLine (string.Format ("  {0} \\", ConvertPathWindowsToGccDependency (dependency)));
            }
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string ConvertPathWindowsToGccDependency (string path)
    {
      string rtn = path.Replace ('\\', '/');

      return Escape (path);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string Escape (string input)
    {
      StringBuilder escapedStringBuilder = new StringBuilder (input);

      escapedStringBuilder.Replace (@"\", @"\\");

      escapedStringBuilder.Replace (@" ", @"\ ");

      return escapedStringBuilder.ToString ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string QuotePathIfNeeded (string arg)
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
