using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ScreenTools;

public sealed class RecordingService
{
    private readonly TempWorkspaceService _tempWorkspaceService;
    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _elapsedTimer;
    private readonly DispatcherTimer _captureTimer;
    private readonly ScreenCaptureService _screenCaptureService;
    private readonly FrameSequenceEncoder _frameSequenceEncoder;
    private readonly MicrophoneCaptureService _microphoneCaptureService;
    private readonly SystemAudioCaptureService _systemAudioCaptureService;
    private readonly List<RecordingFrameInfo> _capturedFrames = [];
    private readonly List<string> _detailMessages = [];
    private readonly SemaphoreSlim _captureLock = new(1, 1);
    private readonly object _frameSync = new();
    private string? _workingDirectory;
    private string? _microphoneAudioPath;
    private string? _systemAudioPath;
    private DateTimeOffset _startedAt;
    private TimeSpan _finalElapsed;
    private int _frameIndex;
    private bool _microphoneCaptureActive;
    private bool _systemAudioCaptureActive;

    public RecordingService(
        ScreenCaptureService screenCaptureService,
        FrameSequenceEncoder frameSequenceEncoder,
        MicrophoneCaptureService microphoneCaptureService,
        SystemAudioCaptureService systemAudioCaptureService,
        TempWorkspaceService tempWorkspaceService)
    {
        _screenCaptureService = screenCaptureService;
        _frameSequenceEncoder = frameSequenceEncoder;
        _microphoneCaptureService = microphoneCaptureService;
        _systemAudioCaptureService = systemAudioCaptureService;
        _tempWorkspaceService = tempWorkspaceService;
        _elapsedTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _elapsedTimer.Tick += (_, _) => ElapsedChanged?.Invoke(this, Elapsed);

        _captureTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _captureTimer.Tick += async (_, _) => await CaptureFrameAsync();
    }

    public RecordingStatus Status { get; private set; } = RecordingStatus.Idle;

    public RecordingSessionSnapshot? ActiveSession { get; private set; }

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public event EventHandler<RecordingStatus>? StatusChanged;

    public event EventHandler<TimeSpan>? ElapsedChanged;

    public event EventHandler? RuntimeInfoChanged;

    public string CurrentAudioSummary =>
        Status == RecordingStatus.Idle
            ? "等待开始录制"
            : $"麦克风 {GetCaptureStateLabel(_microphoneCaptureActive, ActiveSession?.IncludeMicrophone == true)} · 系统声音 {GetCaptureStateLabel(_systemAudioCaptureActive, ActiveSession?.IncludeSystemAudio == true)}";

    public string CurrentOutputSummary =>
        ActiveSession is null
            ? "尚未开始"
            : $"{ActiveSession.QualityPreset} · 输出到 {ActiveSession.OutputDirectory}";

    public void Start(RecordingSessionState session)
    {
        if (Status != RecordingStatus.Idle)
        {
            return;
        }

        _systemAudioCaptureService.RefreshAvailability();

        var profile = CaptureQualityProfile.FromPreset(session.QualityPreset);
        _captureTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(66, 1000.0 / profile.RecordingFrameRate));

        ActiveSession = new RecordingSessionSnapshot(
            session.ScreenshotFormat,
            session.IncludeMicrophone,
            session.IncludeSystemAudio,
            session.QualityPreset,
            session.ReplaySeconds,
            session.OutputDirectory,
            session.LaunchAtStartup);

        _startedAt = DateTimeOffset.Now;
        _tempWorkspaceService.EnsureWorkspace();
        _workingDirectory = Path.Combine(_tempWorkspaceService.RecordingWorkspaceRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workingDirectory);

        _detailMessages.Clear();
        _microphoneAudioPath = null;
        _systemAudioPath = null;
        _microphoneCaptureActive = false;
        _systemAudioCaptureActive = false;
        _capturedFrames.Clear();
        _frameIndex = 0;

        if (session.IncludeMicrophone)
        {
            _microphoneAudioPath = Path.Combine(_workingDirectory, "microphone.wav");
            try
            {
                _microphoneCaptureService.Start(_microphoneAudioPath);
                _microphoneCaptureActive = true;
            }
            catch (Exception ex)
            {
                _microphoneAudioPath = null;
                _detailMessages.Add($"麦克风采集未启动：{ex.Message}");
            }
        }

        if (session.IncludeSystemAudio)
        {
            _systemAudioPath = Path.Combine(_workingDirectory, "system-audio.wav");
            try
            {
                _systemAudioCaptureService.Start(_systemAudioPath);
                _systemAudioCaptureActive = true;
                _detailMessages.Add(_systemAudioCaptureService.AvailabilityMessage);
            }
            catch (Exception ex)
            {
                _systemAudioPath = null;
                _detailMessages.Add($"系统声音采集未启动：{ex.Message}");
            }
        }

