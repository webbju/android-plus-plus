using AndroidPlusPlus.MsBuild.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AndroidPlusPlus.Common.Tests
{
  [TestClass]
  public class JavaUtilsTests
  {
    [TestMethod]
    public void ConvertJavaOutputToVS()
    {
      Assert.AreEqual("warning : The signer's certificate is self-signed.", JavaUtilities.ConvertJavaOutputToVS("warning:  The signer's certificate is self-signed."));
    }
  }
}
