using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using NOR.Models;
using NOR.Services;
using NOR.Views;

namespace NOR
{
    public partial class MainWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly WallpaperGalleryManager _galleryManager;
        private readonly WallpaperPlaybackManager _playbackManager;
        private readonly TrayIconManager _trayManager;
        private readonly DispatcherTimer _scrubberTimer;

        private GalleryItem? _selectedItem;
        private bool _isPreviewPlaying = false;
        private bool _isDraggingScrubber = false;
        private bool _isExplicitExit = false;

        public MainWindow()
        {
            InitializeComponent();

            _settings = new AppSettings();
            _galleryManager = new WallpaperGalleryManager();
            _playbackManager = new WallpaperPlaybackManager(_settings);
            _trayManager = new TrayIconManager(this, _playbackManager);

            // Connect gallery
            LstGallery.ItemsSource = _galleryManager.Items;
            UpdateWallpaperCount();

            // Scrubber timer
            _scrubberTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _scrubberTimer.Tick += ScrubberTimer_Tick;

            // Load settings into UI
            ChkPauseFullScreen.IsChecked = _settings.PauseOnFullScreen;
            ChkAutoMute.IsChecked = _settings.AutoPauseSoundWhenAppOpen;
            ChkStartup.IsChecked = StartupManager.IsStartupEnabled();
            SliderDesktopVolume.Value = _settings.MasterVolume;

            // Auto-select first item if available
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_galleryManager.Items.Count > 0)
            {
                LstGallery.SelectedIndex = 0;
            }
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            // If live wallpaper is running, minimize to system tray instead of stopping
            if (!_isExplicitExit && _playbackManager.IsDesktopActive)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                _scrubberTimer.Stop();
                _playbackManager.Shutdown();
                _trayManager.Dispose();
            }
        }

        private void UpdateWallpaperCount()
        {
            TxtWallpaperCount.Text = _galleryManager.Items.Count.ToString();
        }

        private void LstGallery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstGallery.SelectedItem is GalleryItem item)
            {
                _selectedItem = item;
                TxtSelectedTitle.Text = item.FileName;
                PlaceholderPanel.Visibility = Visibility.Collapsed;

                try
                {
                    PreviewPlayer.Source = new Uri(item.FilePath, UriKind.Absolute);
                    PreviewPlayer.Volume = SliderPreviewVolume.Value;
                    PreviewPlayer.Play();
                    _isPreviewPlaying = true;
                    BtnPreviewPlayPause.Content = "⏸";
                    _scrubberTimer.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not preview video: {ex.Message}", "Preview Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import Live Wallpaper Media",
                Filter = "All Supported Media (*.mp4;*.mov;*.webm;*.mkv;*.avi;*.wmv;*.gif)|*.mp4;*.mov;*.webm;*.mkv;*.avi;*.wmv;*.gif|MP4 Video (*.mp4)|*.mp4|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                GalleryItem? lastImported = null;
                foreach (string file in dialog.FileNames)
                {
                    var item = _galleryManager.ImportFile(file);
                    if (item != null)
                        lastImported = item;
                }

                UpdateWallpaperCount();

                if (lastImported != null)
                {
                    LstGallery.SelectedItem = lastImported;
                }
            }
        }

        private void BtnDeleteWallpaper_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GalleryItem item)
            {
                var result = MessageBox.Show(
                    $"Remove \"{item.FileName}\" from NŌR Gallery?",
                    "Confirm Removal",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    if (_playbackManager.ActiveDesktopItem?.Id == item.Id)
                    {
                        _playbackManager.StopDesktop();
                        UpdateDesktopStatusUI();
                    }

                    if (_selectedItem?.Id == item.Id)
                    {
                        PreviewPlayer.Stop();
                        PreviewPlayer.Source = null;
                        _selectedItem = null;
                        TxtSelectedTitle.Text = "No Media Selected";
                        PlaceholderPanel.Visibility = Visibility.Visible;
                    }

                    _galleryManager.RemoveItem(item);
                    UpdateWallpaperCount();
                }
            }
        }

        private void BtnSetDesktop_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null)
            {
                MessageBox.Show("Please select a wallpaper from the gallery first.", "NŌR Studio", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _playbackManager.SetDesktopWallpaper(_selectedItem);
            UpdateDesktopStatusUI();
        }

        private void BtnPauseResumeDesktop_Click(object sender, RoutedEventArgs e)
        {
            _playbackManager.TogglePlayPauseDesktop();
            UpdateDesktopStatusUI();
        }

        private void BtnStopDesktop_Click(object sender, RoutedEventArgs e)
        {
            _playbackManager.StopDesktop();
            UpdateDesktopStatusUI();
        }

        private void UpdateDesktopStatusUI()
        {
            if (_playbackManager.IsDesktopActive)
            {
                if (_playbackManager.IsDesktopPaused)
                {
                    IndicatorDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                    TxtStatus.Text = "Desktop: Paused";
                    BtnPauseResumeDesktop.Content = "▶ Resume Desktop";
                }
                else
                {
                    IndicatorDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                    TxtStatus.Text = "Desktop: Active (Live)";
                    BtnPauseResumeDesktop.Content = "⏸ Pause Desktop";
                }
            }
            else
            {
                IndicatorDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                TxtStatus.Text = "Desktop: Idle";
                BtnPauseResumeDesktop.Content = "⏸ Pause Desktop";
            }
        }

        private void BtnPreviewPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isPreviewPlaying)
            {
                PreviewPlayer.Pause();
                _isPreviewPlaying = false;
                BtnPreviewPlayPause.Content = "▶";
            }
            else
            {
                PreviewPlayer.Play();
                _isPreviewPlaying = true;
                BtnPreviewPlayPause.Content = "⏸";
            }
        }

        private void PreviewPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            PreviewPlayer.Position = TimeSpan.Zero;
            PreviewPlayer.Play();
        }

        private void PreviewPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (PreviewPlayer.NaturalDuration.HasTimeSpan)
            {
                SliderScrubber.Maximum = PreviewPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            }
        }

        private void ScrubberTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isDraggingScrubber && PreviewPlayer.NaturalDuration.HasTimeSpan)
            {
                SliderScrubber.Value = PreviewPlayer.Position.TotalSeconds;
                TxtTime.Text = $"{FormatTime(PreviewPlayer.Position)} / {FormatTime(PreviewPlayer.NaturalDuration.TimeSpan)}";
            }
        }

        private void SliderScrubber_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Allow user scrubbing
        }

        private string FormatTime(TimeSpan span)
        {
            if (span.Hours > 0)
                return $"{span.Hours}:{span.Minutes:D2}:{span.Seconds:D2}";
            return $"{span.Minutes}:{span.Seconds:D2}";
        }

        private void BtnMutePreview_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewPlayer.IsMuted)
            {
                PreviewPlayer.IsMuted = false;
                BtnMutePreview.Content = "🔊";
            }
            else
            {
                PreviewPlayer.IsMuted = true;
                BtnMutePreview.Content = "🔇";
            }
        }

        private void SliderPreviewVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PreviewPlayer != null)
            {
                PreviewPlayer.Volume = SliderPreviewVolume.Value;
            }
        }

        private void SliderDesktopVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_playbackManager != null)
            {
                _playbackManager.DesktopVolume = SliderDesktopVolume.Value;
            }
        }

        private void ChkPauseFullScreen_Changed(object sender, RoutedEventArgs e)
        {
            if (_playbackManager != null)
            {
                _playbackManager.Settings.PauseOnFullScreen = ChkPauseFullScreen.IsChecked == true;
            }
        }

        private void ChkAutoMute_Changed(object sender, RoutedEventArgs e)
        {
            if (_playbackManager != null)
            {
                _playbackManager.Settings.AutoPauseSoundWhenAppOpen = ChkAutoMute.IsChecked == true;
            }
        }

        private void ChkStartup_Changed(object sender, RoutedEventArgs e)
        {
            StartupManager.SetStartup(ChkStartup.IsChecked == true);
        }

        private void BtnTrim_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            var dlg = new VideoTrimDialog(_selectedItem) { Owner = this };
            dlg.ShowDialog();
        }

        private void BtnCrop_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            var dlg = new VideoCropDialog(_selectedItem) { Owner = this };
            dlg.ShowDialog();
        }
    }
}

