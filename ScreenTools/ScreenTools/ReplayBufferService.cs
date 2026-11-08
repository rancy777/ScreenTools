using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ScreenTools;

/// <summary>
/// Maintains a rolling background frame capture and exports selected replay clips.
/// </summary>
public sealed class ReplayBufferService
{
    private readonly ReplayFrameBuffer _frameBuffer = new();
    private readonly ScreenCaptureService _screenCaptureService;
    private readonly FrameSequenceEncoder _frameSequenceEncoder;
    private readonly RecordingSessionState _session;
    private readonly Dispatcher _dispatcher;
    private CancellationTokenSource? _captureCancellation;
    private Task? _captureLoopTask;

    /// <summary>
    /// Raised when a background replay frame cannot be captured.
    /// </summary>
    public event EventHandler<FrameCaptureFailedEventArgs>? FrameCaptureFailed;

    /// <summary>
    /// Initializes background replay capture and subscribes to session-setting changes.
    /// </summary>
    public ReplayBufferService(
        ScreenCaptureService screenCaptureService,
        FrameSequenceEncoder frameSequenceEncoder,
        RecordingSessionState session)
    {
        _screenCaptureService = screenCaptureService;
        _frameSequenceEncoder = frameSequenceEncoder;
        _session = session;
        _dispatcher = Dispatcher.CurrentDispatcher;
        _session.StateChanged += (_, _) => OnSessionStateChanged();
    }

    /// <summary>
    /// Gets whether background replay buffering is enabled in the current session settings.
    /// </summary>
    public bool IsEnabled => _session.ReplayBufferEnabled;
    /// <summary>
    /// Gets whether the background replay capture loop is currently active.
    /// </summary>
    public bool IsRunning => _captureLoopTask is { IsCompleted: false };

    /// <summary>
    /// Starts the background replay capture loop when replay is enabled and no loop is already running.
    /// </summary>
    public void Start()
    {
        if (!_session.ReplayBufferEnabled || IsRunning)
        {
            return;
        }

        var cancellation = new CancellationTokenSource();
        _captureCancellation = cancellation;
        _captureLoopTask = CaptureLoopAsync(cancellation.Token);
    }

    /// <summary>
    /// Stops the capture loop and releases its active capture resources.
    /// </summary>
    public void Stop()
    {
        _ = StopAsync(clearFrames: true, restartIfEnabled: true);
    }

