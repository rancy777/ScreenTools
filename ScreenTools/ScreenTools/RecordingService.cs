using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ScreenTools;

/// <summary>
/// Coordinates video-frame capture, optional audio capture, and asynchronous recording finalization.
/// </summary>
public sealed class RecordingService
{
    private readonly TempWorkspaceService _tempWorkspaceService;
    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _elapsedTimer;
    private readonly DispatcherTimer _captureTimer;
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly FrameSequenceEncoder _frameSequenceEncoder;
    private readonly IAudioCaptureService _microphoneCaptureService;
    private readonly IAudioCaptureService _systemAudioCaptureService;
    private readonly List<RecordingFrameInfo> _capturedFrames = [];
    private readonly List<string> _detailMessages = [];
    private readonly SemaphoreSlim _captureLock = new(1, 1);
    private readonly Dispatcher _dispatcher;
    private readonly object _frameSync = new();
    private bool _isCapturing;
    private string? _workingDirectory;
    private string? _microphoneAudioPath;
    private DateTimeOffset _startedAt;
    private TimeSpan _finalElapsed;
    private int _frameIndex;
    private bool _microphoneCaptureActive;
    private bool _systemAudioCaptureActive;
    private long _captureRequestCount;
    private long _skippedCaptureCount;
    private long _captureFailureCount;
    private long _capturedFrameBytes;
    private long _peakWorkingSetBytes;
    private long _peakPrivateBytes;
    private Task<CaptureArtifact?>? _stopTask;
    private Task? _abortTask;

