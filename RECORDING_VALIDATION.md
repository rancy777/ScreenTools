# Recording stability and pause/resume validation matrix

This document defines the repeatable acceptance matrix for GitHub issue #2. It separates automated regression coverage from the real-hardware runs that must be performed on each supported Windows version.

## Environment recorded for this validation

| Item | Recorded environment |
|---|---|
| OS used for the automated run | Windows 11 Home, build 26300, x64 |
| FFmpeg | 9.0.2 essentials build |
| FFprobe | 9.0.2 essentials build |
| Playback endpoint used by the audio hardware checks | Speakers (Bose SLIII) |
| Microphone used by the audio hardware checks | Microphone (Realtek High Definition Audio) |
| Screen capture | Windows desktop/display capture through the application's screen-capture service |

The automated FFmpeg integration tests use generated test video and sine-wave audio. They do not depend on the listed physical microphone or playback endpoint. The separate audio hardware tests exercise WASAPI and WaveIn on the physical endpoints.

**Cross-version status:** the automated suite was executed on Windows 11. Windows 10 remains a required physical acceptance run; Windows 10 support must not be described as hardware-verified until that run is completed.

## Automated verification

Run from the repository's solution directory:

```powershell
dotnet test .\ScreenTools.sln -c Release
```

The automated suite covers:

- MP4 creation, stream presence, full decode, and duration using FFmpeg/FFprobe when installed.
- Pause/resume timeline behavior across all four audio combinations: no audio, microphone only, system audio only, and both sources.
- Audio segment offsets across a deliberate pause; the pause wall-clock interval must not be added to the exported timeline.
- A generated three-second video with one audio segment before a simulated pause and another after resume. The test verifies video and audio streams, decodes the complete MP4, and measures PCM energy to ensure the pause interval is silent while both active intervals contain audio.
- Four consecutive recordings with unique artifact directories and no remaining per-recording temporary workspace.
- Abort behavior: both audio sources stop and unpublished temporary files are removed.
- A 15-second sustained capture stress run using disk-backed 64 KiB synthetic frame payloads, including frame count, capture requests/failures, total bytes written, and peak process memory.

FFmpeg-dependent tests return early if FFmpeg is unavailable. The pause-gap test always checks full decoding and the silent interval when FFmpeg is present; exact duration and stream metadata assertions are additionally checked when FFprobe is available beside FFmpeg. On the recorded machine, both are installed and all assertions execute. The artifact-validation script requires both tools.

## Recording diagnostics added

Every recording manifest now includes:

- `CapturedFrameCount`, `CaptureRequests`, `SkippedFrames`, and `CaptureFailures`.
- Total JPEG bytes written to the temporary frame sequence.
- Peak process working-set and private-memory measurements sampled during successful frame capture.

The recording artifact detail also reports the frame rate actually achieved, skipped capture ticks, failed captures, JPEG bytes, and peak memory. Frame images remain disk-backed; the application does not accumulate full JPEG image buffers in RAM.

`SkippedFrames` counts capture requests rejected because a prior frame was still being captured or the capture lock was busy. Dispatcher delays that prevent a timer tick from being delivered are not observable as skipped ticks; compare achieved FPS against elapsed time and the selected preset as well.

## Diagnostic scripts

### Validate saved recordings

After recording one or more clips, run:

```powershell
.\tools\Test-RecordingArtifacts.ps1 -OutputRoot "$env:USERPROFILE\Pictures\LensSnap" -ReportPath .\BenchmarkResults\recording-artifact-validation.csv
```

The script discovers recording manifests, checks MP4 streams and duration with FFprobe, fully decodes each MP4 with FFmpeg, verifies audio sidecars, and checks preserved JPEG bundles when MP4 encoding was unavailable. It exits non-zero if an artifact fails validation.

### Measure process memory during a long run

Start LensSnap and begin a recording, then in another PowerShell window run:

```powershell
.\tools\Measure-RecordingStability.ps1 -DurationMinutes 15 -SampleIntervalSeconds 2
```

