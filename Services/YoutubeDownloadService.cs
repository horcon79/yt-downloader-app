using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using YoutubeDownloader.Infrastructure;
using YoutubeDownloader.Models;

namespace YoutubeDownloader.Services;

/// <summary>
/// Wyjatek serwisu pobierania
/// </summary>
public class YoutubeDownloadException : Exception
{
    public YoutubeDownloadException(string message) : base(message) { }
    public YoutubeDownloadException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Serwis do pobierania filmow z YouTube
/// </summary>
public class YoutubeDownloadService
{
    private readonly ProcessRunner _processRunner;
    private readonly string _appDirectory;

    public YoutubeDownloadService(string appDirectory)
    {
        _appDirectory = appDirectory;
        _processRunner = new ProcessRunner();
    }

    /// <summary>
    /// Pobiera film z YouTube zgodnie z opcjami
    /// </summary>
    public async Task<DownloadProgressInfo> DownloadAsync(
        DownloadOptions options,
        IProgress<DownloadProgressInfo> progress,
        Action<string>? logCallback,
        CancellationToken cancellationToken)
    {
        var result = new DownloadProgressInfo { State = DownloadState.Downloading };

        // Walidacja
        var validationResult = options.Validate();
        if (validationResult != null)
        {
            result.State = DownloadState.Error;
            result.ErrorMessage = validationResult.ErrorMessage ?? "Nieznany blad walidacji";
            return result;
        }

        // Sprawdz narzedzia
        var ytdlpPath = _processRunner.FindTool("yt-dlp.exe", _appDirectory);
        if (string.IsNullOrEmpty(ytdlpPath))
        {
            result.State = DownloadState.Error;
            result.ErrorMessage = "Nie znaleziono yt-dlp.exe. Upewnij sie, ze plik znajduje sie w katalogu tools/.";
            return result;
        }

        // Generuj argumenty dla yt-dlp
        var arguments = GenerateYtDlpArguments(options);

        Logger.LogInfo($"Rozpoczynanie pobierania: yt-dlp {arguments}");

        // Sprawdz dostepnosc FFmpeg dla wideo (potrzebny do mergowania strumieni)
        if (!options.IsAudioOnly)
        {
            var ffmpegPath = _processRunner.FindTool("ffmpeg.exe", _appDirectory);
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                result.State = DownloadState.Error;
                result.ErrorMessage = "Nie znaleziono ffmpeg.exe. FFmpeg jest wymagany do polaczenia strumieni wideo i audio. Umiesc ffmpeg.exe w katalogu tools/.";
                return result;
            }
        }

        var tempFiles = new List<string>();

        try
        {
            await _processRunner.RunAsync(
                ytdlpPath,
                arguments,
                options.OutputFolder,
                cancellationToken,
                output =>
                {
                    ParseProgress(output, result);
                    progress.Report(result);
                    if (!string.IsNullOrWhiteSpace(output) && output.Contains("[download]"))
                        logCallback?.Invoke(output);
                    Logger.LogDebug($"yt-dlp output: {output}");
                },
                error =>
                {
                    ParseError(error, result);
                    if (!string.IsNullOrWhiteSpace(error))
                        logCallback?.Invoke(error);
                    Logger.LogDebug($"yt-dlp error: {error}");
                },
                timeoutMs: 0);

            if (cancellationToken.IsCancellationRequested)
            {
                result.State = DownloadState.Cancelled;
                result.ErrorMessage = "Pobieranie zostalo anulowane przez uzytkownika.";
                CleanupTempFiles(tempFiles);
                return result;
            }

            // Jesli wymagane transkodowanie, uruchom FFmpeg
            if (options.EncodingMode == EncodingMode.Transcode && !options.IsAudioOnly)
            {
                try
                {
                    result.State = DownloadState.Downloading;
                    result.Percentage = 0;
                    result.Speed = "Transkodowanie...";
                    result.Eta = null;
                    progress.Report(result);

                    await TranscodeVideoAsync(options, progress, logCallback, cancellationToken, tempFiles, result);
                }
                catch (Exception ex)
                {
                    result.State = DownloadState.Error;
                    result.ErrorMessage = ex.Message;
                    Logger.LogError($"Blad transkodowania: {ex.Message}");
                    return result;
                }
            }

            result.State = DownloadState.Completed;
            result.Percentage = 100;
            progress.Report(result);

            Logger.LogInfo("Pobieranie zakonczone pomyslnie");
            return result;
        }
        catch (Exception ex)
        {
            result.State = DownloadState.Error;
            result.ErrorMessage = $"Wystapil blad: {ex.Message}";
            Logger.LogError($"Blad podczas pobierania: {ex.Message}");
            CleanupTempFiles(tempFiles);
            return result;
        }
    }

