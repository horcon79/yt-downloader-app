using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using YoutubeDownloader.Infrastructure;
using YoutubeDownloader.ViewModels;
using YoutubeDownloader.Views;

namespace YoutubeDownloader;

/// <summary>
/// Glowa klasa aplikacji
/// </summary>
public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            Logger.LogInfo("Uruchamianie aplikacji YouTube Downloader");

            // Utworz ViewModel
            var viewModel = new MainViewModel();

            // Pokaz okno
            var mainWindow = new MainWindow(viewModel);
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.LogInfo("Zamykanie aplikacji YouTube Downloader");
        base.OnExit(e);
    }

    private void ShowError(Exception ex)
    {
        var errorMsg = $"Wystapil blad podczas uruchamiania aplikacji:\n\n{ex.Message}\n\n{ex.StackTrace}";
        Logger.LogError(errorMsg);
        MessageBox.Show(errorMsg, "Blad aplikacji", MessageBoxButton.OK, MessageBoxImage.Error);
        Current?.Shutdown(1);
    }
}
