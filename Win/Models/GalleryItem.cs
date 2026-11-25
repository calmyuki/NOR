using System;
using System.IO;
using System.Text.Json.Serialization;

namespace NOR.Models
{
    /// <summary>
    /// Model representing a video or animated wallpaper stored in the user's local gallery.
    /// Matches the macOS NŌR GalleryItem specification.
    /// </summary>
    public class GalleryItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OriginalFormat { get; set; } = "MP4";
        public bool IsConverted { get; set; } = false;
        public string FileSizeString { get; set; } = "0 MB";
        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }
        public double DurationSeconds { get; set; }
        public bool HasAudioTrack { get; set; } = true;
        public DateTime DateAdded { get; set; } = DateTime.Now;
        public string? ThumbnailPath { get; set; }

        [JsonIgnore]
        public string ResolutionDescription
        {
            get
            {
                if (ResolutionWidth > 0 && ResolutionHeight > 0)
                    return $"{ResolutionWidth} × {ResolutionHeight}";
                return "1920 × 1080";
            }
        }

        [JsonIgnore]
        public string DurationDescription
        {
            get
            {
                if (DurationSeconds <= 0)
                    return "0:00";

                var time = TimeSpan.FromSeconds(DurationSeconds);
                if (time.Hours > 0)
                    return $"{time.Hours}:{time.Minutes:D2}:{time.Seconds:D2}";
                return $"{time.Minutes}:{time.Seconds:D2}";
            }
        }

        [JsonIgnore]
        public string FormattedDate => DateAdded.ToString("MMM d, yyyy");

        [JsonIgnore]
        public bool ExistsOnDisk => File.Exists(FilePath);
    }
}

