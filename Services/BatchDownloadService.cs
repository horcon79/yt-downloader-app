using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDownloader.Infrastructure;
using YoutubeDownloader.Models;

namespace YoutubeDownloader.Services;

/// <summary>
/// Serwis do zarządzania pobieraniem wsadowym
/// </summary>
public class BatchDownloadService
{
    private readonly YoutubeDownloadService _downloadService;
    private readonly ProcessRunner _processRunner;
    private readonly string _appDirectory;
    private readonly ConcurrentDictionary<Guid, BatchDownloadItem> _items;
    private readonly SemaphoreSlim _parallelSemaphore;
    private CancellationTokenSource? _globalCts;

    public event Action<BatchDownloadItem>? ItemProgressChanged;
    public event Action<BatchDownloadItem>? ItemStateChanged;
    public event Action<int, int>? OverallProgressChanged; // completed, total
    public event Action<string>? LogMessage;

    public IReadOnlyCollection<BatchDownloadItem> Items => _items.Values.ToList();

    public BatchDownloadService(string appDirectory)
    {
        _appDirectory = appDirectory;
        _downloadService = new YoutubeDownloadService(appDirectory);
        _processRunner = new ProcessRunner();
        _items = new ConcurrentDictionary<Guid, BatchDownloadItem>();
        _parallelSemaphore = new SemaphoreSlim(2, 2);
    }

    /// <summary>
    /// Dodaje nowy element do kolejki pobierania
    /// </summary>
    public BatchDownloadItem AddItem(string url, DownloadFormat format = DownloadFormat.Mp4)
    {
        var item = new BatchDownloadItem
        {
            Url = url,
            Format = format,
            State = DownloadState.Pending
        };

        _items.TryAdd(item.Id, item);
        return item;
    }

