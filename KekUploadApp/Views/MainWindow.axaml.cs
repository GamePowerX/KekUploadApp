using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace KekUploadApp.Views
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }
        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
        }

        private async void BrowseButtonClicked(object? sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Title = "Select a file to upload"
            };
            var result = await dialog.ShowAsync(this);
            if (result == null) return;
            if (result.Length <= 0) return;
            FilePathTextBox.Text = result[0];
        }
    }
}