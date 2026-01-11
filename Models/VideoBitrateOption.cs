namespace YoutubeDownloader.Models;

/// <summary>
/// Opcja bitrate wideo dla ComboBox
/// </summary>
public class VideoBitrateOption
{
    public string Name { get; set; } = string.Empty;
    public int? Bitrate { get; set; } // null oznacza opcje "Wlasny"

    public override string ToString() => Name;

    public static List<VideoBitrateOption> GetPresets()
    {
        return new List<VideoBitrateOption>
        {
            new VideoBitrateOption { Name = "Średnia (zalecane) - 1500", Bitrate = 1500 },
            new VideoBitrateOption { Name = "Niska - 500", Bitrate = 500 },
            new VideoBitrateOption { Name = "Wysoka - 4000", Bitrate = 4000 },
            new VideoBitrateOption { Name = "Własny...", Bitrate = null }
        };
    }
}
