﻿using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Kenedia.Modules.QoL
{
    internal static class WindowUtil
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public static Point GetCursorPosition(IntPtr hWnd)
        {
            if (!GetCursorPos(out var pos)
                || !ScreenToClient(hWnd, ref pos)
                || !GetWindowRect(hWnd, out var wndBounds)
                || !GetClientRect(hWnd, out var clientBounds)) return Point.Empty;

            // Border thickness
            var widthOffset = wndBounds.Right - wndBounds.Left - (clientBounds.Right - clientBounds.Left);
            // Titlebar height + Border thickness
            var heightOffset = wndBounds.Bottom - wndBounds.Top - (clientBounds.Bottom - clientBounds.Top);
            pos.X -= wndBounds.Left + widthOffset;
            pos.Y -= wndBounds.Top + heightOffset;

            return !ClientToScreen(hWnd, ref pos) ? Point.Empty : new Point(pos.X, pos.Y);
        }
    }
}