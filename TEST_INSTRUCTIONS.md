# Instrukcja Testów - YouTube Downloader

## Przegląd

Ten dokument zawiera plan testów aplikacji YouTube Downloader, obejmujący przypadki testowe, błędy i sytuacje brzegowe.

---

## Przypadki Testowe

### TC-01: Pomyślne pobieranie wideo (MP4 bez transkodowania)

**Cel:** Weryfikacja poprawnego pobierania wideo w formacie MP4 bez transkodowania.

**Warunki wstępne:**

- Aplikacja jest uruchomiona
- Narzędzia yt-dlp.exe i ffmpeg.exe znajdują się w folderze tools/
- Folder wyjściowy jest wybrany

**Kroki:**

1. Wklej URL filmu YouTube (np. `https://www.youtube.com/watch?v=dQw4w9WgXcQ`)
2. Wybierz format "MP4"
3. Wybierz tryb "Bez transkodowania"
4. Kliknij "Pobierz"

**Oczekiwany wynik:**

- pasek postępu pokazuje postęp pobierania
- wyświetlane są informacje o prędkości i ETA
- po zakończeniu: stan "Pobieranie zakończone!"
- plik .mp4 pojawia się w wybranym folderze

---

### TC-02: Pomyślne pobieranie audio (MP3)

**Cel:** Weryfikacja poprawnego pobierania audio i konwersji do MP3.

**Warunki wstępne:** j.w.

**Kroki:**

1. Wklej URL filmu YouTube
2. Wybierz format "MP3 (audio)"
3. Wybierz bitrate (np. 192 kbps)
4. Kliknij "Pobierz"

**Oczekiwany wynik:**

- pasek postępu pokazuje postęp
- plik .mp3 pojawia się w folderze wyjściowym
- tag ID3 może być obecny (zależne od wersji yt-dlp)

---

### TC-03: Pobieranie z transkodowaniem

**Cel:** Weryfikacja pobierania z użyciem FFmpeg do transkodowania.

**Warunki wstępne:** j.w.

**Kroki:**

1. Wklej URL filmu YouTube
2. Wybierz format "MP4"
3. Wybierz tryb "Transkoduj (FFmpeg) i ustaw bitrate"
4. Wpisz bitrate wideo (np. 2000)
5. Kliknij "Pobierz"

**Oczekiwany wynik:**

- postęp jest aktualizowany (etap pobierania, potem transkodowanie)
- ostrzeżenie o dłuższym czasie/CPU może być wyświetlone
- plik wyjściowy ma ustawiony bitrate

---

### TC-04: Anulowanie pobierania

**Cel:** Weryfikacja poprawnego anulowania pobierania.

**Warunki wstępne:** j.w.

**Kroki:**

1. Wklej URL długiego filmu
2. Kliknij "Pobierz"
3. Poczekaj aż pasek postępu przekroczy 20%
4. Kliknij "Anuluj"

**Oczekiwany wynik:**

- pobieranie zostaje zatrzymane
- wyświetlany jest komunikat "Pobieranie anulowane."
- pliki tymczasowe są usuwane
- przycisk "Pobierz" jest ponownie aktywny

---

### TC-05: Błąd - pusty URL

**Cel:** Weryfikacja walidacji pustego URL.

**Warunki wstępne:** Aplikacja jest uruchomiona

**Kroki:**

1. Pozostaw pole URL puste
2. Wybierz folder wyjściowy
3. Kliknij "Pobierz"

**Oczekiwany wynik:**

- przycisk "Pobierz" jest nieaktywny (wymagany URL)

---

### TC-06: Błąd - niepoprawny URL

**Cel:** Weryfikacja walidacji niepoprawnego URL.

**Warunki wstępne:** j.w.

**Kroki:**

1. Wpisz niepoprawny URL (np. "not-a-url")
2. Wybierz folder wyjściowy
3. Kliknij "Pobierz"

**Oczekiwany wynik:**

- komunikat błędu w logu: "Niepoprawny format URL"
- stan błędu w informacji o postępie

---

### TC-07: Błąd - brak narzędzi

**Cel:** Weryfikacja zachowania przy braku yt-dlp.exe.

**Warunki wstępne:** Usuń tymczasowo plik yt-dlp.exe z folderu tools/

**Kroki:**

1. Uruchom aplikację
2. Wklej URL filmu
3. Kliknij "Pobierz"

**Oczekiwany wynik:**

- komunikat w logu: "OSTRZEŻENIE: Nie znaleziono yt-dlp.exe"
- komunikat błędu: "Nie znaleziono yt-dlp.exe"
- pobieranie nie rozpoczyna się

