using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using KekUploadLibrary;

namespace KekUploadApp;

public class MainWindowViewModel
{
    public float UploadProgress { get; set; }

    public void OnDownloadButtonClicked()
    {
        var instance = MainWindow.Instance!;
        instance.MenuPanel.IsVisible = false;
        instance.DownloadPanel.IsVisible = true;
    }

    public void OnUploadCancelButtonClicked()
    {
        var instance = MainWindow.Instance!;
        instance.UploadStatusPanel.IsVisible = false;
        instance.UploadPanel.IsVisible = false;
        instance.MenuPanel.IsVisible = true;
        instance.ErrorTextBlock.Text = "";
        instance.StatusTextBlock.Text = "";
        instance.ErrorTextBlock.IsVisible = false;
        instance.StatusTextBlock.IsVisible = false;
        instance.UploadProgressBar.Value = 0;
        instance.UploadFilePathTextBox.Text = "";
        instance.ChunkCountTextBlock.Text = "0/0 Chunks completed";
        instance.UploadPercentTextBlock.Text = "0%";
        instance.UploadStatusPanel.IsVisible = false;
        instance.ResultPanel.IsVisible = false;
        instance.UploadButton.IsEnabled = true;
        instance.UploadBrowseButton.IsEnabled = true;
        instance.UploadFilePathTextBox.IsEnabled = true;
        instance.UploadApiUrlTextBox.IsEnabled = true;
        instance.WithNameCheckBox.IsEnabled = true;
        instance.UploadCancelButton.Content = "Cancel";
    }

    public void OnDownloadCancelButtonClicked()
    {
        var instance = MainWindow.Instance!;
        instance.DownloadPanel.IsVisible = false;
        instance.MenuPanel.IsVisible = true;
        instance.ErrorTextBlock.Text = "";
        instance.StatusTextBlock.Text = "";
        instance.ErrorTextBlock.IsVisible = false;
        instance.StatusTextBlock.IsVisible = false;
        instance.DownloadProgressBar.Value = 0;
        instance.DownloadFilePathTextBox.Text = "";
        instance.DownloadStatusPanel.IsVisible = false;
        instance.DownloadId.Text = "";
        instance.DownloadPercentTextBlock.Text = "0%";
        instance.DownloadProgressTextBlock.Text = "0/0 Bytes downloaded";
        instance.StartDownloadButton.IsEnabled = true;
        instance.DownloadBrowseButton.IsEnabled = true;
        instance.DownloadFilePathTextBox.IsEnabled = true;
        instance.DownloadApiBaseUrl.IsEnabled = true;
        instance.DownloadId.IsEnabled = true;
        instance.AutoDetectNameCheckBox.IsEnabled = true;
        instance.DownloadCancelButton.Content = "Cancel";
    }

