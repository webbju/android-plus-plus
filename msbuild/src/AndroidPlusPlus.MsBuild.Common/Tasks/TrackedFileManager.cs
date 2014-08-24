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

  public class TrackedFileManager
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private Dictionary<string, HashSet<string>> m_sourceDependencyTable;

    private Encoding m_defaultEncoding = Encoding.Unicode;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public TrackedFileManager ()
    {
      m_sourceDependencyTable = new Dictionary<string, HashSet<string>> ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ImportFromExistingTLog (ITaskItem tlog)
    {
      // 
      // Parse and collate a TLog. It's best to achieve this by associating dependancy graph 'entries' with associated sources.
      // 
      // Format:
      // 
      //    ^FILE1.C
      //    FILE1.OBJ
      //    ^FILE2.C|FILE3.C
      //    FILE2.OBJ
      //    FILE3.OBJ
      // 

      if (tlog == null)
      {
        throw new ArgumentNullException ("tlog");
      }

      string tlogFullPath = (!string.IsNullOrEmpty (tlog.GetMetadata ("FullPath")) ? tlog.GetMetadata ("FullPath") : Path.GetFullPath (tlog.ItemSpec));

      if (string.IsNullOrEmpty (tlogFullPath))
      {
        throw new ArgumentException ("Could not get 'FullPath' metadata for TLog. " + tlog);
      }

      if (File.Exists (tlogFullPath))
      {
        try
        {
          using (StreamReader reader = new StreamReader (tlogFullPath, m_defaultEncoding))
          {
            if (reader != null)
            {
              string trackedSourceLineData = reader.ReadLine ();

              while (!string.IsNullOrWhiteSpace (trackedSourceLineData))
              {
                if (trackedSourceLineData.StartsWith ("^"))
                {
                  // 
                  // Encountered a canonical source root node. Add each of the sources referenced here to the dependency graph.
                  // 

                  HashSet<string> trackedSources = new HashSet<string> (trackedSourceLineData.Substring (1).ToUpperInvariant ().Split ('|'));

                  foreach (string source in trackedSources)
                  {
                    if (!m_sourceDependencyTable.ContainsKey (ConvertToTrackerFormat (source)))
                    {
                      m_sourceDependencyTable.Add (ConvertToTrackerFormat (source), new HashSet<string> ());
                    }
                  }

                  // 
                  // Parse the next line, if it contains source dependencies process them - otherwise handle a new root node.
                  // 

                  trackedSourceLineData = reader.ReadLine ();

                  if (string.IsNullOrWhiteSpace (trackedSourceLineData) || trackedSourceLineData.StartsWith ("^"))
                  {
                    continue;
                  }

                  HashSet<string> trackedSourceDependencies = new HashSet<string> ();

                  while (trackedSourceLineData != null)
                  {
                    if (trackedSourceLineData.StartsWith ("^"))
                    {
                      break;
                    }

                    if (!string.IsNullOrWhiteSpace (trackedSourceLineData))
                    {
                      if (!trackedSourceDependencies.Contains (ConvertToTrackerFormat (trackedSourceLineData)))
                      {
                        trackedSourceDependencies.Add (ConvertToTrackerFormat (trackedSourceLineData));
                      }
                    }

                    trackedSourceLineData = reader.ReadLine ();
                  }

                  foreach (string dependency in trackedSourceDependencies)
                  {
                    foreach (string source in trackedSources)
                    {
                      if (!m_sourceDependencyTable [ConvertToTrackerFormat (source)].Contains (dependency))
                      {
                        m_sourceDependencyTable [ConvertToTrackerFormat (source)].Add (dependency);
                      }
                    }
                  }
                }
              }

              reader.Close ();
            }
          }
        }
        catch (Exception)
        {
          throw;
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ITaskItem [] ComputeSourcesNeedingCompilation ()
    {
      throw new NotImplementedException ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void AddSourcesToTable (ITaskItem [] sources)
    {
      // 
      // Register a set of provided sources, without an associated dependency.
      // 

      try
      {
        foreach (ITaskItem source in sources)
        {
          string sourceFileTrackerFormat = ConvertToTrackerFormat (source.GetMetadata ("FullPath"));

          if (!m_sourceDependencyTable.ContainsKey (sourceFileTrackerFormat))
          {
            m_sourceDependencyTable.Add (sourceFileTrackerFormat, new HashSet<string> ());
          }
        }
      }
      catch (Exception)
      {
        throw;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void AddDependencyForSources (ITaskItem [] dependencies, ITaskItem [] sources)
    {
      // 
      // Register a dependency for a set of provided sources. Will add unregistered sources to table if required.
      // 

      try
      {
        AddSourcesToTable (sources);

        foreach (ITaskItem dependency in dependencies)
        {
          string dependencyFullPath = dependency.GetMetadata ("FullPath");

          if (!File.Exists (dependencyFullPath))
          {
            throw new FileNotFoundException ("Could not find specified dependency: " + dependencyFullPath);
          }

          foreach (ITaskItem source in sources)
          {
            string sourceFullPath = source.GetMetadata ("FullPath");

            if (dependencyFullPath.Equals (sourceFullPath))
            {
              continue; // Don't list sources as their own dependencies.
            }

            string sourceFileTrackerFormat = ConvertToTrackerFormat (sourceFullPath);

            string dependencyFileTrackerFormat = ConvertToTrackerFormat (dependencyFullPath);

            if (!m_sourceDependencyTable [sourceFileTrackerFormat].Contains (dependencyFileTrackerFormat))
            {
              m_sourceDependencyTable [sourceFileTrackerFormat].Add (dependencyFileTrackerFormat);
            }
          }
        }
      }
      catch (Exception e)
      {
        throw;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void RemoveSourcesFromTable (ITaskItem [] sources)
    {
      // 
      // Iterate through the entire dependency table removing any of the specified sources. Remove empty entries.
      // 

      try
      {
        foreach (ITaskItem source in sources)
        {
          string sourceFileTrackerFormat = ConvertToTrackerFormat (source.GetMetadata ("FullPath"));

          m_sourceDependencyTable.Remove (sourceFileTrackerFormat);
        }
      }
      catch (Exception)
      {
        throw;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Save (ITaskItem tlog)
    {
      // 
      // Output a TLog file for the stored dependency graph.
      // 
      // Format:
      // 
      //    ^FILE1.C
      //    FILE1.OBJ
      //    ^FILE2.C|FILE3.C
      //    FILE2.OBJ
      //    FILE3.OBJ
      // 

      try
      {
        if (tlog == null)
        {
          throw new ArgumentNullException ("tlog");
        }

        string tlogFullPath = (!string.IsNullOrEmpty (tlog.GetMetadata ("FullPath")) ? tlog.GetMetadata ("FullPath") : Path.GetFullPath (tlog.ItemSpec));

        if (string.IsNullOrEmpty (tlogFullPath))
        {
          throw new ArgumentException ("Could not get 'FullPath' metadata for TLog. " + tlog);
        }

#if true
        using (StreamWriter writer = new StreamWriter (tlogFullPath, false, Encoding.Unicode))
        {
          foreach (KeyValuePair<string, HashSet<string>> sourceEntry in m_sourceDependencyTable)
          {
            writer.WriteLine ('^' + sourceEntry.Key);

            foreach (string dependency in sourceEntry.Value)
            {
              writer.WriteLine (dependency);
            }
          }

          writer.Close ();
        }
#else
        // 
        // Reorder the dependency graph data so it can be easily output in a condensed format.
        // 

        Dictionary<string, List<string>> condensedDependencyTable = new Dictionary<string, List<string>> ();

        StringBuilder sourceFileList = new StringBuilder ();

        foreach (KeyValuePair<string, List<string>> tableEntry in m_dependencyTable)
        {
          sourceFileList.Length = 0;

          foreach (string source in tableEntry.Value)
          {
            sourceFileList.Append ("|" + source.ToUpperInvariant ());
          }

          sourceFileList.Replace ('|', '^', 0, 1);

          string key = sourceFileList.ToString ();

          List<string> condensedDependencyEntryList = null;

          if (condensedDependencyTable.TryGetValue (key, out condensedDependencyEntryList))
          {
            condensedDependencyEntryList.Add (tableEntry.Key);

            condensedDependencyTable [key] = condensedDependencyEntryList;
          }
          else
          {
            condensedDependencyEntryList = new List<string> ();

            condensedDependencyEntryList.Add (tableEntry.Key);

            condensedDependencyTable.Add (key, condensedDependencyEntryList);
          }
        }

        // 
        // Output condensed dependency info to file.
        // 

        using (StreamWriter writer = new StreamWriter (tlogFullPath, false, Encoding.Unicode))
        {
          foreach (KeyValuePair<string, List<string>> tableEntry in condensedDependencyTable)
          {
            writer.WriteLine (tableEntry.Key.ToUpperInvariant ());

            foreach (string source in tableEntry.Value)
            {
              writer.WriteLine (source.ToUpperInvariant ());
            }
          }

          writer.Close ();
        }
#endif
      }
      catch (Exception e)
      {
        throw;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string ConvertToTrackerFormat (string original)
    {
      return original.ToUpperInvariant ();
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
