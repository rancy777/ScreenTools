using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

/// <summary>
/// Exercises the actual Windows audio endpoints when tests run on a machine with audio hardware.
/// </summary>
public sealed class AudioCaptureHardwareTests
{
    [Fact]
    public void MicrophoneOnly_SystemOnly_AndCombinedModes_WriteValidWaveFiles()
    {
        var workspace = Path.Combine(Path.GetTempPath(), $"ScreenTools-AudioHardware-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);
        var microphone = new MicrophoneCaptureService();
        using var systemAudio = new WasapiLoopbackCaptureService();

        try
        {
            if (!microphone.IsSupported && !systemAudio.IsSupported)
                return;

            // Microphone-only mode.
            if (microphone.IsSupported)
            {
                var microphoneOnlyPath = Path.Combine(workspace, "microphone-only.wav");
                microphone.Start(microphoneOnlyPath);
                PlayQuietTestTone();
                Thread.Sleep(150);
                Thread.Sleep(600);
                microphone.Stop();
                AssertWaveFileHasAudioPayload(microphoneOnlyPath, requireNonZeroSamples: true);
            }

            // System-only mode. A low-level test tone makes sure loopback receives actual playback samples.
            if (systemAudio.IsSupported)
            {
                var systemOnlyPath = Path.Combine(workspace, "system-only.wav");
                systemAudio.Start(systemOnlyPath);
                PlayQuietTestTone();
                Thread.Sleep(150);
                systemAudio.Stop();
                AssertWaveFileHasAudioPayload(systemOnlyPath, requireNonZeroSamples: true);
            }

            // Combined mode uses the same independent capture services concurrently.
            if (microphone.IsSupported && systemAudio.IsSupported)
            {
                var microphonePath = Path.Combine(workspace, "combined-microphone.wav");
                var systemPath = Path.Combine(workspace, "combined-system.wav");
                microphone.Start(microphonePath);
                systemAudio.Start(systemPath);
                PlayQuietTestTone();
                Thread.Sleep(150);
                microphone.Stop();
                systemAudio.Stop();
                AssertWaveFileHasAudioPayload(microphonePath, requireNonZeroSamples: true);
                AssertWaveFileHasAudioPayload(systemPath, requireNonZeroSamples: true);
            }

        }
        finally
        {
            try { microphone.Dispose(); } catch { }
            try { systemAudio.Stop(); } catch { }
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public void WasapiLoopback_SelectsTheWindowsDefaultPlaybackEndpoint()
    {
        using var capture = new WasapiLoopbackCaptureService();
        if (!capture.IsSupported)
            return;

        Assert.False(string.IsNullOrWhiteSpace(capture.SelectedDeviceId));
        Assert.Equal(capture.SelectedDeviceName, capture.AvailableDevices.First());
        Assert.Contains("无需启用立体声混音", capture.AvailabilityMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WasapiLoopback_PauseAndResume_PreservesSegmentTimelineOffsets()
    {
        using var capture = new WasapiLoopbackCaptureService();
        if (!capture.IsSupported)
            return;

        var workspace = Path.Combine(Path.GetTempPath(), $"ScreenTools-AudioSegments-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);
        var outputPath = Path.Combine(workspace, "system.wav");
        try
        {
            capture.Start(outputPath, TimeSpan.FromMilliseconds(25));
            // Loopback capture needs actual playback. Without a stimulus, short segments may
            // legitimately contain no data chunk at all on an idle Windows endpoint.
            var playback = Task.Run(PlayQuietTestTone);
            Thread.Sleep(300);
            capture.Pause();
            capture.Resume(TimeSpan.FromMilliseconds(900));
            Thread.Sleep(300);
            await playback;
            capture.Stop();

            Assert.Equal(2, capture.OutputSegments.Count);
            Assert.Equal(TimeSpan.FromMilliseconds(25), capture.OutputSegments[0].StartOffset);
            Assert.Equal(TimeSpan.FromMilliseconds(900), capture.OutputSegments[1].StartOffset);
            Assert.All(capture.OutputSegments, segment => AssertWaveFileHasAudioPayload(segment.Path, requireNonZeroSamples: false));
        }
        finally
        {
            capture.Stop();
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    private static void PlayQuietTestTone()
    {
        using var output = new WaveOutEvent { Volume = 0.1f };
        var signal = new SignalGenerator
        {
            Gain = 0.2,
            Frequency = 440,
            Type = SignalGeneratorType.Sin
        };
        output.Init(signal);
        output.Play();
        Thread.Sleep(650);
        output.Stop();
    }

    private static void AssertWaveFileHasAudioPayload(string path, bool requireNonZeroSamples)
    {
        Assert.True(File.Exists(path), $"Expected WAV file was not created: {path}");
        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 44, $"WAV file contains no audio payload: {path}");

        var dataChunk = -1;
        for (var index = 12; index <= bytes.Length - 8; index++)
        {
            if (bytes[index] == (byte)'d' && bytes[index + 1] == (byte)'a' &&
                bytes[index + 2] == (byte)'t' && bytes[index + 3] == (byte)'a')
            {
                dataChunk = index + 8;
                break;
            }
        }

        Assert.True(dataChunk >= 0 && dataChunk < bytes.Length, $"WAV data chunk is missing: {path}");
        if (requireNonZeroSamples)
            Assert.Contains(bytes.Skip(dataChunk), sample => sample != 0);
    }
}
