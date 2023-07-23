using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace KekUploadApp
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }
        public MainWindow()
        {
            InitializeComponent();
            SizeToContent = SizeToContent.WidthAndHeight;
            CanResize = false;
            Instance = this;
        }

        private async void DownloadApiBaseUrl_OnPastingFromClipboard(object? sender, RoutedEventArgs e)
        {
            if(sender == null)
                return;
            if(!sender.GetType().IsAssignableFrom(typeof(TextBox)))
                return;
            if(sender is not TextBox textBox)
                return;
            e.Handled = false;
            var txt = await Clipboard!.GetTextAsync() ?? "";
            var thread = new Thread((() =>
            {
                if(txt.Contains("/e/"))
                    txt = txt.Replace("/e/", "/d/");
                if(txt.Contains("/v/"))
                    txt = txt.Replace("/v/", "/d/");
                if (!txt.Contains("/d/"))
                    return;
                var text = txt.Split("/d/");
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

        private void AutoDetectNameCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if(sender == null)
                return;
            if(!sender.GetType().IsAssignableFrom(typeof(CheckBox)))
                return;
            var checkBox = (CheckBox)sender;
            DownloadFilePathTextBlock.Text = checkBox.IsChecked == true ? "Folder path:" : "File path:";
        }
    }
}