        _stopwatch.Reset();
        _stopwatch.Start();
        _elapsedTimer.Start();
        _captureTimer.Start();
        SetStatus(RecordingStatus.Recording);
        RaiseRuntimeInfoChanged();
        _ = CaptureFrameAsync();
        ElapsedChanged?.Invoke(this, Elapsed);
    }

    public void TogglePause()
    {
        if (Status == RecordingStatus.Recording)
        {
            _stopwatch.Stop();
            _captureTimer.Stop();
            _microphoneCaptureService.Pause();
            _systemAudioCaptureService.Pause();
            SetStatus(RecordingStatus.Paused);
            RaiseRuntimeInfoChanged();
            return;
        }

        if (Status == RecordingStatus.Paused)
        {
            _stopwatch.Start();
            _captureTimer.Start();
            _microphoneCaptureService.Resume();
            _systemAudioCaptureService.Resume();
            SetStatus(RecordingStatus.Recording);
            RaiseRuntimeInfoChanged();
        }
    }

    public CaptureArtifact? Stop()
    {
        CaptureArtifact? artifact = null;
        try
        {
            _captureTimer.Stop();
            _elapsedTimer.Stop();
            _captureLock.Wait();
            _captureLock.Release();
            _stopwatch.Stop();
            _finalElapsed = _stopwatch.Elapsed;
            _microphoneCaptureService.Stop();
            _systemAudioCaptureService.Stop();

            if (ActiveSession is not null && _workingDirectory is not null)
            {
                artifact = PersistArtifact(ActiveSession, _workingDirectory);
            }
        }
        finally
        {
            CleanupRuntimeState();
        }

        return artifact;
    }

    public void Abort()
    {
        try
        {
            _captureTimer.Stop();
            _elapsedTimer.Stop();
            _captureLock.Wait();
            _captureLock.Release();
            _stopwatch.Stop();
            _finalElapsed = _stopwatch.Elapsed;
            _microphoneCaptureService.Stop();
            _systemAudioCaptureService.Stop();
        }
        finally
        {
            TryDeleteWorkingDirectory();
            CleanupRuntimeState();
        }
    }

    private void SetStatus(RecordingStatus status)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        StatusChanged?.Invoke(this, status);
    }

    private async Task CaptureFrameAsync()
    {
        if (Status != RecordingStatus.Recording || ActiveSession is null || _workingDirectory is null)
        {
            return;
        }

        if (!await _captureLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            var frameIndex = _frameIndex;
            var fileName = $"frame-{frameIndex:D4}.jpg";
            var filePath = Path.Combine(_workingDirectory, fileName);
            var frameOffset = _stopwatch.Elapsed;
            var qualityPreset = ActiveSession.QualityPreset;
            await Task.Run(() => _screenCaptureService.CaptureFrameJpeg(filePath, qualityPreset));
            lock (_frameSync)
            {
                _capturedFrames.Add(new RecordingFrameInfo(fileName, frameOffset));
                _frameIndex++;
            }
        }
        catch (Exception ex)
        {
            _captureTimer.Stop();
            _detailMessages.Insert(0, $"录制帧采集失败：{ex.Message}");
        }
        finally
        {
            _captureLock.Release();
        }
    }

    private CaptureArtifact PersistArtifact(RecordingSessionSnapshot snapshot, string workingDirectory)
    {
        var profile = CaptureQualityProfile.FromPreset(snapshot.QualityPreset);
        var outputDirectory = CaptureOutputPathHelper.CreateArtifactDirectory(snapshot.OutputDirectory, "Recordings", "Recording");
        try
        {
            string? mp4Path = null;
            try
            {
                var systemAudioPaths = _systemAudioCaptureService.OutputPaths;
                var encodeAudioPaths = new List<string>();
                if (!string.IsNullOrWhiteSpace(_microphoneAudioPath))
                {
                    encodeAudioPaths.Add(_microphoneAudioPath);
                }

                encodeAudioPaths.AddRange(systemAudioPaths);
                mp4Path = _frameSequenceEncoder.TryEncodeMp4(
                    workingDirectory,
                    "recording",
                    profile.RecordingFrameRate,
                    encodeAudioPaths.ToArray());
            }
            catch (Exception ex)
            {
                _detailMessages.Insert(0, $"MP4 导出失败，已保留帧序列：{ex.Message}");
            }
            var preserveFrameSequence = string.IsNullOrWhiteSpace(mp4Path) || !File.Exists(mp4Path);
            if (preserveFrameSequence)
            {
                foreach (var frame in _capturedFrames)
                {
                    var sourcePath = Path.Combine(workingDirectory, frame.FileName);
                    var destinationPath = Path.Combine(outputDirectory, frame.FileName);
                    if (File.Exists(sourcePath))
                    {
                        File.Move(sourcePath, destinationPath, overwrite: true);
                    }
                }
            }

            var endedAt = _startedAt + _finalElapsed;
            var manifestPath = Path.Combine(outputDirectory, "recording.json");
            var manifest = new RecordingArtifactManifest(
                "recording-frame-bundle",
                _startedAt,
                endedAt,
                _finalElapsed.TotalSeconds,
                profile.RecordingFrameRate,
                snapshot,
                _microphoneCaptureActive,
                _systemAudioCaptureActive,
                _systemAudioCaptureService.AvailabilityMessage,
                preserveFrameSequence,
                preserveFrameSequence ? _capturedFrames : []);

            File.WriteAllText(
                manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

            if (!string.IsNullOrWhiteSpace(_microphoneAudioPath) && File.Exists(_microphoneAudioPath))
            {
                var destinationAudioPath = Path.Combine(outputDirectory, "microphone.wav");
                File.Move(_microphoneAudioPath, destinationAudioPath, overwrite: true);
            }

            foreach (var audioPath in _systemAudioCaptureService.OutputPaths)
            {
                if (!File.Exists(audioPath))
                {
                    continue;
                }

                var destinationAudioPath = Path.Combine(outputDirectory, Path.GetFileName(audioPath));
                File.Move(audioPath, destinationAudioPath, overwrite: true);
            }

            var detail = BuildDetailSummary(profile);
            if (!string.IsNullOrWhiteSpace(mp4Path) && File.Exists(mp4Path))
            {
                var destinationVideoPath = Path.Combine(outputDirectory, "recording.mp4");
                File.Move(mp4Path, destinationVideoPath, overwrite: true);
                Directory.Delete(workingDirectory, recursive: true);
                return new CaptureArtifact(
                    "录制",
                    destinationVideoPath,
                    _capturedFrames.Count,
                    IsVideo: true,
                    Detail: detail);
            }

            Directory.Delete(workingDirectory, recursive: true);
            if (!_frameSequenceEncoder.CanEncodeVideo)
            {
                _detailMessages.Insert(0, "当前未找到 ffmpeg，已保留帧序列。");
            }

            return new CaptureArtifact(
                "录制",
                outputDirectory,
                _capturedFrames.Count,
                Detail: BuildDetailSummary(profile));
        }
        catch
        {
            TryDeleteDirectory(outputDirectory);
            throw;
        }
    }

    private void RaiseRuntimeInfoChanged()
    {
        RuntimeInfoChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CleanupRuntimeState()
    {
        _stopwatch.Reset();
        ActiveSession = null;
        _workingDirectory = null;
        _microphoneAudioPath = null;
        _systemAudioPath = null;
        _microphoneCaptureActive = false;
        _systemAudioCaptureActive = false;
        _capturedFrames.Clear();
        _detailMessages.Clear();
        _finalElapsed = TimeSpan.Zero;
        _frameIndex = 0;
        SetStatus(RecordingStatus.Idle);
        RaiseRuntimeInfoChanged();
        ElapsedChanged?.Invoke(this, Elapsed);
    }

    private void TryDeleteWorkingDirectory()
    {
        if (string.IsNullOrWhiteSpace(_workingDirectory) || !Directory.Exists(_workingDirectory))
        {
            return;
        }

        try
        {
            Directory.Delete(_workingDirectory, recursive: true);
        }
        catch
        {
        }
    }

    private static string GetCaptureStateLabel(bool active, bool requested)
    {
        if (!requested)
        {
            return "关闭";
        }

        return active ? "运行中" : "不可用";
    }

    private string? BuildDetailSummary(CaptureQualityProfile profile)
    {
        var details = new List<string>();
        var averageFps = _finalElapsed.TotalSeconds <= 0
            ? 0
            : _capturedFrames.Count / Math.Max(_finalElapsed.TotalSeconds, 0.001);

        details.Add($"录制时长 {_finalElapsed:mm\\:ss} · {_capturedFrames.Count} 帧 · 目标 {profile.RecordingFrameRate} FPS · 实际约 {averageFps:F1} FPS");

        if (_detailMessages.Count > 0)
        {
            details.AddRange(_detailMessages);
        }

        return details.Count == 0 ? null : string.Join(Environment.NewLine, details);
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

    private sealed record RecordingFrameInfo(string FileName, TimeSpan Offset);

    private sealed record RecordingArtifactManifest(
        string Format,
        DateTimeOffset StartedAt,
        DateTimeOffset EndedAt,
        double DurationSeconds,
        int TargetFrameRate,
        RecordingSessionSnapshot Session,
        bool MicrophoneCaptured,
        bool SystemAudioCaptured,
        string SystemAudioAvailability,
        bool FrameSequencePreserved,
        IReadOnlyList<RecordingFrameInfo> Frames);
}