    /// <summary>
    /// Generuje argumenty dla yt-dlp
    /// </summary>
    private string GenerateYtDlpArguments(DownloadOptions options)
    {
        var args = new List<string>();

        // URL
        args.Add($"\"{options.Url}\"");

        // Format wideo
        if (options.IsAudioOnly)
        {
            // Audio-only - pobierz najlepsze audio i konwertuj
            switch (options.Format)
            {
                case DownloadFormat.Mp3:
                    args.Add("-x"); // Ekstrahuj audio
                    args.Add("--audio-format");
                    args.Add("mp3");
                    args.Add("--audio-quality");
                    args.Add($"{(int)options.AudioBitrate}K");
                    break;
                case DownloadFormat.M4A:
                    args.Add("-x");
                    args.Add("--audio-format");
                    args.Add("m4a");
                    break;
            }
        }
        else
        {
            // Wideo - wybierz format i wymus scalenie do kontenera
            args.Add("-f");
            
            // Format selection - best available video+audio
            if (options.Format == DownloadFormat.Mp4)
            {
                // Dla MP4: bestvideo+bestaudio i wymus scalenie do mp4
                args.Add("bestvideo+bestaudio/best");
                args.Add("--merge-output-format");
                args.Add("mp4");
            }
            else if (options.Format == DownloadFormat.MkV)
            {
                // Dla MKV: bestvideo+bestaudio i wymus scalenie do mkv
                args.Add("bestvideo+bestaudio/best");
                args.Add("--merge-output-format");
                args.Add("mkv");
            }
            else // WebM
            {
                // Dla WebM: bestvideo+bestaudio i wymus scalenie do webm
                args.Add("bestvideo+bestaudio/best");
                args.Add("--merge-output-format");
                args.Add("webm");
            }

            // Embed metadata
            args.Add("--embed-metadata");
        }

        // Plik wyjsciowy - uzyj rozszerzenia zgodnego z wybranym formatem
        var extension = options.IsAudioOnly ? 
            options.Format.ToString().ToLower() : 
            options.Format.ToString().ToLower();
        args.Add("-o");
        args.Add($"\"%(title)s.{extension}\"");

        // Inne opcje
        args.Add("--force-ipv4");
        args.Add("--newline");
        args.Add("--progress");

        return string.Join(" ", args);
    }

