using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public sealed class RecordingAudioFailureTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public Task AudioStartupFailure_DoesNotAbortVideoRecording(bool includeMicrophone, bool includeSystemAudio) =>
        RunOnDispatcherAsync(async () =>
        {
            var outputDirectory = Path.Combine(Path.GetTempPath(), $"ScreenTools-RecordingAudioFailure-{Guid.NewGuid():N}");
            Directory.CreateDirectory(outputDirectory);
            var microphone = new FakeAudioCaptureService(failOnStart: includeMicrophone);
            var systemAudio = new FakeAudioCaptureService(failOnStart: includeSystemAudio);
            var recording = CreateRecordingService(microphone, systemAudio);

            try
            {
                recording.Start(CreateSession(outputDirectory, includeMicrophone, includeSystemAudio));
                Assert.Equal(RecordingStatus.Recording, recording.Status);
                Assert.Contains("不可用", recording.CurrentAudioSummary);
                await Task.Delay(100);

                var artifact = await recording.StopAsync();
                Assert.NotNull(artifact);
                Assert.True(artifact.FrameCount > 0, "The video frame sequence must survive an audio startup failure.");
                Assert.Equal(RecordingStatus.Idle, recording.Status);
                Assert.True(Directory.Exists(artifact.OutputPath));
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                    Directory.Delete(outputDirectory, recursive: true);
            }
        });

    [Fact]
    public Task RuntimeAudioFailure_UpdatesAudioStateButKeepsVideoRecordingActive() =>
        RunOnDispatcherAsync(async () =>
        {
            var outputDirectory = Path.Combine(Path.GetTempPath(), $"ScreenTools-RecordingAudioRuntimeFailure-{Guid.NewGuid():N}");
            Directory.CreateDirectory(outputDirectory);
            var microphone = new FakeAudioCaptureService();
            var systemAudio = new FakeAudioCaptureService();
            var recording = CreateRecordingService(microphone, systemAudio);

            try
            {
                recording.Start(CreateSession(outputDirectory, includeMicrophone: false, includeSystemAudio: true));
                systemAudio.RaiseFailure(new IOException("Simulated endpoint disconnect."));
                Assert.Equal(RecordingStatus.Recording, recording.Status);
                Assert.Contains("不可用", recording.CurrentAudioSummary);
                await Task.Delay(100);

                var artifact = await recording.StopAsync();
                Assert.NotNull(artifact);
                Assert.True(artifact.FrameCount > 0, "The video frame sequence must survive a runtime audio failure.");
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                    Directory.Delete(outputDirectory, recursive: true);
            }
        });

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
            Name = "ScreenTools audio-failure test dispatcher"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return JoinDispatcherThreadAsync(completion.Task, thread);
    }

    private static async Task JoinDispatcherThreadAsync(Task task, Thread thread)
    {
        try { await task; }
        finally
        {
            if (!thread.Join(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("The audio-failure test dispatcher did not shut down.");
        }
    }

    private static RecordingService CreateRecordingService(
        IAudioCaptureService microphone,
        IAudioCaptureService systemAudio) =>
        new(new FakeScreenCaptureService(), new FrameSequenceEncoder(null), microphone, systemAudio, new TempWorkspaceService());

    private sealed class FakeScreenCaptureService : IScreenCaptureService
    {
        public CaptureArtifact CaptureScreenshot(RecordingSessionState session) => throw new NotSupportedException();
        public CaptureArtifact CaptureScreenshotRegion(RecordingSessionState session, Rect region) => throw new NotSupportedException();
        public void CaptureFrameJpeg(string outputPath, string qualityPreset) =>
            File.WriteAllBytes(outputPath, [0xFF, 0xD8, 0xFF, 0xD9]);
        public byte[] CaptureFrameJpegBytes(string qualityPreset) => [0xFF, 0xD8, 0xFF, 0xD9];
    }

    private static RecordingSessionState CreateSession(string outputDirectory, bool includeMicrophone, bool includeSystemAudio) =>
        new()
        {
            IncludeMicrophone = includeMicrophone,
            IncludeSystemAudio = includeSystemAudio,
            OutputDirectory = outputDirectory,
            QualityPreset = "720P 30FPS"
        };

    private sealed class FakeAudioCaptureService(bool failOnStart = false) : IAudioCaptureService
    {
        public bool IsSupported => true;
        public bool IsCapturing { get; private set; }
        public string AvailabilityMessage => "Test audio endpoint.";
        public IReadOnlyList<AudioCaptureSegment> OutputSegments => Array.Empty<AudioCaptureSegment>();
        public event EventHandler<AudioCaptureFailedEventArgs>? CaptureFailed;
        public void RefreshAvailability() { }
        public void Start(string outputPath, TimeSpan timelineOffset = default)
        {
            if (failOnStart) throw new IOException("Simulated audio startup failure.");
            IsCapturing = true;
        }
        public void Pause() => IsCapturing = false;
        public void Resume(TimeSpan timelineOffset = default) => IsCapturing = !failOnStart;
        public void Stop() => IsCapturing = false;
        public void RaiseFailure(Exception exception) =>
            CaptureFailed?.Invoke(this, new AudioCaptureFailedEventArgs("系统声音", exception));
    }
}
