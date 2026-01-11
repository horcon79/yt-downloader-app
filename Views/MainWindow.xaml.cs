using System.Windows;
using System.Windows.Input;
using YoutubeDownloader.ViewModels;

namespace YoutubeDownloader.Views;

/// <summary>
/// Glowne okno aplikacji
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Obsluga wklejania URL
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, (s, e) =>
        {
            if (DataContext is MainViewModel vm && Clipboard.ContainsText())
            {
                vm.Url = Clipboard.GetText();
            }
        }));
    }

    private void LogTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox textBox)
        {
            textBox.ScrollToEnd();
        }
    }
}
