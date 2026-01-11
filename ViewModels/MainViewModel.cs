using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using YoutubeDownloader.Infrastructure;
using YoutubeDownloader.Models;
using YoutubeDownloader.Services;

namespace YoutubeDownloader.ViewModels;

/// <summary>
/// ViewModel glownego okna aplikacji
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly YoutubeDownloadService _downloadService;
    private readonly string _appDirectory;

    private readonly HistoryService _historyService;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isDownloading;
    private string _url = string.Empty;
    private string _outputFolder = string.Empty;
    private DownloadFormat _selectedFormat = DownloadFormat.Mp4;
    private EncodingMode _encodingMode = EncodingMode.NoTranscoding;
    private AudioBitrate _audioBitrate = AudioBitrate.Kbp192;
    private int? _videoBitrate;
    private double _progress;
    private string _progressText = string.Empty;
    private string _speed = string.Empty;
    private List<VideoBitrateOption> _videoBitratePresets;
    private VideoBitrateOption _selectedVideoBitratePreset;
    private bool _isCustomVideoBitrateVisible;
    private string _currentLanguage = "pl";
    private string _eta = string.Empty;
    private string _statusMessage = string.Empty;
    private string _logMessages = string.Empty;
    private ToolAvailability _toolAvailability = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    // Dostepne wartosci bitrate dla combobox
    public Array AudioBitrateValues => Enum.GetValues(typeof(AudioBitrate));
    public List<string> AvailableLanguages { get; } = new List<string> { "pl", "en", "de" };

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                LocalizationService.SetLanguage(_currentLanguage);
                OnPropertyChanged();
                // Odswiez komunikaty, ktore moga byc w locie
                RefreshLocalizedMessages();
            }
        }
    }

    public MainViewModel()
    {
        _appDirectory = AppContext.BaseDirectory;
        _downloadService = new YoutubeDownloadService(_appDirectory);

        _historyService = new HistoryService();

        // Komendy
        DownloadCommand = new AsyncRelayCommand(DownloadAsync, () => !IsDownloading && CanDownload());
        CancelCommand = new AsyncRelayCommand(CancelAsync, () => IsDownloading);
        SelectFolderCommand = new RelayCommand(SelectFolder);
        ClearLogCommand = new RelayCommand(() => LogMessages = string.Empty);
        OpenFolderCommand = new RelayCommand(OpenOutputFolder);
        ShowHistoryCommand = new RelayCommand(ShowHistory);

        // Inicjalizacja presetów
        _videoBitratePresets = VideoBitrateOption.GetPresets();
        _selectedVideoBitratePreset = _videoBitratePresets[0]; // Srednia
        _videoBitrate = _selectedVideoBitratePreset.Bitrate;
        _isCustomVideoBitrateVisible = false;

        // Sprawdz dostepnosc narzedzi
        CheckTools();
    }

    #region Wlasciwosci

    public string Url
    {
        get => _url;
        set
        {
            _url = value;
            OnPropertyChanged();
            (DownloadCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public string OutputFolder
    {
        get => _outputFolder;
        set
        {
            _outputFolder = value;
            OnPropertyChanged();
            (DownloadCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public DownloadFormat SelectedFormat
    {
        get => _selectedFormat;
        set
        {
            _selectedFormat = value;
            OnPropertyChanged();
            UpdateUIForFormat();
            (DownloadCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public EncodingMode EncodingMode
    {
        get => _encodingMode;
        set
        {
            _encodingMode = value;
            OnPropertyChanged();
            (DownloadCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public AudioBitrate AudioBitrate
    {
        get => _audioBitrate;
        set
        {
            _audioBitrate = value;
            OnPropertyChanged();
        }
    }

    public int? VideoBitrate
    {
        get => _videoBitrate;
        set
        {
            _videoBitrate = value;
            OnPropertyChanged();
        }
    }

    public double Progress
    {
        get => _progress;
        set
        {
            _progress = value;
            OnPropertyChanged();
        }
    }

    public string ProgressText
    {
        get => _progressText;
        set
        {
            _progressText = value;
            OnPropertyChanged();
        }
    }

    public string Speed
    {
        get => _speed;
        set
        {
            _speed = value;
            OnPropertyChanged();
        }
    }

    public List<VideoBitrateOption> VideoBitratePresets => _videoBitratePresets;

    public VideoBitrateOption SelectedVideoBitratePreset
    {
        get => _selectedVideoBitratePreset;
        set
        {
            if (_selectedVideoBitratePreset != value)
            {
                _selectedVideoBitratePreset = value;
                OnPropertyChanged();
                
                if (_selectedVideoBitratePreset.Bitrate.HasValue)
                {
                    VideoBitrate = _selectedVideoBitratePreset.Bitrate.Value;
                    IsCustomVideoBitrateVisible = false;
                }
                else
                {
                    IsCustomVideoBitrateVisible = true;
                }
            }
        }
    }

    public bool IsCustomVideoBitrateVisible
    {
        get => _isCustomVideoBitrateVisible;
        set
        {
            _isCustomVideoBitrateVisible = value;
            OnPropertyChanged();
        }
    }

    public string Eta
    {
        get => _eta;
        set
        {
            _eta = value;
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

    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            _isDownloading = value;
            OnPropertyChanged();
            (DownloadCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (CancelCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public ToolAvailability ToolAvailability
    {
        get => _toolAvailability;
        set
        {
            _toolAvailability = value;
            OnPropertyChanged();
        }
    }

    public bool IsAudioOnly => SelectedFormat == DownloadFormat.Mp3 || SelectedFormat == DownloadFormat.M4A;

    #endregion

    #region Komendy

    public ICommand DownloadCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SelectFolderCommand { get; }
    public ICommand ClearLogCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand ShowHistoryCommand { get; }

    #endregion

    #region Metody

    private void CheckTools()
    {
        ToolAvailability = _downloadService.CheckToolAvailability();

        if (!ToolAvailability.YtdlpAvailable)
        {
            AddLogMessage(string.Format(LocalizationService.GetString("LogToolNotFound"), "yt-dlp.exe"));
            AddLogMessage($"Szukano w: {Path.Combine(_appDirectory, "tools")}");
        }

        if (!ToolAvailability.FfmpegAvailable)
        {
            AddLogMessage(LocalizationService.GetString("LogFfmpegNotFound"));
            AddLogMessage($"Szukano w: {Path.Combine(_appDirectory, "tools")}");
        }

        // Wyswietl znalezione sciezki
        if (ToolAvailability.FfmpegAvailable)
        {
            AddLogMessage($"FFmpeg znaleziony: {ToolAvailability.FfmpegPath}");
        }
        if (ToolAvailability.YtdlpAvailable)
        {
            AddLogMessage($"yt-dlp znaleziony: {ToolAvailability.YtdlpPath}");
        }
    }

    private bool CanDownload()
    {
        if (string.IsNullOrWhiteSpace(Url))
            return false;

        if (string.IsNullOrWhiteSpace(OutputFolder))
            return false;

        if (!ToolAvailability.YtdlpAvailable)
            return false;

        if (!Directory.Exists(OutputFolder))
            return false;

        return true;
    }

    private void UpdateUIForFormat()
    {
        OnPropertyChanged(nameof(IsAudioOnly));
    }

    private void SelectFolder()
    {
        try
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = LocalizationService.GetString("SelectFolderDescription"),
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                OutputFolder = dialog.SelectedPath;
                AddLogMessage($"Wybrano folder: {OutputFolder}");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"Blad wyboru folderu: {ex.Message}");
            // Fallback - uzyj dokumentow
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            OutputFolder = documentsPath;
            AddLogMessage($"Uzywam folderu dokumentow: {OutputFolder}");
        }
    }

    private void ShowHistory()
    {
        // To zostanie zaimplementowane po utworzeniu HistoryWindow
        var historyWindow = new Views.HistoryWindow(_historyService);
        historyWindow.Owner = System.Windows.Application.Current.MainWindow;
        historyWindow.ShowDialog();
    }

    private void OpenOutputFolder()
    {
        if (!string.IsNullOrWhiteSpace(OutputFolder) && Directory.Exists(OutputFolder))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = OutputFolder,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AddLogMessage($"Blad podczas otwierania folderu: {ex.Message}");
            }
        }
    }

    private async Task DownloadAsync()
    {
        if (IsDownloading)
            return;

        IsDownloading = true;
        Progress = 0;
        ProgressText = "0%";
        StatusMessage = LocalizationService.GetString("StatusStarting");
        AddLogMessage(LocalizationService.GetString("LogStart"));
        AddLogMessage($"URL: {Url}");
        AddLogMessage($"Format: {SelectedFormat}");
        AddLogMessage($"Folder: {OutputFolder}");

        _cancellationTokenSource = new CancellationTokenSource();

        var options = new DownloadOptions
        {
            Url = Url,
            Format = SelectedFormat,
            EncodingMode = EncodingMode,
            AudioBitrate = AudioBitrate,
            VideoBitrate = VideoBitrate,
            OutputFolder = OutputFolder
        };

        var progress = new Progress<DownloadProgressInfo>(info =>
        {
            Progress = info.Percentage;
            ProgressText = $"{info.Percentage:F1}%";

            if (!string.IsNullOrEmpty(info.Speed))
                Speed = info.Speed;

            if (!string.IsNullOrEmpty(info.Eta))
                Eta = info.Eta;

            if (!string.IsNullOrEmpty(info.ErrorMessage))
            {
                StatusMessage = info.ErrorMessage;
                AddLogMessage($"BLAD: {info.ErrorMessage}");
            }
            else
            {
                Progress = info.Percentage;
                ProgressText = $"{info.Percentage:F1}%";

                if (!string.IsNullOrEmpty(info.Speed))
                    Speed = info.Speed;

                if (!string.IsNullOrEmpty(info.Eta))
                    Eta = info.Eta;

                switch (info.State)
                {
                    case DownloadState.Downloading:
                        StatusMessage = LocalizationService.GetString("StatusDownloading");
                        break;
                    case DownloadState.Completed:
                        StatusMessage = LocalizationService.GetString("StatusFinished");
                        AddLogMessage(LocalizationService.GetString("LogFinished"));
                        break;
                    case DownloadState.Cancelled:
                        StatusMessage = LocalizationService.GetString("StatusCancelled");
                        AddLogMessage(LocalizationService.GetString("LogCancelled"));
                        break;
                }
            }
        });

        try
        {
            var result = await _downloadService.DownloadAsync(options, progress, AddLogMessage, _cancellationTokenSource.Token);

            if (result.State == DownloadState.Completed)
            {
                Progress = 100;
                ProgressText = "100%";
                StatusMessage = LocalizationService.GetString("StatusFinished");
                
                // Dodaj do historii
                _historyService.AddEntry(new DownloadHistoryItem
                {
                    Title = result.FileName ?? "Film YouTube",
                    Url = Url,
                    Date = DateTime.Now,
                    FilePath = Path.Combine(OutputFolder, result.FileName ?? ""),
                    Format = SelectedFormat.ToString()
                });
            }
            else if (result.State == DownloadState.Cancelled)
            {
                StatusMessage = LocalizationService.GetString("StatusCancelled");
            }
            else if (result.State == DownloadState.Error)
            {
                StatusMessage = $"Blad: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Wystapil blad: {ex.Message}";
            AddLogMessage($"BLAD: {ex.Message}");
            Logger.LogError($"Blad podczas pobierania: {ex.Message}");
        }
        finally
        {
            IsDownloading = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private async Task CancelAsync()
    {
        if (_cancellationTokenSource != null)
        {
            AddLogMessage(LocalizationService.GetString("StatusCancelling"));
            _cancellationTokenSource.Cancel();
        }
    }

    private void AddLogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogMessages += $"[{timestamp}] {message}{Environment.NewLine}";
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void RefreshLocalizedMessages()
    {
        // Odswiez status message jesli jest w stanie spoczinku/konca
        if (!IsDownloading)
        {
            if (StatusMessage == "Pobieranie zakończone!" || StatusMessage == "Download completed!" || StatusMessage == "Download abgeschlossen!")
                StatusMessage = LocalizationService.GetString("StatusFinished");
            else if (StatusMessage == "Anulowano" || StatusMessage == "Cancelled" || StatusMessage == "Abgebrochen" || StatusMessage == "Pobieranie anulowane.")
                StatusMessage = LocalizationService.GetString("StatusCancelled");
            else if (StatusMessage == "Rozpoczynanie pobierania..." || StatusMessage == "Starting download..." || StatusMessage == "Download wird gestartet...")
                StatusMessage = LocalizationService.GetString("StatusStarting");
        }
        else
        {
            StatusMessage = LocalizationService.GetString("StatusDownloading");
        }
        
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(AudioBitrateValues));
    }

    #endregion
}

/// <summary>
/// RelayCommand dla akcji bez parametrow
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// AsyncRelayCommand dla akcji asynchronicznych
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (_isExecuting)
            return false;
        return _canExecute?.Invoke() ?? true;
    }

    public async void Execute(object? parameter)
    {
        if (_isExecuting)
            return;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
