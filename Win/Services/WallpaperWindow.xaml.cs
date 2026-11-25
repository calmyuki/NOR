using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using NOR.Interop;

namespace NOR.Services
{
    /// <summary>
    /// The borderless desktop-level window hosting the hardware-accelerated MediaElement.
    /// Injected behind Windows Explorer's desktop icons using Win32 WorkerW reparenting.
    /// </summary>
    public partial class WallpaperWindow : Window
    {
        private bool _isPlaybackActive = false;
        private double _userVolume = 1.0;
        private bool _isMuted = false;

        public WallpaperWindow()
        {
            InitializeComponent();
        }

        public void Play(string videoPath)
        {
            try
            {
                Player.Source = new Uri(videoPath, UriKind.Absolute);
                Player.Volume = _isMuted ? 0 : _userVolume;
                Player.Play();
                _isPlaybackActive = true;
                Debug.WriteLine($"[NŌR-Window] Started playback: {videoPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NŌR-Window] Playback failed: {ex.Message}");
            }
        }

        public void Pause()
        {
            if (_isPlaybackActive)
            {
                Player.Pause();
                Debug.WriteLine("[NŌR-Window] Playback paused.");
            }
        }

        public void Resume()
        {
            if (_isPlaybackActive)
            {
                Player.Play();
                Debug.WriteLine("[NŌR-Window] Playback resumed.");
            }
        }

        public void Stop()
        {
            _isPlaybackActive = false;
            Player.Stop();
            Player.Source = null;
            Debug.WriteLine("[NŌR-Window] Playback stopped.");
        }

        public void SetVolume(double volume)
        {
            _userVolume = Math.Clamp(volume, 0.0, 1.0);
            if (!_isMuted)
            {
                Player.Volume = _userVolume;
            }
        }

        public void SetMuted(bool muted)
        {
            _isMuted = muted;
            Player.Volume = _isMuted ? 0.0 : _userVolume;
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Seamless looping for video wallpaper
            Player.Position = TimeSpan.Zero;
            Player.Play();
        }

        private void Player_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            Debug.WriteLine($"[NŌR-Window] Media error: {e.ErrorException?.Message}");
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Set window styles to be transparent to mouse events and hidden from Alt-Tab
            var helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;

            int exStyle = (int)Win32.GetWindowLongPtr(hwnd, Win32.GWL_EXSTYLE);
            exStyle |= (int)(Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_TRANSPARENT | Win32.WS_EX_NOACTIVATE);
            Win32.SetWindowLongPtr(hwnd, Win32.GWL_EXSTYLE, (IntPtr)exStyle);
        }
    }
}

