////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using AndroidPlusPlus.Common;
using AndroidPlusPlus.MsBuild.Common;
using AndroidPlusPlus.MsBuild.Common.Attributes;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.DeployTasks
{

  public class AndroidAapt : TrackedOutOfDateToolTask
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidAapt ()
      : base (new ResourceManager ("AndroidPlusPlus.MsBuild.DeployTasks.Properties.Resources", Assembly.GetExecutingAssembly ()))
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [SwitchEnum]
    [SwitchEnumValue(Name = "Package", Switch = "package")]
    public string Mode { get; set; } = "Package";

    [Required]
    [SwitchString(IsRequired = true, Subtype = "file", Switch = "-M", Separator = " ", DisplayName = "Project Manifest", Description = "Default (primary) 'AndroidManifest.xml' for the project. For applications this is a manifest which specifies an &lt;application%gt; tag.")]
    public ITaskItem ProjectManifest { get; set; }

    [SwitchBool(Switch = "-a", DisplayName = "Print Android Specific Data", Description = "Print Android-specific data (resources, manifest) when listing")]
    public bool PrintAndroidSpecificData { get; set; }

    [SwitchString(Switch = "-c", Separator = " ", DisplayName = "Configurations To Include", Description = "Specify which configurations to include. The default is all configurations. Locales should be specified as either a language or language-region pair. I.e. 'port,land,en_US'")]
    public string ConfigurationsToInclude { get; set; }

    [SwitchStringList(Switch = "-d", Separator = " ", CommandLineValueSeparator = ",", DisplayName = "Device Assets To Include", Description = "One or more device assets to include.")]
    public string[] DeviceAssetsToInclude { get; set; }

    [SwitchBool(Switch = "-f", DisplayName = "Overwrite Existing Files")]
    public bool OverwriteExistingFiles { get; set; }

    [SwitchInt(Switch = "-g", Separator = " ", DisplayName = "Greyscale Pixel Tolerance", Description = "Specify a pixel tolerance to force images to grayscale, default 0.")]
    public int? GreyscalePixelTolerance { get; set; }

    [SwitchStringList(Subtype = "file", Switch = "-j", Separator = " ", DisplayName = "Include .Jar Or .Zip", Description = "Specify a .jar or .zip file containing classes to include.")]
    public ITaskItem[] IncludeJarOrZip { get; set; }

    [SwitchBool(Switch = "-m", DisplayName = "Create Package Directories Under Output", Description = "Make package directories under location specified by -J.")]
    public bool CreatePackageDirectoriesUnderOutput { get; set; }

    [SwitchBool(Switch = "-u", DisplayName = "Update Existing Packages", Description = "Update existing packages (add new, replace older, remove deleted files).")]
    public bool UpdateExistingPackages { get; set; }

    [SwitchBool(Switch = "-v", DisplayName = "Verbose Output")]
    public bool Verbose { get; set; }

    [SwitchBool(Switch = "-x", DisplayName = "Create Existing (Non-Application) Resource Ids")]
    public bool CreateExistingResourceIds { get; set; }

    [SwitchBool(Switch = "-z", DisplayName = "Require Suggested Localisation Resources", Description = "Require localisation of resource attributes marked with 'localization=&quot;suggested&quot;'.")]
    public bool RequireSuggestedLocalisationResources { get; set; }

    [SwitchString(Subtype = "folder", Switch = "-A", Separator = " ", DisplayName = "Include Raw 'Assets' Directory", Description = "Additional directory in which to find raw asset files.")]
    public string IncludeRawAssetsDirectory { get; set; }

    [Output]
    [SwitchString(Subtype = "file", Switch = "-G", Separator = " ", DisplayName = "Proguard Options Output File", Description = "A file to output proguard options into.")]
    public ITaskItem ProguardOptionsOutputFile { get; set; }

    [Output]
    [SwitchString(Subtype = "file", Switch = "-F", Separator = " ", DisplayName = ".APK Output File", Description = "Specify the .apk filename to output.")]
    public ITaskItem ApkOutputFile { get; set; }

    [SwitchStringList(Switch = "-I", Separator = " ", DisplayName = "Include Existing Packages", Description = "Add an existing package to base include set.")]
    public string[] IncludeExistingPackages { get; set; }

    [SwitchString(Subtype = "folder", Switch = "-J", Separator = " ", DisplayName = "Resource Constants Output Directory", Description = "Specify where to output 'R.java' resource constant definitions.")]
    public ITaskItem ResourceConstantsOutputDirectory { get; set; }

    [Output]
    [SwitchString(Subtype = "file", Switch = "-P", Separator = " ", DisplayName = "Public Resource Defintions Output File", Description = "Specify where to output public resource definitions.")]
    public string PublicResourceDefintionsOutputFile { get; set; }

    [SwitchStringList(Subtype = "folder", Switch = "-S", Separator = " ", DisplayName = "Include Resource Directories", Description = "Directories in which to find resources. Multiple directories will be scanned and the first match found (left to right) will take precedence.")]
    public ITaskItem[] IncludeResourceDirectories { get; set; }

    [SwitchStringList(Switch = "-0", Separator = " ", DisplayName = "No Not Compress Extensions", Description = "Specifies an additional extension for which such files will not be stored compressed in the .apk. An empty string means to not compress any files at all.")]
    public string[] NoNotCompressExtensions { get; set; }

    [SwitchBool(Switch = "--debug-mode", DisplayName = "Debug Mode", Description = "Inserts android:debuggable='true' in to the application node of the manifest, making the application debuggable even on production devices.")]
    public bool DebugMode { get; set; }

    [SwitchInt(Switch = "--min-sdk-version", Separator = " ", DisplayName = "Insert Manifest Min SDK Version", Description = "Inserts android:minSdkVersion in to manifest.")]
    public int? InsertManifestMinSdkVersion { get; set; }

    [SwitchInt(Switch = "--target-sdk-version", Separator = " ", DisplayName = "Insert Manifest Target SDK Version", Description = "Inserts android:targetSdkVersion in to manifest.")]
    public int? InsertManifestTargetSdkVersion { get; set; }

    [SwitchString(Switch = "--version-code", Separator = " ", DisplayName = "Insert Manifest Version Code", Description = "Inserts android:versionCode in to manifest.")]
    public string InsertManifestVersionCode { get; set; }

    [SwitchString(Switch = "--version-name", Separator = " ", DisplayName = "Insert Manifest Version Name", Description = "Inserts android:versionName in to manifest.")]
    public string InsertManifestVersionName { get; set; }

    [SwitchString(Switch = "--max-res-version", Separator = " ", DisplayName = "Max Res Version", Description = "Ignores versioned resource directories above the given value.")]
    public string MaxResVersion { get; set; }

    [SwitchString(Switch = "--custom-package", Separator = " ", DisplayName = "Custom Package", Description = "Generates R.java into a different package.")]
    public string CustomPackage { get; set; }

    [SwitchStringList(Switch = "--extra-packages", Separator = " ", CommandLineValueSeparator = ":", DisplayName = "Extra Packages", Description = "Generate R.java for libraries. Separate libraries with ':'.")]
    public string[] ExtraPackages { get; set; }

    [SwitchBool(Switch = "--generate-dependencies", DisplayName = "Generate Dependencies", Description = "Generate dependency files in the same directories for R.java and resource package.")]
    public bool GenerateDependencies { get; set; }

    [SwitchBool(Switch = "--auto-add-overlay", DisplayName = "Auto Add Overlay", Description = "Automatically add resources that are only in overlays.")]
    public bool AutoAddOverlay { get; set; }

    [SwitchString(Switch = "--preferred-configurations", Separator = " ", DisplayName = "Preferred Configurations", Description = "Like the -c option for filtering out unneeded configurations, but only expresses a preference.  If there is no resource available with the preferred configuration then it will not be stripped.")]
    public string PreferredConfigurations { get; set; }

    [SwitchString(Switch = "--rename-manifest-package", Separator = " ", DisplayName = "Rename Manifest Package", Description = "Rewrite the manifest so that its package name is the package name given here.  Relative class names (for example .Foo) will be changed to absolute names with the old package so that the code does not need to change.")]
    public string RenameManifestPackage { get; set; }

    [SwitchString(Switch = "--rename-instrumentation-target-package", Separator = " ", DisplayName = "Rename Instrumentation Target Package", Description = "Rewrite the manifest so that all of its instrumentation components target the given package.  Useful when used in conjunction with --rename-manifest-package to fix tests against a package that has been renamed.")]
    public string RenameInstrumentationTargetPackage { get; set; }

    [SwitchString(Switch = "--product", Separator = " ", DisplayName = "String Product Variant", Description = "Specifies which variant to choose for strings that have product variants.")]
    public string StringProductVariant { get; set; }

    [SwitchBool(Switch = "--utf16", DisplayName = "Use UTF-16 Encoding", Description = "Changes default encoding for resources to UTF-16.  Only useful when API level is set to 7 or higher where the default encoding is UTF-8.")]
    public bool UseUtf16Encoding { get; set; }

    [SwitchBool(Switch = "--non-constant-id", DisplayName = "Non Constant Resource Id", Description = "Make the resources ID non constant. This is required to make an R java class that does not contain the final value but is used to make reusable compil ed libraries that need to access resources.")]
    public bool NonConstantResourceId { get; set; }

    [SwitchBool(Switch = "--error-on-failed-insert", DisplayName = "Error On Failed Manifest Insert", Description = "Forces aapt to return an error if it fails to insert values into the manifest with --debug-mode, --min-sdk-version, --target-sdk-version --version-code and --version-name. Insertion typically fails if the manifest already defines the attribute.")]
    public bool ErrorOnFailedManifestInsert { get; set; }

    [SwitchString(Switch = "--output-text-symbols", Separator = " ", DisplayName = "Output Text Symbols", Description = "Generates a text file containing the resource symbols of the R class in the specified folder.")]
    public string OutputTextSymbols { get; set; }

    [SwitchString(Switch = "--ignore-assets", Separator = " ", DisplayName = "Ignore Asset Patterns", Description = "Assets to be ignored. Default pattern is: !.svn:!.git:!.ds_store:!*.scc:.*:&lt;dir&gt;_*:!CVS:!thumbs.db:!picasa.ini:!*~")]
    public string IgnoreAssetPatterns { get; set; }

    [SwitchStringList(Subtype = "folder", DisplayName = "Include Raw Directories", Description = "Directories which contents will be included as raw (unprocessed) files.")]
    public ITaskItem[] IncludeRawDirectories { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override bool ValidateParameters()
    {
      try
      {
        // 
        // Validate the target AndroidManifest input is at least parsable.
        // 

        string sourcePath = ProjectManifest.GetMetadata("FullPath");

        var sourceManifest = new AndroidManifest(sourcePath);
      }
      catch (Exception e)
      {
        Log.LogErrorFromException(e, true);

        return false;
      }

      var inputFiles = new List<ITaskItem>();

      inputFiles.Add(ProjectManifest);

      if (IncludeJarOrZip != null)
      {
        inputFiles.AddRange(IncludeJarOrZip);
      }

      InputFiles = inputFiles.ToArray();

      return base.ValidateParameters();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override string GenerateResponseFileCommands()
    {
      return string.Empty; // AAPT does not support response files.
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

}
