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
                    MessageBox.Show($"Blad podczas otwierania folderu: {ex.Message}", "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Folder juz nie istnieje.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Czy na pewno chcesz wyczyscic cala historie pobran?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _historyService.ClearHistory();
            LoadHistory();
        }
    }
}