---

### TC-08: Błąd - brak praw do zapisu

**Cel:** Weryfikacja zachowania przy braku praw do folderu wyjściowego.

**Warunki wstępne:** Wybierz folder bez praw zapisu (np. systemowy folder Windows)

**Kroki:**

1. Wklej URL filmu
2. Wybierz folder tylko do odczytu
3. Kliknij "Pobierz"

**Oczekiwany wynik:**

- komunikat błędu o braku praw do zapisu
- plik nie jest tworzony

---

### TC-09: Błąd - niedostępny film

**Cel:** Weryfikacja zachowania dla usuniętego/prywatnego filmu.

**Warunki wstępne:** j.w.

**Kroki:**

1. Wklej URL nieistniejącego/usuniętego filmu
2. Kliknij "Pobierz"

**Oczekiwany wynik:**

- komunikat błędu z yt-dlp: "Film jest niedostępny" lub podobny
- stan błędu jest wyświetlony

---

### TC-10: Błąd - problem sieciowy

**Cel:** Weryfikacja zachowania przy utracie połączenia sieciowego.

**Warunki wstępne:** j.w., aktywne połączenie sieciowe

**Kroki:**

1. Wklej URL filmu
2. Kliknij "Pobierz"
3. Podczas pobierania odłącz sieć

**Oczekiwany wynik:**

- błąd sieciowy jest wykryty
- komunikat błędu jest wyświetlony
- proces jest zakończony

---

### TC-11: Wybór folderu wyjściowego

**Cel:** Weryfikacja działania dialogu wyboru folderu.

**Warunki wstępne:** Aplikacja jest uruchomiona

**Kroki:**

1. Kliknij "Wybierz..."
2. Wybierz folder z listy
3. Kliknij "OK"

**Oczekiwany wynik:**

- ścieżka folderu jest wyświetlona w polu tekstowym
- przycisk "Pobierz" staje się aktywny (jeśli URL jest podany)

---

### TC-12: Wiele pobrań - tylko jedno na raz

**Cel:** Weryfikacja, że tylko jedno pobieranie jest możliwe jednocześnie.

**Warunki wstępne:** j.w.

**Kroki:**

1. Wklej URL pierwszego filmu
2. Kliknij "Pobierz"
3. Wklej URL drugiego filmu
4. Kliknij "Pobierz"

**Oczekiwany wynik:**

- drugie kliknięcie "Pobierz" jest zablokowane
- przycisk jest nieaktywny podczas pobierania
- drugie pobieranie rozpoczyna się dopiero po zakończeniu pierwszego

---

## Testy Sytuacji Brzegowych (Edge Cases)

### EC-01: Bardzo długi film (ponad 1 godzina)

**Weryfikacja:** Obsługa długich filmów i poprawny parsing ETA.

### EC-02: Film w niskiej jakości

**Weryfikacja:** Pobieranie filmów bez dostępnej wysokiej jakości.

### EC-03: YouTube Shorts

**Weryfikacja:** Pobieranie Shorts (format `youtube.com/shorts/...`).

### EC-04: Film z napisami

**Weryfikacja:** Obsługa filmów z dostępnymi napisami (bez pobierania napisów).

### EC-05: Polskie znaki w nazwie

**Weryfikacja:** Poprawne zapisanie pliku z polskimi znakami.

### EC-06: Bardzo mało miejsca na dysku

**Weryfikacja:** Obsługa błędu braku miejsca na dysku.

---

## Narzędzia Testowe

Do testów potrzebne będą:

1. **yt-dlp** - najnowsza wersja z <https://github.com/yt-dlp/yt-dlp>
2. **FFmpeg** - najnowsza wersja z <https://ffmpeg.org/download.html>
3. **Przykładowe URL do testów:**
   - Krótki film: `https://www.youtube.com/watch?v=dQw4w9WgXcQ`
   - Długi film: dowolny film dokumentalny
   - Shorts: `https://www.youtube.com/shorts/dQw4w9WgXcQ`

---

## Raportowanie Błędów

Każdy znaleziony błąd powinien zawierać:

1. Numer przypadku testowego (TC-xx lub EC-xx)
2. Opis błędu
3. Oczekiwany wynik
4. Rzeczywisty wynik
5. Zrzut ekranu (jeśli możliwe)
6. Log z aplikacji (z %LOCALAPPDATA%\YoutubeDownloader\logs)
7. Wersje systemu i aplikacji
