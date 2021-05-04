using AndroidPlusPlus.MsBuild.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
