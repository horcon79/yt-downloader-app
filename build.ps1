#
# Skrypt budowania aplikacji YouTube Downloader
#
# Ten skrypt buduje aplikacje i kopiuje narzedzia do folderu output/
#

param(
    [switch]$Clean = $false,
    [switch]$NoBuild = $false,
    [string]$Configuration = "Release"
)

# Konfiguracja sciezek
$ProjectRoot = $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "YoutubeDownloader.csproj"
$OutputDir = Join-Path $ProjectRoot "output"
$ToolsSourceDir = Join-Path $ProjectRoot "tools"
$ToolsDestDir = Join-Path $OutputDir "tools"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "YouTube Downloader - Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Project Root: $ProjectRoot" -ForegroundColor Gray
Write-Host ""

# Czyszczenie
if ($Clean) {
    Write-Host "Czyszczenie..." -ForegroundColor Yellow
    if (Test-Path $OutputDir) {
        Remove-Item -Recurse -Force $OutputDir
        Write-Host "  Usunieto: $OutputDir"
    }
    Write-Host ""
}

# Budowanie
if (-not $NoBuild) {
    Write-Host "Budowanie aplikacji..." -ForegroundColor Yellow
    Write-Host "  Konfiguracja: $Configuration"
    Write-Host ""

    if (-not (Test-Path $ProjectFile)) {
        Write-Error "Nie znaleziono pliku projektu: $ProjectFile"
        exit 1
    }

    $buildResult = dotnet build $ProjectFile -c $Configuration 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Budowanie nie powiodlo sie!"
        Write-Host $buildResult
        exit 1
    }
    Write-Host "  Budowanie zakonczone pomyslnie!" -ForegroundColor Green
    Write-Host ""

    # Skopiuj pliki wyjsciowe
    Write-Host "Kopiowanie plikow wyjsciowych..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
    
    $sourceBinDir = Join-Path $ProjectRoot "bin/$Configuration/net10.0-windows"
    if (Test-Path $sourceBinDir) {
        Copy-Item -Path (Join-Path $sourceBinDir "*") -Destination $OutputDir -Recurse
        Write-Host "  Skopiowano do: $OutputDir" -ForegroundColor Gray
    }
    Write-Host ""
}

# Kopiowanie narzedzi
Write-Host "Kopiowanie narzedzi..." -ForegroundColor Yellow
if (Test-Path $ToolsSourceDir) {
    New-Item -ItemType Directory -Force -Path $ToolsDestDir | Out-Null
    $toolFiles = Get-ChildItem -Path $ToolsSourceDir -Filter "*.exe"
    foreach ($file in $toolFiles) {
        $destPath = Join-Path $ToolsDestDir $file.Name
        Copy-Item -Path $file.FullName -Destination $destPath -Force
        Write-Host "  Skopiowano: $($file.Name)" -ForegroundColor Gray
    }
    if ($toolFiles.Count -eq 0) {
        Write-Host "  OSTRZEZENIE: Brak plikow w folderze tools/" -ForegroundColor Yellow
    }
}
else {
    Write-Host "  OSTRZEZENIE: Folder tools/ nie istnieje" -ForegroundColor Yellow
    Write-Host "  Utworz go i umiesc yt-dlp.exe oraz ffmpeg.exe" -ForegroundColor Yellow
}
Write-Host ""

# Informacje koncowe
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Budowanie zakonczone!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Aplikacja: $OutputDir\YoutubeDownloader.exe" -ForegroundColor White
Write-Host ""
Write-Host "Aby utruchomic:" -ForegroundColor White
Write-Host "  $OutputDir\YoutubeDownloader.exe" -ForegroundColor Gray
Write-Host ""
Write-Host "UWAGA: Dodaj yt-dlp.exe i ffmpeg.exe do folderu tools/" -ForegroundColor Yellow
Write-Host ""
