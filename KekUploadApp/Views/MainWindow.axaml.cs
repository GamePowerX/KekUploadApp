using System;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace KekUploadApp.Views
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }
        public MainWindow()
        {
            InitializeComponent();
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.CanResize = false;
            Instance = this;
        }

        private void DownloadApiBaseUrl_OnPastingFromClipboard(object? sender, RoutedEventArgs e)
        {
            if(sender == null)
                return;
            if(!sender.GetType().IsAssignableFrom(typeof(TextBox)))
                return;
            var textBox = (TextBox)sender;
            e.Handled = false;
            var thread = new Thread((() =>
            {
                Thread.Sleep(100);
                if (!textBox.Text.Contains("/d/")) return;
                var text = textBox.Text.Split("/d/");
                if (text.Length > 1)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        textBox.Text = text[0];
                        DownloadId.Text = text[1];
                    });
                }
            }));
            thread.Start();
        }

        private void AutoDetectNameCheckBox_OnChecked(object? sender, RoutedEventArgs e)
        {
            if(sender == null)
                return;
            if(!sender.GetType().IsAssignableFrom(typeof(CheckBox)))
                return;
            var checkBox = (CheckBox)sender;
            DownloadFilePathTextBlock.Text = "Folder path:";
        }

        private void AutoDetectNameCheckBox_OnUnchecked(object? sender, RoutedEventArgs e)
        {
            if(sender == null)
                return;
            if(!sender.GetType().IsAssignableFrom(typeof(CheckBox)))
                return;
            var checkBox = (CheckBox)sender;
            DownloadFilePathTextBlock.Text = "File path:";
        }
    }
}