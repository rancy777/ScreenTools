# Remaining Work

**Documentation:** [English](REMAINING_WORK.md) · [Français](REMAINING_WORK.fr.md) · [简体中文](REMAINING_WORK.zh-CN.md)

## Current status

ScreenTools has a working foundation for screenshots, clipboard history, screen recording, instant replay, WASAPI system-audio capture without Stereo Mix, and MSIX packaging. The P1/P2 engineering work and the requested P3 cleanup, regression tests, and benchmark harness are implemented. The project is not yet production-ready because long-duration and cross-version device behavior still requires acceptance testing.

## Recently completed

### P1 — asynchronous capture and finalization

- Replay frame acquisition and JPEG encoding run on a background capture loop instead of the WPF dispatcher.
- Replay export uses asynchronous file I/O and asynchronous FFmpeg execution.
- Recording stop and abort drain in-flight capture work asynchronously and perform audio shutdown and artifact finalization away from the UI thread.
- FFmpeg uses argument-safe process launching, asynchronous stdout/stderr draining, process-tree termination, cancellation-token support, and a three-minute default timeout.
- WPF-facing service events are marshalled to the owning dispatcher; recording controls are disabled while an artifact is being finalized.

### P2 — memory and persistence

- The screenshot encoder reuses the captured bitmap when resizing is unnecessary. Resized frames use a 24-bit pixel format to reduce intermediate bitmap memory.
- Replay frames are managed by a dedicated buffer with a 160 MiB hard cap and time-based trimming. Export snapshots copy frame references, not JPEG payload arrays.
- Settings, output history, and clipboard history use same-directory temporary files and atomic replacement instead of writing directly over the active JSON file.
- Settings values are normalized and bounded when loaded. Corrupted settings and history JSON files are quarantined rather than silently overwritten.
- Persistence services accept explicit file paths, allowing tests to use isolated temporary directories instead of the real user profile.

### P3 — generated files, regression tests, and benchmarks

- Removed the accidentally generated WPF temporary project file and added ignore rules for regenerated WPF temporary projects and benchmark output.
- Added regression coverage for replay-buffer byte limits, snapshot payload reuse, settings round-trips, invalid settings, corrupted-file recovery, and isolated output-history persistence.
- Added a standalone benchmark project for real desktop capture/JPEG encoding and synthetic replay-buffer insertion. It reports latency percentiles, managed allocations, retained payload size, and process working set; optional CSV output is supported.
- The current Release test suite contains 97 tests, including per-monitor physical capture probes, corner/center and adjacent-seam captures, PNG/JPG/BMP full-desktop dimension checks, and recording JPEG profile-dimension checks, in addition to recording/audio regression coverage.

### GitHub issue #1 — reliable Windows system-audio loopback (implementation complete; Windows 10 hardware validation pending)

- Replaced device-name-based system-audio capture with WASAPI loopback on the default Windows Multimedia playback endpoint. Stereo Mix is not required.
- Added endpoint refresh/fallback, safe capture shutdown, WAV segmentation across pause/resume, and visible runtime failure reporting. Audio failures mark the source unavailable without stopping screen capture or discarding the video.
- Defined a shared recording timeline: the first video frame is time zero; audio segments carry offsets; FFmpeg trims/pads segments and mixes microphone plus system audio without using `-shortest` to truncate video.
- Added hardware tests for microphone-only, system-only, combined, default endpoint selection, and loopback pause/resume. Added FFmpeg mix tests for all three audio modes and failure-isolation tests.
- Verified on Windows 11 Home build 26300 x64 using the default Bose SLIII playback endpoint and Realtek microphone. Windows 10 remains to be physically verified.
- Documented supported Windows targets, tested hardware, architecture, and manual verification steps in `AUDIO_TESTING.md` (French and Simplified Chinese versions are also provided).

### GitHub issue #2 — recording stability and pause/resume validation (automated coverage complete; long physical runs pending)

- Added lifecycle tests for all four audio modes, pause/resume offsets, pause-time exclusion, repeated recordings, unique output folders, and abort cleanup.
- Added an FFmpeg/FFprobe integration test that checks MP4 video/audio streams, full decoding, a three-second duration, and a silent pause interval between two audible audio segments.
- Added a 15-second sustained capture stress test using disk-backed 64 KiB synthetic frame payloads and per-recording diagnostics for capture requests, skipped frames, failures, JPEG bytes, peak working set, and peak private memory.
- Prevented closing the active-recording window or HUD from leaving a recording running invisibly: the other recording control surface is shown instead.
- Added `Test-RecordingArtifacts.ps1` to validate MP4 playback/decode, duration, audio sidecars, and preserved frame bundles; added `Measure-RecordingStability.ps1` to collect process memory/CPU/thread/handle and temporary-disk metrics during long runs.
- Documented the automated suite, manual R01–R12 matrix, test environment, scripts, and Windows 10/11 execution requirements in `RECORDING_VALIDATION.md` and its French/Chinese translations.