    public void OnCopyButtonClicked()
    {
        var instance = MainWindow.Instance!;
        instance.Clipboard!.SetTextAsync(instance.ResultTextBox.Text);
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
        var apiUrl = instance.UploadApiUrlTextBox.Text;
        var filePath = instance.UploadFilePathTextBox.Text;
        if (apiUrl == null || filePath == null)
        {
            instance.ErrorTextBlock.IsVisible = true;
            instance.ErrorTextBlock.Text = "Please enter API URL and file path";
            return;
        }

        UploadProgress = 0;
        var client = new UploadClient(apiUrl, instance.WithNameCheckBox.IsChecked ?? false);
        client.UploadChunkCompleteEvent += (_, args) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                UploadProgress = args.CurrentChunkCount * 100 / (float)args.TotalChunkCount;
                instance.UploadProgressBar.Value = UploadProgress;
                instance.UploadPercentTextBlock.Text = $"{Math.Round((decimal)UploadProgress, 2)}%    ";
                instance.ChunkCountTextBlock.Text = $"{args.CurrentChunkCount}/{args.TotalChunkCount} Chunks completed";
            });
        };
        client.UploadCompleteEvent += (_, _) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                instance.StatusTextBlock.IsVisible = true;
                instance.StatusTextBlock.Text = "Upload complete";
                instance.UploadCancelButton.Content = "Finish";
            });
        };
        client.UploadErrorEvent += (_, args) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                instance.ErrorTextBlock.IsVisible = true;
                if (args.ErrorResponse != null)
                    instance.ErrorTextBlock.Text = $"Upload Error: {args.ErrorResponse.Error}\n";
                instance.ErrorTextBlock.Text += $"Exception: {args.Exception.Message}\nTrying again...";
            });
        };
        instance.UploadStatusPanel.IsVisible = true;
        var thread = new Thread(() =>
        {
            try
            {
                var downloadUrl = client.Upload(new UploadItem(filePath));
                Dispatcher.UIThread.InvokeAsync(() => instance.ResultTextBox.Text = downloadUrl).Wait();
                Dispatcher.UIThread.InvokeAsync(() => instance.ResultPanel.IsVisible = true).Wait();
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    instance.ErrorTextBlock.IsVisible = true;
                    instance.ErrorTextBlock.Text = $"Error: {ex.Message}\nAborting...";
                }).Wait();
            }
        });
        thread.Start();
        instance.UploadButton.IsEnabled = false;
        instance.UploadBrowseButton.IsEnabled = false;
        instance.UploadFilePathTextBox.IsEnabled = false;
        instance.UploadApiUrlTextBox.IsEnabled = false;
        instance.WithNameCheckBox.IsEnabled = false;
    }

    public void OnStartDownloadButtonClicked()
    {
        var instance = MainWindow.Instance!;
        instance.StatusTextBlock.IsVisible = false;
        var apiUrl = instance.DownloadApiBaseUrl.Text;
        var downloadId = instance.DownloadId.Text;
        var downloadFilePath = instance.DownloadFilePathTextBox.Text;
        if (apiUrl == null || downloadId == null || downloadFilePath == null)
        {
            instance.ErrorTextBlock.IsVisible = true;
            instance.ErrorTextBlock.Text = "Please enter API URL, download Id and a path to save the file";
            return;
        }

        if (instance.AutoDetectNameCheckBox.IsChecked == true)
        {
            var c = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl + "/d/" + downloadId);
            var result = c.Send(request, HttpCompletionOption.ResponseHeadersRead);
            var header = result.Content.Headers.ContentDisposition;
            if (header == null)
            {
                instance.ErrorTextBlock.IsVisible = true;
                instance.ErrorTextBlock.Text = "Could not get file name from server";
                return;
            }

            var value = header.FileName;

            if (value == null)
            {
                instance.ErrorTextBlock.IsVisible = true;
                instance.ErrorTextBlock.Text = "Could not get file name from server";
                return;
            }

            instance.DownloadFilePathTextBox.Text = Path.Combine(downloadFilePath, value.Replace("\"", ""));
            instance.DownloadFilePathTextBlock.Text = "File path:";
            downloadFilePath = instance.DownloadFilePathTextBox.Text;
        }

        var client = new DownloadClient();
        client.ProgressChangedEvent += (totalDownloadSize, totalBytesRead, progressPercentage) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                instance.DownloadProgressBar.Value = progressPercentage ?? 0;
                instance.DownloadPercentTextBlock.Text = $"{progressPercentage}%    ";
                instance.DownloadProgressTextBlock.Text = totalDownloadSize != null
                    ? $"{Utils.SizeToString(totalBytesRead)}/{Utils.SizeToString((long)totalDownloadSize)} downloaded"
                    : $"{Utils.SizeToString(totalBytesRead)} downloaded";
                if ((long)(progressPercentage ?? 0) != 100) return;
                instance.StatusTextBlock.IsVisible = true;
                instance.StatusTextBlock.Text = "Download complete";
                instance.DownloadCancelButton.Content = "Finish";
            }).Wait();
        };
        instance.DownloadStatusPanel.IsVisible = true;
        var thread = new Thread(() =>
        {
            try
            {
                client.Download(apiUrl + "/d/" + downloadId, new DownloadItem(downloadFilePath));
            }
            catch (KekException e)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    instance.ErrorTextBlock.IsVisible = true;
                    instance.ErrorTextBlock.Text = $"Error: {e.Message}\nAborting...";
                }).Wait();
            }
            catch (Exception e)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    instance.ErrorTextBlock.IsVisible = true;
                    instance.ErrorTextBlock.Text = $"Error: {e.Message}\nAborting...";
                }).Wait();
            }
        });
        thread.Start();
        instance.StartDownloadButton.IsEnabled = false;
        instance.DownloadBrowseButton.IsEnabled = false;
        instance.DownloadFilePathTextBox.IsEnabled = false;
        instance.DownloadApiBaseUrl.IsEnabled = false;
        instance.DownloadId.IsEnabled = false;
        instance.AutoDetectNameCheckBox.IsEnabled = false;
    }

    public async void OnUploadBrowseButtonClicked()
    {
        var instance = MainWindow.Instance!;
        var result = await instance.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            SuggestedStartLocation = await instance.StorageProvider.TryGetFolderFromPathAsync(
                Directory.GetParent(instance.UploadFilePathTextBox.Text ?? Environment.CurrentDirectory)?.FullName ??
                Environment.CurrentDirectory),
            Title = "Select a file to upload"
        });
        if (result.Count <= 0) return;
        instance.UploadFilePathTextBox.Text = result[0].Path.LocalPath;
    }

    public async void OnDownloadBrowseButtonClicked()
    {
        var instance = MainWindow.Instance!;
        if (!(instance.AutoDetectNameCheckBox.IsChecked ?? false))
        {
            var result = await instance.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                SuggestedStartLocation = await instance.StorageProvider.TryGetFolderFromPathAsync(
                    Directory.GetParent(instance.DownloadFilePathTextBox.Text ?? Environment.CurrentDirectory)
                        ?.FullName ?? Environment.CurrentDirectory),
                Title = "Specify where to save the file"
            });
            if (result.Count <= 0) return;
            instance.DownloadFilePathTextBox.Text = result[0].Path.LocalPath;
        }
        else
        {
            var result = await instance.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                SuggestedStartLocation =
                    await instance.StorageProvider.TryGetFolderFromPathAsync(instance.DownloadFilePathTextBox.Text ??
                                                                             Environment.CurrentDirectory),
                Title = "Select a folder to save the file"
            });
            if (result.Count <= 0) return;
            instance.DownloadFilePathTextBox.Text = result[0].Path.LocalPath;
        }
    }
}