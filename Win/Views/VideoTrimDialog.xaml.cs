using System;
using System.Windows;
using NOR.Models;

namespace NOR.Views
{
    public partial class VideoTrimDialog : Window
    {
        private readonly GalleryItem _item;
        private double _startTime = 0;
        private double _endTime = 10;

        public VideoTrimDialog(GalleryItem item)
        {
            InitializeComponent();
            _item = item;
            Loaded += VideoTrimDialog_Loaded;
            Closed += VideoTrimDialog_Closed;
        }

        private void VideoTrimDialog_Loaded(object sender, RoutedEventArgs e)
        {
            TrimPlayer.Source = new Uri(_item.FilePath, UriKind.Absolute);
            TrimPlayer.MediaOpened += (s, ev) =>
            {
                if (TrimPlayer.NaturalDuration.HasTimeSpan)
                {
                    double total = TrimPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    SliderStart.Maximum = total;
                    SliderEnd.Maximum = total;
                    SliderEnd.Value = total;
                    _endTime = total;
                    UpdateLabels();
                }
            };
            TrimPlayer.Play();
        }

        private void VideoTrimDialog_Closed(object? sender, EventArgs e)
        {
            TrimPlayer.Stop();
            TrimPlayer.Source = null;
        }

        private void SliderStart_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SliderStart.Value >= SliderEnd.Value)
            {
                SliderStart.Value = Math.Max(0, SliderEnd.Value - 1);
            }
            _startTime = SliderStart.Value;
            UpdateLabels();
        }

        private void SliderEnd_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SliderEnd.Value <= SliderStart.Value)
            {
                SliderEnd.Value = SliderStart.Value + 1;
            }
            _endTime = SliderEnd.Value;
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (TxtStartTime != null)
                TxtStartTime.Text = $"Start: {FormatSeconds(_startTime)}";
            if (TxtEndTime != null)
                TxtEndTime.Text = $"End: {FormatSeconds(_endTime)}";
        }

        private string FormatSeconds(double sec)
        {
            var ts = TimeSpan.FromSeconds(sec);
            return $"{ts.Minutes}:{ts.Seconds:D2}";
        }

        private void BtnPlaySegment_Click(object sender, RoutedEventArgs e)
        {
            TrimPlayer.Position = TimeSpan.FromSeconds(_startTime);
            TrimPlayer.Play();
        }

        private void TrimPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            TrimPlayer.Position = TimeSpan.FromSeconds(_startTime);
            TrimPlayer.Play();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _item.DurationSeconds = _endTime - _startTime;
            DialogResult = true;
            Close();
        }
    }
}