### Localization and documentation

- Runtime status and error messages now pass through the localization catalog, including dynamically assembled recording, replay, audio, clipboard, and shortcut messages.
- Screenshot sticker dialogs and image-format filters use the selected language.
- English, French, and Simplified Chinese documentation is maintained in separate files, with consistent terminology and no inline language-slash formatting.

### GitHub issue #4 — multi-monitor and high-DPI capture behavior (implementation and automated geometry coverage complete; physical DPI matrix pending)

- Declared Per-Monitor V2 DPI awareness in the application manifest and retained the .NET WPF PerMonitorV2 project setting.
- Rebuilt region selection as one native-positioned overlay per physical monitor. Cursor positions and returned selections use physical desktop pixels, while each overlay renders through its own monitor DPI transform.
- Corrected virtual-desktop bounds, including negative monitor origins; region bounds round outward, clip to the virtual desktop, and report actual output dimensions.
- Moved the recording HUD, screenshot toast, and pinned screenshot windows to the cursor monitor using its work area and effective window DPI.
- Added automated virtual-desktop, negative-origin, fractional-edge, clipping, and monitor-window-placement tests. Added a display diagnostics script and English/French/Simplified Chinese acceptance matrices.
- The development environment reports two 1920×1080 displays side-by-side at 100% scaling. This is configuration evidence only; physical 125%/150%, mixed-DPI, vertical-layout, and Windows 10 UI runs remain pending.
- The application has no selectable recording-region feature; recording and instant replay capture the full virtual desktop. This scope is explicitly documented rather than claimed as a tested region-recording feature.

## Remaining acceptance work

### 1. Recording on real hardware

- Execute the documented 15-minute and 60-minute GUI recording runs on physical Windows 11 and Windows 10 machines; retain the memory CSV and artifact-validation report.
- Repeat the matrix with the intended display resolutions, quality presets, microphone/system-audio combinations, and a second audio endpoint.
- Validate disk exhaustion and endpoint changes on physical hardware; automated lifecycle, pause/resume, close-window routing, repeated-session cleanup, and synthetic audio-gap tests are already covered.

### 2. Replay on real hardware

- Verify that each supported replay duration produces the expected coverage and playback duration.
- Compare image quality and smoothness across quality profiles.
- Measure frame drops and memory behavior under sustained capture load.
- Test replay export while a regular recording is active.
- Validate cancellation and timeout behavior, including preservation of captured frames when MP4 encoding fails.
- Run the benchmark harness on multiple display resolutions and compare results against a recorded baseline.

### 3. Audio and device coverage

- Validate Windows 10 hardware and additional playback/input devices, sample rates, and device changes during a session.
- Measure long-session A/V drift and verify pause/resume synchronization in exported MP4s on multiple hardware configurations.
- Verify audio-file finalization and cleanup after normal stop, abort, and application shutdown.
- Complete the physical issue #4 display matrix: 125%/150%, mixed DPI/resolutions, vertical layouts, monitor changes, and Windows 10; automated coordinate geometry and screenshot dimensions are covered.

### 4. Output recovery and runtime UX

- Document the final folder layout for screenshots, recordings, and replays.
- Decide whether microphone and system-audio WAV files should remain as sidecar files after successful MP4 export.
- Define recovery behavior for failures during output finalization and disk exhaustion.
- Add visible progress and cancellation for long-running exports where appropriate.

### 5. Architecture, CI, and release

- Reduce orchestration responsibilities in `WindowFlowCoordinator` as service boundaries become clearer.
- Add end-to-end integration tests for capture, audio, replay, and application shutdown.
- Add CI validation for Release build, tests, and benchmark compilation.
- Finalize MSIX package configuration and automate signed package creation and GitHub releases.

## Recommended next step

Perform the remaining cross-version acceptance pass on Windows 10 hardware and a long-duration recording: enable microphone and system audio, test pause/resume and endpoint changes, inspect the MP4 and WAV sidecars, and repeat the workflow to verify cleanup. Save benchmark output as a baseline so future performance changes can be compared objectively.