using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class FrameSequenceEncoderTests
{
    [Fact]
    public async Task RunProcessAsync_CompletesWithoutBlockingAndReturnsOutput()
    {
        var result = await FrameSequenceEncoder.RunProcessAsync(
            CreateCommandProcess("echo process-complete"),
            TimeSpan.FromSeconds(5));

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("process-complete", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunProcessAsync_KillsProcessTreeWhenTimeoutExpires()
    {
        await Assert.ThrowsAsync<TimeoutException>(() => FrameSequenceEncoder.RunProcessAsync(
            CreateCommandProcess("ping -n 10 127.0.0.1 > nul"),
            TimeSpan.FromMilliseconds(200)));
    }

    [Fact]
    public async Task RunProcessAsync_KillsProcessTreeWhenCancellationIsRequested()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FrameSequenceEncoder.RunProcessAsync(
            CreateCommandProcess("ping -n 10 127.0.0.1 > nul"),
            TimeSpan.FromSeconds(10),
            cancellation.Token));
    }

    [Fact]
    public async Task TryEncodeMp4Async_ExportsARealFrameSequence_WhenFfmpegIsAvailable()
    {
        var encoder = new FrameSequenceEncoder();
        if (!encoder.CanEncodeVideo || string.IsNullOrWhiteSpace(encoder.FfmpegPath))
            return;

        var workspace = Path.Combine(Path.GetTempPath(), $"ScreenTools-EncodeIntegration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);
        try
        {
            var generator = new ProcessStartInfo
            {
                FileName = encoder.FfmpegPath,
                WorkingDirectory = workspace,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            foreach (var argument in new[]
            {
                "-y", "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i",
                "color=c=blue:s=320x240:r=5", "-frames:v", "5", "-q:v", "3", "frame-%04d.jpg"
            })
                generator.ArgumentList.Add(argument);

            var generation = await FrameSequenceEncoder.RunProcessAsync(generator, TimeSpan.FromSeconds(20));
            Assert.Equal(0, generation.ExitCode);

            var outputPath = await encoder.TryEncodeMp4Async(workspace, "encoded", 5, timeout: TimeSpan.FromSeconds(20));

            Assert.NotNull(outputPath);
            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task TryEncodeMp4WithAudioSegments_PreservesVideoDurationAndPauseGapContinuity()
    {
        var encoder = new FrameSequenceEncoder();
        if (!encoder.CanEncodeVideo || string.IsNullOrWhiteSpace(encoder.FfmpegPath))
            return;

        var workspace = Path.Combine(Path.GetTempPath(), $"ScreenTools-RecordingTimeline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);
        try
        {
            var frameGenerator = CreateFfmpegProcess(encoder.FfmpegPath, workspace,
                "-y", "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i",
                "color=c=navy:s=320x240:r=10", "-frames:v", "30", "-q:v", "3", "frame-%04d.jpg");
            var frameResult = await FrameSequenceEncoder.RunProcessAsync(frameGenerator, TimeSpan.FromSeconds(20));
            Assert.Equal(0, frameResult.ExitCode);

            var firstAudio = Path.Combine(workspace, "audio-before-pause.wav");
            var secondAudio = Path.Combine(workspace, "audio-after-resume.wav");
            foreach (var (path, frequency) in new[] { (firstAudio, 440), (secondAudio, 880) })
            {
                var audioGenerator = CreateFfmpegProcess(encoder.FfmpegPath, workspace,
                    "-y", "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i",
                    $"sine=frequency={frequency}:sample_rate=44100:duration=1", "-c:a", "pcm_s16le", path);
                var audioResult = await FrameSequenceEncoder.RunProcessAsync(audioGenerator, TimeSpan.FromSeconds(20));
                Assert.Equal(0, audioResult.ExitCode);
            }

            var outputPath = await encoder.TryEncodeMp4WithAudioSegmentsAsync(
                workspace,
                "pause-resume",
                10,
                CancellationToken.None,
                [new AudioCaptureSegment(firstAudio, TimeSpan.Zero), new AudioCaptureSegment(secondAudio, TimeSpan.FromSeconds(2))],
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(30));

            Assert.NotNull(outputPath);
            Assert.True(File.Exists(outputPath));

            var ffprobePath = Path.Combine(Path.GetDirectoryName(encoder.FfmpegPath)!, "ffprobe.exe");
            if (File.Exists(ffprobePath))
            {
                var probe = CreateFfmpegProcess(ffprobePath, workspace,
                    "-v", "error", "-show_entries", "format=duration:stream=codec_type", "-of", "json", outputPath!);
                var probeResult = await FrameSequenceEncoder.RunProcessAsync(probe, TimeSpan.FromSeconds(20));
                Assert.Equal(0, probeResult.ExitCode);
                using var probeJson = JsonDocument.Parse(probeResult.StandardOutput);
                var duration = double.Parse(probeJson.RootElement.GetProperty("format").GetProperty("duration").GetString()!,
                    System.Globalization.CultureInfo.InvariantCulture);
                Assert.InRange(duration, 2.75, 3.25);
                Assert.Contains(probeJson.RootElement.GetProperty("streams").EnumerateArray(),
                    stream => stream.GetProperty("codec_type").GetString() == "video");
                Assert.Contains(probeJson.RootElement.GetProperty("streams").EnumerateArray(),
                    stream => stream.GetProperty("codec_type").GetString() == "audio");
            }

            var fullDecode = CreateFfmpegProcess(encoder.FfmpegPath, workspace,
                "-v", "error", "-i", outputPath!, "-f", "null", "-");
            var decodeResult = await FrameSequenceEncoder.RunProcessAsync(fullDecode, TimeSpan.FromSeconds(20));
            Assert.Equal(0, decodeResult.ExitCode);

            var firstRms = await ExtractAudioRmsAsync(encoder.FfmpegPath, workspace, outputPath!, "before-pause", 0.2);
            var pauseRms = await ExtractAudioRmsAsync(encoder.FfmpegPath, workspace, outputPath!, "during-pause", 1.2);
            var resumedRms = await ExtractAudioRmsAsync(encoder.FfmpegPath, workspace, outputPath!, "after-resume", 2.2);
            Assert.True(firstRms > 500, $"Expected audible samples before pause; RMS was {firstRms:F0}.");
            Assert.True(resumedRms > 500, $"Expected audible samples after resume; RMS was {resumedRms:F0}.");
            Assert.True(pauseRms < Math.Min(firstRms, resumedRms) * 0.15,
                $"The pause interval should be silent; RMS was {pauseRms:F0}.");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    private static async Task<double> ExtractAudioRmsAsync(string ffmpegPath, string workspace, string inputPath, string label, double startSeconds)
    {
        var pcmPath = Path.Combine(workspace, $"{label}.pcm");
        var extraction = CreateFfmpegProcess(ffmpegPath, workspace,
            "-y", "-v", "error", "-ss", startSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "-t", "0.3", "-i", inputPath, "-map", "0:a:0", "-ac", "1", "-ar", "44100", "-f", "s16le", pcmPath);
        var result = await FrameSequenceEncoder.RunProcessAsync(extraction, TimeSpan.FromSeconds(20));
        Assert.Equal(0, result.ExitCode);
        var samples = File.ReadAllBytes(pcmPath)
            .Chunk(2)
            .Where(pair => pair.Length == 2)
            .Select(pair => (short)(pair[0] | pair[1] << 8))
            .Select(sample => (double)sample)
            .ToArray();
        Assert.NotEmpty(samples);
        return Math.Sqrt(samples.Average(sample => sample * sample));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task TryEncodeMp4WithAudioSegmentsAsync_EncodesMicrophoneSystemAndCombinedModes(
        bool includeMicrophone,
        bool includeSystemAudio)
    {
        var encoder = new FrameSequenceEncoder();
        if (!encoder.CanEncodeVideo || string.IsNullOrWhiteSpace(encoder.FfmpegPath))
            return;

        var workspace = Path.Combine(Path.GetTempPath(), $"ScreenTools-AudioMix-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);
        try
        {
            var frameGenerator = CreateFfmpegProcess(encoder.FfmpegPath, workspace,
                "-y", "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i",
                "color=c=blue:s=320x240:r=5", "-frames:v", "5", "-q:v", "3", "frame-%04d.jpg");
            var frameResult = await FrameSequenceEncoder.RunProcessAsync(frameGenerator, TimeSpan.FromSeconds(20));
            Assert.Equal(0, frameResult.ExitCode);

            var segments = new System.Collections.Generic.List<AudioCaptureSegment>();
            if (includeMicrophone)
            {
                var microphonePath = Path.Combine(workspace, "microphone.wav");
                var microphoneGenerator = CreateFfmpegProcess(encoder.FfmpegPath, workspace,
                    "-y", "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i",
                    "sine=frequency=440:sample_rate=44100:duration=1", "-c:a", "pcm_s16le", microphonePath);
                var microphoneResult = await FrameSequenceEncoder.RunProcessAsync(microphoneGenerator, TimeSpan.FromSeconds(20));
                Assert.Equal(0, microphoneResult.ExitCode);
                segments.Add(new AudioCaptureSegment(microphonePath, TimeSpan.FromMilliseconds(-100)));
            }

            if (includeSystemAudio)
            {
                var systemAudioPath = Path.Combine(workspace, "system-audio.wav");
                var systemGenerator = CreateFfmpegProcess(encoder.FfmpegPath, workspace,
                    "-y", "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i",
                    "sine=frequency=660:sample_rate=44100:duration=1", "-c:a", "pcm_s16le", systemAudioPath);
                var systemResult = await FrameSequenceEncoder.RunProcessAsync(systemGenerator, TimeSpan.FromSeconds(20));
                Assert.Equal(0, systemResult.ExitCode);
                segments.Add(new AudioCaptureSegment(systemAudioPath, TimeSpan.FromMilliseconds(150)));
            }

            var outputPath = await encoder.TryEncodeMp4WithAudioSegmentsAsync(
                workspace,
                "audio-mode",
                5,
                CancellationToken.None,
                segments,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(30));

            Assert.NotNull(outputPath);
            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);

            if (segments.Count > 0)
            {
                var audioDecode = CreateFfmpegProcess(encoder.FfmpegPath, workspace,
                    "-v", "error", "-i", outputPath!, "-map", "0:a:0", "-f", "null", "-");
                var audioDecodeResult = await FrameSequenceEncoder.RunProcessAsync(audioDecode, TimeSpan.FromSeconds(20));
                Assert.Equal(0, audioDecodeResult.ExitCode);
            }
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    private static ProcessStartInfo CreateCommandProcess(string command)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add(command);
        return startInfo;
    }

    private static ProcessStartInfo CreateFfmpegProcess(string ffmpegPath, string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        return startInfo;
    }

    [Fact]
    public async Task TryEncodeMp4Async_WhenFfmpegIsUnavailable_ReturnsNull()
    {
        var workspace = Path.Combine(Path.GetTempPath(), $"ScreenTools-Encoder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);
        try
        {
            var encoder = new FrameSequenceEncoder(null);
            var result = await encoder.TryEncodeMp4Async(workspace, "test", 30);
            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task TryEncodeMp4Async_WhenCancellationIsRequested_ThrowsBeforeStartingProcess()
    {
        var workspace = Path.Combine(Path.GetTempPath(), $"ScreenTools-Encoder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        try
        {
            var encoder = new FrameSequenceEncoder("ffmpeg-test-placeholder.exe");
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                encoder.TryEncodeMp4Async(workspace, "test", 30, cancellation.Token));
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task TryEncodeMp4Async_RejectsInvalidFrameRate()
    {
        var workspace = Path.Combine(Path.GetTempPath(), $"ScreenTools-Encoder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);
        try
        {
            var encoder = new FrameSequenceEncoder("ffmpeg-test-placeholder.exe");
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                encoder.TryEncodeMp4Async(workspace, "test", 0));
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task TryEncodeMp4Async_RejectsNonPositiveTimeout()
    {
        var workspace = Path.Combine(Path.GetTempPath(), $"ScreenTools-Encoder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);
        try
        {
            var encoder = new FrameSequenceEncoder("ffmpeg-test-placeholder.exe");
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                encoder.TryEncodeMp4Async(workspace, "test", 30, timeout: TimeSpan.Zero));
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public void IsValidMp4_ValidMp4Header_ReturnsTrue()
    {
        // Create a temporary file with a valid MP4 ftyp header
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D, 0x00, 0x00, 0x00, 0x01, 0x69, 0x73, 0x6F, 0x6D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            var method = typeof(FrameSequenceEncoder).GetMethod("IsValidMp4",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);
            var result = (bool)method.Invoke(null, new object[] { tempFile })!;
            Assert.True(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void IsValidMp4_InvalidHeader_ReturnsFalse()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            var method = typeof(FrameSequenceEncoder).GetMethod("IsValidMp4",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);
            var result = (bool)method.Invoke(null, new object[] { tempFile })!;
            Assert.False(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void IsValidMp4_EmptyFile_ReturnsFalse()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, string.Empty);
            var method = typeof(FrameSequenceEncoder).GetMethod("IsValidMp4",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);
            var result = (bool)method.Invoke(null, new object[] { tempFile })!;
            Assert.False(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
