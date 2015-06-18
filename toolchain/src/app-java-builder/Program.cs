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

namespace app_java_builder
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

    private static string s_compilerOutputPath = string.Empty;

    private static string s_archiverOutputPath = string.Empty;

    private static string s_archiverManifestPath = string.Empty;

    private static List<string> s_compilerArguments = new List<string> ();

    private static List<string> s_archiverArguments = new List<string> ();

    private static HashSet<string> s_compilerInputJavaFiles = new HashSet<string> ();

    private static HashSet<string> s_compilerInputSourcePaths = new HashSet<string> ();

    private static HashSet<string> s_compilerInputClassPaths = new HashSet<string> ();

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

        string workingDirectory = Path.Combine (Path.GetTempPath (), "app-java-builder", Guid.NewGuid ().ToString ());

        if (Directory.Exists (workingDirectory))
        {
          Directory.Delete (workingDirectory);
        }

        Directory.CreateDirectory (workingDirectory);

        // 
        // Run the javac tool to compile any specified files (and indirectory those referenced by source paths).
        // 

        HashSet<string> compilerFilesRead = new HashSet<string> ();

        HashSet<string> compilerFilesWritten = new HashSet<string> ();

        {
          int exitCode = -1;

          string executable = Path.Combine (s_jdkHomePath, "bin", "javac.exe");

          string arguments = string.Join (" ", s_compilerArguments.ToArray ());

#if true
          string sourcesFile = Path.Combine (workingDirectory, "sources.txt");

          using (StreamWriter writer = new StreamWriter (sourcesFile, false))
          {
            foreach (string file in s_compilerInputJavaFiles)
            {
              writer.WriteLine (file);
            }

            writer.Close ();
          }

          string fileArgs = arguments += " @" + sourcesFile;

          {
#else
          foreach (string file in s_compilerInputJavaFiles)
          {
            string fileArgs = arguments + " " + file;
#endif
            using (Process process = CreateSynchronousProcess (executable, fileArgs, string.Empty))
            {
              process.OutputDataReceived += (sender, e) =>
              {
                if (e.Data != null)
                {
                  ProcessJavacOutput (e.Data, ref compilerFilesRead, ref compilerFilesWritten);
                }
              };

              process.ErrorDataReceived += (sender, e) =>
              {
                if (e.Data != null)
                {
                  ProcessJavacOutput (e.Data, ref compilerFilesRead, ref compilerFilesWritten);
                }
              };

              if (process.Start ())
              {
                process.BeginOutputReadLine ();

                process.BeginErrorReadLine ();

                process.WaitForExit ();

                exitCode = process.ExitCode;
              }

              if (exitCode != 0)
              {
                throw new InvalidOperationException (process.StartInfo.FileName + " failed with exit code: " + exitCode);
              }
            }
          }

          // 
          // Export dependency files alongside any exported output.
          // 

          if (exitCode == 0)
          {
            foreach (string file in compilerFilesWritten)
            {
              using (StreamWriter writer = new StreamWriter (file + ".d", false, Encoding.Unicode))
              {
                writer.WriteLine (string.Format ("{0}: \\", ConvertPathWindowsToGccDependency (file)));

                foreach (string dependency in compilerFilesRead)
                {
                  writer.WriteLine (string.Format ("  {0} \\", ConvertPathWindowsToGccDependency (dependency)));
                }

                writer.Close ();
              }
            }
          }

#if true
          File.Delete (sourcesFile);
#endif
        }

        // 
        // Copy exported class files to a temporary directory to ensure packages are handled properly (and old classes are cleaned).
        // 

        {
          long failedParsedClassFiles = 0;

          foreach (string classFile in compilerFilesWritten) // Parallel.ForEach (compilerFilesWritten, classFile =>
          {
#if DEBUG
            Debug.WriteLine ("Processing: " + classFile);
#endif

            try
            {
              string classId = string.Empty;

              using (Stream stream = File.Open (classFile, FileMode.Open))
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

                File.Copy (classFile, targetPath, true);
#if DEBUG
                Debug.WriteLine (string.Format ("Copied: '{0}' to '{1}'", classFile, targetPath));
#endif
              }
            }
            catch (Exception e)
            {
              LogException (e);

              Interlocked.Increment (ref failedParsedClassFiles);
            }
          }

          if (failedParsedClassFiles > 0)
          {
            throw new InvalidOperationException (string.Format ("Failed to parse {0} files.", failedParsedClassFiles));
          }
        }

        // 
        // Run the jar tool to package any compiled class files.
        // 

        HashSet<string> archiverFilesRead = new HashSet<string> ();

        HashSet<string> archiverFilesWritten = new HashSet<string> ();

        if (!string.IsNullOrEmpty (s_archiverOutputPath))
        {
          // 
          // c    create new archive
          // u    update an existing archive
          // f    specify archive file name
          // m    specify manifest file name
          // 0    store only; use no ZIP compression
          // 

          StringBuilder argumentsBuilder = new StringBuilder ();

          StringBuilder argumentsModeBuilder = new StringBuilder ();

          argumentsModeBuilder.Append ("c");

          argumentsModeBuilder.Append ("v");

          argumentsModeBuilder.Append ("f");

          argumentsBuilder.Append (s_archiverOutputPath + " ");

          if (!string.IsNullOrEmpty (s_archiverManifestPath))
          {
            argumentsModeBuilder.Append ("m");

            argumentsBuilder.Append (s_archiverManifestPath + " ");
          }

          argumentsModeBuilder.Append ("0");

          argumentsBuilder.Append ("-C " + QuotePathIfNeeded (workingDirectory) + " . ");

          string executable = Path.Combine (s_jdkHomePath, "bin", "jar.exe");

          string arguments = argumentsModeBuilder.ToString () + " " + argumentsBuilder.ToString ();

          string workingDir = string.Empty;

          int exitCode = -1;

          using (Process process = CreateSynchronousProcess (executable, arguments, workingDir))
          {
            process.OutputDataReceived += (sender, e) =>
            {
              if (e.Data != null)
              {
                ProcessJarOutput (e.Data, ref archiverFilesRead, ref archiverFilesWritten);
              }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
              if (e.Data != null)
              {
                ProcessJarOutput (e.Data, ref archiverFilesRead, ref archiverFilesWritten);
              }
            };

            if (process.Start ())
            {
              process.BeginOutputReadLine ();

              process.BeginErrorReadLine ();

              process.WaitForExit ();

              exitCode = process.ExitCode;
            }

            if (exitCode != 0)
            {
              throw new InvalidOperationException (process.StartInfo.FileName + " failed with exit code: " + exitCode);
            }

            // 
            // Export dependency files alongside any exported output.
            // 

            if (exitCode == 0)
            {
              foreach (string file in compilerFilesWritten)
              {
                using (StreamWriter writer = new StreamWriter (file + ".d", false, Encoding.Unicode))
                {
                  writer.WriteLine (string.Format ("{0}: \\", ConvertPathWindowsToGccDependency (file)));

                  foreach (string dependency in compilerFilesRead)
                  {
                    writer.WriteLine (string.Format ("  {0} \\", ConvertPathWindowsToGccDependency (dependency)));
                  }

                  writer.Close ();
                }
              }
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

    private static void ProcessJavacOutput (string singleLine, ref HashSet<string> filesRead, ref HashSet<string> filesWritten)
    {
#if DEBUG
      Debug.WriteLine (singleLine);
#endif

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

    private static Process CreateSynchronousProcess (string exe, string arguments, string workingDir)
    {
      Process process = new Process ();

      process.StartInfo = new ProcessStartInfo (exe, arguments);

      if (!string.IsNullOrEmpty (workingDir))
      {
        process.StartInfo.WorkingDirectory = workingDir;
      }

      process.StartInfo.CreateNoWindow = true;

      process.StartInfo.UseShellExecute = false;

      process.StartInfo.LoadUserProfile = false;

      process.StartInfo.ErrorDialog = false;

      process.StartInfo.RedirectStandardOutput = true;

      process.StartInfo.RedirectStandardError = true;

      process.StartInfo.RedirectStandardInput = true;

      return process;
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
            if (!string.IsNullOrEmpty (s_jdkHomePath))
            {
              throw new ArgumentException ("--jdk-home argument multiply defined.");
            }

            s_jdkHomePath = args [++i];

            break;
          }

          case "--jar-output":
          {
            if (!string.IsNullOrEmpty (s_archiverOutputPath))
            {
              throw new ArgumentException ("--jar-output argument multiply defined.");
            }

            s_archiverOutputPath = args [++i];

            Directory.CreateDirectory (Path.GetDirectoryName (s_archiverOutputPath));

            break;
          }

          case "--jar-manifest":
          {
            if (!string.IsNullOrEmpty (s_archiverManifestPath))
            {
              throw new ArgumentException ("--jar-manifest argument multiply defined.");
            }

            s_archiverManifestPath = args [++i];

            break;
          }

          default:
          {
            if (args [i].StartsWith ("@"))
            {
              using (StreamReader reader = new StreamReader (args [i].Substring (1)))
              {
                string line = reader.ReadLine ();

                while (!string.IsNullOrEmpty (line))
                {
                  string [] lineArgs = line.Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                  ProcessArguments (lineArgs);

                  line = reader.ReadLine ();
                }

                reader.Close ();
              }
            }
            else if (Path.GetExtension (args [i]).ToLowerInvariant ().Equals (".java"))
            {
              string javaInput = args [i];

              if (!s_compilerInputJavaFiles.Contains (javaInput))
              {
                s_compilerInputJavaFiles.Add (javaInput);
              }
            }
            else if (args [i].StartsWith ("-"))
            {
              s_compilerArguments.Add (args [i]);

              switch (args [i])
              {
                case "-d":
                {
                  s_compilerOutputPath = args [++i];

                  s_compilerArguments.Add (s_compilerOutputPath);

                  break;
                }

                case "-sourcepath":
                {
                  string sourcePath = args [++i];

                  s_compilerArguments.Add (sourcePath);

                  string [] paths = sourcePath.Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                  foreach (string path in paths)
                  {
                    if (!s_compilerInputSourcePaths.Contains (path))
                    {
                      s_compilerInputSourcePaths.Add (path);
                    }
                  }

                  break;
                }

                case "-cp":
                case "-classpath":
                {
                  string classPath = args [++i];

                  s_compilerArguments.Add (classPath);

                  string [] paths = classPath.Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                  foreach (string path in paths)
                  {
                    if (!s_compilerInputClassPaths.Contains (path))
                    {
                      s_compilerInputClassPaths.Add (path);
                    }
                  }

                  break;
                }

                default:
                {
                  // 
                  // Consume any additional two-step arguments.
                  // 

                  if (((i + 1) < args.Length) && (!args [i + 1].StartsWith ("-")))
                  {
                    s_compilerArguments.Add (args [++i]);
                  }

                  break;
                }
              }
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
      // Required
      if (string.IsNullOrWhiteSpace (s_jdkHomePath) || !Directory.Exists (s_jdkHomePath))
      {
        throw new ArgumentException ("--jdk-home references an invalid or non-existent directory: " + s_jdkHomePath);
      }

      // Required
      if (!File.Exists (Path.Combine (s_jdkHomePath, "bin", "javac.exe")))
      {
        throw new ArgumentException ("--jdk-home references an invalid JDK installation. Can not find required 'bin\\javac.exe' tool.");
      }

      // Required
      if (!File.Exists (Path.Combine (s_jdkHomePath, "bin", "jar.exe")))
      {
        throw new ArgumentException ("--jdk-home references an invalid JDK installation. Can not find required 'bin\\jar.exe' tool.");
      }

      // Optional
      if (!string.IsNullOrWhiteSpace (s_archiverManifestPath) && !File.Exists (s_archiverManifestPath))
      {
        throw new ArgumentException ("--jar-manifest references an invalid or non-existent file: " + s_archiverManifestPath);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void LogException (Exception e)
    {
      string exception = string.Format ("[app-java-builder] Encountered exception: [{0}] {1}\nStack trace: {2}", e.GetType ().ToString (), e.Message, e.StackTrace);

      Debug.WriteLine (exception);

      Console.WriteLine (exception);
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
