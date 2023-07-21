using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace p4KFinder
{
    internal static class Helper
    {
        public static List<Monitor> Monitors = new();
        public static List<Rect> MonitorRects = new();


        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int top;
            public int Right;
            public int bottom;
        }
        public struct Monitor
        {
            public int Width;
            public int Height;
        }

        public static bool MonitorEnumCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
        {
            MonitorRects.Add(lprcMonitor);
            Monitors.Add(new Monitor { Width = lprcMonitor.Right - lprcMonitor.Left, Height = lprcMonitor.bottom - lprcMonitor.top });
            Console.WriteLine($"Monitor {Monitors.Count}:");
            Console.WriteLine($"    Coordinates: (Left: {lprcMonitor.Left}, Top: {lprcMonitor.top}, Right: {lprcMonitor.Right}, Bottom: {lprcMonitor.bottom})");
            Console.WriteLine($"    Width: {lprcMonitor.Right - lprcMonitor.Left}");
            Console.WriteLine($"    Height: {lprcMonitor.bottom - lprcMonitor.top}\n");
            return true;
        }
    }
}
