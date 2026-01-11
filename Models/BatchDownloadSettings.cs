namespace YoutubeDownloader.Models;

/// <summary>
/// Ustawienia pobierania wsadowego
/// </summary>
public class BatchDownloadSettings
{
    /// <summary>
    /// Folder docelowy dla pobranych plików
    /// </summary>
    public string OutputFolder { get; set; } = string.Empty;

    /// <summary>
    /// Domyślny format pobierania
    /// </summary>
    public DownloadFormat DefaultFormat { get; set; } = DownloadFormat.Mp4;

    /// <summary>
    /// Domyślny bitrate audio
    /// </summary>
    public AudioBitrate DefaultAudioBitrate { get; set; } = AudioBitrate.Kbp192;

    /// <summary>
    /// Domyślny bitrate wideo (dla transkodowania)
    /// </summary>
    public int? DefaultVideoBitrate { get; set; }

    /// <summary>
    /// Domyślny tryb transkodowania
    /// </summary>
    public EncodingMode DefaultEncodingMode { get; set; } = EncodingMode.NoTranscoding;

    /// <summary>
    /// Maksymalna liczba równoległych pobierań
    /// </summary>
    public int MaxConcurrentDownloads { get; set; } = 2;

    /// <summary>
    /// Liczba prób dla nieudanych pobierań
    /// </summary>
    public int RetryFailedItems { get; set; } = 3;

    /// <summary>
    /// Czy zatrzymać pobieranie przy pierwszym błędzie
    /// </summary>
    public bool StopOnError { get; set; } = false;

    /// <summary>
    /// Czy automatycznie rozpocząć pobieranie po załadowaniu
    /// </summary>
    public bool AutoStartOnLoad { get; set; } = false;

    /// <summary>
    /// Czy utworzyć podfolder dla każdej playlisty
    /// </summary>
    public bool CreateSubfolderPerPlaylist { get; set; } = true;

    /// <summary>
    /// Czy dodać datę do nazwy pliku
    /// </summary>
    public bool AddDateToFilename { get; set; } = false;

    /// <summary>
    /// Czy pobierać tylko nowe pliki (sprawdzać istniejące)
    /// </summary>
    public bool SkipExistingFiles { get; set; } = false;
}
