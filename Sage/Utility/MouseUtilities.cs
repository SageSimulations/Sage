/* This source code licensed under the GNU Affero General Public License */
#if NODELETE
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Drawing;

namespace Highpoint.Sage.Utility {
    public static class MouseUtilities {
        public static System.Windows.Point CorrectGetPosition(Visual relativeTo) {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return relativeTo.PointFromScreen(new System.Windows.Point(w32Mouse.X, w32Mouse.Y));
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point {
            public Int32 X;
            public Int32 Y;
        };

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);
    }
}
#endif