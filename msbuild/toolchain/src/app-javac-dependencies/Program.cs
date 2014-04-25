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

    private static Dictionary <string, List <string>> s_sourceDependencyList = new Dictionary<string,List<string>> ();

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

              returnCode = trackedProcess.ExitCode;
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
          case "-sourcepath":
          case "-target":
          {
            if (args [i+1].StartsWith ("-"))
            {
              throw new ArgumentException ("Expected data for argument (" + args [i] + "). Got: " + args [i+1]);
            }

            s_javacToolArguments.Add (string.Format ("{0} {1}", args [i], QuotePathIfNeeded (args [++i])));

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
            writer.WriteLine (string.Format ("{0}: \\", classFileWritten.Replace (" ", "\\ ")));

            foreach (string dependency in sourceKeyPair.Value)
            {
              writer.WriteLine (string.Format ("  {0} \\", dependency.Replace (" ", "\\ ")));
            }
          }
        }
      }
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