    /// <summary>
    /// Stops the background capture loop, optionally clears retained frames, and can restart capture when replay remains enabled.
    /// </summary>
    public async Task StopAsync(bool clearFrames = true, bool restartIfEnabled = false)
    {
        var cancellation = _captureCancellation;
        var loopTask = _captureLoopTask;
        if (cancellation is not null)
            await cancellation.CancelAsync().ConfigureAwait(false);

        if (loopTask is not null)
        {
            try { await loopTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }

        if (ReferenceEquals(_captureCancellation, cancellation))
        {
            _captureCancellation = null;
            _captureLoopTask = null;
        }
        cancellation?.Dispose();

        if (clearFrames)
            _frameBuffer.Clear();

        if (restartIfEnabled && _session.ReplayBufferEnabled)
            Start();
    }

    /// <summary>
    /// Snapshots the buffered frames and session options required to export a replay.
    /// </summary>
    public ReplayExport? PrepareReplayExport(RecordingSessionState session)
    {
        if (!session.ReplayBufferEnabled)
        {
            return null;
        }

        var profile = CaptureQualityProfile.FromPreset(session.QualityPreset);
        var cutoff = DateTimeOffset.Now.AddSeconds(-Math.Max(1, session.ReplaySeconds));
        var replayFrames = _frameBuffer.SnapshotAfter(cutoff);

        if (replayFrames.Length == 0)
        {
            return null;
        }

        return new ReplayExport(
            session.OutputDirectory,
            session.ReplaySeconds,
            session.QualityPreset,
            profile.ReplayFrameRate,
            replayFrames);
    }

    /// <summary>
    /// Exports the prepared replay frames and returns the resulting capture artifact.
    /// </summary>
    public async Task<CaptureArtifact> SaveReplayAsync(ReplayExport export, CancellationToken cancellationToken = default)
    {
        var replayFrames = export.Frames;
        var paths = await Task.Run(() =>
        {
            var output = CaptureOutputPathHelper.CreateArtifactDirectory(export.OutputDirectory, "Replays", "Replay");
            try
            {
                return (OutputDirectory: output, ExportWorkspace: CreateExportWorkspace());
            }
            catch
            {
                TryDeleteDirectory(output);
                throw;
            }
        }, cancellationToken).ConfigureAwait(false);
        var outputDirectory = paths.OutputDirectory;
        var exportWorkspace = paths.ExportWorkspace;
        try
        {
            for (var i = 0; i < replayFrames.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileName = $"frame-{i:D4}.jpg";
                await File.WriteAllBytesAsync(Path.Combine(exportWorkspace, fileName), replayFrames[i].JpegBytes, cancellationToken).ConfigureAwait(false);
            }

            var actualDurationSeconds = replayFrames.Count <= 1
                ? 0
                : (replayFrames[^1].CapturedAt - replayFrames[0].CapturedAt).TotalSeconds;
            var encodedDurationSeconds = replayFrames.Count / (double)Math.Max(1, export.TargetFrameRate);
            var preserveFrameSequence = false;
            var manifest = new ReplayArtifactManifest(
                "replay-frame-bundle",
                DateTimeOffset.Now,
                export.ReplaySeconds,
                actualDurationSeconds,
                encodedDurationSeconds,
                export.QualityPreset,
                export.TargetFrameRate,
                IncludesAudio: false,
                AudioMode: "silent",
                PreserveFrameSequence: preserveFrameSequence,
                replayFrames.Select((frame, index) => new ReplayArtifactFrame(
                    $"frame-{index:D4}.jpg",
                    frame.CapturedAt)).ToList());

            var manifestPath = Path.Combine(exportWorkspace, "replay.json");
            await File.WriteAllTextAsync(
                manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), cancellationToken).ConfigureAwait(false);

            string? mp4Path = null;
            string? detail = BuildReplayDetail(export.ReplaySeconds, actualDurationSeconds, encodedDurationSeconds);
            try
            {
                mp4Path = await _frameSequenceEncoder.TryEncodeMp4Async(
                    exportWorkspace, "replay", export.TargetFrameRate, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                preserveFrameSequence = true;
                manifest = manifest with { PreserveFrameSequence = true };
                await File.WriteAllTextAsync(
                    manifestPath,
                    JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), cancellationToken).ConfigureAwait(false);
                detail = AppendDetail(detail, $"MP4 导出失败，已保留帧序列：{ex.Message}");
            }
            if (!string.IsNullOrWhiteSpace(mp4Path) && File.Exists(mp4Path))
            {
                var finalMp4Path = Path.Combine(outputDirectory, Path.GetFileName(mp4Path));
                File.Move(mp4Path, finalMp4Path, overwrite: true);
                File.Move(manifestPath, Path.Combine(outputDirectory, "replay.json"), overwrite: true);
                return new CaptureArtifact("回录", finalMp4Path, replayFrames.Count, IsVideo: true, Detail: detail);
            }

            var fallbackDetail = _frameSequenceEncoder.CanEncodeVideo
                ? detail
                : AppendDetail(detail, "当前未找到 ffmpeg，已保留帧序列。");
            manifest = manifest with { PreserveFrameSequence = true };
            await File.WriteAllTextAsync(
                manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), cancellationToken).ConfigureAwait(false);
            MoveDirectoryContents(exportWorkspace, outputDirectory);
            return new CaptureArtifact("回录", outputDirectory, replayFrames.Count, Detail: fallbackDetail);
        }
        catch
        {
            TryDeleteDirectory(outputDirectory);
            throw;
        }
        finally
        {
            TryDeleteDirectory(exportWorkspace);
        }
    }

    private async Task CaptureLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_session.ReplayBufferEnabled)
                    await CaptureFrameAsync(cancellationToken).ConfigureAwait(false);

                var profile = CaptureQualityProfile.FromPreset(_session.QualityPreset);
                var interval = TimeSpan.FromMilliseconds(Math.Max(120, 1000.0 / profile.ReplayFrameRate));
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private async Task CaptureFrameAsync(CancellationToken cancellationToken)
    {
        try
        {
            var qualityPreset = _session.QualityPreset;
            var jpegBytes = await Task.Run(() => _screenCaptureService.CaptureFrameJpegBytes(qualityPreset), cancellationToken).ConfigureAwait(false);
            if (jpegBytes.Length == 0)
                return;

            if (cancellationToken.IsCancellationRequested || !_session.ReplayBufferEnabled)
                return;
            if (!cancellationToken.IsCancellationRequested)
                _frameBuffer.Add(new ReplayFrameSnapshot(DateTimeOffset.Now, jpegBytes));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            RaiseFrameCaptureFailed(ex);
        }
    }

    private void RaiseFrameCaptureFailed(Exception exception)
    {
        void Raise() => FrameCaptureFailed?.Invoke(this, new FrameCaptureFailedEventArgs(exception));
        if (_dispatcher.CheckAccess())
            Raise();
        else if (!_dispatcher.HasShutdownStarted && !_dispatcher.HasShutdownFinished)
            _dispatcher.BeginInvoke(Raise);
    }

    private void OnSessionStateChanged()
    {
        if (!_session.ReplayBufferEnabled)
        {
            Stop();
            return;
        }

        var cutoff = DateTimeOffset.Now.AddSeconds(-(Math.Max(30, _session.ReplaySeconds) + 5));
        _frameBuffer.TrimOlderThan(cutoff);
        if (!IsRunning)
        {
            Start();
        }
    }

    /// <summary>
    /// Holds one timestamped JPEG frame retained by the replay buffer.
    /// </summary>
    public sealed record ReplayFrameSnapshot(DateTimeOffset CapturedAt, byte[] JpegBytes);
    /// <summary>
    /// Contains the immutable frame and session data prepared for replay export.
    /// </summary>
    public sealed record ReplayExport(
        string OutputDirectory,
        int ReplaySeconds,
        string QualityPreset,
        int TargetFrameRate,
        IReadOnlyList<ReplayFrameSnapshot> Frames);

    /// <summary>
    /// Carries the exception raised when a background replay frame cannot be captured.
    /// </summary>
    public sealed record FrameCaptureFailedEventArgs(Exception Exception);

    private sealed record ReplayArtifactManifest(
        string Format,
        DateTimeOffset SavedAt,
        int ReplaySeconds,
        double ActualDurationSeconds,
        double EncodedDurationSeconds,
        string QualityPreset,
        int TargetFrameRate,
        bool IncludesAudio,
        string AudioMode,
        bool PreserveFrameSequence,
        IReadOnlyList<ReplayArtifactFrame> Frames);

    private sealed record ReplayArtifactFrame(string FileName, DateTimeOffset CapturedAt);

    private static string? BuildReplayDetail(int requestedSeconds, double actualDurationSeconds, double encodedDurationSeconds)
    {
        var details = new List<string>();
        if (actualDurationSeconds + 0.75 >= requestedSeconds)
        {
            if (Math.Abs(encodedDurationSeconds - actualDurationSeconds) > 0.75)
            {
                details.Add($"导出视频约 {encodedDurationSeconds:F1}s，缓存覆盖约 {actualDurationSeconds:F1}s。");
            }
            return details.Count == 0 ? null : string.Join(Environment.NewLine, details);
        }

        details.Add($"回录实际覆盖约 {actualDurationSeconds:F1}s，短于目标 {requestedSeconds}s。");
        if (Math.Abs(encodedDurationSeconds - actualDurationSeconds) > 0.75)
        {
            details.Add($"导出视频约 {encodedDurationSeconds:F1}s。");
        }

        return string.Join(Environment.NewLine, details);
    }

    private static string? AppendDetail(string? current, string next)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return next;
        }

        return $"{current}{Environment.NewLine}{next}";
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
        }
    }

    private static string CreateExportWorkspace()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "LensSnap", "ReplayExport", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        return workspace;
    }

    private static void MoveDirectoryContents(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        foreach (var filePath in Directory.EnumerateFiles(sourceDirectory))
        {
            var targetPath = Path.Combine(destinationDirectory, Path.GetFileName(filePath));
            File.Move(filePath, targetPath, overwrite: true);
        }
    }
}
