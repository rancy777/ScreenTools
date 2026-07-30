# Contributing to ScreenTools

Thanks for considering work on this unfinished project.

## Before starting

1. Read `README.md` and `REMAINING_WORK.md`.
2. Check existing issues and comment on the item you intend to work on.
3. For architectural changes, describe the approach and tradeoffs before writing a large patch.

## Development

```powershell
dotnet restore .\ScreenTools\ScreenTools.sln
dotnet build .\ScreenTools\ScreenTools.sln
```

Use a focused branch and keep generated files out of commits. Never commit `bin`, `obj`, local archives, recordings, user-specific project settings, or an `ffmpeg.exe` binary.

## Pull requests

Please include:

- The problem and the chosen approach
- What is intentionally left out
- Test steps and results
- Windows version and relevant hardware details for capture bugs
- Screenshots or short recordings for visible UI changes

For recording changes, test at least start/stop, pause/resume, repeated recordings, output playback, and cleanup. For replay changes, report the requested and actual exported duration.

## Scope

Reliability work is more valuable than adding features right now. The highest priorities are system audio, recording stability, replay validation, multi-monitor/DPI behavior, and automated tests.

