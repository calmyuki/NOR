using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using NOR.Models;

namespace NOR.Services
{
    /// <summary>
    /// System tray companion for NŌR. Runs in the Windows Notification Area.
    /// Equivalent to macOS MenuBarManager.
    /// </summary>
    public class TrayIconManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly WallpaperPlaybackManager _playbackManager;
        private readonly Window _mainWindow;

        public TrayIconManager(Window mainWindow, WallpaperPlaybackManager playbackManager)
        {
            _mainWindow = mainWindow;
            _playbackManager = playbackManager;

            _notifyIcon = new NotifyIcon
            {
                Text = "NŌR - Dynamic Live Wallpaper Engine",
                Visible = true
            };

            // Load icon
            LoadIcon();

            // Setup Context Menu
            BuildContextMenu();

            _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
        }

        private void LoadIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "tray.ico");
                if (File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    // Fallback to application default icon
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NŌR-Tray] Failed to load icon: {ex.Message}");
                _notifyIcon.Icon = SystemIcons.Application;
            }
        }

        private void BuildContextMenu()
        {
            var menu = new ContextMenuStrip();

            var openItem = new ToolStripMenuItem("Open NŌR Studio", null, (s, e) => ShowMainWindow());
            openItem.Font = new Font(openItem.Font, System.Drawing.FontStyle.Bold);
            menu.Items.Add(openItem);

            menu.Items.Add(new ToolStripSeparator());

            var playPauseItem = new ToolStripMenuItem("Play / Pause Wallpaper", null, (s, e) =>
            {
                _playbackManager.TogglePlayPauseDesktop();
            });
            menu.Items.Add(playPauseItem);

            var muteItem = new ToolStripMenuItem("Mute Wallpaper Sound", null, (s, e) =>
            {
                _playbackManager.IsDesktopMuted = !_playbackManager.IsDesktopMuted;
            });
            menu.Items.Add(muteItem);

            menu.Items.Add(new ToolStripSeparator());

            var fullscreenItem = new ToolStripMenuItem("Suspend on Fullscreen Apps", null, (s, e) =>
            {
                var item = (ToolStripMenuItem)s!;
                _playbackManager.Settings.PauseOnFullScreen = item.Checked;
            })
            {
                CheckOnClick = true,
                Checked = _playbackManager.Settings.PauseOnFullScreen
            };
            menu.Items.Add(fullscreenItem);

            var autoMuteItem = new ToolStripMenuItem("Auto-Mute on App Focus", null, (s, e) =>
            {
                var item = (ToolStripMenuItem)s!;
                _playbackManager.Settings.AutoPauseSoundWhenAppOpen = item.Checked;
            })
            {
                CheckOnClick = true,
                Checked = _playbackManager.Settings.AutoPauseSoundWhenAppOpen
            };
            menu.Items.Add(autoMuteItem);

            var startupItem = new ToolStripMenuItem("Launch at Windows Startup", null, (s, e) =>
            {
                var item = (ToolStripMenuItem)s!;
                StartupManager.SetStartup(item.Checked);
                _playbackManager.Settings.StartAtLogin = item.Checked;
            })
            {
                CheckOnClick = true,
                Checked = StartupManager.IsStartupEnabled()
            };
            menu.Items.Add(startupItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit NŌR", null, (s, e) =>
            {
                _playbackManager.Shutdown();
                _notifyIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            });
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;
        }

        public void ShowMainWindow()
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}