    /// <summary>
    /// Parsuje postep z outputu yt-dlp
    /// </summary>
    private void ParseProgress(string output, DownloadProgressInfo result)
    {
        try
        {
            // Parsuj procent
            if (output.Contains("[download]"))
            {
                var percentMatch = Regex.Match(
                    output, @"(\s?\d+\.?\d*)% of (\s?\d+\.?\d*)([KMG]iB)?");

                if (percentMatch.Success)
                {
                    result.Percentage = double.Parse(percentMatch.Groups[1].Value.Trim(), CultureInfo.InvariantCulture);

                    var downloaded = percentMatch.Groups[2].Value.Trim();
                    var unit = percentMatch.Groups[3].Value;
                    result.DownloadedSize = $"{downloaded} {unit}".Trim();
                }

                // Parsuj predkosc
                var speedMatch = Regex.Match(
                    output, @"at (\d+\.?\d*)([KMG]?B/s)");
                if (speedMatch.Success)
                {
                    result.Speed = speedMatch.Groups[1].Value + " " + speedMatch.Groups[2].Value;
                }

                // Parsuj ETA
                var etaMatch = Regex.Match(
                    output, @"ETA (\d{2}:\d{2}:\d{2})");
                if (etaMatch.Success)
                {
                    result.Eta = etaMatch.Groups[1].Value;
                }
            }

            // Parsuj filename
            if (output.Contains("[download] Destination:"))
            {
                var filenameMatch = Regex.Match(
                    output, @"Destination: (.+)");
                if (filenameMatch.Success)
                {
                    result.FileName = Path.GetFileName(filenameMatch.Groups[1].Value);
                }
            }

            // Parsuj total size
            var sizeMatch = Regex.Match(
                output, @"of (\d+\.?\d*)([KMG]iB)? at");
            if (sizeMatch.Success)
            {
                result.TotalSize = $"{sizeMatch.Groups[1].Value} {sizeMatch.Groups[2].Value}".Trim();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Blad parsowania postepu: {output} - {ex.Message}");
        }
    }

    /// <summary>
    /// Parsuje bledy z outputu yt-dlp
    /// </summary>
    private void ParseError(string error, DownloadProgressInfo result)
    {
        if (error.Contains("Unable to extract") || error.Contains("404"))
        {
            result.ErrorMessage = $"Nie mozna pobrac filmu: {error}";
        }
        else if (error.Contains("HTTP Error"))
        {
            result.ErrorMessage = $"Blad HTTP: {error}";
        }
        else if (error.Contains("This video is unavailable"))
        {
            result.ErrorMessage = "Film jest niedostepny (usuniety lub prywatny).";
        }
        else if (error.Contains("Sign in to confirm your age"))
        {
            result.ErrorMessage = "Film wymaga potwierdzenia wieku.";
        }
    }

    /// <summary>
    /// Transkoduje wideo z uzyciem FFmpeg
    /// </summary>
    private async Task TranscodeVideoAsync(
        DownloadOptions options,
        IProgress<DownloadProgressInfo> progress,
        Action<string>? logCallback,
        CancellationToken cancellationToken,
        List<string> tempFiles,
        DownloadProgressInfo progressInfo)
    {
        var ffmpegPath = _processRunner.FindTool("ffmpeg.exe", _appDirectory);
        if (string.IsNullOrEmpty(ffmpegPath))
        {
            throw new Exception("Nie znaleziono ffmpeg.exe. Transkodowanie niemozliwe.");
        }

        Logger.LogInfo("Rozpoczynanie transkodowania z FFmpeg");

        // Znajdz plik do transkodowania
        string? inputFile = null;
        if (!string.IsNullOrEmpty(progressInfo.FileName))
        {
            inputFile = Path.Combine(options.OutputFolder, progressInfo.FileName);
        }

        if (inputFile == null || !File.Exists(inputFile))
        {
            // Fallback - znajdz najnowszy plik w folderze
            inputFile = Directory.GetFiles(options.OutputFolder, "*.*")
                .OrderByDescending(File.GetCreationTime)
                .FirstOrDefault();
        }

        if (inputFile == null || !File.Exists(inputFile))
        {
            throw new Exception("Nie znaleziono pliku do transkodowania.");
        }

        // Nazwa tymczasowa dla pliku wyjsciowego, aby uniknac bledu "input and output same file"
        var outputExt = options.Format.ToString().ToLower();
        var outputFile = Path.Combine(options.OutputFolder, $"transcoded_{Guid.NewGuid().ToString().Substring(0, 8)}.{outputExt}");
        var finalFile = Path.Combine(options.OutputFolder, Path.GetFileNameWithoutExtension(inputFile) + "." + outputExt);

        // Argumenty FFmpeg
        // Uzywamy -progress pipe:1 dla maszynowego formatu postepu
        var arguments = $"-i \"{inputFile}\" ";

        if (options.VideoBitrate.HasValue)
        {
            arguments += $"-b:v {options.VideoBitrate.Value}K ";
        }

        arguments += $"-b:a {(int)options.AudioBitrate}K ";
        arguments += $"-max_muxing_queue_size 1024 ";
        arguments += $"-progress pipe:1 ";
        arguments += $"-y \"{outputFile}\"";

        TimeSpan duration = TimeSpan.Zero;

        var ffmpegResult = await _processRunner.RunAsync(
            ffmpegPath,
            arguments,
            options.OutputFolder,
            cancellationToken,
            output => 
            {
                if (ParseFfmpegOutput(output, ref duration, out double percentage))
                {
                    progressInfo.Percentage = percentage;
                    progressInfo.Speed = "Transkodowanie...";
                    progress.Report(progressInfo);
                }
                if (!string.IsNullOrWhiteSpace(output))
                    logCallback?.Invoke(output);
                Logger.LogDebug($"FFmpeg: {output}");
            },
            error => 
            {
                ParseFfmpegOutput(error, ref duration, out _);
                if (!string.IsNullOrWhiteSpace(error) && error.Contains("time="))
                    logCallback?.Invoke(error);
                Logger.LogDebug($"FFmpeg error: {error}");
            });

        if (cancellationToken.IsCancellationRequested)
        {
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
            return;
        }

        if (!ffmpegResult.Success)
        {
            var errorDetails = !string.IsNullOrEmpty(ffmpegResult.StandardError) ? ffmpegResult.StandardError : "Brak szczegolow bledu FFmpeg";
            throw new Exception($"Transkodowanie nie powiodlo sie:\n{errorDetails}");
        }

        // Podmien pliki
        try
        {
            // Usun oryginalny, jesli nazwa docelowa jest inna
            if (File.Exists(finalFile) && !string.Equals(inputFile, finalFile, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(finalFile);
            }
            
            // Jesli to ten sam plik (np. zmiana bitrate'u bez zmiany formatu), usun oryginalny przed przeniesieniem
            if (File.Exists(finalFile))
            {
                File.Delete(finalFile);
            }

            File.Move(outputFile, finalFile);
            
            // Usun plik wejsciowy, jesli byl inny niz finalny
            if (File.Exists(inputFile) && !string.Equals(inputFile, finalFile, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(inputFile);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Blad podczas finalizowania pliku: {ex.Message}");
        }

        Logger.LogInfo($"Transkodowanie zakonczone: {finalFile}");
    }

    /// <summary>
    /// Parsuje wyjscie FFmpeg w poszukiwaniu czasu trwania i postepu
    /// </summary>
    private bool ParseFfmpegOutput(string output, ref TimeSpan duration, out double percentage)
    {
        percentage = 0;
        try
        {
            // Szukaj czasu trwania pliku wejsciowego (zwykle na poczatku w stderr)
            // Przyklad: Duration: 00:03:45.12, start: 0.000000, bitrate: 1234 kb/s
            var durationMatch = Regex.Match(output, @"Duration: (\d{2}:\d{2}:\d{2}\.\d{2})");
            if (durationMatch.Success)
            {
                if (TimeSpan.TryParse(durationMatch.Groups[1].Value, out var parsedDuration))
                {
                    duration = parsedDuration;
                }
            }

            // Szukaj postepu (z progress pipe:1 lub standardowo z stderr)
            // progress pipe:1 daje out_time_ms=... lub out_time=HH:MM:SS.mmmmmm
            // stderr daje time=HH:MM:SS.mm
            var timeMatch = Regex.Match(output, @"time=(\d{2}:\d{2}:\d{2}\.\d{2})");
            if (timeMatch.Success && duration > TimeSpan.Zero)
            {
                if (TimeSpan.TryParse(timeMatch.Groups[1].Value, out var currentTime))
                {
                    percentage = (currentTime.TotalMilliseconds / duration.TotalMilliseconds) * 100;
                    percentage = Math.Min(100, Math.Max(0, percentage));
                    return true;
                }
            }
            
            // Maszynowy format z pipe:1
            var outTimeMatch = Regex.Match(output, @"out_time=(\d{2}:\d{2}:\d{2}\.\d+)");
            if (outTimeMatch.Success && duration > TimeSpan.Zero)
            {
                if (TimeSpan.TryParse(outTimeMatch.Groups[1].Value, out var currentTime))
                {
                    percentage = (currentTime.TotalMilliseconds / duration.TotalMilliseconds) * 100;
                    percentage = Math.Min(100, Math.Max(0, percentage));
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Czysci pliki tymczasowe
    /// </summary>
    private void CleanupTempFiles(List<string> tempFiles)
    {
        foreach (var file in tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Nie udalo sie usunac pliku tymczasowego: {file} - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Sprawdza dostepnosc narzedzi
    /// </summary>
    public ToolAvailability CheckToolAvailability()
    {
        var ytdlpPath = _processRunner.FindTool("yt-dlp.exe", _appDirectory);
        var ffmpegPath = _processRunner.FindTool("ffmpeg.exe", _appDirectory);

        return new ToolAvailability
        {
            YtdlpAvailable = !string.IsNullOrEmpty(ytdlpPath),
            FfmpegAvailable = !string.IsNullOrEmpty(ffmpegPath),
            YtdlpPath = ytdlpPath,
            FfmpegPath = ffmpegPath
        };
    }
}

/// <summary>
/// Informacja o dostepnosci narzedzi
/// </summary>
public class ToolAvailability
{
    public bool YtdlpAvailable { get; set; }
    public bool FfmpegAvailable { get; set; }
    public string? YtdlpPath { get; set; }
    public string? FfmpegPath { get; set; }

    public bool AllToolsAvailable => YtdlpAvailable;
}
