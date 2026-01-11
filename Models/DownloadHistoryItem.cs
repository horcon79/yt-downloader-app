using System;

namespace YoutubeDownloader.Models;

/// <summary>
/// Element historii pobierania
/// </summary>
public class DownloadHistoryItem
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;

    public string FileName => System.IO.Path.GetFileName(FilePath);
}
