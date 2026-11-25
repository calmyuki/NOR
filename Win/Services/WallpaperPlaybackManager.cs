using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NOR.Models;

namespace NOR.Services
{
    /// <summary>
    /// Central coordinator for in-app video preview, desktop live wallpaper playback,
    /// power management, and intelligent audio ducking on Windows.
    /// Equivalent to macOS WallpaperPlaybackManager.
    /// </summary>
    public class WallpaperPlaybackManager : INotifyPropertyChanged
    {
        private readonly WallpaperWindowController _windowController = new();
        private readonly FullScreenAppMonitor _monitor = new();
        private readonly AppSettings _settings;

        private GalleryItem? _activeDesktopItem;
        private GalleryItem? _selectedPreviewItem;
        private bool _isDesktopActive = false;
        private bool _isDesktopPaused = false;
        private bool _isDesktopSuspendedForFullScreen = false;
        private bool _isSoundAutoPausedForApp = false;
        private double _desktopVolume = 1.0;
        private bool _isDesktopMuted = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public GalleryItem? ActiveDesktopItem
        {
            get => _activeDesktopItem;
            set { _activeDesktopItem = value; OnPropertyChanged(); }
        }

        public GalleryItem? SelectedPreviewItem
        {
            get => _selectedPreviewItem;
            set { _selectedPreviewItem = value; OnPropertyChanged(); }
        }

        public bool IsDesktopActive
        {
            get => _isDesktopActive;
            set { _isDesktopActive = value; OnPropertyChanged(); }
        }

        public bool IsDesktopPaused
        {
            get => _isDesktopPaused;
            set { _isDesktopPaused = value; OnPropertyChanged(); }
        }

        public bool IsDesktopSuspendedForFullScreen
        {
            get => _isDesktopSuspendedForFullScreen;
            set { _isDesktopSuspendedForFullScreen = value; OnPropertyChanged(); }
        }

        public double DesktopVolume
        {
            get => _desktopVolume;
            set
            {
                _desktopVolume = Math.Clamp(value, 0.0, 1.0);
                _settings.MasterVolume = _desktopVolume;
                _windowController.CurrentWindow?.SetVolume(_desktopVolume);
                OnPropertyChanged();
            }
        }

        public bool IsDesktopMuted
        {
            get => _isDesktopMuted;
            set
            {
                _isDesktopMuted = value;
                _settings.IsMuted = _isDesktopMuted;
                UpdateAudioState();
                OnPropertyChanged();
            }
        }

        public AppSettings Settings => _settings;

        public WallpaperPlaybackManager(AppSettings settings)
        {
            _settings = settings;
            _desktopVolume = settings.MasterVolume;
            _isDesktopMuted = settings.IsMuted;

            _monitor.FullScreenStateChanged += OnFullScreenChanged;
            _monitor.AppDisplayStateChanged += OnAppDisplayChanged;
            _monitor.Start();
        }

        public void SetDesktopWallpaper(GalleryItem item)
        {
            if (!_windowController.IsAttached)
            {
                if (!_windowController.AttachToDesktop())
                {
                    Debug.WriteLine("[NŌR-Playback] Failed to attach wallpaper to desktop.");
                    return;
                }
            }

            ActiveDesktopItem = item;
            _settings.ActiveWallpaperId = item.Id;
            IsDesktopActive = true;
            IsDesktopPaused = false;

            _windowController.CurrentWindow?.Play(item.FilePath);
            _windowController.CurrentWindow?.SetVolume(_desktopVolume);
            UpdateAudioState();

            Debug.WriteLine($"[NŌR-Playback] Live desktop wallpaper active: {item.FileName}");
        }

        public void TogglePlayPauseDesktop()
        {
            if (!IsDesktopActive) return;

            if (IsDesktopPaused)
            {
                _windowController.CurrentWindow?.Resume();
                IsDesktopPaused = false;
            }
            else
            {
                _windowController.CurrentWindow?.Pause();
                IsDesktopPaused = true;
            }
        }

        public void StopDesktop()
        {
            _windowController.DetachFromDesktop();
            IsDesktopActive = false;
            IsDesktopPaused = false;
            ActiveDesktopItem = null;
            _settings.ActiveWallpaperId = null;
            Debug.WriteLine("[NŌR-Playback] Stopped live desktop wallpaper.");
        }

        private void OnFullScreenChanged(bool isFullScreen)
        {
            if (!_settings.PauseOnFullScreen || !IsDesktopActive)
                return;

            IsDesktopSuspendedForFullScreen = isFullScreen;

            if (isFullScreen)
            {
                Debug.WriteLine("[NŌR-Playback] Fullscreen app detected -> Suspending video decoding (0% GPU).");
                _windowController.CurrentWindow?.Pause();
            }
            else if (!IsDesktopPaused)
            {
                Debug.WriteLine("[NŌR-Playback] Exited fullscreen -> Resuming video playback.");
                _windowController.CurrentWindow?.Resume();
            }
        }

        private void OnAppDisplayChanged(bool isAppOpen)
        {
            if (!_settings.AutoPauseSoundWhenAppOpen || !IsDesktopActive)
                return;

            _isSoundAutoPausedForApp = isAppOpen;
            UpdateAudioState();
        }

        private void UpdateAudioState()
        {
            bool shouldMute = _isDesktopMuted || (_settings.AutoPauseSoundWhenAppOpen && _isSoundAutoPausedForApp);
            _windowController.CurrentWindow?.SetMuted(shouldMute);
        }

        public void Shutdown()
        {
            _monitor.Stop();
            _windowController.DetachFromDesktop();
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

