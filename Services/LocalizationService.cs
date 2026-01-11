using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace YoutubeDownloader.Services;

/// <summary>
/// Serwis odpowiedzialny za zmianę języka aplikacji w czasie rzeczywistym
/// </summary>
public static class LocalizationService
{
    private const string DictionaryPathPrefix = "Resources/Languages/Dictionary-";
    private const string DictionaryPathSuffix = ".xaml";

    /// <summary>
    /// Zmienia język aplikacji na podstawie kodu (pl, en, de)
    /// </summary>
    /// <param name="langCode">Kod języka</param>
    public static void SetLanguage(string langCode)
    {
        var dictPath = $"{DictionaryPathPrefix}{langCode.ToLower()}{DictionaryPathSuffix}";
        var newDict = new ResourceDictionary
        {
            Source = new Uri(dictPath, UriKind.Relative)
        };

        // Znajdź obecny słownik językowy i go usuń
        var oldDict = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains(DictionaryPathPrefix));

        if (oldDict != null)
        {
            int index = Application.Current.Resources.MergedDictionaries.IndexOf(oldDict);
            Application.Current.Resources.MergedDictionaries[index] = newDict;
        }
        else
        {
            Application.Current.Resources.MergedDictionaries.Add(newDict);
        }
    }

    /// <summary>
    /// Pobiera przetłumaczony ciąg znaków na podstawie klucza
    /// </summary>
    public static string GetString(string key)
    {
        return Application.Current.TryFindResource(key) as string ?? key;
    }
}
