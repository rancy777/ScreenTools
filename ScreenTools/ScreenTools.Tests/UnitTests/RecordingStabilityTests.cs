using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public sealed class RecordingStabilityTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task PauseResume_PreservesTimelineAndAllRequestedAudioModes(bool microphoneEnabled, bool systemAudioEnabled)
    {
        using var workspace = new TestWorkspace();
        await RunOnDispatcherAsync(async () =>
        {
            var microphone = new FakeAudioCaptureService("microphone");
            var systemAudio = new FakeAudioCaptureService("system-audio");
            var recording = CreateRecordingService(workspace, microphone, systemAudio);
            var session = CreateSession(workspace.OutputRoot, microphoneEnabled, systemAudioEnabled);

            recording.Start(session);
            await Task.Delay(140);
            recording.TogglePause();
            Assert.Equal(RecordingStatus.Paused, recording.Status);
            var elapsedAtPause = recording.Elapsed;

            await Task.Delay(450); // Deliberately long wall-clock pause; this must not enter the media timeline.

            recording.TogglePause();
            Assert.Equal(RecordingStatus.Recording, recording.Status);
            await Task.Delay(140);
            var artifact = await recording.StopAsync();

            Assert.NotNull(artifact);
            Assert.True(artifact.FrameCount > 0);
            Assert.Equal(RecordingStatus.Idle, recording.Status);
            Assert.True(recording.Elapsed == TimeSpan.Zero);
            Assert.Empty(Directory.EnumerateDirectories(workspace.RecordingRoot));

            using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(artifact.OutputPath, "recording.json")));
            var root = manifest.RootElement;
            var duration = root.GetProperty("DurationSeconds").GetDouble();
            Assert.True(duration < 0.5, $"Paused wall-clock time leaked into the recording timeline: {duration:F3}s.");
            Assert.True(duration >= elapsedAtPause.TotalSeconds, "The active portion before pause was lost from the recording timeline.");
            Assert.Equal(artifact.FrameCount, root.GetProperty("CapturedFrameCount").GetInt32());
            Assert.True(root.GetProperty("CaptureRequests").GetInt64() > 0);
            Assert.True(root.GetProperty("CapturedFrameBytes").GetInt64() > 0);
            Assert.True(root.GetProperty("PeakWorkingSetBytes").GetInt64() > 0);

            var segments = root.GetProperty("AudioSegments").EnumerateArray().ToArray();
            var expectedSources = (microphoneEnabled ? 1 : 0) + (systemAudioEnabled ? 1 : 0);
            Assert.Equal(expectedSources * 2, segments.Length);
            Assert.Equal(microphoneEnabled, segments.Any(segment => segment.GetProperty("Source").GetString() == "microphone"));
            Assert.Equal(systemAudioEnabled, segments.Any(segment => segment.GetProperty("Source").GetString() == "system"));

            foreach (var source in segments.GroupBy(segment => segment.GetProperty("Source").GetString()))
            {
                var offsets = source.Select(segment => segment.GetProperty("StartOffsetSeconds").GetDouble()).OrderBy(value => value).ToArray();
                Assert.Equal(2, offsets.Length);
                Assert.True(offsets[1] > 0.05, "The resumed audio segment must retain its post-pause timeline offset.");
            }
        });
    }

    [Fact]
    public async Task RepeatedRecordings_FinalizeIndependentlyAndRemoveEveryTemporaryWorkspace()
    {
        using var workspace = new TestWorkspace();
        await RunOnDispatcherAsync(async () =>
        {
            var microphone = new FakeAudioCaptureService("microphone");
            var systemAudio = new FakeAudioCaptureService("system-audio");
            var recording = CreateRecordingService(workspace, microphone, systemAudio);
            var outputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < 4; index++)
            {
                recording.Start(CreateSession(workspace.OutputRoot, microphoneEnabled: false, systemAudioEnabled: false));
                await Task.Delay(80);
                var artifact = await recording.StopAsync();

                Assert.NotNull(artifact);
                Assert.Equal(RecordingStatus.Idle, recording.Status);
                Assert.True(artifact.FrameCount > 0);
                Assert.True(Directory.Exists(artifact.OutputPath));
                Assert.True(File.Exists(Path.Combine(artifact.OutputPath, "recording.json")));
                Assert.True(outputs.Add(artifact.OutputPath), "Each recording must have a unique output directory.");
                Assert.Empty(Directory.EnumerateDirectories(workspace.RecordingRoot));
            }

            Assert.Equal(4, outputs.Count);
        });
    }

    [Fact]
    public async Task SustainedCapture_RecordsFrameAndProcessMemoryDiagnosticsWithoutGrowingAnImageBuffer()
    {
        using var workspace = new TestWorkspace();
        await RunOnDispatcherAsync(async () =>
        {
            var recording = CreateRecordingService(
                workspace,
                new FakeAudioCaptureService("microphone"),
                new FakeAudioCaptureService("system-audio"));

            recording.Start(CreateSession(workspace.OutputRoot, microphoneEnabled: false, systemAudioEnabled: false));
            await Task.Delay(TimeSpan.FromSeconds(15));
            var artifact = await recording.StopAsync();

            Assert.NotNull(artifact);
            Assert.True(artifact.FrameCount >= 100, $"Only {artifact.FrameCount} frames were captured during the 15-second stress run.");
            using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(artifact.OutputPath, "recording.json")));
            var root = manifest.RootElement;
            var frameCount = root.GetProperty("CapturedFrameCount").GetInt32();
            var requests = root.GetProperty("CaptureRequests").GetInt64();
            var failures = root.GetProperty("CaptureFailures").GetInt64();
            var privatePeak = root.GetProperty("PeakPrivateBytes").GetInt64();

            Assert.Equal(artifact.FrameCount, frameCount);
            Assert.True(requests >= frameCount);
            Assert.Equal(0, failures);
            Assert.True(privatePeak > 0);
            Assert.True(privatePeak < 2L * 1024 * 1024 * 1024,
                $"The recording process peak private bytes exceeded the 2 GiB stability budget: {privatePeak}.");
            Assert.Equal(frameCount * 64L * 1024, root.GetProperty("CapturedFrameBytes").GetInt64());
            Assert.Empty(Directory.EnumerateDirectories(workspace.RecordingRoot));
        });
    }

    [Fact]
    public async Task AbortAsync_StopsAudioAndDeletesUnpublishedTemporaryRecording()
    {
        using var workspace = new TestWorkspace();
        await RunOnDispatcherAsync(async () =>
        {
            var microphone = new FakeAudioCaptureService("microphone");
            var systemAudio = new FakeAudioCaptureService("system-audio");
            var recording = CreateRecordingService(workspace, microphone, systemAudio);

            recording.Start(CreateSession(workspace.OutputRoot, microphoneEnabled: true, systemAudioEnabled: true));
            Assert.True(microphone.IsCapturing);
            Assert.True(systemAudio.IsCapturing);
            Assert.Single(Directory.EnumerateDirectories(workspace.RecordingRoot));

            await recording.AbortAsync();

            Assert.Equal(RecordingStatus.Idle, recording.Status);
            Assert.False(microphone.IsCapturing);
            Assert.False(systemAudio.IsCapturing);
            Assert.Empty(Directory.EnumerateDirectories(workspace.RecordingRoot));
            Assert.False(Directory.Exists(Path.Combine(workspace.OutputRoot, "Recordings")));
        });
    }

    private static Task RunOnDispatcherAsync(Func<Task> action)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    await action();
                    completion.TrySetResult(true);
                }
                catch (Exception exception)
                {
                    completion.TrySetException(exception);
                }
                finally
                {
                    dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                }
            }));
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "ScreenTools recording test dispatcher"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return JoinDispatcherThreadAsync(completion.Task, thread);
    }

    private static async Task JoinDispatcherThreadAsync(Task task, Thread thread)
    {
        try
        {
            await task;
        }
        finally
        {
            if (!thread.Join(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("The recording test dispatcher did not shut down.");
        }
    }

    private static RecordingService CreateRecordingService(
        TestWorkspace workspace,
        FakeAudioCaptureService microphone,
        FakeAudioCaptureService systemAudio) =>
        new(new FakeScreenCaptureService(), new FrameSequenceEncoder(null), microphone, systemAudio,
            new TempWorkspaceService(workspace.RecordingRoot));

    private static RecordingSessionState CreateSession(string outputRoot, bool microphoneEnabled, bool systemAudioEnabled) => new()
    {
        IncludeMicrophone = microphoneEnabled,
        IncludeSystemAudio = systemAudioEnabled,
        OutputDirectory = outputRoot,
        QualityPreset = "720P 30FPS"
    };

    private sealed class TestWorkspace : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), $"ScreenTools-RecordingStability-{Guid.NewGuid():N}");
        public string OutputRoot => Path.Combine(Root, "output");
        public string RecordingRoot => Path.Combine(Root, "temp-recording");

        public TestWorkspace()
        {
            Directory.CreateDirectory(OutputRoot);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }

    private sealed class FakeScreenCaptureService : IScreenCaptureService
    {
        private static readonly byte[] FramePayload = new byte[64 * 1024];

        public CaptureArtifact CaptureScreenshot(RecordingSessionState session) => throw new NotSupportedException();
        public CaptureArtifact CaptureScreenshotRegion(RecordingSessionState session, Rect region) => throw new NotSupportedException();

        public void CaptureFrameJpeg(string outputPath, string qualityPreset) =>
            File.WriteAllBytes(outputPath, FramePayload);

        public byte[] CaptureFrameJpegBytes(string qualityPreset) => FramePayload;
    }

    private sealed class FakeAudioCaptureService(string sourceName) : IAudioCaptureService
    {
        private readonly List<AudioCaptureSegment> _segments = [];
        private string? _basePath;
        private bool _isCapturing;

        public bool IsSupported => true;
        public bool IsCapturing => _isCapturing;
        public string AvailabilityMessage => $"Fake {sourceName} endpoint.";
        public IReadOnlyList<AudioCaptureSegment> OutputSegments => _segments.ToArray();
        public event EventHandler<AudioCaptureFailedEventArgs>? CaptureFailed;

        public void RefreshAvailability() { }

        public void Start(string outputPath, TimeSpan timelineOffset = default)
        {
            _basePath = outputPath;
            StartSegment(outputPath, timelineOffset);
        }

        public void Pause() => _isCapturing = false;

        public void Resume(TimeSpan timelineOffset = default)
        {
            if (_isCapturing || _basePath is null)
                return;
            var directory = Path.GetDirectoryName(_basePath)!;
            var name = Path.GetFileNameWithoutExtension(_basePath);
            StartSegment(Path.Combine(directory, $"{name}-segment-{_segments.Count}.wav"), timelineOffset);
        }

        public void Stop() => _isCapturing = false;

        public void RaiseFailure(Exception exception) =>
            CaptureFailed?.Invoke(this, new AudioCaptureFailedEventArgs(sourceName, exception));

        private void StartSegment(string path, TimeSpan offset)
        {
            // A small WAV-shaped sidecar is sufficient for exercising the service's
            // segment discovery, manifest, move and cleanup lifecycle without hardware.
            File.WriteAllBytes(path, new byte[128]);
            _segments.Add(new AudioCaptureSegment(path, offset));
            _isCapturing = true;
        }
    }
}