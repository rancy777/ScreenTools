# Contributing to ScreenTools

**Documentation:** [English](CONTRIBUTING.md) · [Français](CONTRIBUTING.fr.md) · [简体中文](CONTRIBUTING.zh-CN.md)

Thank you for considering a contribution. Before starting, read [README.md](README.md) and [REMAINING_WORK.md](REMAINING_WORK.md), review the open issues, and comment on the work you plan to take on. For architectural changes, describe the proposed approach and its trade-offs before preparing a large patch.

## Development setup

Run solution commands from the `ScreenTools` solution directory:

```powershell
Push-Location .\ScreenTools
try {
    dotnet restore .\ScreenTools.sln
    dotnet build .\ScreenTools.sln -c Release
    dotnet test .\ScreenTools.sln -c Release
} finally {
    Pop-Location
}
```

Run the benchmark harness when changing capture, encoding, or replay-buffer code:

```powershell
Push-Location .\ScreenTools
try {
    dotnet run --project ..\ScreenTools.Benchmarks\ScreenTools.Benchmarks.csproj -c Release -- all --iterations 60
} finally {
    Pop-Location
}
```

Keep changes focused and do not commit generated or machine-specific files. Never commit `bin`, `obj`, WPF temporary project files, local archives, recordings, benchmark output, user-specific settings, or an `ffmpeg.exe` binary.

## Pull requests

Please include:

- The problem being addressed and the chosen approach.
- Any intentionally out-of-scope work.
- Reproduction steps, test steps, and results.
- Windows version and relevant hardware details for capture-related issues.
- Screenshots or a short recording for visible UI changes.
- Benchmark methodology and before/after measurements for performance changes.

For recording changes, test start/stop, pause/resume, repeated sessions, output playback, and cleanup. For replay changes, report both the requested clip duration and the actual exported duration. For persistence changes, test valid files, malformed JSON, interrupted writes, and recovery without touching the real user profile.

## Scope and priorities

Reliability work is currently more valuable than adding features. The highest priorities are real-device acceptance, system-audio capture, recording stability, replay validation, multi-monitor/DPI behavior, and automated tests.