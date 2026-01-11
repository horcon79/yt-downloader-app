using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using YoutubeDownloader.Models;
using YoutubeDownloader.Services;

namespace YoutubeDownloader.Views;

public partial class HistoryWindow : Window
{
    private readonly HistoryService _historyService;

    public HistoryWindow(HistoryService historyService)
    {
        InitializeComponent();
        _historyService = historyService;
        LoadHistory();
    }

    private void LoadHistory()
    {
        HistoryListView.ItemsSource = _historyService.GetHistory();
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is DownloadHistoryItem item)
        {
            var folder = Path.GetDirectoryName(item.FilePath);
            if (Directory.Exists(folder))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = folder!,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(LocalizationService.GetString("ErrorOpeningFolder"), ex.Message), 
                        LocalizationService.GetString("ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show(LocalizationService.GetString("ErrorItemNotFound"), 
                    LocalizationService.GetString("InfoTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(LocalizationService.GetString("ConfirmClearHistory"), 
            LocalizationService.GetString("ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _historyService.ClearHistory();
            LoadHistory();
        }
    }
}
