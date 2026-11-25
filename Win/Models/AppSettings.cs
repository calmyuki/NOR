using System;

namespace NOR.Models
{
    /// <summary>
    /// User preferences and persistent state for NŌR Live Wallpaper on Windows.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Automatically pause wallpaper sound when any application window is active.
        /// Unmutes sound when the user returns to the clean desktop.
        /// </summary>
        public bool AutoPauseSoundWhenAppOpen { get; set; } = true;

        /// <summary>
        /// Suspends video decoding when a full-screen game, presentation, or video is active,
        /// bringing idle GPU and CPU usage down to 0%.
        /// </summary>
        public bool PauseOnFullScreen { get; set; } = true;

        /// <summary>
        /// Automatically launch NŌR on Windows startup.
        /// </summary>
        public bool StartAtLogin { get; set; } = false;

        /// <summary>
        /// Master volume for the desktop live wallpaper (0.0 to 1.0).
        /// </summary>
        public double MasterVolume { get; set; } = 1.0;

        /// <summary>
        /// Global desktop audio mute toggle.
        /// </summary>
        public bool IsMuted { get; set; } = false;

        /// <summary>
        /// ID of the currently active desktop wallpaper in the gallery.
        /// </summary>
        public Guid? ActiveWallpaperId { get; set; }
    }
}

