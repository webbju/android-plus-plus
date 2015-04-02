////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace app_jar_dependencies
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

    private static string s_jarToolOutputFile = string.Empty;

    private static string s_jarToolManifestFile = string.Empty;

    private static HashSet<string> s_classFileList = new HashSet<string> ();

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

        // 
        // Copy provided class files to a temporary directory to ensure packages are handled properly.
        // 

        string workingDirectory = Path.Combine (Path.GetTempPath (), "app-jar-dependencies", Guid.NewGuid ().ToString ());

        if (Directory.Exists (workingDirectory))
        {
          Directory.Delete (workingDirectory, true);
        }

        long failedParsedClassFiles = 0;

#if false
        Parallel.ForEach (s_classFileList, classFilePath =>
#else
        foreach (string classFilePath in s_classFileList)
#endif
        {
          try
          {
#if DEBUG
            Log ("Processing: " + classFilePath);
#endif

            string classId = string.Empty;

            using (Stream stream = File.Open (classFilePath, FileMode.Open))
            {
              using (BinaryReader reader = new BinaryReader (stream))
              {
                JavaClassParser.ClassFile processedClassFile = new JavaClassParser.ClassFile (reader);

                JavaClassParser.ConstantClassInfo thisClassInfo = (JavaClassParser.ConstantClassInfo) processedClassFile.constant_pool [processedClassFile.this_class];

                JavaClassParser.ConstantUtf8Info thisClassId = (JavaClassParser.ConstantUtf8Info) processedClassFile.constant_pool [thisClassInfo.name_index];

                classId = Encoding.UTF8.GetString (thisClassId.bytes);

                reader.Close ();
              }

              stream.Close ();
            }

            if (!string.IsNullOrWhiteSpace (classId))
            {
              string classPackage = classId.Substring (0, classId.LastIndexOf ('/'));

              string className = classId.Substring (classId.LastIndexOf ('/') + 1);

              string classPackageAsDir = Path.Combine (workingDirectory, classPackage);

              Directory.CreateDirectory (classPackageAsDir);

              string targetPath = string.Format ("{0}/{1}.class", classPackageAsDir, className);

              File.Copy (classFilePath, targetPath, true);
#if DEBUG
              Log (string.Format ("Copied: '{0}' to '{1}'", classFilePath, targetPath));
#endif
            }
          }
          catch (Exception e)
          {
            LogException (e);

            Interlocked.Increment (ref failedParsedClassFiles);
          }
#if false
        });
#else
        }
#endif

        if (failedParsedClassFiles > 0)
        {
          throw new InvalidOperationException (string.Format ("Failed to parse {0} files.", failedParsedClassFiles));
        }

        // 
        // c    create new archive
        // u    update an existing archive
        // f    specify archive file name
        // m    specify manifest file name
        // 0    store only; use no ZIP compression
        // 

        StringBuilder argumentsBuilder = new StringBuilder ();

        StringBuilder argumentsModeBuilder = new StringBuilder ();

#if false
        if (File.Exists (s_jarToolOutputFile))
        {
          argumentsModeBuilder.Append ("u");
        }
        else
