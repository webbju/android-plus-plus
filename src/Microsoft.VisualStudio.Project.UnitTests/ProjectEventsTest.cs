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
using System.Reflection;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.Project.UnitTests
{
    public static class ProjectEventsUtilities
    {
        private static ConstructorInfo afterProjectFileOpenedEventArgsCtr;
        public static AfterProjectFileOpenedEventArgs CreateAfterProjectFileOpenedEventArgs(bool added)
        {
            if(null == afterProjectFileOpenedEventArgsCtr)
            {
                //afterProjectFileOpenedEventArgsCtr = typeof(AfterProjectFileOpenedEventArgs).GetConstructor(new Type[] { typeof(bool) });
                afterProjectFileOpenedEventArgsCtr = typeof(AfterProjectFileOpenedEventArgs).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(bool) }, null);
            }
            return afterProjectFileOpenedEventArgsCtr.Invoke(new object[] { added }) as AfterProjectFileOpenedEventArgs;
        }

        private static ConstructorInfo beforeProjectFileClosedEventArgsCtr;
        public static BeforeProjectFileClosedEventArgs CreateBeforeProjectFileClosedEventArgs(bool removed)
        {
            if(null == beforeProjectFileClosedEventArgsCtr)
            {
                beforeProjectFileClosedEventArgsCtr = typeof(BeforeProjectFileClosedEventArgs).GetConstructor(new Type[] { typeof(bool) });
            }
            return beforeProjectFileClosedEventArgsCtr.Invoke(new object[] { removed }) as BeforeProjectFileClosedEventArgs;
        }
    }

    [TestClass]
    public class ProjectEventsTest
    {
        private class ProjectEventsSource : IProjectEvents, IDisposable
        {
            public enum ProjectEventsSinkType
            {
                AfterOpened,
                BeforeClosed,
                AnyEvent
            }
            public event EventHandler<AfterProjectFileOpenedEventArgs> AfterProjectFileOpened;
            public event EventHandler<BeforeProjectFileClosedEventArgs> BeforeProjectFileClosed;

            public void SignalOpenStatus(bool isOpened)
            {
                if(isOpened)
                {
                    if(null != AfterProjectFileOpened)
                    {
                        AfterProjectFileOpened(this, ProjectEventsUtilities.CreateAfterProjectFileOpenedEventArgs(true));
                    }
                }
                else
                {
                    if(null != BeforeProjectFileClosed)
                    {
                        BeforeProjectFileClosed(this, ProjectEventsUtilities.CreateBeforeProjectFileClosedEventArgs(true));
                    }
                }
            }

            public bool IsSinkRegister(ProjectEventsSinkType sinkType)
            {
                if(ProjectEventsSinkType.AfterOpened == sinkType)
                {
                    return (null != AfterProjectFileOpened);
                }
                if(ProjectEventsSinkType.BeforeClosed == sinkType)
                {
                    return (null != BeforeProjectFileClosed);
                }
                return (null != AfterProjectFileOpened) || (null != BeforeProjectFileClosed);
            }

            public void Dispose()
            {
                Assert.IsFalse(IsSinkRegister(ProjectEventsSinkType.AnyEvent), "ProjectEvents sink registered at shutdown.");
            }
        }

        private static FieldInfo projectOpened;
        private static bool IsProjectOpened(ProjectNode project)
        {
            if(null == projectOpened)
            {
                projectOpened = typeof(VisualStudio.Project.ProjectNode).GetField("projectOpened", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            return (bool)projectOpened.GetValue(project);
        }

        [TestMethod]
        public void SetOpenStatusTest()
        {
            using(ProjectEventsSource eventSource = new ProjectEventsSource())
            {
                ProjectTestClass project = new ProjectTestClass();
                IProjectEventsProvider eventProvider = project as IProjectEventsProvider;
                Assert.IsNotNull(eventProvider, "Project class does not implements IProjectEventsProvider.");
                Assert.IsFalse(IsProjectOpened(project), "Project is opened right after its creation.");
                eventProvider.ProjectEventsProvider = eventSource;
                eventSource.SignalOpenStatus(true);
                Assert.IsTrue(IsProjectOpened(project), "Project is not opened after the AfterProjectFileOpened is signaled.");
                project.Close();
            }
        }

        [TestMethod]
        public void SetMultipleSource()
        {
            using(ProjectEventsSource firstSource = new ProjectEventsSource())
            {
                using(ProjectEventsSource secondSource = new ProjectEventsSource())
                {
                    ProjectTestClass project = new ProjectTestClass();
                    IProjectEventsProvider eventProvider = project as IProjectEventsProvider;
                    Assert.IsNotNull(eventProvider, "Project class does not implements IProjectEventsProvider.");
                    eventProvider.ProjectEventsProvider = firstSource;
                    eventProvider.ProjectEventsProvider = secondSource;
                    Assert.IsFalse(IsProjectOpened(project));
                    firstSource.SignalOpenStatus(true);
                    Assert.IsFalse(IsProjectOpened(project));
                    secondSource.SignalOpenStatus(true);
                    Assert.IsTrue(IsProjectOpened(project));
                    project.Close();
                }
            }
        }

        [TestMethod]
        public void SetNullSource()
        {
            ProjectTestClass project = new ProjectTestClass();
            IProjectEventsProvider eventProvider = project as IProjectEventsProvider;
            Assert.IsNotNull(eventProvider, "Project class does not implements IProjectEventsProvider.");
            eventProvider.ProjectEventsProvider = null;
            project.Close();
        }
    }
}
