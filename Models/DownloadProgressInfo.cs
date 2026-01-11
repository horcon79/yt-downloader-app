namespace YoutubeDownloader.Models;

/// <summary>
/// Informacje o postępie pobierania
/// </summary>
public class DownloadProgressInfo
{
    /// <summary>
    /// Procent ukończenia (0-100)
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Prędkość pobierania (format: np. "5.2 MB/s")
    /// </summary>
    public string? Speed { get; set; }

    /// <summary>
    /// Szacowany czas do końca (format: np. "2m 30s")
    /// </summary>
    public string? Eta { get; set; }

    /// <summary>
    /// Aktualny stan
    /// </summary>
    public DownloadState State { get; set; } = DownloadState.Idle;

    /// <summary>
    /// Komunikat o błędzie (jeśli wystąpił)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Nazwa pobieranego pliku
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Rozmiar pobranego fragmentu
    /// </summary>
    public string? DownloadedSize { get; set; }

    /// <summary>
    /// Całkowity rozmiar
    /// </summary>
    public string? TotalSize { get; set; }
}
