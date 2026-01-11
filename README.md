# Horcon - YouTube Downloader v.1.3

Aplikacja desktopowa do pobierania wideo z YouTube na platformę Windows.

## Funkcje

- **Pobieranie wideo** w formatach MP4, MKV, WebM
- **Pobieranie audio-only** w formatach MP3, M4A
- **Ustawienia jakości** z wyborem bitrate audio (96/128/192/256/320 kbps)
- **Tryb transkodowania** - kompresja wideo z FFmpeg
- **Wskaźnik postępu** z prędkością pobierania i ETA
- **Logi** - szczegółowe informacje o procesie pobierania zapisywane w folderze aplikacji
- **Anulowanie pobierania** - możliwość zatrzymania w każdym momencie
- **Historia pobierania** - lista poprzednich pobieranych filmów z statusem

## Wymagania

- Windows 10/11
- .NET 10.0 Runtime (lub nowszy)
- yt-dlp.exe (do pobierania)
- ffmpeg.exe (do łączenia strumieni/transkodowania)

## Instalacja

1. Pobierz najnowszą wersję aplikacji
2. Pobierz [yt-dlp](https://github.com/yt-dlp/yt-dlp/releases) i zapisz jako `yt-dlp.exe`
3. Pobierz [FFmpeg](https://www.gyan.dev/ffmpeg/builds/) i zapisz jako `ffmpeg.exe`
4. Umieść wszystkie pliki w tym samym folderze

Struktura folderów:

```
YoutubeDownloader/
├── YoutubeDownloader.exe
├── yt-dlp.exe
├── ffmpeg.exe
└── tools/
    ├── yt-dlp.exe
    └── ffmpeg.exe
```

## Użycie

1. Uruchom aplikację `YoutubeDownloader.exe`
2. Wklej URL filmu YouTube
3. Wybierz format docelowy (MP4, MKV, WebM, MP3, M4A)
4. Opcjonalnie włącz transkodowanie i ustaw bitrate
5. Wybierz folder zapisu
6. Kliknij "Pobierz"

## Transkodowanie i kompresja

Aby zmniejszyć rozmiar pliku:

1. Wybierz tryb **"B) Transkoduj (FFmpeg) i ustaw bitrate"**
2. Wpisz niższy bitrate wideo:
   - 3000-5000 kbps - wysoka jakość
   - 1500-2000 kbps - średnia jakość (zalecane)
   - 500-1000 kbps - niska jakość
3. Ustaw bitrate audio (domyślnie 192 kbps)

## Rozwiązywanie problemów

### Film bez dźwięku lub obrazu

Upewnij się, że `ffmpeg.exe` znajduje się w folderze `tools/`. FFmpeg jest wymagany do łączenia strumieni wideo i audio.

### Nie można wybrać formatu

Sprawdź logi w dolnej części okna. Upewnij się, że wszystkie narzędzia są dostępne.

### Błąd "Nie znaleziono yt-dlp.exe"

Skopiuj `yt-dlp.exe` do folderu `tools/` obok pliku EXE aplikacji.

## Licencja

Ten projekt jest dystrybuowany w celach edukacyjnych. Użytkownik jest odpowiedzialny za przestrzeganie praw autorskich i regulaminów serwisów, z których pobiera treści.

yt-dlp jest wydawany na licencji LGPLv3
FFmpeg jest wydawany na licencji GPLv3

## Autor

Horcon - YouTube Downloader
