using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Threading;

namespace ScreenTools;

public sealed class ReplayBufferService
{
    private const long MaxReplayBufferBytes = 384L * 1024 * 1024;
    private readonly object _sync = new();
    private readonly ScreenCaptureService _screenCaptureService;
    private readonly FrameSequenceEncoder _frameSequenceEncoder;
    private readonly RecordingSessionState _session;
    private readonly DispatcherTimer _captureTimer;
    private readonly LinkedList<ReplayFrameSnapshot> _frames = [];
    private long _bufferedBytes;

    public ReplayBufferService(
        ScreenCaptureService screenCaptureService,
        FrameSequenceEncoder frameSequenceEncoder,
        RecordingSessionState session)
    {
        _screenCaptureService = screenCaptureService;
        _frameSequenceEncoder = frameSequenceEncoder;
        _session = session;
        _captureTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _captureTimer.Tick += (_, _) => CaptureFrame();
        _session.StateChanged += (_, _) => OnSessionStateChanged();
    }

    public bool IsEnabled => _session.ReplayBufferEnabled;
    public bool IsRunning => _captureTimer.IsEnabled;

    public void Start()
    {
        if (!_session.ReplayBufferEnabled || _captureTimer.IsEnabled)
        {
            return;
        }

        UpdateCaptureInterval();
        _captureTimer.Start();
        CaptureFrame();
    }

    public void Stop()
    {
        _captureTimer.Stop();
        _frames.Clear();
        _bufferedBytes = 0;
    }

    public ReplayExport? PrepareReplayExport(RecordingSessionState session)
    {
        if (!session.ReplayBufferEnabled)
        {
            return null;
        }

        var profile = CaptureQualityProfile.FromPreset(session.QualityPreset);
        var cutoff = DateTimeOffset.Now.AddSeconds(-Math.Max(1, session.ReplaySeconds));
        List<ReplayFrameSnapshot> replayFrames;
        lock (_sync)
        {
            replayFrames = _frames
                .Where(frame => frame.CapturedAt >= cutoff)
                .Select(frame => frame with { })
                .ToList();
        }

        if (replayFrames.Count == 0)
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

    public CaptureArtifact SaveReplay(ReplayExport export)
    {
        var replayFrames = export.Frames;

        var outputDirectory = CaptureOutputPathHelper.CreateArtifactDirectory(export.OutputDirectory, "Replays", "Replay");
        var exportWorkspace = CreateExportWorkspace();
        try
        {
            for (var i = 0; i < replayFrames.Count; i++)
            {
                var fileName = $"frame-{i:D4}.jpg";
                File.WriteAllBytes(Path.Combine(exportWorkspace, fileName), replayFrames[i].JpegBytes);
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
            File.WriteAllText(
                manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

            string? mp4Path = null;
            string? detail = BuildReplayDetail(export.ReplaySeconds, actualDurationSeconds, encodedDurationSeconds);
            try
            {
                mp4Path = _frameSequenceEncoder.TryEncodeMp4(exportWorkspace, "replay", export.TargetFrameRate);
            }
            catch (Exception ex)
            {
                preserveFrameSequence = true;
                manifest = manifest with { PreserveFrameSequence = true };
                File.WriteAllText(
                    manifestPath,
                    JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
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
            File.WriteAllText(
                manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
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

    private void CaptureFrame()
    {
        if (!_session.ReplayBufferEnabled)
        {
            return;
        }

        try
        {
            var jpegBytes = _screenCaptureService.CaptureFrameJpegBytes(_session.QualityPreset);
            if (jpegBytes.Length == 0)
            {
                return;
            }

            lock (_sync)
            {
                _frames.AddLast(new ReplayFrameSnapshot(DateTimeOffset.Now, jpegBytes));
                _bufferedBytes += jpegBytes.LongLength;
                TrimFrames();
            }
        }
        catch (Exception ex)
        {
            // Surface the error so the active recording can log it instead of silently dropping frames.
            _session.RecordingError = ex;
        }
    }

    private void TrimFrames()
    {
        var cutoff = DateTimeOffset.Now.AddSeconds(-(Math.Max(30, _session.ReplaySeconds) + 5));
        while (_frames.First is not null && _frames.First.Value.CapturedAt < cutoff)
        {
            _bufferedBytes -= _frames.First.Value.JpegBytes.LongLength;
            _frames.RemoveFirst();
        }

        while (_frames.First is not null && _bufferedBytes > MaxReplayBufferBytes)
        {
            _bufferedBytes -= _frames.First.Value.JpegBytes.LongLength;
            _frames.RemoveFirst();
        }
    }

    private void UpdateCaptureInterval()
    {
        var profile = CaptureQualityProfile.FromPreset(_session.QualityPreset);
        _captureTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(120, 1000.0 / profile.ReplayFrameRate));
    }

    private void OnSessionStateChanged()
    {
        if (!_session.ReplayBufferEnabled)
        {
            Stop();
            return;
        }

        UpdateCaptureInterval();
        lock (_sync)
        {
            TrimFrames();
        }
        if (!_captureTimer.IsEnabled)
        {
            Start();
        }
    }

    public sealed record ReplayFrameSnapshot(DateTimeOffset CapturedAt, byte[] JpegBytes);
    public sealed record ReplayExport(
        string OutputDirectory,
        int ReplaySeconds,
        string QualityPreset,
        int TargetFrameRate,
        IReadOnlyList<ReplayFrameSnapshot> Frames);

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
