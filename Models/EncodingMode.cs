namespace YoutubeDownloader.Models;

/// <summary>
/// Tryb transkodowania wideo
/// </summary>
public enum EncodingMode
{
    /// <summary>
    /// Bez transkodowania - najszybsze pobieranie
    /// </summary>
    NoTranscoding,
    
    /// <summary>
    /// Transkodowanie z użyciem FFmpeg i ustawionym bitrate
    /// </summary>
    Transcode
}