For longer runs, use `-DurationMinutes 60`. The script writes timestamped process metrics to CSV: working set, private bytes, CPU time, thread count, handle count, and temporary recording workspace size. Use `-ProcessId` if more than one LensSnap process is present.

## Manual hardware acceptance matrix

Run each row on Windows 11 and repeat the same matrix on Windows 10. Use a local SSD with sufficient free space. Record the OS build, display resolution, selected quality preset, audio devices, FFmpeg/FFprobe versions, and test date with the results.

| ID | Scenario | Procedure | Pass criteria |
|---|---|---|---|
| R01 | MP4 playback and duration | Record 30 seconds with no audio, then open the MP4 in Windows Media Player or VLC and run the artifact validation script | MP4 plays from beginning to end; video stream decodes; duration is within 1 second or 10% of the manifest duration |
| R02 | Microphone only | Enable microphone, disable system audio, record 30 seconds while speaking, then inspect the MP4 and manifest | Video and microphone audio are present; no system-audio stream is required |
| R03 | System audio only | Disable microphone, enable system audio, play a known sound for 30 seconds | Video and system audio are present without Stereo Mix |
| R04 | Combined audio | Enable both sources, speak while a known system sound plays | Both sources are audible; MP4 decodes fully; no source is unexpectedly absent |
| R05 | Pause/resume continuity | Record, pause for 10 seconds, resume, then repeat twice; perform with microphone only, system only, and both | Paused wall-clock intervals are absent from media; audio resumes; manifest offsets increase only by active recording time |
| R06 | Repeated sessions | Record and stop 10 clips in succession, including one paused clip | Every output path is unique; all MP4s or frame bundles validate; no per-recording temporary directories remain |
| R07 | 15-minute stability | Record continuously for 15 minutes with the intended quality and audio combination while the memory monitor runs | No crash, deadlock, or unexplained capture stop; memory does not grow continuously without bound; final MP4 decodes |
| R08 | 60-minute stability | Repeat for 60 minutes on the primary target machine | No crash; finalization completes; memory/CPU/disk metrics and artifact diagnostics are retained |
| R09 | Active-window close | While recording, press Alt+F4 on the active-recording window | Recording continues and the HUD becomes visible; no invisible orphan recording |
| R10 | HUD close | While recording, press Alt+F4 on the HUD | Active recording controls become visible again; recording is not silently abandoned |
| R11 | Graceful application exit | Return to settings through the HUD, allow the recording to finalize, then close the main window | Recording is finalized before process exit; audio devices are released; no active workspace remains |
| R12 | Abrupt process termination | In a disposable test, terminate the process from Task Manager during recording, then relaunch | No corrupt MP4 is published; stale temporary data is eligible for cleanup after the configured 12-hour age threshold |

### Frame-rate interpretation

The current capture scheduler is deliberately capped at approximately 15 capture requests per second. The current profile configuration is 12 FPS for the 720P preset and 15 FPS for the 1080P and 1440P presets, despite the preset display names containing 30/60 FPS. The manifest and artifact diagnostics report the engine's configured target and measured achieved rate; do not treat the preset display text as proof of 30/60 FPS capture. Resolving that preset-label/throughput mismatch should be tracked separately from this stability matrix.

## Result sheet

For each manual run, record:

- Windows edition, version and build:
- Machine / display:
- Quality preset:
- Microphone and playback endpoints:
- Audio mode:
- Recording duration:
- Frame count / achieved FPS / skipped frames / capture failures:
- Peak working set / peak private bytes:
- MP4 duration / full-decode result:
- Audio pause-gap result:
- Temporary workspace cleanup result:
- Pass / fail and notes:

## Current execution status

- Automated tests: executed on Windows 11 with FFmpeg 9.0.2 and FFprobe 9.0.2.
- Synthetic pause-gap test: verifies silence during the simulated pause and audible PCM before and after it.
- Physical Windows 11 15/60-minute desktop stability matrix: documented; requires a deliberate long-running GUI recording run.
- Physical Windows 10 matrix: pending.
