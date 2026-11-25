using System;
using System.Diagnostics;
using System.Windows.Threading;
using NOR.Interop;

namespace NOR.Services
{
    /// <summary>
    /// Monitors system-wide foreground application states and fullscreen occlusion.
    /// Equivalent to macOS FullScreenAppMonitor.
    /// 1. Suspends video decoding when full-screen apps are active to reduce idle GPU/CPU to 0%.
    /// 2. Context-aware audio ducking: auto-mutes wallpaper sound when working in apps,
    ///    and restores sound when on the desktop.
    /// </summary>
    public class FullScreenAppMonitor
    {
        private readonly DispatcherTimer _timer;
        private bool _isFullScreenCovered = false;
        private bool _isAppOpenOnDisplay = false;

        public event Action<bool>? FullScreenStateChanged;
        public event Action<bool>? AppDisplayStateChanged;

        public bool IsFullScreenCovered => _isFullScreenCovered;
        public bool IsAppOpenOnDisplay => _isAppOpenOnDisplay;

        public FullScreenAppMonitor()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += MonitorTick;
        }

        public void Start()
        {
            _timer.Start();
            Debug.WriteLine("[NŌR-Monitor] FullScreenAppMonitor started.");
        }

        public void Stop()
        {
            _timer.Stop();
            Debug.WriteLine("[NŌR-Monitor] FullScreenAppMonitor stopped.");
        }

        private void MonitorTick(object? sender, EventArgs e)
        {
            try
            {
                // 1. Check Fullscreen occlusion
                bool isFullscreen = Win32.IsForegroundAppFullscreen(out _);
                if (isFullscreen != _isFullScreenCovered)
                {
                    _isFullScreenCovered = isFullscreen;
                    Debug.WriteLine($"[NŌR-Monitor] Fullscreen state changed: {_isFullScreenCovered}");
                    FullScreenStateChanged?.Invoke(_isFullScreenCovered);
                }

                // 2. Check Desktop focus (for context-aware audio ducking)
                bool isDesktop = Win32.IsDesktopForeground();
                bool isAppOpen = !isDesktop;

                if (isAppOpen != _isAppOpenOnDisplay)
                {
                    _isAppOpenOnDisplay = isAppOpen;
                    Debug.WriteLine($"[NŌR-Monitor] App display state changed: isAppOpen={_isAppOpenOnDisplay}");
                    AppDisplayStateChanged?.Invoke(_isAppOpenOnDisplay);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NŌR-Monitor] Tick error: {ex.Message}");
            }
        }
    }
}

