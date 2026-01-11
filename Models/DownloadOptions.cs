using System.ComponentModel.DataAnnotations;

namespace YoutubeDownloader.Models;

/// <summary>
/// Opcje pobierania
/// </summary>
public class DownloadOptions
{
    /// <summary>
    /// URL filmu do pobrania
    /// </summary>
    [Required(ErrorMessage = "URL jest wymagany")]
    [Url(ErrorMessage = "Niepoprawny format URL")]
    public string? Url { get; set; }

    /// <summary>
    /// Format docelowy
    /// </summary>
    public DownloadFormat Format { get; set; } = DownloadFormat.Mp4;

    /// <summary>
    /// Tryb transkodowania (tylko dla wideo)
    /// </summary>
    public EncodingMode EncodingMode { get; set; } = EncodingMode.NoTranscoding;

    /// <summary>
    /// Bitrate audio (dla trybu audio-only lub transkodowania)
    /// </summary>
    public AudioBitrate AudioBitrate { get; set; } = AudioBitrate.Kbp192;

    /// <summary>
    /// Bitrate wideo dla trybu transkodowania (kbps)
    /// </summary>
    public int? VideoBitrate { get; set; }

    /// <summary>
    /// Folder zapisu
    /// </summary>
    [Required(ErrorMessage = "Folder zapisu jest wymagany")]
    public string? OutputFolder { get; set; }

    /// <summary>
    /// Czy pobieranie jest dla audio-only (mp3, m4a)
    /// </summary>
    public bool IsAudioOnly => Format == DownloadFormat.Mp3 || Format == DownloadFormat.M4A;

    /// <summary>
    /// Sprawdza czy opcje są poprawne
    /// </summary>
    public ValidationResult? Validate()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            return new ValidationResult("URL jest wymagany", new[] { nameof(Url) });
        }

        if (string.IsNullOrWhiteSpace(OutputFolder))
        {
            return new ValidationResult("Folder zapisu jest wymagany", new[] { nameof(OutputFolder) });
        }

        try
        {
            var uri = new Uri(Url);
            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return new ValidationResult("URL musi być http lub https", new[] { nameof(Url) });
            }
        }
        catch
        {
            return new ValidationResult("Niepoprawny format URL", new[] { nameof(Url) });
        }

        return null;
    }
}
