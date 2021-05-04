////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using AndroidPlusPlus.Common;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.DeployTasks
{

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
          var androidManifestDocument = new AndroidManifest (manifestItem.GetMetadata("FullPath"));

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
          HashSet<string> extraPackages = new HashSet<string> ();

          HashSet<string> extraResourcePaths = new HashSet<string> ();

          extraResourcePaths.Add (MergedManifest.GetMetadata ("IncludeResourceDirectories"));

          foreach (ITaskItem item in ProjectManifests)
          {
            var androidManifest = new AndroidManifest(item.GetMetadata("FullPath"));

            if (item == MergedManifest)
            {
              PackageName = androidManifest.PackageName;
            }
            else
            {
              if (!extraPackages.Contains (androidManifest.PackageName))
              {
                extraPackages.Add (androidManifest.PackageName);
              }

              if (!extraResourcePaths.Contains (item.GetMetadata ("IncludeResourceDirectories")))
              {
                extraResourcePaths.Add (item.GetMetadata ("IncludeResourceDirectories"));
              }
            }
          }

          MergedManifest.SetMetadata ("ExtraPackages", string.Join<string> (":", extraPackages));

          MergedManifest.SetMetadata ("IncludeResourceDirectories", string.Join<string> (";", extraResourcePaths));

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

  }

}
