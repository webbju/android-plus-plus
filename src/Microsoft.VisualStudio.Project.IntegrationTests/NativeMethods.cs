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
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.VisualStudio.Project.IntegrationTests
{
    /// <summary>
    /// Defines pinvoked utility methods and internal VS Constants
    /// </summary>
    internal static class NativeMethods
    {
        internal delegate bool CallBack(IntPtr hwnd, IntPtr lParam);

        // Declare two overloaded SendMessage functions
        [DllImport("user32.dll")]
        internal static extern UInt32 SendMessage(IntPtr hWnd, UInt32 Msg,
            UInt32 wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern bool PeekMessage([In, Out] ref Microsoft.VisualStudio.OLE.Interop.MSG msg, HandleRef hwnd, int msgMin, int msgMax, int remove);

        [DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern bool TranslateMessage([In, Out] ref Microsoft.VisualStudio.OLE.Interop.MSG msg);

        [DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern int DispatchMessage([In] ref Microsoft.VisualStudio.OLE.Interop.MSG msg);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern int MsgWaitForMultipleObjects(int nCount, int pHandles, bool fWaitAll, int dwMilliseconds, int dwWakeMask);

        [DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

        [DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern uint GetCurrentThreadId();

        [DllImport("user32")]
        internal static extern int EnumChildWindows(IntPtr hwnd, CallBack x, IntPtr y);

        [DllImport("user32")]
        internal static extern bool IsWindowVisible(IntPtr hDlg);

        [DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32")]
        internal static extern int GetClassName(IntPtr hWnd,
                                               StringBuilder className,
                                               int stringLength);
        [DllImport("user32")]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder className, int stringLength);


        [DllImport("user32")]
        internal static extern bool EndDialog(IntPtr hDlg, int result);

        [DllImport("Kernel32")]
        internal static extern long GetLastError();

        internal const int QS_KEY = 0x0001,
                        QS_MOUSEMOVE = 0x0002,
                        QS_MOUSEBUTTON = 0x0004,
                        QS_POSTMESSAGE = 0x0008,
                        QS_TIMER = 0x0010,
                        QS_PAINT = 0x0020,
                        QS_SENDMESSAGE = 0x0040,
                        QS_HOTKEY = 0x0080,
                        QS_ALLPOSTMESSAGE = 0x0100,
                        QS_MOUSE = QS_MOUSEMOVE | QS_MOUSEBUTTON,
                        QS_INPUT = QS_MOUSE | QS_KEY,
                        QS_ALLEVENTS = QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY,
                        QS_ALLINPUT = QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY | QS_SENDMESSAGE;

        internal const int Facility_Win32 = 7;

        internal const int WM_CLOSE = 0x0010;

        internal const int
                       S_FALSE = 0x00000001,
                       S_OK = 0x00000000,

                       IDOK = 1,
                       IDCANCEL = 2,
                       IDABORT = 3,
                       IDRETRY = 4,
                       IDIGNORE = 5,
                       IDYES = 6,
                       IDNO = 7,
                       IDCLOSE = 8,
                       IDHELP = 9,
                       IDTRYAGAIN = 10,
                       IDCONTINUE = 11;

        internal static long HResultFromWin32(long error)
        {
            if(error <= 0)
            {
                return error;
            }

            return ((error & 0x0000FFFF) | (Facility_Win32 << 16) | 0x80000000);
        }

        /// <devdoc>
        /// Please use this "approved" method to compare file names.
        /// </devdoc>
        public static bool IsSamePath(string file1, string file2)
        {
            if(file1 == null || file1.Length == 0)
            {
                return (file2 == null || file2.Length == 0);
            }

            Uri uri1 = null;
            Uri uri2 = null;

            try
            {
                if(!Uri.TryCreate(file1, UriKind.Absolute, out uri1) || !Uri.TryCreate(file2, UriKind.Absolute, out uri2))
                {
                    return false;
                }

                if(uri1 != null && uri1.IsFile && uri2 != null && uri2.IsFile)
                {
                    return 0 == String.Compare(uri1.LocalPath, uri2.LocalPath, StringComparison.OrdinalIgnoreCase);
                }

                return file1 == file2;
            }
            catch(UriFormatException e)
            {
                System.Diagnostics.Trace.WriteLine("Exception " + e.Message);
            }

            return false;
        }

    }
}
