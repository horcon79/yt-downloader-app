using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using YoutubeDownloader.Models;
using YoutubeDownloader.Infrastructure;

namespace YoutubeDownloader.Services;

/// <summary>
/// Serwis do zarządzania historią pobrań
/// </summary>
public class HistoryService
{
    private readonly string _historyFilePath;
    private static readonly object _lock = new();

    public HistoryService()
    {
        _historyFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.json");
    }

    /// <summary>
    /// Pobiera listę historii
    /// </summary>
    public List<DownloadHistoryItem> GetHistory()
    {
        lock (_lock)
        {
            if (!File.Exists(_historyFilePath))
                return new List<DownloadHistoryItem>();

            try
            {
                var json = File.ReadAllText(_historyFilePath);
                return JsonSerializer.Deserialize<List<DownloadHistoryItem>>(json) ?? new List<DownloadHistoryItem>();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Blad podczas odczytu historii: {ex.Message}");
                return new List<DownloadHistoryItem>();
            }
        }
    }

    /// <summary>
    /// Dodaje wpis do historii
    /// </summary>
    public void AddEntry(DownloadHistoryItem item)
    {
        lock (_lock)
        {
            var history = GetHistory();
            history.Insert(0, item); // Najnowsze na poczatku

            // Ogranicz do 100 wpisow
            if (history.Count > 100)
            {
                history = history.Take(100).ToList();
            }

            try
            {
                var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_historyFilePath, json);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Blad podczas zapisu historii: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Czysci historie
    /// </summary>
    public void ClearHistory()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_historyFilePath))
                    File.Delete(_historyFilePath);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Blad podczas czyszczenia historii: {ex.Message}");
            }
        }
    }
}