    /// <summary>
    /// Initializes frame capture, audio capture, encoding, temporary storage, and dispatcher coordination.
    /// </summary>
    public RecordingService(
        IScreenCaptureService screenCaptureService,
        FrameSequenceEncoder frameSequenceEncoder,
        IAudioCaptureService microphoneCaptureService,
        IAudioCaptureService systemAudioCaptureService,
        TempWorkspaceService tempWorkspaceService)
    {
        _screenCaptureService = screenCaptureService;
        _frameSequenceEncoder = frameSequenceEncoder;
        _microphoneCaptureService = microphoneCaptureService;
        _systemAudioCaptureService = systemAudioCaptureService;
        _tempWorkspaceService = tempWorkspaceService;
        _microphoneCaptureService.CaptureFailed += OnAudioCaptureFailed;
        _systemAudioCaptureService.CaptureFailed += OnAudioCaptureFailed;
        _dispatcher = Dispatcher.CurrentDispatcher;
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

    /// <summary>
    /// Gets the current recording lifecycle state.
    /// </summary>
    public RecordingStatus Status { get; private set; } = RecordingStatus.Idle;

    /// <summary>
    /// Gets the immutable settings snapshot for the active recording, or null when idle.
    /// </summary>
    public RecordingSessionSnapshot? ActiveSession { get; private set; }

    /// <summary>
    /// Gets the elapsed capture duration, excluding time spent paused.
    /// </summary>
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <summary>
    /// Raised when the current application status message or severity changes.
    /// </summary>
    public event EventHandler<RecordingStatus>? StatusChanged;

    /// <summary>
    /// Raised when the elapsed recording duration is refreshed.
    /// </summary>
    public event EventHandler<TimeSpan>? ElapsedChanged;

    /// <summary>
    /// Raised when recording audio or output information changes.
    /// </summary>
    public event EventHandler? RuntimeInfoChanged;

    /// <summary>
    /// Gets a user-facing summary of microphone and system-audio capture state.
    /// </summary>
    public string CurrentAudioSummary =>
        Status == RecordingStatus.Idle
            ? "等待开始录制"
            : $"麦克风 {GetCaptureStateLabel(_microphoneCaptureActive, ActiveSession?.IncludeMicrophone == true)} · 系统声音 {GetCaptureStateLabel(_systemAudioCaptureActive, ActiveSession?.IncludeSystemAudio == true)}";

    /// <summary>
    /// Gets a summary of the active recording quality preset and output directory.
    /// </summary>
    public string CurrentOutputSummary =>
        ActiveSession is null
            ? "尚未开始"
            : $"{ActiveSession.QualityPreset} · 输出到 {ActiveSession.OutputDirectory}";

    /// <summary>
    /// Starts the requested capture operation when the service is in a state that permits it.
    /// </summary>
    public void Start(RecordingSessionState session)
    {
        if (Status != RecordingStatus.Idle)
        {
            return;
        }

        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        if (!TryValidateOutputDirectory(session.OutputDirectory, out var outputError))
        {
            throw new InvalidOperationException($"无法开始录制：{outputError}");
        }

        _microphoneCaptureService.RefreshAvailability();
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
        _microphoneCaptureActive = false;
        _systemAudioCaptureActive = false;
        _capturedFrames.Clear();
        _frameIndex = 0;
        _captureRequestCount = 0;
        _skippedCaptureCount = 0;
        _captureFailureCount = 0;
        _capturedFrameBytes = 0;
        _peakWorkingSetBytes = 0;
        _peakPrivateBytes = 0;
        _stopwatch.Reset();
        _stopwatch.Start();

        if (session.IncludeMicrophone)
        {
            _microphoneAudioPath = Path.Combine(_workingDirectory, "microphone.wav");
            try
            {
                _microphoneCaptureService.Start(_microphoneAudioPath, _stopwatch.Elapsed);
                _microphoneCaptureActive = _microphoneCaptureService.IsCapturing;
            }
            catch (Exception ex)
            {
                _microphoneAudioPath = null;
                _detailMessages.Add($"麦克风采集未启动：{ex.Message}");
            }
        }

        if (session.IncludeSystemAudio)
        {
            var systemAudioPath = Path.Combine(_workingDirectory, "system-audio.wav");
            try
            {
                _systemAudioCaptureService.Start(systemAudioPath, _stopwatch.Elapsed);
                _systemAudioCaptureActive = _systemAudioCaptureService.IsCapturing;
                _detailMessages.Add(LocalizationManager.Translate(_systemAudioCaptureService.AvailabilityMessage));
            }
            catch (Exception ex)
            {
                _detailMessages.Add($"系统声音采集未启动：{ex.Message}");
            }
        }

        _elapsedTimer.Start();
        _captureTimer.Start();
        SetStatus(RecordingStatus.Recording);
        RaiseRuntimeInfoChanged();
        _ = CaptureFrameAsync();
        ElapsedChanged?.Invoke(this, Elapsed);
    }

    /// <summary>
    /// Switches an active recording between recording and paused states.
    /// </summary>
    public void TogglePause()
    {
        if (Status == RecordingStatus.Recording)
        {
            _stopwatch.Stop();
            _captureTimer.Stop();
            PauseAudioCaptureSafely();
            SetStatus(RecordingStatus.Paused);
            _detailMessages.Insert(0, $"录制已暂停于 {DateTimeOffset.Now:HH:mm:ss}。");
            RaiseRuntimeInfoChanged();
            return;
        }

        if (Status == RecordingStatus.Paused)
        {
            _stopwatch.Start();
            _captureTimer.Start();
            ResumeAudioCaptureSafely(_stopwatch.Elapsed);
            SetStatus(RecordingStatus.Recording);
            _detailMessages.Insert(0, $"录制已恢复于 {DateTimeOffset.Now:HH:mm:ss}。");
            RaiseRuntimeInfoChanged();
        }
    }

    /// <summary>
    /// Stops capture, finalizes audio, and persists the recording artifact asynchronously.
    /// </summary>
    public Task<CaptureArtifact?> StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_dispatcher.CheckAccess())
            return _dispatcher.InvokeAsync(() => StopAsync(cancellationToken)).Task.Unwrap();

