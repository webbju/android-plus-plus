/********************************************************************************************

Copyright (c) Microsoft Corporation 
All rights reserved. 

Microsoft Public License: 

This license governs use of the accompanying software. If you use the software, you 
accept this license. If you do not accept the license, do not use the software. 

1. Definitions 
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the 
same meaning here as under U.S. copyright law. 
A "contribution" is the original software, or any additions or changes to the software. 
A "contributor" is any person that distributes its contribution under this license. 
"Licensed patents" are a contributor's patent claims that read directly on its contribution. 

2. Grant of Rights 
(A) Copyright Grant- Subject to the terms of this license, including the license conditions 
and limitations in section 3, each contributor grants you a non-exclusive, worldwide, 
royalty-free copyright license to reproduce its contribution, prepare derivative works of 
its contribution, and distribute its contribution or any derivative works that you create. 
(B) Patent Grant- Subject to the terms of this license, including the license conditions 
and limitations in section 3, each contributor grants you a non-exclusive, worldwide, 
royalty-free license under its licensed patents to make, have made, use, sell, offer for 
sale, import, and/or otherwise dispose of its contribution in the software or derivative 
works of the contribution in the software. 

3. Conditions and Limitations 
(A) No Trademark License- This license does not grant you rights to use any contributors' 
name, logo, or trademarks. 
(B) If you bring a patent claim against any contributor over patents that you claim are 
infringed by the software, your patent license from such contributor to the software ends 
automatically. 
(C) If you distribute any portion of the software, you must retain all copyright, patent, 
trademark, and attribution notices that are present in the software. 
(D) If you distribute any portion of the software in source code form, you may do so only 
under this license by including a complete copy of this license with your distribution. 
If you distribute any portion of the software in compiled or object code form, you may only 
do so under a license that complies with this license. 
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give 
no express warranties, guarantees or conditions. You may have additional consumer rights 
under your local laws which this license cannot change. To the extent permitted under your 
local laws, the contributors exclude the implied warranties of merchantability, fitness for 
a particular purpose and non-infringement.

********************************************************************************************/

