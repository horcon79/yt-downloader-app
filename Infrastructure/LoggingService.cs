using System.Diagnostics;
using System.IO;

namespace YoutubeDownloader.Infrastructure;

/// <summary>
/// Prosty serwis logowania - zapisuje logi do pliku w %LOCALAPPDATA%\YoutubeDownloader\logs
/// </summary>
public static class Logger
{
    private static readonly string _logFolder;
    private static readonly object _lock = new();

    static Logger()
    {
        // Logi w katalogu aplikacji
        _logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        // Utworz folder jesli nie istnieje
        try
        {
            if (!Directory.Exists(_logFolder))
            {
                Directory.CreateDirectory(_logFolder);
            }
        }
        catch
        {
            // Ignoruj bledy podczas tworzenia folderu
        }
    }

    /// <summary>
    /// Sciezka do pliku logu
    /// </summary>
    public static string CurrentLogFile => Path.Combine(_logFolder, $"log_{DateTime.Now:yyyyMMdd}.txt");

    /// <summary>
    /// Zapisz wiadomosc do logu
    /// </summary>
    public static void Log(string message, LogLevel level = LogLevel.Info)
    {
        try
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            
            lock (_lock)
            {
                File.AppendAllText(CurrentLogFile, logMessage + Environment.NewLine);
            }

            // Rowniez wypisz do konsoli debug
            Debug.WriteLine(logMessage);
        }
        catch
        {
            // Ignoruj bledy podczas logowania
        }
    }

    /// <summary>
    /// Zapisz wiadomosc z poziomu Debug
    /// </summary>
    public static void LogDebug(string message) => Log(message, LogLevel.Debug);

    /// <summary>
    /// Zapisz wiadomosc z poziomu Info
    /// </summary>
    public static void LogInfo(string message) => Log(message, LogLevel.Info);

    /// <summary>
    /// Zapisz wiadomosc z poziomu Warning
    /// </summary>
    public static void LogWarning(string message) => Log(message, LogLevel.Warning);

    /// <summary>
    /// Zapisz wiadomosc z poziomu Error
    /// </summary>
    public static void LogError(string message) => Log(message, LogLevel.Error);

    /// <summary>
    /// Wyczysc stare logi (starsze niz X dni)
    /// </summary>
    public static void CleanupOldLogs(int daysToKeep = 7)
    {
        try
        {
            var files = Directory.GetFiles(_logFolder, "log_*.txt");
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

            foreach (var file in files)
            {
                try
                {
                    var fileDate = File.GetCreationTime(file);
                    if (fileDate < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // Ignoruj bledy dla pojedynczych plikow
                }
            }
        }
        catch
        {
            // Ignoruj bledy podczas czyszczenia
        }
    }
}

/// <summary>
/// Poziom logowania
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}
