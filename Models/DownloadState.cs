namespace YoutubeDownloader.Models;

/// <summary>
/// Stan pobierania
/// </summary>
public enum DownloadState
{
    Idle,
    Downloading,
    Completed,
    Cancelled,
    Error
}
