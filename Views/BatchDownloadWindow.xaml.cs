using System.Windows;
using YoutubeDownloader.Services;
using YoutubeDownloader.ViewModels;

namespace YoutubeDownloader.Views;

/// <summary>
/// Okno pobierania wsadowego
/// </summary>
public partial class BatchDownloadWindow : Window
{
    private readonly BatchDownloadViewModel _viewModel;

    public BatchDownloadWindow(BatchDownloadViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        
        // Subskrypcja zdarzenia zamykania okna
        Closing += Window_Closing;
    }

    public BatchDownloadWindow(string appDirectory) : this(new BatchDownloadViewModel(appDirectory))
    {
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Jeśli trwa pobieranie, pokaż potwierdzenie
        if (_viewModel.IsDownloading)
        {
            var result = MessageBox.Show(
                LocalizationService.GetString("BatchConfirmClose"),
                LocalizationService.GetString("ConfirmTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }
        }
        
        // Anuluj pobieranie
        _viewModel.CancelCommand.Execute(null);
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Anuluj subskrypcję zdarzenia, aby uniknąć podwójnego wywołania
        Closing -= Window_Closing;
        Close();
    }
}
