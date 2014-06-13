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
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Win32;
using Microsoft.Build.Utilities;

using AndroidPlusPlus.MsBuild.Common;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.DeployTasks
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class AndroidManifestMerge : Task
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidManifestMerge ()
      : base (new ResourceManager ("AndroidPlusPlus.MsBuild.DeployTasks.Properties.Resources", Assembly.GetExecutingAssembly ()))
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [Required]
    public ITaskItem [] PrimaryManifest { get; set; }

    [Required]
    public ITaskItem [] ProjectManifests { get; set; }

    [Output]
    public ITaskItem MergedManifest { get; set; }

    [Output]
    public string PackageName { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override bool Execute ()
    {
      try
      {
        if (PrimaryManifest.Length == 0)
        {
          Log.LogError ("No 'PrimaryManifest' file(s) specified.");

          return false;
        }

        if (PrimaryManifest.Length > 1)
        {
          Log.LogError ("Too many 'PrimaryManifest' files specified. Expected one.");

          return false;
        }

        if (ProjectManifests.Length == 0)
        {
          Log.LogError ("No 'ProjectManifests' file(s) specified.");

          return false;
        }

        // 
        // Identify which is the primary AndroidManifest.xml provided.
        // 

        MergedManifest = null;

        string primaryManifestFullPath = PrimaryManifest [0].GetMetadata ("FullPath");

        foreach (ITaskItem manifestItem in ProjectManifests)
        {
          string manifestItemFullPath = manifestItem.GetMetadata ("FullPath");

          if (primaryManifestFullPath.Equals (manifestItemFullPath))
          {
            MergedManifest = manifestItem;

            break;
          }
        }

        if (MergedManifest == null)
        {
          Log.LogError ("Could not find 'primary' manifest in provided list of project manifests. Expected: " + primaryManifestFullPath);

          return false;
        }

        // 
        // Sanity check all manifests to ensure that there's not another which defines <application> that's not 'primary'.
        // 

        ITaskItem applicationManifest = null;

        foreach (ITaskItem manifestItem in ProjectManifests)
        {
          AndroidManifestDocument androidManifestDocument = new AndroidManifestDocument ();

          androidManifestDocument.Load (manifestItem.GetMetadata ("FullPath"));

          if (androidManifestDocument.IsApplication)
          {
            if (applicationManifest == null)
            {
              applicationManifest = manifestItem;

              break;
            }
            else
            {
              Log.LogError ("Found multiple AndroidManifest files which define an <application> node.");

              return false;
            }
          }
        }

        if ((applicationManifest != null) && (applicationManifest != MergedManifest))
        {
          Log.LogError ("Specified project manifest does not define an <application> node.");

          return false;
        }

        // 
        // Process other 'third-party' manifests merging required metadata.
        // 

        if (MergedManifest != null)
        {
          List<string> extraPackages = new List<string> ();

          List<string> extraResourcePaths = new List<string> ();

          extraResourcePaths.Add (MergedManifest.GetMetadata ("IncludeResourceDirectories"));

          foreach (ITaskItem item in ProjectManifests)
          {
            AndroidManifestDocument androidManifestDocument = new AndroidManifestDocument ();

            androidManifestDocument.Load (item.GetMetadata ("FullPath"));

            if (item == MergedManifest)
            {
              PackageName = androidManifestDocument.PackageName;
            }
            else
            {
              if (!extraPackages.Contains (androidManifestDocument.PackageName))
              {
                extraPackages.Add (androidManifestDocument.PackageName);
              }

              if (!extraResourcePaths.Contains (item.GetMetadata ("IncludeResourceDirectories")))
              {
                extraResourcePaths.Add (item.GetMetadata ("IncludeResourceDirectories"));
              }
            }
          }

          MergedManifest.SetMetadata ("ExtraPackages", String.Join (":", extraPackages.ToArray ()));

          MergedManifest.SetMetadata ("IncludeResourceDirectories", String.Join (";", extraResourcePaths.ToArray ()));

          return true;
        }
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

    private void MergeXmlManifests ()
    {

    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /*public override bool Execute ()
    {
      if (Manifests.Length == 0)
      {
        Log.LogError ("No input 'Manifests' entries specified.");

        return false;
      }

      // 
      // Sort manifest elements so that the ApplicationManifest task is first in the list. Remove duplicates.
      // 

      Dictionary<string, ITaskItem> sortedManifests = new Dictionary<string, ITaskItem> ();

      string applicationManifestFullPath = ApplicationManifest.GetMetadata ("FullPath");

      foreach (ITaskItem manifest in Manifests)
      {
        string manifestFullPath = manifest.GetMetadata ("FullPath");

        if (manifestFullPath == applicationManifestFullPath)
        {
          sortedManifests.Add (manifestFullPath, manifest);

          break;
        }
      }

      if (sortedManifests.Count == 0)
      {
        Log.LogError ("Input 'Manifest' list does not contain a reference matching 'ApplicationManifest'");

        return false;
      }

      foreach (ITaskItem manifest in Manifests)
      {
        string manifestFullPath = manifest.GetMetadata ("FullPath");

        if (!sortedManifests.ContainsKey (manifestFullPath))
        {
          sortedManifests.Add (manifestFullPath, manifest);
        }
      }

      // 
      // Manually compound the sorted list and export to array.
      // 

      List<ITaskItem> outputManifestList = new List<ITaskItem> ();

      foreach (KeyValuePair<string, ITaskItem> sortedKeyPair in sortedManifests)
      {
        ITaskItem manifestItem = new TaskItem (sortedKeyPair.Key);

        sortedKeyPair.Value.CopyMetadataTo (manifestItem);

        outputManifestList.Add (manifestItem);
      }

      SortedManifests = outputManifestList.ToArray ();

      return true;
    }*/

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

