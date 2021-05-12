using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidPlusPlus.Common.Tests
{
  [TestClass]
  public class RedirectProcessTests
  {

    [TestMethod]
    public void SyncRedirect()
    {
      using var syncProcess = new SyncRedirectProcess(@"C:\Users\Justin\AppData\Local\Android\Sdk\platform-tools\adb.exe", "shell \"ps\""); // -s emulator-5554 shell -n -x 

      //SyncRedirectProcess("C:\\Windows\\System32\\cmd.exe", "/C echo \"Hello, World!\"");

      var (exitCode, standardOutput, _) = syncProcess.StartAndWaitForExit();

      Debug.WriteLine(string.Join("\n", standardOutput));
    }

    [TestMethod]
    public void AsyncRedirect()
    {
      using var asyncProcess = new AsyncRedirectProcess(@"C:\Users\Justin\AppData\Local\Android\Sdk\ndk\22.1.7171670\prebuilt\windows-x86_64\bin\gdb.exe",
      @"--interpreter=mi  -fullname -x C:/Users/Justin/AppData/Local/Android++/Cache/emulator-5554/com.example.hellogdbserver/gdb.setup");

      asyncProcess.Start(null);


    }
  }
}
