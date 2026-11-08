# ScreenTools

**Documentation:** [English](README.md) · [Français](README.fr.md) · [简体中文](README.zh-CN.md)

ScreenTools is an experimental Windows desktop utility that brings screenshots, clipboard history, screen recording, and instant replay into one lightweight application.

> **Project status:** The application is functional but is not yet production-ready. The documented 15/60-minute physical recording matrix, Windows 10 hardware verification, multi-monitor behavior, and release automation still need real-world validation.

## Features

- Screenshots with global shortcuts, format and output-folder settings, and recent-output history.
- Clipboard history management.
- Screen recording with start, pause, resume, quality profiles, microphone capture, and MP4 export through FFmpeg.
- Instant replay with a rolling frame buffer capped at 160 MiB, time-based trimming, and clip export.
- Lower-allocation capture encoding: captured bitmaps are reused when possible, and resized frames use a 24-bit pixel format before JPEG encoding.
- Settings, output history, and clipboard history are written through temporary files and replaced atomically. Corrupted JSON settings and history files are preserved separately for recovery.
- System-audio loopback capture through the Windows default playback endpoint using NAudio/WASAPI; Stereo Mix is not required. Microphone and system audio are timestamped and mixed during MP4 export.
- Recording diagnostics include captured/skipped/failed frame counts, JPEG bytes written, and peak process working-set/private memory in the recording manifest.
- Per-Monitor V2 DPI awareness with per-monitor region-selection overlays and monitor-aware floating-window placement.
- MSIX packaging project for Store and sideload distribution.
- 97 xUnit tests covering persistence, replay limits, localization, recording lifecycle/pause-resume, MP4 duration and audio continuity, FFmpeg mixing, audio failure isolation, real Windows microphone/WASAPI capture when endpoints are available, and physical capture probes on every connected display.
- A standalone benchmark harness for real desktop capture and synthetic replay-buffer workloads.

## Known gaps

- Execute the documented 15/60-minute recording matrix on physical Windows 11 and Windows 10 hardware.
- Measure replay duration, frame drops, memory use, and concurrent recording/replay workloads under sustained load.
- Complete the physical multi-monitor/high-DPI acceptance matrix (125%, 150%, mixed DPI, vertical layouts, and Windows 10).
- Expand integration tests and automate packaging and releases.

See [AUDIO_TESTING.md](AUDIO_TESTING.md) for the WASAPI capture design and hardware checks; [RECORDING_VALIDATION.md](RECORDING_VALIDATION.md) for the recording stability matrix; [MULTI_MONITOR_VALIDATION.md](MULTI_MONITOR_VALIDATION.md) for issue #4's display matrix and diagnostic script; [REMAINING_WORK.md](REMAINING_WORK.md) for the current backlog; and [DESIGN.md](DESIGN.md) for the UI standards.

## Technology

- C# and WPF on .NET 8 for Windows.
- FFmpeg for MP4 encoding.
- NAudio for WASAPI system-audio capture.
- xUnit for automated tests.

## Build, test, and run

Requirements:

- Windows 10 or Windows 11.
- .NET 8 SDK.
- FFmpeg for MP4 export. The application checks its application directory, common installation locations (including Chocolatey), and trusted entries on `PATH`.

Run the solution commands from the `ScreenTools` solution directory so that project-relative paths resolve correctly:

```powershell
Push-Location .\ScreenTools
try {
    dotnet restore .\ScreenTools.sln
    dotnet build .\ScreenTools.sln -c Release
    dotnet test .\ScreenTools.sln -c Release
    dotnet run --project .\ScreenTools\ScreenTools.csproj -c Release
} finally {
    Pop-Location
}
```

### Performance benchmarks

Run the benchmark harness from the solution directory:

```powershell
Push-Location .\ScreenTools
try {
    dotnet run --project ..\ScreenTools.Benchmarks\ScreenTools.Benchmarks.csproj -c Release -- capture --iterations 60 --csv ..\BenchmarkResults\capture.csv
    dotnet run --project ..\ScreenTools.Benchmarks\ScreenTools.Benchmarks.csproj -c Release -- buffer --iterations 340 --csv ..\BenchmarkResults\buffer.csv
} finally {
    Pop-Location
}
```

`capture` measures real desktop capture, resizing, and JPEG encoding. `buffer` measures replay-buffer insertion and verifies the configured memory ceiling with synthetic 512 KiB frame payloads. `all` runs both scenarios. CSV output is optional; benchmark results are intentionally ignored by Git.

Without FFmpeg, captured frames can still be retained as an image sequence, but MP4 export is unavailable.

## Contributing

Reliability improvements are more valuable than adding features at this stage. Please read [CONTRIBUTING.md](CONTRIBUTING.md), review the open issues, and discuss substantial architectural changes before submitting a large patch.

## License

MIT. See [LICENSE](LICENSE).