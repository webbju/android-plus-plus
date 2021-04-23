using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AndroidPlusPlus.Common.Tests
{
  [TestClass]
  public class PathUtilsTests
  {
    [TestMethod]
    public void GetExactPathName()
    {
      Assert.AreEqual("C:\\Windows\\System32", PathUtils.GetExactPathName("C:\\WiNdOws\\SyStEm32"));
      Assert.AreEqual("C:\\Windows\\System32\\cmd.exe", PathUtils.GetExactPathName("C:\\WiNdOws\\SyStEm32\\cMd.eXe"));
    }
  }
}
