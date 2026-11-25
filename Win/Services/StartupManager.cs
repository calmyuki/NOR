using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace NOR.Services
{
    /// <summary>
    /// Manages running NŌR automatically at Windows startup using the registry.
    /// </summary>
    public static class StartupManager
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "NORLiveWallpaper";

        public static bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NŌR-Startup] Check error: {ex.Message}");
                return false;
            }
        }

        public static void SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\" --minimized");
                        Debug.WriteLine("[NŌR-Startup] Enabled launch on Windows startup.");
                    }
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    Debug.WriteLine("[NŌR-Startup] Disabled launch on Windows startup.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NŌR-Startup] Set error: {ex.Message}");
            }
        }
    }
}

