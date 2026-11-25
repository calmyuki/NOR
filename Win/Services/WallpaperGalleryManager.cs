using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using NOR.Models;

namespace NOR.Services
{
    /// <summary>
    /// Manages the user's wallpaper gallery, metadata extraction,
    /// persistence, and thumbnail generation.
    /// Equivalent to macOS WallpaperGalleryManager.
    /// </summary>
    public class WallpaperGalleryManager
    {
        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NOR"
        );
        private static readonly string GalleryFilePath = Path.Combine(AppDataDir, "gallery.json");
        private static readonly string ThumbnailsDir = Path.Combine(AppDataDir, "Thumbnails");

        public ObservableCollection<GalleryItem> Items { get; } = new();

        public WallpaperGalleryManager()
        {
            Directory.CreateDirectory(AppDataDir);
            Directory.CreateDirectory(ThumbnailsDir);
            LoadGallery();
        }

        public void LoadGallery()
        {
            try
            {
                if (File.Exists(GalleryFilePath))
                {
                    string json = File.ReadAllText(GalleryFilePath);
                    var items = JsonSerializer.Deserialize<GalleryItem[]>(json);
                    if (items != null)
                    {
                        Items.Clear();
                        foreach (var item in items)
                        {
                            if (File.Exists(item.FilePath))
                            {
                                Items.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NŌR-Gallery] Failed to load gallery: {ex.Message}");
            }
        }

        public void SaveGallery()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Items.ToArray(), options);
                File.WriteAllText(GalleryFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NŌR-Gallery] Failed to save gallery: {ex.Message}");
            }
        }

        public GalleryItem? ImportFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                var fileInfo = new FileInfo(filePath);
                string extension = fileInfo.Extension.TrimStart('.').ToUpperInvariant();
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                // Format file size
                string fileSizeStr;
                double mb = fileInfo.Length / (1024.0 * 1024.0);
                if (mb >= 1000)
                    fileSizeStr = $"{mb / 1024.0:F2} GB";
                else
                    fileSizeStr = $"{mb:F1} MB";

                var item = new GalleryItem
                {
                    Id = Guid.NewGuid(),
                    FilePath = filePath,
                    FileName = fileName,
                    OriginalFormat = extension,
                    FileSizeString = fileSizeStr,
                    DateAdded = DateTime.Now,
                    ResolutionWidth = 1920,
                    ResolutionHeight = 1080,
                    DurationSeconds = 15.0
                };

                // Generate simple thumbnail placeholder
                string thumbPath = Path.Combine(ThumbnailsDir, $"{item.Id}.png");
                GenerateThumbnail(item, thumbPath);
                item.ThumbnailPath = thumbPath;

                Items.Insert(0, item);
                SaveGallery();

                Debug.WriteLine($"[NŌR-Gallery] Imported {item.FileName} ({item.FileSizeString})");
                return item;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NŌR-Gallery] Import failed: {ex.Message}");
                return null;
            }
        }

        public void RemoveItem(GalleryItem item)
        {
            if (Items.Contains(item))
            {
                Items.Remove(item);
                if (!string.IsNullOrEmpty(item.ThumbnailPath) && File.Exists(item.ThumbnailPath))
                {
                    try { File.Delete(item.ThumbnailPath); } catch { }
                }
                SaveGallery();
            }
        }

        private void GenerateThumbnail(GalleryItem item, string destPath)
        {
            try
            {
                using var bmp = new Bitmap(320, 180);
                using var g = Graphics.FromImage(bmp);
                g.Clear(Color.FromArgb(20, 24, 32));

                using var brush = new SolidBrush(Color.FromArgb(96, 165, 250));
                using var font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold);
                g.DrawString("NŌR LIVE", font, brush, new PointF(20, 20));

                using var subBrush = new SolidBrush(Color.FromArgb(160, 170, 190));
                using var subFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular);
                g.DrawString(item.FileName, subFont, subBrush, new PointF(20, 50));
                g.DrawString($"{item.OriginalFormat} • {item.FileSizeString}", subFont, subBrush, new PointF(20, 80));

                bmp.Save(destPath, ImageFormat.Png);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NŌR-Gallery] Thumbnail generation error: {ex.Message}");
            }
        }
    }
}

