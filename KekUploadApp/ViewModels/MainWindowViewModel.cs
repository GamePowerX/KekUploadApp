using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Avalonia;
using Avalonia.Threading;
using KekUploadApp.Views;
using KekUploadLibrary;

namespace KekUploadApp.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public float UploadProgress
        {
            get;
            set;
        }

        public void OnUploadCancelButtonClicked()
        {
            var instance = MainWindow.Instance!;
            instance.UploadStatusPanel.IsVisible = false;
            instance.UploadPanel.IsVisible = false;
            instance.MenuPanel.IsVisible = true;
        }

        public void OnCopyButtonClicked()
        {
            Application.Current!.Clipboard!.SetTextAsync(MainWindow.Instance!.ResultTextBox.Text);
        }

        public void OnUploadMenuButtonClicked()
        {
            var instance = MainWindow.Instance!;
            instance.UploadPanel.IsVisible = true;
            instance.MenuPanel.IsVisible = false;
        }

        public void OnUploadButtonClicked()
        {
            var instance = MainWindow.Instance!;
            instance.StatusTextBlock.IsVisible = false;
            var apiUrl = instance.ApiUrlTextBox.Text;
            var filePath = instance.FilePathTextBox.Text;
            if(apiUrl == null || filePath == null)
            {
                instance.ErrorTextBlock.IsVisible = true;
                instance.ErrorTextBlock.Text = "Please enter API URL and file path";
                return;
            }
            UploadProgress = 0;
            var client = new UploadClient(apiUrl, true);
            client.UploadChunkCompleteEvent += (sender, args) =>
            {
                Dispatcher.UIThread.InvokeAsync((() =>
                {
                    UploadProgress = (args.CurrentChunkCount+1) * 100 / (float)args.TotalChunkCount;
                    instance.UploadProgressBar.Value = UploadProgress;
                    instance.PercentTextBlock.Text = $"{Math.Round((decimal)UploadProgress, 2)}%    ";
                    instance.ChunkCountTextBlock.Text = $"{args.CurrentChunkCount+1}/{args.TotalChunkCount} Chunks completed";
                }));
            };
            client.UploadCompleteEvent += (sender, args) =>
            {
                Dispatcher.UIThread.InvokeAsync((() =>
                {
                    instance.StatusTextBlock.IsVisible = true;
                    instance.StatusTextBlock.Text = "Upload complete";
                }));
            };
            client.UploadErrorEvent += (sender, args) =>
            {
                Dispatcher.UIThread.InvokeAsync((() =>
                {
                    instance.ErrorTextBlock.IsVisible = true;
                    if(args.ErrorResponse != null)
                    {
                        instance.ErrorTextBlock.Text = $"Upload Error: {args.ErrorResponse.Error}\n";
                    }
                    instance.ErrorTextBlock.Text += $"Exception: {args.Exception.Message}\nTrying again...";
                }));
            };
            instance.UploadStatusPanel.IsVisible = true;
            var thread = new Thread(() =>
            {
                try
                {
                    var downloadUrl = client.Upload(new UploadItem(filePath));
                    Dispatcher.UIThread.InvokeAsync((() => instance.ResultTextBox.Text = downloadUrl)).Wait();
                    Dispatcher.UIThread.InvokeAsync((() => instance.ResultPanel.IsVisible = true)).Wait();
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.InvokeAsync((() =>
                    {
                        instance.ErrorTextBlock.IsVisible = true;
                        instance.ErrorTextBlock.Text = $"Error: {ex.Message}\nAborting...";
                    })).Wait();
                }
            });
            thread.Start();
        }
    }
}