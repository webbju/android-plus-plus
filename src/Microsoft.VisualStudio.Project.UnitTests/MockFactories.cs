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
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VsSDK.UnitTestLibrary;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.Project.UnitTests
{
    internal static class MockFactories
    {
        private static GenericMockFactory hierarchyFactory;
        public static GenericMockFactory HierarchyFactory
        {
            get
            {
                if(null == hierarchyFactory)
                {
                    hierarchyFactory = new GenericMockFactory("EmptyMockHierarchy", new Type[] { typeof(IVsHierarchy) });
                }
                return hierarchyFactory;
            }
        }

        public static IVsHierarchy HierarchyForLogger(IOleServiceProvider provider)
        {
            BaseMock mock = HierarchyFactory.GetInstance();
            mock.AddMethodReturnValues(
                string.Format("{0}.{1}", typeof(IVsHierarchy).FullName, "GetSite"),
                new object[] { 0, provider });

            return mock as IVsHierarchy;
        }

        private static GenericMockFactory outputWindowPaneFactory;
        public static GenericMockFactory OutputWindowPaneFactory
        {
            get
            {
                if(null == outputWindowPaneFactory)
                {
                    outputWindowPaneFactory = new GenericMockFactory("EmptyMockIVsOutputWindowPane", new Type[] { typeof(IVsOutputWindowPane) });
                }
                return outputWindowPaneFactory;
            }
        }

        private static void OutputStringCallback(object sender, CallbackArgs args)
        {
            BaseMock mock = (BaseMock)sender;
            System.Text.StringBuilder builder = (System.Text.StringBuilder)mock["StringBuilder"];
            string text = (string)args.GetParameter(0);
            builder.Append(text);
            args.ReturnValue = 0;
        }
        private static void ClearOutputCallback(object sender, CallbackArgs args)
        {
            BaseMock mock = (BaseMock)sender;
            System.Text.StringBuilder builder = (System.Text.StringBuilder)mock["StringBuilder"];
            builder.Length = 0;
            args.ReturnValue = 0;
        }
        public static IVsOutputWindowPane OutputPaneWithStringFunctions()
        {
            BaseMock mock = OutputWindowPaneFactory.GetInstance();
            mock["StringBuilder"] = new System.Text.StringBuilder();
            mock.AddMethodCallback(
                string.Format("{0}.{1}", typeof(IVsOutputWindowPane).FullName, "OutputString"),
                new EventHandler<CallbackArgs>(OutputStringCallback));
            mock.AddMethodCallback(
                string.Format("{0}.{1}", typeof(IVsOutputWindowPane).FullName, "OutputStringThreadSafe"),
                new EventHandler<CallbackArgs>(OutputStringCallback));
            mock.AddMethodCallback(
                string.Format("{0}.{1}", typeof(IVsOutputWindowPane).FullName, "Clear"),
                new EventHandler<CallbackArgs>(ClearOutputCallback));

            return mock as IVsOutputWindowPane;
        }

        private static GenericMockFactory eventSourceFactory;
        public static GenericMockFactory MSBuildEventSourceFactory
        {
            get
            {
                if(null == eventSourceFactory)
                {
                    eventSourceFactory = new GenericMockFactory("EmptyMockIEventSource", new Type[] { typeof(IEventSource) });
                }
                return eventSourceFactory;
            }
        }

        #region Default callback functions for the event source.
        private static void EventSourceAddMessageRaised(object sender, CallbackArgs args)
        {
            BaseMock mock = (BaseMock)sender;
            mock["MessageRaised"] = args.GetParameter(0);
        }
        private static void EventSourceAddBuildStarted(object sender, CallbackArgs args)
        {
            BaseMock mock = (BaseMock)sender;
            mock["BuildStarted"] = args.GetParameter(0);
        }
        private static void EventSourceAddBuildFinished(object sender, CallbackArgs args)
        {
            BaseMock mock = (BaseMock)sender;
            mock["BuildFinished"] = args.GetParameter(0);
        }
        private static void EventSourceAddTaskStarted(object sender, CallbackArgs args)
        {
            BaseMock mock = (BaseMock)sender;
            mock["TaskStarted"] = args.GetParameter(0);
        }
        private static void EventSourceAddTaskFinished(object sender, CallbackArgs args)
        {
            BaseMock mock = (BaseMock)sender;
            mock["TaskFinished"] = args.GetParameter(0);
        }
        #endregion

        public static BaseMock CreateMSBuildEventSource()
        {
            BaseMock mockSource = MSBuildEventSourceFactory.GetInstance();
            mockSource.AddMethodCallback(
                string.Format("{0}.{1}", typeof(IEventSource).FullName, "add_MessageRaised"),
                new EventHandler<CallbackArgs>(EventSourceAddMessageRaised));
            mockSource.AddMethodCallback(
                string.Format("{0}.{1}", typeof(IEventSource).FullName, "add_BuildFinished"),
                new EventHandler<CallbackArgs>(EventSourceAddBuildFinished));
            mockSource.AddMethodCallback(
                string.Format("{0}.{1}", typeof(IEventSource).FullName, "add_BuildStarted"),
                new EventHandler<CallbackArgs>(EventSourceAddBuildStarted));
            mockSource.AddMethodCallback(
                string.Format("{0}.{1}", typeof(IEventSource).FullName, "add_TaskStarted"),
                new EventHandler<CallbackArgs>(EventSourceAddTaskStarted));
            mockSource.AddMethodCallback(
                string.Format("{0}.{1}", typeof(IEventSource).FullName, "add_TaskFinished"),
                new EventHandler<CallbackArgs>(EventSourceAddTaskFinished));

            return mockSource;
        }
    }
}