    /// <summary>
    /// Dodaje wiele elementów z listy URL
    /// </summary>
    public void AddItems(IEnumerable<string> urls, DownloadFormat defaultFormat = DownloadFormat.Mp4)
    {
        foreach (var url in urls)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                AddItem(url.Trim(), defaultFormat);
            }
        }
    }

    /// <summary>
    /// Importuje URL z pliku tekstowego (jeden URL na linię)
    /// </summary>
    public int ImportFromTextFile(string filePath, DownloadFormat defaultFormat = DownloadFormat.Mp4)
    {
        if (!File.Exists(filePath))
        {
            LogMessage?.Invoke($"Plik nie istnieje: {filePath}");
            return 0;
        }

        var lines = File.ReadAllLines(filePath);
        var count = 0;

        foreach (var line in lines)
        {
            var url = ExtractUrl(line);
            if (!string.IsNullOrEmpty(url))
            {
                AddItem(url, defaultFormat);
                count++;
            }
        }

        LogMessage?.Invoke($"Zaimportowano {count} URL z pliku");
        return count;
    }

    /// <summary>
    /// Importuje URL z pliku CSV (pierwsza kolumna = URL)
    /// </summary>
    public int ImportFromCsvFile(string filePath, int urlColumnIndex = 0, DownloadFormat defaultFormat = DownloadFormat.Mp4)
    {
        if (!File.Exists(filePath))
        {
            LogMessage?.Invoke($"Plik nie istnieje: {filePath}");
            return 0;
        }

        var lines = File.ReadAllLines(filePath);
        var count = 0;

        foreach (var line in lines)
        {
            var columns = ParseCsvLine(line);
            if (columns.Count > urlColumnIndex)
            {
                var url = ExtractUrl(columns[urlColumnIndex]);
                if (!string.IsNullOrEmpty(url))
                {
                    AddItem(url, defaultFormat);
                    count++;
                }
            }
        }

        LogMessage?.Invoke($"Zaimportowano {count} URL z pliku CSV");
        return count;
    }

    /// <summary>
    /// Importuje URL z pliku JSON (tablica obiektów z właściwością url)
    /// </summary>
    public int ImportFromJsonFile(string filePath, string urlPropertyName = "url", DownloadFormat defaultFormat = DownloadFormat.Mp4)
    {
        if (!File.Exists(filePath))
        {
            LogMessage?.Invoke($"Plik nie istnieje: {filePath}");
            return 0;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var urls = ParseJsonUrls(json, urlPropertyName);

            foreach (var url in urls)
            {
                AddItem(url, defaultFormat);
            }

            LogMessage?.Invoke($"Zaimportowano {urls.Count} URL z pliku JSON");
            return urls.Count;
        }
        catch (System.Text.Json.JsonException ex)
        {
            LogMessage?.Invoke($"Błąd parsowania JSON: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Usuwa element z kolejki
    /// </summary>
    public void RemoveItem(Guid id)
    {
        _items.TryRemove(id, out _);
    }

    /// <summary>
    /// Czyści całą kolejkę
    /// </summary>
    public void Clear()
    {
        _items.Clear();
    }

    /// <summary>
    /// Uruchamia pobieranie wszystkich zaznaczonych elementów
    /// </summary>
    public async Task StartDownloadAsync(BatchDownloadSettings settings, CancellationToken cancellationToken = default)
    {
        _globalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var selectedItems = _items.Values
            .Where(i => i.IsSelected && i.State == DownloadState.Pending)
            .ToList();

        var total = selectedItems.Count;
        var completed = 0;

        LogMessage?.Invoke($"Rozpoczynanie pobierania wsadowego: {total} elementów");

        await Parallel.ForEachAsync(
            selectedItems,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = settings.MaxConcurrentDownloads,
                CancellationToken = _globalCts.Token
            },
            async (item, ct) =>
            {
                await DownloadItemAsync(item, settings, ct);
                Interlocked.Increment(ref completed);
                OverallProgressChanged?.Invoke(completed, total);
            });

        LogMessage?.Invoke($"Pobieranie wsadowe zakończone");
    }

    /// <summary>
    /// Wstrzymuje wszystkie pobierania
    /// </summary>
    public void PauseAll()
    {
        _globalCts?.Cancel();
        LogMessage?.Invoke("Pobieranie wstrzymane");
    }

    /// <summary>
    /// Anuluje wszystkie pobierania
    /// </summary>
    public void CancelAll()
    {
        _globalCts?.Cancel();
        
        foreach (var item in _items.Values)
        {
            if (item.State == DownloadState.Downloading)
            {
                item.State = DownloadState.Cancelled;
                ItemStateChanged?.Invoke(item);
            }
        }

        LogMessage?.Invoke("Pobieranie anulowane");
    }

    /// <summary>
    /// Ponawia nieudane pobierania
    /// </summary>
    public void RetryFailed(BatchDownloadSettings settings)
    {
        var failedItems = _items.Values
            .Where(i => i.State == DownloadState.Error && i.RetryCount < settings.RetryFailedItems)
            .ToList();

        foreach (var item in failedItems)
        {
            item.State = DownloadState.Pending;
            item.RetryCount++;
            item.ErrorMessage = null;
            item.Progress = 0;
            item.StartedAt = null;
            item.CompletedAt = null;
            ItemStateChanged?.Invoke(item);
        }

        LogMessage?.Invoke($"Ponowienie {failedItems.Count} nieudanych pobrań");
    }

    /// <summary>
    /// Usuwa ukończone elementy z kolejki
    /// </summary>
    public void RemoveCompleted()
    {
        var completedIds = _items.Values
            .Where(i => i.State == DownloadState.Completed)
            .Select(i => i.Id)
            .ToList();

        foreach (var id in completedIds)
        {
            _items.TryRemove(id, out _);
        }

        LogMessage?.Invoke($"Usunięto {completedIds.Count} ukończonych elementów");
    }

    /// <summary>
    /// Wybiera wszystkie elementy
    /// </summary>
    public void SelectAll()
    {
        foreach (var item in _items.Values)
        {
            item.IsSelected = true;
        }
    }

    /// <summary>
    /// Odznacza wszystkie elementy
    /// </summary>
    public void DeselectAll()
    {
        foreach (var item in _items.Values)
        {
            item.IsSelected = false;
        }
    }

    private async Task DownloadItemAsync(BatchDownloadItem item, BatchDownloadSettings settings, CancellationToken ct)
    {
        item.State = DownloadState.Downloading;
        item.StartedAt = DateTime.Now;
        ItemStateChanged?.Invoke(item);

        var progress = new Progress<DownloadProgressInfo>(info =>
        {
            item.Progress = info.Percentage;
            item.Speed = info.Speed ?? string.Empty;
            item.Eta = info.Eta;
            item.DownloadedSize = info.DownloadedSize ?? string.Empty;
            item.TotalSize = info.TotalSize ?? string.Empty;

            if (!string.IsNullOrEmpty(info.ErrorMessage))
            {
                item.ErrorMessage = info.ErrorMessage;
            }

            ItemProgressChanged?.Invoke(item);
        });

        try
        {
            // Sprawdź czy plik już istnieje
            var outputFolder = settings.OutputFolder;
            if (settings.SkipExistingFiles)
            {
                var expectedFilename = GetExpectedFilename(item);
                var expectedPath = Path.Combine(outputFolder, expectedFilename);
                if (File.Exists(expectedPath))
                {
                    item.State = DownloadState.Completed;
                    item.FilePath = expectedPath;
                    item.Progress = 100;
                    item.CompletedAt = DateTime.Now;
                    LogMessage?.Invoke($"Plik już istnieje, pominięto: {expectedFilename}");
                    ItemStateChanged?.Invoke(item);
                    return;
                }
            }

            var options = new DownloadOptions
            {
                Url = item.Url,
                Format = item.Format,
                EncodingMode = item.EncodingMode,
                AudioBitrate = item.AudioBitrate,
                VideoBitrate = item.VideoBitrate,
                OutputFolder = outputFolder
            };

            var result = await _downloadService.DownloadAsync(options, progress, 
                msg => LogMessage?.Invoke(msg), ct);

            if (result.State == DownloadState.Completed)
            {
                item.State = DownloadState.Completed;
                item.FilePath = Path.Combine(outputFolder, result.FileName ?? "");
                item.CompletedAt = DateTime.Now;
                item.Progress = 100;
                LogMessage?.Invoke($"Pobrano: {item.Title ?? item.Url}");
            }
            else if (result.State == DownloadState.Cancelled)
            {
                item.State = DownloadState.Cancelled;
                item.CompletedAt = DateTime.Now;
            }
            else
            {
                item.State = DownloadState.Error;
                item.ErrorMessage = result.ErrorMessage ?? "Nieznany błąd";
                item.CompletedAt = DateTime.Now;
                LogMessage?.Invoke($"Błąd: {item.ErrorMessage}");
            }
        }
        catch (OperationCanceledException)
        {
            item.State = DownloadState.Cancelled;
            item.CompletedAt = DateTime.Now;
            LogMessage?.Invoke($"Anulowano: {item.Url}");
        }
        catch (Exception ex)
        {
            item.State = DownloadState.Error;
            item.ErrorMessage = ex.Message;
            item.CompletedAt = DateTime.Now;
            LogMessage?.Invoke($"Błąd: {ex.Message}");
        }

        ItemStateChanged?.Invoke(item);
    }

    private string GetExpectedFilename(BatchDownloadItem item)
    {
        var extension = item.Format.ToString().ToLower();
        var title = SanitizeFilename(item.Title ?? "video");
        return $"{title}.{extension}";
    }

    private string SanitizeFilename(string filename)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            filename = filename.Replace(c, '_');
        }
        return filename.Length > 100 ? filename.Substring(0, 100) : filename;
    }

    private static string? ExtractUrl(string line)
    {
        var urlMatch = Regex.Match(line, @"https?://[^\s]+");
        return urlMatch.Success ? urlMatch.Value : null;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = string.Empty;
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = string.Empty;
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);
        return result;
    }

    private static List<string> ParseJsonUrls(string json, string urlPropertyName)
    {
        var urls = new List<string>();

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Sprawdź czy to tablica
            if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    if (element.TryGetProperty(urlPropertyName, out var urlElement))
                    {
                        urls.Add(urlElement.GetString() ?? string.Empty);
                    }
                }
            }
            // Sprawdź czy to obiekt z właściwością urls (lista)
            else if (root.TryGetProperty("urls", out var urlsArray))
            {
                foreach (var element in urlsArray.EnumerateArray())
                {
                    urls.Add(element.GetString() ?? string.Empty);
                }
            }
        }
        catch
        {
            // Ignoruj błędy parsowania
        }

        return urls;
    }

    /// <summary>
    /// Sprawdza dostępność narzędzi
    /// </summary>
    public ToolAvailability CheckToolAvailability()
    {
        return _downloadService.CheckToolAvailability();
    }

    /// <summary>
    /// Pobiera tytuł filmu z URL
    /// </summary>
    public async Task<string?> GetVideoTitleAsync(string url)
    {
        var ytdlpPath = _processRunner.FindTool("yt-dlp.exe", _appDirectory);
        if (string.IsNullOrEmpty(ytdlpPath))
        {
            return null;
        }

        try
        {
            var result = await _processRunner.RunAsync(
                ytdlpPath,
                $"--get-title \"{url}\"",
                _appDirectory,
                CancellationToken.None,
                output => { },
                error => { });

            return result.Success ? result.StandardOutput.Trim() : null;
        }
        catch
        {
            return null;
        }
    }
}
