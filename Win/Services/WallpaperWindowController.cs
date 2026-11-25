using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using NOR.Interop;

namespace NOR.Services
{
    /// <summary>
    /// Manages the lifecycle and desktop injection of the live wallpaper window.
    /// Equivalent to macOS WallpaperWindowController.
    /// </summary>
    public class WallpaperWindowController
    {
        private WallpaperWindow? _window;
        private IntPtr _workerWHwnd = IntPtr.Zero;

        public bool IsAttached => _window != null && _workerWHwnd != IntPtr.Zero;

        public WallpaperWindow? CurrentWindow => _window;

        /// <summary>
        /// Creates the wallpaper window and injects it behind Windows desktop icons via WorkerW.
        /// </summary>
        public bool AttachToDesktop()
        {
            try
            {
                if (_window == null)
                {
                    _window = new WallpaperWindow();
                }

                // Show window offscreen initially to initialize the HWND
                _window.Show();

                var helper = new WindowInteropHelper(_window);
                IntPtr windowHwnd = helper.Handle;

                // Resolve WorkerW behind desktop icons
                _workerWHwnd = Win32.GetDesktopWorkerW();
                if (_workerWHwnd == IntPtr.Zero)
                {
                    Debug.WriteLine("[NŌR-Controller] Could not find desktop WorkerW.");
                    return false;
                }

                // Parent the window to WorkerW
                Win32.SetParent(windowHwnd, _workerWHwnd);

                // Set styles for borderless child window that does not steal focus
                int style = (int)Win32.GetWindowLongPtr(windowHwnd, Win32.GWL_STYLE);
                style = (style & ~(int)Win32.WS_POPUP) | (int)(Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_CLIPSIBLINGS);
                Win32.SetWindowLongPtr(windowHwnd, Win32.GWL_STYLE, (IntPtr)style);

                // Fit to virtual screen or primary monitor
                int width = (int)SystemParameters.VirtualScreenWidth;
                int height = (int)SystemParameters.VirtualScreenHeight;
                int left = (int)SystemParameters.VirtualScreenLeft;
                int top = (int)SystemParameters.VirtualScreenTop;

                Win32.SetWindowPos(
                    windowHwnd,
                    Win32.HWND_BOTTOM,
                    left,
                    top,
                    width,
                    height,
                    Win32.SWP_SHOWWINDOW | Win32.SWP_NOACTIVATE | Win32.SWP_FRAMECHANGED
                );

                Debug.WriteLine($"[NŌR-Controller] Successfully injected wallpaper window (Size: {width}x{height}) into WorkerW 0x{_workerWHwnd.ToInt64():X}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NŌR-Controller] Failed to attach wallpaper: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unhooks the wallpaper window and restores normal desktop view.
        /// </summary>
        public void DetachFromDesktop()
        {
            try
            {
                if (_window != null)
                {
                    _window.Stop();
                    _window.Close();
                    _window = null;
                }

                _workerWHwnd = IntPtr.Zero;

                // Refresh Windows desktop wallpaper
                Win32.RefreshDesktop();
                Debug.WriteLine("[NŌR-Controller] Wallpaper detached and desktop refreshed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NŌR-Controller] Error while detaching: {ex.Message}");
            }
        }
    }
}

