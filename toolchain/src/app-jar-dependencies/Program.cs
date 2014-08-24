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
using System.Text.RegularExpressions;

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

        Parallel.ForEach (s_classFileList, classFile =>
        //foreach (string classFile in s_classFileList)
        {
          try
          {
            string packageName;

            string className;

            Debug.WriteLine ("Processing: " + classFile);

            if (!GetPackageNameFromClassFile (classFile, out packageName, out className))
            {
              throw new InvalidOperationException ("Could not evaluate package and class names from source class file.");
            }

            string packageNameAsDir = packageName.Replace (".", "/");

            packageNameAsDir = Path.Combine (workingDirectory, packageNameAsDir);

            Directory.CreateDirectory (packageNameAsDir);

            //Debug.WriteLine ("packageNameAsDir: " + packageNameAsDir);

            //Debug.WriteLine ("className: " + className);

            File.Copy (classFile, string.Format ("{0}/{1}.class", packageNameAsDir, className));
          }
          catch (Exception e)
          {
            LogException (e);
          }
        });

        // 
        // c    create new archive
        // u    update an existing archive
        // f    specify archive file name
        // m    specify manifest file name
        // 0    store only; use no ZIP compression
        // 

        StringBuilder argumentsBuilder = new StringBuilder ();

        StringBuilder argumentsModeBuilder = new StringBuilder ();

        if (File.Exists (s_jarToolOutputFile))
        {
          argumentsModeBuilder.Append ("u");
        }
        else
        {
          argumentsModeBuilder.Append ("c");
        }

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

        using (Process trackedProcess = new Process ())
        {
          trackedProcess.StartInfo = new ProcessStartInfo (Path.Combine (s_jdkHomePath, "bin", "jar.exe"), compositeArguments);

          trackedProcess.StartInfo.UseShellExecute = false;

          trackedProcess.StartInfo.RedirectStandardOutput = true;

          trackedProcess.StartInfo.RedirectStandardError = true;

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
          using (StreamWriter writer = new StreamWriter (s_jarToolOutputFile + ".d", false, Encoding.Unicode))
          {
            writer.WriteLine (string.Format ("{0}: \\", ConvertPathWindowsToGccDependency (s_jarToolOutputFile)));

            foreach (string dependency in s_classFileList)
            {
              writer.WriteLine (string.Format ("  {0} \\", ConvertPathWindowsToGccDependency (dependency)));
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
                string fileContents = reader.ReadToEnd ();

                fileContents = fileContents.Replace ("\r", "").Replace ("\n", " ").Trim (); // pad new lines with spaces

                while (fileContents.Length > 0)
                {
                  int end = FindEndOfFilename (fileContents);

                  string filename = fileContents.Substring (0, end);

                  if (!string.IsNullOrWhiteSpace (filename))
                  {
                    s_classFileList.Add (filename.Trim ());
                  }

                  if (end == fileContents.Length)
                  {
                    break;
                  }

                  fileContents = fileContents.Substring (end + 1);
                }
              }
            }
            else if (Path.GetExtension (args [i]).ToLowerInvariant () == ".class")
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
      Debug.WriteLine (singleLine);

      Console.WriteLine (singleLine);

      if (string.IsNullOrWhiteSpace (singleLine))
      {
        return;
      }
      else if (singleLine.StartsWith ("adding:"))
      {
        string fileRead = singleLine.Substring ("adding: ".Length);

        fileRead = fileRead.Substring (0, fileRead.IndexOf ('('));

        if (!filesRead.Contains (fileRead))
        {
          filesRead.Add (fileRead);
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static bool GetPackageNameFromClassFile (string classFilePath, out string packageAddress, out string className)
    {
      // 
      // Using the javap.exe tool; extract the package address of the given class file.
      // 

      packageAddress = string.Empty;

      className = string.Empty;

      if (!File.Exists (classFilePath))
      {
        throw new FileNotFoundException ("Could not locate specified class file: " + classFilePath);
      }

      using (Process javapProcess = new Process ())
      {
        string classFileRootPath = Path.GetDirectoryName (classFilePath);

        string classFileClassName = Path.GetFileNameWithoutExtension (classFilePath);

        string arguments = string.Format ("-package -classpath {0} {1}", classFileRootPath, classFileClassName);

        javapProcess.StartInfo = new ProcessStartInfo (Path.Combine (s_jdkHomePath, "bin", "javap.exe"), arguments);

        javapProcess.StartInfo.UseShellExecute = false;

        javapProcess.StartInfo.RedirectStandardOutput = true;

        javapProcess.StartInfo.RedirectStandardError = true;

        if (javapProcess.Start ())
        {
          string output = javapProcess.StandardOutput.ReadToEnd ();

          javapProcess.WaitForExit ();

          if (javapProcess.ExitCode == 0)
          {
            // 
            // Parse javap.exe reported class prototype. From this we can get package address and class name.
            // 
            //  public [fina] class package.address.ClassName extends java.lang.Object{
            //  public interface package.address.ClassName implements package.address.AnInterface{
            //  abstract class package.address.ClassName<T> implements package.address.AnInterface{
            // 

            string [] lines = output.Replace ("\r", "").Split ('\n');

            string pattern = @"^(public |private |protected )?(final |abstract )?(class|interface)+ (?<prototype>[^ \{]*)";

            Regex regExMatcher = new Regex (pattern, RegexOptions.Compiled);

            for (int i = 0; i < lines.Length; ++i)
            {
              Match regExMatch = regExMatcher.Match (lines [i]);

              if (regExMatch.Success)
              {
                string prototype = regExMatch.Result ("${prototype}");

                string [] prototypeComponents = prototype.Split (new string [] { "." }, StringSplitOptions.None);

                string classSignature = prototypeComponents [prototypeComponents.Length - 1];

                packageAddress = string.Join (".", prototypeComponents, 0, prototypeComponents.Length - 1);

                // 
                // Return santisied class name; removing template/abstract types.
                // 

                className = classSignature;

                int classSignatureTemplateIndex = classSignature.IndexOf ('<');

                if (classSignatureTemplateIndex != -1)
                {
                  className = classSignature.Substring (0, classSignatureTemplateIndex);
                }

                return true;
              }
            }
          }
        }
      }

      return false;
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

    private static int FindEndOfFilename (string line)
    {
      // 
      // Search line for an unescaped space character (which represents the end of file), or EOF.
      // 

      int i;

      bool escapedSequence = false;

      for (i = 0; i < line.Length; ++i)
      {
        if (line [i] == '\\')
        {
          escapedSequence = true;
        }
        else if ((line [i] == ' ') && !escapedSequence)
        {
          break;
        }
        else if (escapedSequence)
        {
          escapedSequence = false;
        }
      }

      return i;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void LogException (Exception e)
    {
      Debug.WriteLine ("[app-jar-depedencies] Encountered exception: " + e.Message + "\nStack trace: " + e.StackTrace);

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
