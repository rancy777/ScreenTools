# Windows audio capture verification

## Supported Windows versions

ScreenTools targets Windows 10 and Windows 11 and uses Windows Core Audio through NAudio for system playback capture. WASAPI loopback captures the selected render endpoint directly; it does not require Stereo Mix, Stereo Mix to be enabled, or an FFmpeg DirectShow input device name.

The microphone remains a separate Windows WaveIn capture source and uses the default Windows recording device. System audio and microphone are recorded to independent WAV files before being mixed into the MP4.

**Runtime verification performed here:** Windows 11 Home, version 10.0.26300, 64-bit. Windows 10 is within the application target, but this machine-level test was performed on Windows 11 only. A Windows 10 hardware pass remains part of release validation.

## Capture and synchronization design

- System audio uses the Windows default Multimedia render endpoint, identified by its Core Audio endpoint ID. The service does not choose an arbitrary device by alphabetic friendly-name order.
- Active endpoints are refreshed before capture and when resuming from a pause. If an endpoint disappears between enumeration and startup, the service retries the current default endpoint.
- WASAPI writes the endpoint's native sample format to a WAV file. Each uninterrupted recording interval is a separate segment.
- Microphone capture uses the default WaveIn input and writes mono 44.1 kHz, 16-bit PCM to its own WAV file. It pauses and resumes with the recording stopwatch.
- The first captured video frame establishes the exported timeline's zero point. Audio segment start offsets are converted to that timeline. Positive offsets are padded with silence; negative offsets are trimmed.
- FFmpeg resamples audio asynchronously, mixes all available microphone and system segments with `amix` (`duration=longest`, `normalize=1`), and bounds the result to the encoded video duration. The mixer therefore does not use `-shortest` to truncate the screen recording when an audio source ends early.
- The recording stopwatch stops while the user pauses. WASAPI closes the current WAV segment on pause and starts another segment with the resumed timeline offset. Segment names and offsets are written to `recording.json`.
- If an audio source cannot start or fails during capture, its state changes to unavailable in the recording HUD. The localized failure detail is retained in the recording artifact details. Screen capture, video finalization, and frame-bundle preservation continue independently.

A device disconnect during an active session is reported; capture does not silently switch to a different output mid-file. The video continues, and the user can start a new recording or pause/resume after choosing a working default endpoint.

## Automated verification

Run from the solution directory:

```powershell
dotnet test .\ScreenTools.sln -c Release
```

The xUnit suite includes:

- Microphone-only, system-only, and combined hardware capture. The system-only and combined checks play a low-level generated tone and verify non-zero loopback samples. The microphone checks verify non-zero captured samples.
- Default-render-endpoint selection without a Stereo Mix input.
- WASAPI pause/resume segment creation and recording-timeline offsets.
- FFmpeg encoding for microphone-only, system-only, and combined audio. The tests cover both positive delay and trimming an audio segment that begins before the first video frame.
- Simulated microphone/system startup failures and runtime audio failure. These verify that video recording remains active and that the audio state changes to unavailable.

Hardware tests run when an audio endpoint is available. On machines with no active input or output device, the hardware smoke test returns without exercising the hardware; the FFmpeg and failure-isolation tests remain hardware-independent.

## Hardware used for the recorded verification

| Component | Detected device |
|---|---|
| Operating system | Windows 11 Home, 10.0.26300, 64-bit |
| Default playback endpoint | Speakers (Bose SLIII) |
| Other active playback endpoints | DELL S2421HGF / Intel Display Audio; Realtek High Definition Audio; Steam Streaming endpoints |
| Default recording endpoint | Microphone (Realtek High Definition Audio) |
| Other active recording endpoint | Microphone (Steam Streaming Microphone) |

The hardware test used the default Bose playback endpoint through WASAPI loopback. It did not use a Stereo Mix input. The default Realtek microphone was used by WaveIn.

## Manual verification checklist

1. On Windows 10 and Windows 11, set the intended speakers/headphones as the default **Multimedia** playback device and a working microphone as the default recording device.
2. Stereo Mix may remain disabled or absent. Enable only the microphone/system-audio toggles being tested.
3. Record three short clips: microphone only (speak or clap), system audio only (play a known sound), and both together.
4. Pause for several seconds, resume, and check that the exported MP4 contains no audio from the paused interval and that both sources resume in sync.
5. Inspect `recording.json` and the WAV sidecars. It records source names, segment file names, and offsets used by the MP4 mixer.
6. As a failure test, change or disconnect the playback endpoint during a recording. The system-audio status should become unavailable, a failure detail should be shown, and the video recording should continue to completion.
7. Repeat on Windows 10 hardware before declaring cross-version release validation complete.
