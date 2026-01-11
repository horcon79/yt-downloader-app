namespace YoutubeDownloader.Models;

/// <summary>
/// Stan pobierania
/// </summary>
public enum DownloadState
{
    Idle,
    Pending,
    Downloading,
    Completed,
    Cancelled,
    Error
}