#endif
        {
          argumentsModeBuilder.Append ("c");
        }

        argumentsModeBuilder.Append ("v");

        argumentsModeBuilder.Append ("f");

        argumentsBuilder.Append (s_jarToolOutputFile + " ");

        if (!string.IsNullOrEmpty (s_jarToolManifestFile))
        {
          argumentsModeBuilder.Append ("m");

          argumentsBuilder.Append (s_jarToolManifestFile + " ");
        }

        argumentsModeBuilder.Append ("0");

        argumentsBuilder.Append ("-C " + QuotePathIfNeeded (workingDirectory) + " . ");

        string compositeArguments = argumentsModeBuilder.ToString () + " " + argumentsBuilder.ToString ();

        // 
        // Create a jar based on the contents of the intermediate working directory.
        // 

        HashSet<string> filesRead = new HashSet<string> ();

        HashSet<string> filesWritten = new HashSet<string> ();

        filesWritten.Add (s_jarToolOutputFile);

        using (Process trackedProcess = new Process ())
        {
          trackedProcess.StartInfo = new ProcessStartInfo (Path.Combine (s_jdkHomePath, "bin", "jar.exe"), compositeArguments);

          trackedProcess.StartInfo.CreateNoWindow = true;

          trackedProcess.StartInfo.UseShellExecute = false;

          trackedProcess.StartInfo.LoadUserProfile = false;

          trackedProcess.StartInfo.ErrorDialog = false;

          trackedProcess.StartInfo.RedirectStandardOutput = true;

          trackedProcess.StartInfo.RedirectStandardError = true;

          trackedProcess.StartInfo.RedirectStandardInput = true;

          trackedProcess.OutputDataReceived += (sender, e) =>
          {
            if (e.Data != null)
            {
              ProcessJarOutput (e.Data, ref filesRead, ref filesWritten);
            }
          };

          trackedProcess.ErrorDataReceived += (sender, e) =>
          {
            if (e.Data != null)
            {
              ProcessJarOutput (e.Data, ref filesRead, ref filesWritten);
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
        // Dump a dependency file alongside any of the tool's output archive.
        // 

        if (returnCode == 0)
        {
          foreach (string output in filesWritten)
          {
            using (StreamWriter writer = new StreamWriter (output + ".d", false, Encoding.Unicode))
            {
              writer.WriteLine (string.Format ("{0}: \\", ConvertPathWindowsToGccDependency (output)));

              foreach (string dependency in s_classFileList)
              {
                writer.WriteLine (string.Format ("  {0} \\", ConvertPathWindowsToGccDependency (dependency)));
              }

              writer.Close ();
            }
          }
        }

        if (Directory.Exists (workingDirectory))
        {
          Directory.Delete (workingDirectory, true);
        }
      }
      catch (Exception e)
      {
        LogException (e);

        returnCode = -1;
      }

      return returnCode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void ProcessArguments (string [] args)
    {
      for (int i = 0; i < args.Length; ++i)
      {
        switch (args [i])
        {
          case "--jdk-home":
          {
            s_jdkHomePath = args [++i];

            break;
          }

          case "--jar-output":
          {
            s_jarToolOutputFile = args [++i];

            Directory.CreateDirectory (Path.GetDirectoryName (s_jarToolOutputFile));

            break;
          }

          case "--jar-manifest":
          {
            s_jarToolManifestFile = args [++i];

            break;
          }

          default:
          {
            if (args [i].StartsWith ("@"))
            {
              using (StreamReader reader = new StreamReader (args [i].Substring (1)))
              {
                string line = reader.ReadLine ();

                int startIndex = 0;

                while ((line.Length > 1) && !string.IsNullOrWhiteSpace (line))
                {
                  int end = FindEndOfFilename (line, startIndex);

                  if ((end - startIndex) == 0)
                  {
                    break;
                  }

                  string filename = line.Substring (startIndex, end - startIndex);

                  if (string.IsNullOrWhiteSpace (filename))
                  {
                    break;
                  }

                  s_classFileList.Add (filename);

                  startIndex = end + 1;
                }
              }
            }
            else if (Path.GetExtension (args [i]).ToLowerInvariant ().Equals (".class"))
            {
              s_classFileList.Add (args [i]);
            }
            else
            {
              throw new InvalidOperationException ("Provided source files must use '.class' extension.");
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

      if (!File.Exists (Path.Combine (s_jdkHomePath, "bin", "jar.exe")))
      {
        throw new ArgumentException ("--jdk-home references an invalid JDK installation. Can not find required 'bin\\jar.exe' tool.");
      }

      if (string.IsNullOrWhiteSpace (s_jarToolOutputFile))
      {
        throw new ArgumentException ("--jar-output argument not specified. Please provide an output file.");
      }

      if (!string.IsNullOrWhiteSpace (s_jarToolManifestFile) && !File.Exists (s_jarToolManifestFile))
      {
        throw new ArgumentException ("--jar-manifest references an invalid or non-existent file.");
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void ProcessJarOutput (string singleLine, ref HashSet<string> filesRead, ref HashSet<string> filesWritten)
    {
      if (string.IsNullOrWhiteSpace (singleLine))
      {
        return;
      }

#if DEBUG
      Debug.WriteLine (singleLine);
#endif

      Console.WriteLine (singleLine);

      if (singleLine.StartsWith ("adding:"))
      {
        string fileRead = singleLine.Substring ("adding: ".Length);

        int index = fileRead.IndexOf ('(');

        if (index == -1)
        {
          return;
        }

        fileRead = fileRead.Substring (0, index);

        if (fileRead.EndsWith ("\\") || fileRead.EndsWith ("/"))
        {
          return;
        }

        if (!filesRead.Contains (fileRead))
        {
          filesRead.Add (fileRead);
        }
      }
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

    private static int FindEndOfFilename (string line, int startIndex)
    {
      // 
      // Search line for an unescaped space character (which represents the end of file), or EOF.
      // 

      int index = startIndex;

      while (index != -1)
      {
        index = line.IndexOfAny (new char [] { ' ' }, index);

        if (index == -1)
        {
          break;
        }
        else if (line [index - 1] != '\\')
        {
          return index;
        }
      }

      return line.Length;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void Log (string log)
    {
#if DEBUG
      Debug.WriteLine ("[app-jar-dependencies] " + log);
#endif
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void LogException (Exception e)
    {
#if DEBUG
      Debug.WriteLine ("[app-jar-depedencies] Encountered exception: " + e.Message + "\nStack trace: " + e.StackTrace);
#endif

      Console.WriteLine ("[app-jar-depedencies] Encountered exception: " + e.Message + "\nStack trace: " + e.StackTrace);
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
