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
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.Project.UnitTests
{
    [TestClass]
    public class TokenProcessorTest
    {
        [TestMethod]
        public void UntokenFileBadParameters()
        {
            TokenProcessor processor = new TokenProcessor();
            Assert.IsTrue(Utilities.HasFunctionThrown<ArgumentNullException>(delegate { processor.UntokenFile(null, null); }));
            Assert.IsTrue(Utilities.HasFunctionThrown<ArgumentNullException>(delegate { processor.UntokenFile(@"C:\SomeFile", null); }));
            Assert.IsTrue(Utilities.HasFunctionThrown<ArgumentNullException>(delegate { processor.UntokenFile(null, @"C:\SomeFile"); }));
            Assert.IsTrue(Utilities.HasFunctionThrown<ArgumentNullException>(delegate { processor.UntokenFile(@"C:\SomeFile", string.Empty); }));
            Assert.IsTrue(Utilities.HasFunctionThrown<ArgumentNullException>(delegate { processor.UntokenFile(string.Empty, @"C:\SomeFile"); }));
            Assert.IsTrue(Utilities.HasFunctionThrown<ArgumentNullException>(delegate { processor.UntokenFile(string.Empty, string.Empty); }));

            using(TestDirectory dir = new TestDirectory(@"MPFProjectTests\UntokenFileBadParameters"))
            {
                string sourcePath = Path.Combine(dir.Path, "NotExistingSource");
                string destinationPath = Path.Combine(dir.Path, "NotExistingDestination");
                Assert.IsTrue(Utilities.HasFunctionThrown<FileNotFoundException>(delegate { processor.UntokenFile(sourcePath, destinationPath); }));
            }
        }

        [TestMethod]
        public void UntokenFilePreserveEncoding()
        {
            using(TestDirectory dir = new TestDirectory(@"MPFProjectTests\UntokenFilePreserveEncoding"))
            {
                TokenProcessor processor = new TokenProcessor();

                // Test 1: Unicode file.
                string sourcePath = Path.Combine(dir.Path, "UnicodeSource");
                string destinationPath = Path.Combine(dir.Path, "UnicodeDestination");
                Encoding expectedEncoding = Encoding.Unicode;
                File.WriteAllText(sourcePath, "Test", expectedEncoding);
                processor.UntokenFile(sourcePath, destinationPath);
                Encoding actualEncoding;
                using(StreamReader reader = new StreamReader(destinationPath, Encoding.ASCII, true))
                {
                    // Read the content to force the encoding detection.
                    reader.ReadToEnd();
                    actualEncoding = reader.CurrentEncoding;
                }
                Assert.AreEqual<Encoding>(expectedEncoding, actualEncoding);

                // Test 2: UTF8 file.
                sourcePath = Path.Combine(dir.Path, "UTF8Source");
                destinationPath = Path.Combine(dir.Path, "UTF8Destination");
                expectedEncoding = Encoding.UTF8;
                File.WriteAllText(sourcePath, "Test", expectedEncoding);
                processor.UntokenFile(sourcePath, destinationPath);
                using(StreamReader reader = new StreamReader(destinationPath, Encoding.ASCII, true))
                {
                    // Read the content to force the encoding detection.
                    reader.ReadToEnd();
                    actualEncoding = reader.CurrentEncoding;
                }
                Assert.AreEqual<Encoding>(expectedEncoding, actualEncoding);

                // Test 3: ASCII file.
                sourcePath = Path.Combine(dir.Path, "AsciiSource");
                destinationPath = Path.Combine(dir.Path, "AsciiDestination");
                expectedEncoding = Encoding.ASCII;
                File.WriteAllText(sourcePath, "Test", expectedEncoding);
                processor.UntokenFile(sourcePath, destinationPath);
                using(StreamReader reader = new StreamReader(destinationPath, Encoding.ASCII, true))
                {
                    // Read the content to force the encoding detection.
                    reader.ReadToEnd();
                    actualEncoding = reader.CurrentEncoding;
                }
                Assert.AreEqual<Encoding>(expectedEncoding, actualEncoding);
            }
        }
    }
}
