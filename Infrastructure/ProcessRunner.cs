using System.Diagnostics;
using System.IO;

namespace YoutubeDownloader.Infrastructure;

/// <summary>
/// Wynik uruchomienia procesu
/// </summary>
public class ProcessResult
{
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}

/// <summary>
/// Serwis do uruchamiania procesow zewnetrznych (yt-dlp, ffmpeg)
/// </summary>
public class ProcessRunner
{
    /// <summary>
    /// Uruchamia proces i zwraca wynik
    /// </summary>
    /// <param name="fileName">Sciezka do pliku wykonywalnego</param>
    /// <param name="arguments">Argumenty wiersza polecen</param>
    /// <param name="workingDirectory">Katalog roboczy</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <param name="outputCallback">Callback dla linii stdout</param>
    /// <param name="errorCallback">Callback dla linii stderr</param>
    /// <param name="timeoutMs">Timeout w milisekundach (0 = bez limitu)</param>
    public async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default,
        Action<string>? outputCallback = null,
        Action<string>? errorCallback = null,
        int timeoutMs = 0)
    {
        var result = new ProcessResult();

        using var process = new Process();
        
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                result.StandardOutput += e.Data + Environment.NewLine;
                outputCallback?.Invoke(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                result.StandardError += e.Data + Environment.NewLine;
                errorCallback?.Invoke(e.Data);
            }
        };

        try
        {
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var task = process.WaitForExitAsync(cancellationToken);

            if (timeoutMs > 0)
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMs, cancellationToken));
                if (completedTask != task)
                {
                    // Timeout - zabij proces
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                        // Proces mogl sie juz zakonczyc sam
                    }
                    
                    result.Success = false;
                    result.Exception = new TimeoutException($"Proces przekroczyl limit czasu ({timeoutMs}ms)");
                    return result;
                }
            }
            else
            {
                await task;
            }

            result.ExitCode = process.ExitCode;
            result.Success = process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            result.Exception = ex;
            result.Success = false;
        }

        return result;
    }

    /// <summary>
    /// Sprawdza czy plik wykonywalny istnieje
    /// </summary>
    public bool ExecutableExists(string path)
    {
        return File.Exists(path);
    }

    /// <summary>
    /// Znajduje sciezke do narzedzia w katalogu aplikacji
    /// </summary>
    public string? FindTool(string toolName, string appDirectory)
    {
        var toolPath = Path.Combine(appDirectory, "tools", toolName);
        if (File.Exists(toolPath))
        {
            return toolPath;
        }

        // Sprobuj w PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var path in pathEnv.Split(';'))
        {
            var fullPath = Path.Combine(path.Trim(), toolName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}
