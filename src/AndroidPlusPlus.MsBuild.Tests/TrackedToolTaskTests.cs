using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using AndroidPlusPlus.MsBuild.Common;
using AndroidPlusPlus.MsBuild.CppTasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AndroidPlusPlus.MsBuild.Tests
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  internal class PowershellSelfContainedTask : TrackedToolTask
  {
    internal PowershellSelfContainedTask()
      : base(new ResourceManager("AndroidPlusPlus.MsBuild.Common.Properties.Resources", Assembly.GetExecutingAssembly()))
    {
      ToolExe = "pwsh.exe";
    }

    internal string ExpectedOutputFile { get; } = Path.Combine(".", Path.GetRandomFileName());

    protected override string GenerateCommandLineCommands()
    {
      return $"-NoLogo -NonInteractive -NoProfile -Command \"Get-Process | Out-File {ExpectedOutputFile}\"";
    }
  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  [TestClass]
  public class TrackedToolTaskTests
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public TestContext TestContext { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [TestMethod]
    public void PowershellSelfContainedTask()
    {
      var mock = new Mock<IBuildEngine>();

      var trackedToolTask = new PowershellSelfContainedTask
      {
        TrackerLogDirectory = new TaskItem(Path.GetFullPath(TestContext.TestDir)),
        BuildEngine = mock.Object
      };

      Assert.IsTrue(trackedToolTask.Execute());
      Assert.IsNotNull(trackedToolTask.OutputFiles);
      Assert.IsTrue(trackedToolTask.OutputFiles.Length > 0);
      Assert.IsTrue(trackedToolTask.OutputFiles.Where(ti => string.Equals(ti.GetMetadata("FullPath"), Path.GetFullPath(trackedToolTask.ExpectedOutputFile), StringComparison.OrdinalIgnoreCase)).Any());
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [TestMethod]
    public void NativeCompileTaskWithSources()
    {
      var mock = new Mock<IBuildEngine>();

      var sourceFiles = new List<ITaskItem>
      {
        new TaskItem(Path.Combine(TestContext.TestDir, "test1.cpp")),
        new TaskItem(Path.Combine(TestContext.TestDir, "test2.cpp")),
        new TaskItem(Path.Combine(TestContext.TestDir, "test3.cpp")),
      };
      foreach (var file in sourceFiles)
      {
        File.WriteAllText(file.ItemSpec, "int main() { return 0; }", Encoding.UTF8);
      }

      var trackedToolTask = new NativeCompile()
      {
        BuildEngine = mock.Object,
        ToolExe = "clang++.exe",
        TrackerLogDirectory = new TaskItem(Path.GetFullPath(TestContext.TestDir)),
        InputFiles = sourceFiles.ToArray(),
      };

      Assert.IsTrue(trackedToolTask.Execute());
      Assert.IsFalse(trackedToolTask.SkippedExecution);
      Assert.IsTrue(trackedToolTask.OutOfDateInputFiles.Length == sourceFiles.Count);
      Assert.IsTrue(trackedToolTask.OutputFiles.Length > 0);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [TestMethod]
    public void TestNativeLib()
    {
      var mock = new Mock<IBuildEngine>();

      var trackerLogDirectory = new TaskItem(Path.GetFullPath(TestContext.TestDir));

      var nativeCompile = new NativeCompile();
      nativeCompile.BuildEngine = mock.Object;
      nativeCompile.ToolExe = "clang++.exe";
      nativeCompile.TrackerLogDirectory = trackerLogDirectory;

      var sourceFile = new TaskItem(Path.Combine(TestContext.TestDir, "test1.cpp"));
      File.WriteAllText(sourceFile.ItemSpec, "int main() { return 0; }", Encoding.UTF8);
      nativeCompile.InputFiles = new List<ITaskItem> { sourceFile }.ToArray();

      Assert.IsTrue(nativeCompile.Execute());
      Assert.IsFalse(nativeCompile.SkippedExecution);
      Assert.IsTrue(nativeCompile.OutputFiles.Length == 1);

      var nativeLib = new NativeLib();
      nativeLib.BuildEngine = mock.Object;
      nativeLib.ToolExe = "llvm-ar.exe";
      nativeLib.InputFiles = new List<ITaskItem> { new TaskItem("test1.o") }.ToArray();
      nativeLib.InputFiles[0].SetMetadata("OutputFile", Path.ChangeExtension(Path.GetRandomFileName(), ".a"));
      nativeLib.TrackerLogDirectory = trackerLogDirectory;
      Assert.IsTrue(nativeLib.Execute());
      Assert.IsFalse(nativeCompile.SkippedExecution);
      Assert.IsTrue(nativeLib.OutputFiles.Length == 1);

      // Minimal rebuild.
      nativeLib.MinimalRebuildFromTracking = true;
      Assert.IsTrue(nativeLib.Execute());
      Assert.IsTrue(nativeLib.SkippedExecution);
      Assert.IsTrue(nativeLib.OutputFiles.Length == 0);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }
}