using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.Project.UnitTests
{
    [TestClass]
    public class ImageHandlerTest
    {
        private static void VerifySameBitmap(Bitmap expected, Bitmap actual)
        {
            // The size should be the same.
            Assert.AreEqual<int>(expected.Height, actual.Height);
            Assert.AreEqual<int>(expected.Width, actual.Width);
            // every pixel should match.
            for(int x = 0; x < expected.Width; ++x)
            {
                for(int y = 0; y < expected.Height; ++y)
                {
                    Assert.AreEqual<Color>(expected.GetPixel(x, y), actual.GetPixel(x, y), "Pixel ({0}, {1}) is different in the two bitmaps", x, y);
                }
            }
        }

        [TestMethod]
        public void ImageHandlerConstructors()
        {
            // Default constructor.
            ImageHandler handler = new ImageHandler();
            Assert.IsNull(handler.ImageList);

            // Constructor from resource stream.
            System.IO.Stream nullStream = null;
            Assert.IsTrue(Utilities.HasFunctionThrown<ArgumentNullException>(delegate { new ImageHandler(nullStream); }));
            try
            {
                handler = new ImageHandler(typeof(ImageHandlerTest).Assembly.GetManifestResourceStream("Resources.ImageList.bmp"));
                Assert.IsNotNull(handler.ImageList);
                Assert.AreEqual<int>(3, handler.ImageList.Images.Count);
            }
            finally
            {
                handler.Close();
                handler = null;
            }

            ImageList imageList = null;
            Assert.IsTrue(Utilities.HasFunctionThrown<ArgumentNullException>(delegate { new ImageHandler(imageList); }));
            imageList = Microsoft.VisualStudio.Shell.PackageUtilities.GetImageList(typeof(ImageHandlerTest).Assembly.GetManifestResourceStream("Resources.ImageList.bmp"));
            Assert.IsNotNull(imageList);
            try
            {
                handler = new ImageHandler(imageList);
                Assert.IsNotNull(handler.ImageList);
                Assert.AreEqual<ImageList>(imageList, handler.ImageList);
            }
            finally
            {
                handler.Close();
                handler = null;
            }
        }

        [TestMethod]
        public void ImageHandlerClose()
        {
            // Verify that it is possible to close an empty object.
            ImageHandler handler = new ImageHandler();
            handler.Close();

            // We can not verify that if the image handler is not empty, then
            // the image list is disposed, but we can verify that at least it
            // is released.
            handler = new ImageHandler(typeof(ImageHandlerTest).Assembly.GetManifestResourceStream("Resources.ImageList.bmp"));
            Assert.IsNotNull(handler.ImageList);
            handler.Close();
            Assert.IsNull(handler.ImageList);
        }

        [TestMethod]
        public void GetIconHandleTest()
        {
            // Verify the kind of exception when the ImageList is empty.
            ImageHandler handler = new ImageHandler();
            Assert.IsTrue(Utilities.HasFunctionThrown<InvalidOperationException>(delegate { handler.GetIconHandle(0); }));

            try
            {
                // Set the image list property so that the object is no more empty.
                handler.ImageList = Microsoft.VisualStudio.Shell.PackageUtilities.GetImageList(typeof(ImageHandlerTest).Assembly.GetManifestResourceStream("Resources.ImageList.bmp"));
                Assert.IsNotNull(handler.ImageList);

                // Verify the kind of exception in case of a bad index.
                Assert.IsTrue(Utilities.HasFunctionThrown<ArgumentOutOfRangeException>(delegate { handler.GetIconHandle(-1); }));
                Assert.IsTrue(Utilities.HasFunctionThrown<ArgumentOutOfRangeException>(delegate { handler.GetIconHandle(handler.ImageList.Images.Count); }));

                // Get the handle for one of the images
                IntPtr iconHandle = handler.GetIconHandle(1);
                Assert.AreNotEqual<IntPtr>(IntPtr.Zero, iconHandle);

                // Verify the image.
                Icon icon = Icon.FromHandle(iconHandle);
                Bitmap resultBmp = icon.ToBitmap();
                Bitmap expectedBmp = handler.ImageList.Images[1] as Bitmap;
                // The bitmaps should match.
                VerifySameBitmap(expectedBmp, resultBmp);
            }
            finally
            {
                handler.Close();
            }
        }

        [TestMethod]
        public void AddImageTest()
        {
            Bitmap newBmp = new Bitmap(typeof(ImageHandlerTest).Assembly.GetManifestResourceStream("Resources.Image1.bmp"));

            // Case 1: Add an image to an empty ImageHandler.
            ImageHandler handler = new ImageHandler();
            try
            {
                Assert.IsTrue(Utilities.HasFunctionThrown<ArgumentNullException>(delegate { handler.AddImage(null); }));
                handler.AddImage(newBmp);
                Assert.IsNotNull(handler.ImageList);
                Assert.AreEqual(1, handler.ImageList.Images.Count);
            }
            finally
            {
                handler.Close();
            }

            // Case 2: Add a new image to a not empty image handler
            handler = new ImageHandler(typeof(ImageHandlerTest).Assembly.GetManifestResourceStream("Resources.ImageList.bmp"));
            try
            {
                Assert.IsTrue(Utilities.HasFunctionThrown<ArgumentNullException>(delegate { handler.AddImage(null); }));
                handler.AddImage(newBmp);
                Assert.IsNotNull(handler.ImageList);
                Assert.AreEqual(4, handler.ImageList.Images.Count);

                // Verify that it is possible to get the icon handle for the
                // last (new) element in the list.
                IntPtr iconHandle = handler.GetIconHandle(3);
                Assert.AreNotEqual<IntPtr>(IntPtr.Zero, iconHandle);

                // Verify the image.
                Icon icon = Icon.FromHandle(iconHandle);
                Bitmap resultBmp = icon.ToBitmap();
                Bitmap expectedBmp = handler.ImageList.Images[3] as Bitmap;
                VerifySameBitmap(expectedBmp, resultBmp);
            }
            finally
            {
                handler.Close();
            }
        }
    }
}
