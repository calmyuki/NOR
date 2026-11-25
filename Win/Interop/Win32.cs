using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace NOR.Interop
{
    /// <summary>
    /// Native Windows API definitions and helpers for desktop window injection,
    /// fullscreen occlusion detection, and hardware-accelerated video canvas positioning.
    /// </summary>
    public static class Win32
    {
        #region Constants & Flags

        public const int WM_ERASEBKGND = 0x0014;
        public const int WM_WINDOWPOSCHANGING = 0x0046;
        public const int WM_NCDESTROY = 0x0082;
        
        // Window Long indexes
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const int GWLP_HWNDPARENT = -8;

        // Window Styles
        public const uint WS_CHILD = 0x40000000;
        public const uint WS_VISIBLE = 0x10000000;
        public const uint WS_CLIPSIBLINGS = 0x04000000;
        public const uint WS_CLIPCHILDREN = 0x02000000;
        public const uint WS_POPUP = 0x80000000;

        // Extended Window Styles
        public const uint WS_EX_TOOLWINDOW = 0x00000080;
        public const uint WS_EX_TRANSPARENT = 0x00000020;
        public const uint WS_EX_NOACTIVATE = 0x08000000;
        public const uint WS_EX_LAYERED = 0x00080000;

        // SetWindowPos Flags
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOREDRAW = 0x0008;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_HIDEWINDOW = 0x0080;
        public const uint SWP_ASYNCWINDOWPOS = 0x4000;

        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public static readonly IntPtr HWND_TOP = new IntPtr(0);

        // SendMessageTimeout flags
        [Flags]
        public enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0,
            SMTO_BLOCK = 0x1,
            SMTO_ABORTIFHUNG = 0x2,
            SMTO_NOTIMEOUTIFNOTHUNG = 0x8
        }

        public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        public const uint SPI_SETDESKWALLPAPER = 0x0014;
        public const uint SPIF_UPDATEINIFILE = 0x01;
        public const uint SPIF_SENDCHANGE = 0x02;

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        #endregion

        #region Delegates

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        #endregion

        #region P/Invoke Signatures

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam,
            SendMessageTimeoutFlags fuFlags,
            uint uTimeout,
            out UIntPtr lpdwResult);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(uint uAction, uint uParam, string? lpvParam, uint fuWinIni);

        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            return GetWindowLong32(hWnd, nIndex);
        }

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            return SetWindowLong32(hWnd, nIndex, dwNewLong);
        }

        #endregion

        #region High-Level Desktop Hook Helpers

        /// <summary>
        /// Retrieves the special WorkerW window handle positioned immediately behind
        /// the Windows desktop icons (SHELLDLL_DefView).
        /// </summary>
        public static IntPtr GetDesktopWorkerW()
        {
            // 1. Fetch Progman handle
            IntPtr progman = FindWindow("Progman", null);
            if (progman == IntPtr.Zero)
            {
                Debug.WriteLine("[NŌR-Win32] Failed to find Progman window.");
                return IntPtr.Zero;
            }

            // 2. Send the special 0x052C message to Progman.
            // This instructs Windows Explorer to spawn a WorkerW window behind the desktop icon view.
            UIntPtr result;
            SendMessageTimeout(
                progman,
                0x052C,
                new IntPtr(0xD),
                new IntPtr(0x1),
                SendMessageTimeoutFlags.SMTO_NORMAL,
                1000,
                out result
            );

            // 3. Enumerate top-level windows to locate the WorkerW sibling created behind SHELLDLL_DefView
            IntPtr workerW = IntPtr.Zero;

            EnumWindows((tophandle, topparamhandle) =>
            {
                IntPtr shellView = FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellView != IntPtr.Zero)
                {
                    // Gets the WorkerW window immediately behind SHELLDLL_DefView
                    workerW = FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
                }
                return true;
            }, IntPtr.Zero);

            // Fallback: On certain Windows 11 updates, SHELLDLL_DefView might be parented directly
            if (workerW == IntPtr.Zero)
            {
                EnumWindows((tophandle, topparamhandle) =>
                {
                    var sb = new StringBuilder(256);
                    GetClassName(tophandle, sb, sb.Capacity);
                    if (sb.ToString() == "WorkerW")
                    {
                        IntPtr shellView = FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (shellView == IntPtr.Zero)
                        {
                            workerW = tophandle;
                            return false; // found candidate
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }

            Debug.WriteLine($"[NŌR-Win32] Resolved WorkerW handle: 0x{workerW.ToInt64():X}");
            return workerW;
        }

        /// <summary>
        /// Checks whether the current foreground application window is running full-screen
        /// (covering the entire screen, e.g. games, full-screen video, presentations).
        /// </summary>
        public static bool IsForegroundAppFullscreen(out IntPtr foregroundHwnd)
        {
            foregroundHwnd = GetForegroundWindow();
            if (foregroundHwnd == IntPtr.Zero)
                return false;

            // If minimized, not fullscreen
            if (IsIconic(foregroundHwnd))
                return false;

            // Desktop / Shell windows should never be treated as fullscreen apps
            var classNameBuilder = new StringBuilder(256);
            GetClassName(foregroundHwnd, classNameBuilder, classNameBuilder.Capacity);
            string className = classNameBuilder.ToString();

            if (className == "Progman" || className == "WorkerW" || className == "Shell_TrayWnd")
                return false;

            // Get foreground window bounds
            if (!GetWindowRect(foregroundHwnd, out RECT appRect))
                return false;

            // Get monitor bounds for the window
            IntPtr hMonitor = MonitorFromWindow(foregroundHwnd, MONITOR_DEFAULTTONEAREST);
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
            if (!GetMonitorInfo(hMonitor, ref mi))
                return false;

            // Check if window completely covers the monitor bounds
            bool isCovering = (appRect.Left <= mi.rcMonitor.Left &&
                               appRect.Top <= mi.rcMonitor.Top &&
                               appRect.Right >= mi.rcMonitor.Right &&
                               appRect.Bottom >= mi.rcMonitor.Bottom);

            return isCovering;
        }

        /// <summary>
        /// Checks if the user is currently on the desktop (no app active or Progman/WorkerW active).
        /// </summary>
        public static bool IsDesktopForeground()
        {
            IntPtr foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero)
                return true;

            var sb = new StringBuilder(256);
            GetClassName(foreground, sb, sb.Capacity);
            string cls = sb.ToString();

            return (cls == "Progman" || cls == "WorkerW");
        }

        /// <summary>
        /// Restores desktop wallpaper background, removing any leftover artifacts.
        /// </summary>
        public static void RefreshDesktop()
        {
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, null, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }

        #endregion
    }
}

