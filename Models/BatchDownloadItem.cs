using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YoutubeDownloader.Models;

/// <summary>
/// Pojedynczy element w kolejce pobierania wsadowego
/// </summary>
public class BatchDownloadItem : INotifyPropertyChanged
{
    private bool _isSelected = true;
    private string _url = string.Empty;
    private string? _title;
    private DownloadFormat _format = DownloadFormat.Mp4;
    private EncodingMode _encodingMode = EncodingMode.NoTranscoding;
    private AudioBitrate _audioBitrate = AudioBitrate.Kbp192;
    private int? _videoBitrate;
    private DownloadState _state = DownloadState.Pending;
    private double _progress;
    private string? _errorMessage;
    private int _retryCount;
    private DateTime _addedAt = DateTime.Now;
    private DateTime? _startedAt;
    private DateTime? _completedAt;
    private string? _filePath;
    private string _speed = string.Empty;
    private string? _eta;
    private string _downloadedSize = string.Empty;
    private string _totalSize = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Unikalny identyfikator elementu
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// URL filmu do pobrania
    /// </summary>
    public string Url
    {
        get => _url;
        set
        {
            _url = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Tytuł filmu (pobierany z YouTube)
    /// </summary>
    public string? Title
    {
        get => _title;
        set
        {
            _title = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Format pobierania
    /// </summary>
    public DownloadFormat Format
    {
        get => _format;
        set
        {
            _format = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Tryb transkodowania
    /// </summary>
    public EncodingMode EncodingMode
    {
        get => _encodingMode;
        set
        {
            _encodingMode = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Bitrate audio
    /// </summary>
    public AudioBitrate AudioBitrate
    {
        get => _audioBitrate;
        set
        {
            _audioBitrate = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Bitrate wideo (dla trybu transkodowania)
    /// </summary>
    public int? VideoBitrate
    {
        get => _videoBitrate;
        set
        {
            _videoBitrate = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Stan pobierania
    /// </summary>
    public DownloadState State
    {
        get => _state;
        set
        {
            _state = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Postęp pobierania (0-100)
    /// </summary>
    public double Progress
    {
        get => _progress;
        set
        {
            _progress = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Komunikat błędu (jeśli wystąpił)
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Liczba prób pobierania
    /// </summary>
    public int RetryCount
    {
        get => _retryCount;
        set
        {
            _retryCount = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Data dodania do kolejki
    /// </summary>
    public DateTime AddedAt
    {
        get => _addedAt;
        set
        {
            _addedAt = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Data rozpoczęcia pobierania
    /// </summary>
    public DateTime? StartedAt
    {
        get => _startedAt;
        set
        {
            _startedAt = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Data zakończenia pobierania
    /// </summary>
    public DateTime? CompletedAt
    {
        get => _completedAt;
        set
        {
            _completedAt = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Ścieżka do pobranego pliku
    /// </summary>
    public string? FilePath
    {
        get => _filePath;
        set
        {
            _filePath = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Czy element jest zaznaczony do pobrania
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Prędkość pobierania
    /// </summary>
    public string Speed
    {
        get => _speed;
        set
        {
            _speed = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Szacowany czas do końca
    /// </summary>
    public string? Eta
    {
        get => _eta;
        set
        {
            _eta = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Rozmiar pobrany
    /// </summary>
    public string DownloadedSize
    {
        get => _downloadedSize;
        set
        {
            _downloadedSize = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Całkowity rozmiar
    /// </summary>
    public string TotalSize
    {
        get => _totalSize;
        set
        {
            _totalSize = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
