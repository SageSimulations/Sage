/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace Highpoint.Sage.Utility {
    public static class MouseUtilities {
        public static Point CorrectGetPosition(Visual relativeTo) {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return relativeTo.PointFromScreen(new Point(w32Mouse.X, w32Mouse.Y));
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);
    }
}
