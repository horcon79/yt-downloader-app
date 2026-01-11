using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using YoutubeDownloader.Models;
using YoutubeDownloader.Services;

namespace YoutubeDownloader.ViewModels;

/// <summary>
/// ViewModel dla okna pobierania wsadowego
/// </summary>
public class BatchDownloadViewModel : INotifyPropertyChanged
{
    private readonly BatchDownloadService _downloadService;
    private readonly string _appDirectory;
    private CancellationTokenSource? _downloadCts;
    private bool _isDownloading;
    private bool _isPaused;
    private int _completedCount;
    private int _totalCount;
    private double _overallProgress;
    private string _overallProgressText = "0%";
    private string _statusMessage = string.Empty;
    private string _logMessages = string.Empty;
    private BatchDownloadSettings _settings;
    private BatchDownloadItem? _selectedItem;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<BatchDownloadItem> Items { get; } = new();

    public BatchDownloadSettings Settings
    {
        get => _settings;
        set
        {
            _settings = value;
            OnPropertyChanged();
        }
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            _isDownloading = value;
            OnPropertyChanged();
            (StartCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (PauseCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            _isPaused = value;
            OnPropertyChanged();
        }
    }

    public int CompletedCount
    {
        get => _completedCount;
        set
        {
            _completedCount = value;
            OnPropertyChanged();
            UpdateOverallProgress();
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        set
        {
            _totalCount = value;
            OnPropertyChanged();
            UpdateOverallProgress();
        }
    }

    public double OverallProgress
    {
        get => _overallProgress;
        set
        {
            _overallProgress = value;
            OnPropertyChanged();
        }
    }

    public string OverallProgressText
    {
        get => _overallProgressText;
        set
        {
            _overallProgressText = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string LogMessages
    {
        get => _logMessages;
        set
        {
            _logMessages = value;
            OnPropertyChanged();
        }
    }

    public BatchDownloadItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            OnPropertyChanged();
            (RemoveItemCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public Array AvailableFormats => Enum.GetValues(typeof(DownloadFormat));
    public Array AvailableAudioBitrates => Enum.GetValues(typeof(AudioBitrate));
    public Array AvailableEncodingModes => Enum.GetValues(typeof(EncodingMode));
    public List<int> ParallelOptions { get; } = new List<int> { 1, 2, 3, 4, 5 };

    public ICommand ImportFromFileCommand { get; }
    public ICommand AddUrlCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RetryFailedCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand RemoveCompletedCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand SelectFolderCommand { get; }
    public ICommand ClearLogCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand CloseCommand { get; }

    public BatchDownloadViewModel(string appDirectory)
    {
        _appDirectory = appDirectory;
        _downloadService = new BatchDownloadService(appDirectory);
        _settings = new BatchDownloadSettings();

        // Inicjalizacja ustawień domyślnych
        _settings.OutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // Subskrypcja zdarzeń serwisu
        _downloadService.ItemProgressChanged += OnItemProgressChanged;
        _downloadService.ItemStateChanged += OnItemStateChanged;
        _downloadService.OverallProgressChanged += OnOverallProgressChanged;

        // Komendy
        ImportFromFileCommand = new RelayCommand(ImportFromFile);
        AddUrlCommand = new RelayCommand(AddUrl);
        StartCommand = new AsyncRelayCommand(StartDownloadAsync, () => !IsDownloading && HasSelectedItems());
        PauseCommand = new RelayCommand(PauseDownload, () => IsDownloading && !IsPaused);
        CancelCommand = new RelayCommand(CancelDownload, () => IsDownloading || IsPaused);
        RetryFailedCommand = new RelayCommand(RetryFailed, () => HasFailedItems());
        RemoveItemCommand = new RelayCommand(RemoveSelectedItem, () => SelectedItem != null);
        RemoveCompletedCommand = new RelayCommand(RemoveCompleted, () => HasCompletedItems());
        SelectAllCommand = new RelayCommand(SelectAll);
        DeselectAllCommand = new RelayCommand(DeselectAll);
        SelectFolderCommand = new RelayCommand(SelectFolder);
        ClearLogCommand = new RelayCommand(() => LogMessages = string.Empty);
        OpenFolderCommand = new RelayCommand(OpenOutputFolder, () => !string.IsNullOrEmpty(Settings.OutputFolder) && Directory.Exists(Settings.OutputFolder));
        CloseCommand = new RelayCommand(Close);
        
        System.Diagnostics.Debug.WriteLine("[DEBUG] BatchDownloadViewModel constructor completed");
    }

    private void ImportFromFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = LocalizationService.GetString("BatchImportTitle"),
            Filter = LocalizationService.GetString("BatchImportFilter"),
            Multiselect = false
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
            int count = 0;

            switch (extension)
            {
                case ".txt":
                    count = _downloadService.ImportFromTextFile(dialog.FileName, Settings.DefaultFormat);
                    break;
                case ".csv":
                    count = _downloadService.ImportFromCsvFile(dialog.FileName, 0, Settings.DefaultFormat);
                    break;
                case ".json":
                    count = _downloadService.ImportFromJsonFile(dialog.FileName, "url", Settings.DefaultFormat);
                    break;
            }

            RefreshItemsList();
            TotalCount = Items.Count(i => i.IsSelected);
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ImportFromFile: Added {count} items, Total in Items={Items.Count}, Selected={TotalCount}");
            
            AddLogMessage(string.Format(LocalizationService.GetString("BatchImportCount"), count));
        }
    }

    private void AddUrl()
    {
        var url = GetUrlFromClipboard();
        if (!string.IsNullOrEmpty(url))
        {
            _downloadService.AddItem(url, Settings.DefaultFormat);
            RefreshItemsList();
            TotalCount = Items.Count(i => i.IsSelected);
            AddLogMessage(string.Format(LocalizationService.GetString("BatchUrlAdded"), url));
        }
        else
        {
            System.Windows.MessageBox.Show(
                LocalizationService.GetString("BatchNoUrlInClipboard"),
                LocalizationService.GetString("InfoTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private async Task StartDownloadAsync()
    {
        if (IsDownloading)
            return;

        if (!HasSelectedItems())
        {
            System.Windows.MessageBox.Show(
                LocalizationService.GetString("BatchNoItemsSelected"),
                LocalizationService.GetString("InfoTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrEmpty(Settings.OutputFolder) || !Directory.Exists(Settings.OutputFolder))
        {
            System.Windows.MessageBox.Show(
                LocalizationService.GetString("BatchInvalidOutputFolder"),
                LocalizationService.GetString("ErrorTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        IsDownloading = true;
        IsPaused = false;
        CompletedCount = 0;
        StatusMessage = LocalizationService.GetString("BatchDownloadStarting");

        _downloadCts = new CancellationTokenSource();

        try
        {
            await _downloadService.StartDownloadAsync(Settings, _downloadCts.Token);
            StatusMessage = LocalizationService.GetString("BatchDownloadCompleted");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = LocalizationService.GetString("BatchDownloadCancelled");
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(LocalizationService.GetString("BatchDownloadError"), ex.Message);
        }
        finally
        {
            IsDownloading = false;
            IsPaused = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
            RefreshItemsList();
            TotalCount = Items.Count(i => i.IsSelected);
        }
    }

    private void PauseDownload()
    {
        _downloadService.PauseAll();
        IsPaused = true;
        StatusMessage = LocalizationService.GetString("BatchDownloadPaused");
    }

    private void CancelDownload()
    {
        _downloadService.CancelAll();
        IsDownloading = false;
        IsPaused = false;
        StatusMessage = LocalizationService.GetString("BatchDownloadCancelled");
        RefreshItemsList();
    }

    private void RetryFailed()
    {
        _downloadService.RetryFailed(Settings);
        RefreshItemsList();
    }

    private void RemoveSelectedItem()
    {
        if (SelectedItem != null)
        {
            _downloadService.RemoveItem(SelectedItem.Id);
            RefreshItemsList();
            TotalCount = Items.Count(i => i.IsSelected);
        }
    }

    private void RemoveCompleted()
    {
        _downloadService.RemoveCompleted();
        RefreshItemsList();
        TotalCount = Items.Count(i => i.IsSelected);
    }

    private void SelectAll()
    {
        _downloadService.SelectAll();
        RefreshItemsList();
        TotalCount = Items.Count(i => i.IsSelected);
        System.Diagnostics.Debug.WriteLine($"[DEBUG] SelectAll: Selected={TotalCount}");
        
        // Wymuś aktualizację stanu poleceń
        UpdateCommandsState();
    }

    private void DeselectAll()
    {
        _downloadService.DeselectAll();
        RefreshItemsList();
        TotalCount = 0;
        System.Diagnostics.Debug.WriteLine("[DEBUG] DeselectAll: Selected=0");
        
        // Wymuś aktualizację stanu poleceń
        UpdateCommandsState();
    }

    private void UpdateCommandsState()
    {
        (StartCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (RetryFailedCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RemoveCompletedCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void SelectFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = LocalizationService.GetString("SelectFolderDescription"),
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            Settings.OutputFolder = dialog.SelectedPath;
            AddLogMessage(string.Format(LocalizationService.GetString("BatchOutputFolderChanged"), Settings.OutputFolder));
        }
    }

    private void OpenOutputFolder()
    {
        if (!string.IsNullOrEmpty(Settings.OutputFolder) && Directory.Exists(Settings.OutputFolder))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Settings.OutputFolder,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AddLogMessage(string.Format(LocalizationService.GetString("ErrorOpeningFolder"), ex.Message));
            }
        }
    }

    private void Close()
    {
        if (IsDownloading)
        {
            var result = System.Windows.MessageBox.Show(
                LocalizationService.GetString("BatchConfirmClose"),
                LocalizationService.GetString("ConfirmTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
                return;
        }

        _downloadCts?.Cancel();
    }

    private void RefreshItemsList()
    {
        Items.Clear();
        foreach (var item in _downloadService.Items.OrderBy(i => i.AddedAt))
        {
            Items.Add(item);
        }
        
        // Debug: Sprawdź czy są zaznaczone elementy
        var selectedCount = Items.Count(i => i.IsSelected);
        System.Diagnostics.Debug.WriteLine($"[DEBUG] RefreshItemsList: Items={Items.Count}, Selected={selectedCount}");
        
        // Wymuś aktualizację stanu poleceń
        UpdateCommandsState();
    }

    private bool HasSelectedItems()
    {
        var result = Items.Any(i => i.IsSelected);
        System.Diagnostics.Debug.WriteLine($"[DEBUG] HasSelectedItems: Items={Items.Count}, Selected={Items.Count(i => i.IsSelected)}, Result={result}");
        return result;
    }

    private bool HasFailedItems()
    {
        return Items.Any(i => i.State == DownloadState.Error);
    }

    private bool HasCompletedItems()
    {
        return Items.Any(i => i.State == DownloadState.Completed);
    }

    private static string? GetUrlFromClipboard()
    {
        if (System.Windows.Clipboard.ContainsText())
        {
            var text = System.Windows.Clipboard.GetText();
            if (text.Contains("youtube.com") || text.Contains("youtu.be"))
            {
                var urlMatch = System.Text.RegularExpressions.Regex.Match(text, @"https?://[^\s]+");
                if (urlMatch.Success)
                    return urlMatch.Value;
            }
        }
        return null;
    }

    private void UpdateOverallProgress()
    {
        if (TotalCount > 0)
        {
            OverallProgress = (double)CompletedCount / TotalCount * 100;
            OverallProgressText = $"{CompletedCount}/{TotalCount} ({OverallProgress:F1}%)";
        }
        else
        {
            OverallProgress = 0;
            OverallProgressText = "0%";
        }
    }

    private void AddLogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogMessages += $"[{timestamp}] {message}{Environment.NewLine}";
    }

    private void OnItemProgressChanged(BatchDownloadItem item)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var existing = Items.FirstOrDefault(i => i.Id == item.Id);
            if (existing != null)
            {
                existing.Progress = item.Progress;
                existing.Speed = item.Speed;
                existing.Eta = item.Eta;
                existing.DownloadedSize = item.DownloadedSize;
                existing.TotalSize = item.TotalSize;
            }
        });
    }

    private void OnItemStateChanged(BatchDownloadItem item)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var existing = Items.FirstOrDefault(i => i.Id == item.Id);
            if (existing != null)
            {
                existing.State = item.State;
                existing.Progress = item.Progress;
                existing.ErrorMessage = item.ErrorMessage;
                existing.CompletedAt = item.CompletedAt;
                existing.FilePath = item.FilePath;
            }

            if (item.State == DownloadState.Completed)
            {
                CompletedCount++;
            }
        });
    }

    private void OnOverallProgressChanged(int completed, int total)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            CompletedCount = completed;
            TotalCount = total;
        });
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
