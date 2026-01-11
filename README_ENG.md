# Horcon - YouTube Downloader v.1.4

Desktop application for downloading YouTube videos on Windows.

## Features

- **Video Downloading** in MP4, MKV, WebM formats
- **Audio-only Downloading** in MP3, M4A formats
- **Quality Settings** with selectable audio bitrate (96/128/192/256/320 kbps)
- **Transcoding Mode** - video compression using FFmpeg
- **Progress Indicator** with download speed and ETA
- **Multi-language Support (i18n)** - Polish, English, German (on-the-fly change)
- **Logs** - detailed download process info saved in the app folder
- **Download Cancellation** - ability to stop at any time
- **Download History** - list of previous downloads with status

## Requirements

- Windows 10/11
- .NET 10.0 Runtime (or newer)
- yt-dlp.exe (for downloading)
- ffmpeg.exe (for stream merging/transcoding)

## Installation

1. Download the latest version of the application
2. Download [yt-dlp](https://github.com/yt-dlp/yt-dlp/releases) and save as `yt-dlp.exe`
3. Download [FFmpeg](https://www.gyan.dev/ffmpeg/builds/) and save as `ffmpeg.exe`
4. Place all files in the same folder

Folder structure:

```
YoutubeDownloader/
├── YoutubeDownloader.exe
├── yt-dlp.exe
├── ffmpeg.exe
└── tools/
    ├── yt-dlp.exe
    └── ffmpeg.exe
```

## Usage

1. Run `YoutubeDownloader.exe`
2. Paste YouTube video URL
3. Select target format (MP4, MKV, WebM, MP3, M4A)
4. Optionally enable transcoding and set bitrate
5. Select output folder
6. Click "Download"

## Transcoding and Compression

To reduce file size:

1. Select mode **"B) Transcode (FFmpeg) and set bitrate"**
2. Enter lower video bitrate:
   - 3000-5000 kbps - high quality
   - 1500-2000 kbps - medium quality (recommended)
   - 500-1000 kbps - low quality
3. Set audio bitrate (default 192 kbps)

## Troubleshooting

### Video without sound or picture

Ensure `ffmpeg.exe` is in the `tools/` folder. FFmpeg is required to merge video and audio streams.

### Format cannot be selected

Check logs at the bottom of the window. Ensure all tools are available.

### Error "yt-dlp.exe not found"

Copy `yt-dlp.exe` to the `tools/` folder next to the application EXE.

## License

This project is distributed for educational purposes. The user is responsible for complying with copyright laws and the terms of service of the platforms from which content is downloaded.

yt-dlp is released under LGPLv3
FFmpeg is released under GPLv3

## Author

Horcon - YouTube Downloader