        if (_stopTask is not null)
            return _stopTask;
        if (Status == RecordingStatus.Idle)
            return Task.FromResult<CaptureArtifact?>(null);

        _stopTask = StopCoreAsync(cancellationToken);
        return _stopTask;
    }

    private async Task<CaptureArtifact?> StopCoreAsync(CancellationToken cancellationToken)
    {
        CaptureArtifact? artifact = null;
        _captureTimer.Stop();
        _elapsedTimer.Stop();
        _stopwatch.Stop();
        _finalElapsed = _stopwatch.Elapsed;
        SetStatus(RecordingStatus.Finalizing);
        RaiseRuntimeInfoChanged();

        try
        {
            await _captureLock.WaitAsync().ConfigureAwait(true);
            try
            {
                await Task.Run(() =>
                {
                    StopAudioCaptureSafely();
                }).ConfigureAwait(true);
            }
            finally
            {
                _captureLock.Release();
            }

            if (ActiveSession is not null && _workingDirectory is not null)
            {
                var snapshot = ActiveSession;
                var workingDirectory = _workingDirectory;
                artifact = await Task.Run(
                    () => PersistArtifactAsync(snapshot, workingDirectory, cancellationToken),
                    CancellationToken.None).ConfigureAwait(true);
            }

            return artifact;
        }
        finally
        {
            CleanupRuntimeState();
            _stopTask = null;
        }
    }

    /// <summary>
    /// Stops capture and removes the temporary recording without publishing an output artifact.
    /// </summary>
    public async Task AbortAsync()
    {
        if (!_dispatcher.CheckAccess())
        {
            await _dispatcher.InvokeAsync(() => AbortAsync()).Task.Unwrap().ConfigureAwait(false);
            return;
        }

        if (_stopTask is not null)
        {
            await _stopTask.ConfigureAwait(true);
            return;
        }
        if (_abortTask is not null)
        {
            await _abortTask.ConfigureAwait(true);
            return;
        }
        if (Status == RecordingStatus.Idle)
            return;
        _abortTask = AbortCoreAsync();
        await _abortTask.ConfigureAwait(true);
    }

    private async Task AbortCoreAsync()
    {
        _captureTimer.Stop();
        _elapsedTimer.Stop();
        _stopwatch.Stop();
        _finalElapsed = _stopwatch.Elapsed;
        SetStatus(RecordingStatus.Finalizing);
        try
        {
            await _captureLock.WaitAsync().ConfigureAwait(true);
            try
            {
                await Task.Run(() =>
                {
                    StopAudioCaptureSafely();
                    TryDeleteWorkingDirectory();
                }).ConfigureAwait(true);
            }
            finally
            {
                _captureLock.Release();
            }
        }
        finally
        {
            CleanupRuntimeState();
            _abortTask = null;
        }
    }

    private void SetStatus(RecordingStatus status)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        RaiseOnDispatcher(() => StatusChanged?.Invoke(this, status));
    }

    private async Task CaptureFrameAsync()
    {
        if (Status != RecordingStatus.Recording || ActiveSession is null || _workingDirectory is null)
        {
            return;
        }

        Interlocked.Increment(ref _captureRequestCount);
        if (_isCapturing)
        {
            Interlocked.Increment(ref _skippedCaptureCount);
            return;
        }

        _isCapturing = true;
        try
        {
            if (!await _captureLock.WaitAsync(0))
            {
                Interlocked.Increment(ref _skippedCaptureCount);
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
                var frameBytes = new FileInfo(filePath).Length;
                lock (_frameSync)
                {
                    _capturedFrames.Add(new RecordingFrameInfo(fileName, frameOffset));
                    _frameIndex++;
                }
                Interlocked.Add(ref _capturedFrameBytes, frameBytes);
                UpdatePeakProcessMemory();
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _captureFailureCount);
                _detailMessages.Insert(0, LocalizationManager.Translate("录制帧采集失败：") + ex.Message);
                if (Status == RecordingStatus.Recording)
                {
                    _captureTimer.Interval = ActiveSession is not null
                        ? TimeSpan.FromMilliseconds(Math.Max(66, 1000.0 / CaptureQualityProfile.FromPreset(ActiveSession.QualityPreset).RecordingFrameRate))
                        : TimeSpan.FromMilliseconds(500);
                    _captureTimer.Start();
                }
            }
            finally
            {
                _captureLock.Release();
            }
        }
        finally
        {
            _isCapturing = false;
        }
    }

    private void UpdatePeakProcessMemory()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            UpdatePeak(ref _peakWorkingSetBytes, process.WorkingSet64);
            UpdatePeak(ref _peakPrivateBytes, process.PrivateMemorySize64);
        }
        catch (InvalidOperationException)
        {
            // Process metrics are diagnostic only and must never interrupt capture.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Process metrics are diagnostic only and must never interrupt capture.
        }
    }

    private static void UpdatePeak(ref long peak, long value)
    {
        long observed;
        do
        {
            observed = Interlocked.Read(ref peak);
            if (value <= observed)
                return;
        }
        while (Interlocked.CompareExchange(ref peak, value, observed) != observed);
    }

    private async Task<CaptureArtifact> PersistArtifactAsync(
        RecordingSessionSnapshot snapshot,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var profile = CaptureQualityProfile.FromPreset(snapshot.QualityPreset);
        var outputDirectory = CaptureOutputPathHelper.CreateArtifactDirectory(snapshot.OutputDirectory, "Recordings", "Recording");
        try
        {
            var firstFrameOffset = _capturedFrames.Count > 0
                ? _capturedFrames[0].Offset
                : TimeSpan.Zero;
            var microphoneSegments = _microphoneCaptureService.OutputSegments;
            var systemAudioSegments = _systemAudioCaptureService.OutputSegments;
            var audioSegments = microphoneSegments
                .Select(segment => (Source: "microphone", Segment: segment))
                .Concat(systemAudioSegments.Select(segment => (Source: "system", Segment: segment)))
                .Where(item => File.Exists(item.Segment.Path) && new FileInfo(item.Segment.Path).Length > 44)
                .Select(item => (item.Source, Segment: item.Segment with { StartOffset = item.Segment.StartOffset - firstFrameOffset }))
                .ToArray();
            var audioSegmentManifest = audioSegments
                .Select(item => new AudioSegmentManifest(
                    item.Source,
                    Path.GetFileName(item.Segment.Path),
                    item.Segment.StartOffset.TotalSeconds))
                .ToArray();
            string? mp4Path = null;
            try
            {
                var videoDuration = TimeSpan.FromSeconds(_capturedFrames.Count / (double)profile.RecordingFrameRate);
                mp4Path = await _frameSequenceEncoder.TryEncodeMp4WithAudioSegmentsAsync(
                    workingDirectory,
                    "recording",
                    profile.RecordingFrameRate,
                    cancellationToken,
                    audioSegments.Select(item => item.Segment).ToArray(),
                    videoDuration).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _detailMessages.Insert(0, "MP4 导出已取消，已保留采集到的帧序列。");
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
            var systemAudioAvailability = !snapshot.IncludeSystemAudio
                ? LocalizationManager.Translate("未启用系统声音采集。")
                : !_systemAudioCaptureActive
                    ? LocalizationManager.Translate("系统声音采集不可用或已中断。")
                    : LocalizationManager.Translate(_systemAudioCaptureService.AvailabilityMessage);
            var manifest = new RecordingArtifactManifest(
                "recording-frame-bundle",
                _startedAt,
                endedAt,
                _finalElapsed.TotalSeconds,
                profile.RecordingFrameRate,
                snapshot,
                audioSegmentManifest.Any(segment => segment.Source == "microphone"),
                audioSegmentManifest.Any(segment => segment.Source == "system"),
                systemAudioAvailability,
                preserveFrameSequence,
                audioSegmentManifest,
                preserveFrameSequence ? _capturedFrames : [],
                _capturedFrames.Count,
                Interlocked.Read(ref _captureRequestCount),
                Interlocked.Read(ref _skippedCaptureCount),
                Interlocked.Read(ref _captureFailureCount),
                Interlocked.Read(ref _capturedFrameBytes),
                Interlocked.Read(ref _peakWorkingSetBytes),
                Interlocked.Read(ref _peakPrivateBytes));

            await File.WriteAllTextAsync(
                manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), cancellationToken: CancellationToken.None).ConfigureAwait(false);

            foreach (var audioPath in audioSegments.Select(item => item.Segment.Path).Distinct(StringComparer.OrdinalIgnoreCase))
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
                TryDeleteDirectory(workingDirectory);
                return new CaptureArtifact(
                    "录制",
                    destinationVideoPath,
                    _capturedFrames.Count,
                    IsVideo: true,
                    Detail: detail);
            }

            TryDeleteDirectory(workingDirectory);
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
        RaiseOnDispatcher(() => RuntimeInfoChanged?.Invoke(this, EventArgs.Empty));
    }

    private void CleanupRuntimeState()
    {
        _stopwatch.Reset();
        ActiveSession = null;
        _workingDirectory = null;
        _microphoneAudioPath = null;
        _microphoneCaptureActive = false;
        _systemAudioCaptureActive = false;
        _capturedFrames.Clear();
        _detailMessages.Clear();
        _finalElapsed = TimeSpan.Zero;
        _frameIndex = 0;
        _captureRequestCount = 0;
        _skippedCaptureCount = 0;
        _captureFailureCount = 0;
        _capturedFrameBytes = 0;
        _peakWorkingSetBytes = 0;
        _peakPrivateBytes = 0;
        SetStatus(RecordingStatus.Idle);
        RaiseRuntimeInfoChanged();
        RaiseOnDispatcher(() => ElapsedChanged?.Invoke(this, Elapsed));
    }

    private void RaiseOnDispatcher(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        if (!_dispatcher.HasShutdownStarted && !_dispatcher.HasShutdownFinished)
            _dispatcher.BeginInvoke(action);
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

    private void StopAudioCaptureSafely()
    {
        try
        {
            _microphoneCaptureService.Stop();
        }
        catch (Exception ex)
        {
            _detailMessages.Insert(0, LocalizationManager.Translate("麦克风采集未能正常结束：") + ex.Message);
        }

        try
        {
            _systemAudioCaptureService.Stop();
        }
        catch (Exception ex)
        {
            _detailMessages.Insert(0, LocalizationManager.Translate("系统声音采集未能正常结束：") + ex.Message);
        }
    }

    private void PauseAudioCaptureSafely()
    {
        TryAudioOperation(_microphoneCaptureService.Pause, "麦克风暂停失败", () => _microphoneCaptureActive = false);
        TryAudioOperation(_systemAudioCaptureService.Pause, "系统声音暂停失败", () => _systemAudioCaptureActive = false);
    }

    private void ResumeAudioCaptureSafely(TimeSpan timelineOffset)
    {
        TryAudioOperation(() =>
        {
            _microphoneCaptureService.Resume(timelineOffset);
            _microphoneCaptureActive = _microphoneCaptureService.IsCapturing;
        }, "麦克风恢复失败", () => _microphoneCaptureActive = false);
        TryAudioOperation(() =>
        {
            _systemAudioCaptureService.Resume(timelineOffset);
            _systemAudioCaptureActive = _systemAudioCaptureService.IsCapturing;
        }, "系统声音恢复失败", () => _systemAudioCaptureActive = false);
    }

    private void TryAudioOperation(Action operation, string failurePrefix, Action onFailure)
    {
        try
        {
            operation();
        }
        catch (Exception ex)
        {
            onFailure();
            _detailMessages.Insert(0, $"{failurePrefix}：{ex.Message}");
            RaiseRuntimeInfoChanged();
        }
    }

    private void OnAudioCaptureFailed(object? sender, AudioCaptureFailedEventArgs e)
    {
        RaiseOnDispatcher(() =>
        {
            if (Status is RecordingStatus.Idle or RecordingStatus.Finalizing)
                return;

            if (string.Equals(e.SourceName, "麦克风", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.SourceName, "Microphone", StringComparison.OrdinalIgnoreCase))
                _microphoneCaptureActive = false;
            else
                _systemAudioCaptureActive = false;

            var failureMessage = LocalizationManager.Translate("录制期间") +
                                 LocalizationManager.Translate(e.SourceName) +
                                 LocalizationManager.Translate("采集失败，视频录制将继续：") +
                                 e.Exception.Message;
            _detailMessages.Insert(0, failureMessage);
            RaiseRuntimeInfoChanged();
        });
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

        details.Add(LocalizationManager.Translate("录制时长 ") +
                    $"{_finalElapsed:mm\\:ss} · {_capturedFrames.Count}" +
                    LocalizationManager.Translate("帧 · 目标 ") +
                    $"{profile.RecordingFrameRate}" +
                    LocalizationManager.Translate(" FPS · 实际约 ") +
                    $"{averageFps:F1} FPS");
        details.Add(LocalizationManager.Translate("采集诊断：请求 ") +
                    Interlocked.Read(ref _captureRequestCount) +
                    LocalizationManager.Translate(" 次 · 跳过 ") +
                    Interlocked.Read(ref _skippedCaptureCount) +
                    LocalizationManager.Translate(" 帧 · 失败 ") +
                    Interlocked.Read(ref _captureFailureCount) +
                    LocalizationManager.Translate(" 次 · JPEG ") +
                    FormatBytes(Interlocked.Read(ref _capturedFrameBytes)) +
                    LocalizationManager.Translate(" · 峰值工作集 ") +
                    FormatBytes(Interlocked.Read(ref _peakWorkingSetBytes)) +
                    LocalizationManager.Translate(" · 峰值私有内存 ") +
                    FormatBytes(Interlocked.Read(ref _peakPrivateBytes)));

        if (_detailMessages.Count > 0)
        {
            details.AddRange(_detailMessages);
        }

        return details.Count == 0 ? null : string.Join(Environment.NewLine, details);
    }

    private static string FormatBytes(long bytes) =>
        bytes <= 0 ? "0 B" : $"{bytes / 1024d / 1024d:F1} MB";

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

    private static bool TryValidateOutputDirectory(string outputDirectory, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            error = "输出目录为空，请在设置中指定一个有效的输出路径。";
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(outputDirectory);
            var root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrWhiteSpace(root))
            {
                error = "输出目录路径无效，请选择一个本地驱动器上的文件夹。";
                return false;
            }

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            var testFile = Path.Combine(fullPath, $"._writetest_{Guid.NewGuid():N}");
            try
            {
                File.WriteAllText(testFile, string.Empty);
                File.Delete(testFile);
            }
            catch
            {
                error = $"输出目录不可写：{fullPath}。请检查磁盘空间和文件夹权限。";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = $"输出目录验证失败：{ex.Message}";
            return false;
        }
    }

    private sealed record RecordingFrameInfo(string FileName, TimeSpan Offset);

    private sealed record AudioSegmentManifest(string Source, string FileName, double StartOffsetSeconds);

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
        IReadOnlyList<AudioSegmentManifest> AudioSegments,
        IReadOnlyList<RecordingFrameInfo> Frames,
        int CapturedFrameCount,
        long CaptureRequests,
        long SkippedFrames,
        long CaptureFailures,
        long CapturedFrameBytes,
        long PeakWorkingSetBytes,
        long PeakPrivateBytes);
}
