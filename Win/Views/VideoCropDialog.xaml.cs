using System.Windows;
using NOR.Models;

namespace NOR.Views
{
    public partial class VideoCropDialog : Window
    {
        private readonly GalleryItem _item;

        public VideoCropDialog(GalleryItem item)
        {
            InitializeComponent();
            _item = item;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            if (Rb16x9.IsChecked == true)
            {
                _item.ResolutionWidth = 1920;
                _item.ResolutionHeight = 1080;
            }
            else if (Rb16x10.IsChecked == true)
            {
                _item.ResolutionWidth = 1920;
                _item.ResolutionHeight = 1200;
            }
            else if (Rb21x9.IsChecked == true)
            {
                _item.ResolutionWidth = 2560;
                _item.ResolutionHeight = 1080;
            }

            DialogResult = true;
            Close();
        }
    }
}

