using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace ScreenTools;

/// <summary>
/// Captures the default Windows playback endpoint through WASAPI loopback without requiring Stereo Mix.
/// </summary>
public sealed class WasapiLoopbackCaptureService : IAudioCaptureService, IDisposable
{
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);
    private readonly List<string> _availableDevices = [];
    private readonly List<AudioCaptureSegment> _outputSegments = [];
    private string? _selectedDeviceId;
    private string? _selectedDeviceName;
    private string? _availabilityError;
    private string? _baseOutputPath;
    private int _segmentIndex;
    private bool _isPaused;
    private bool _stopRequested;
    private int _failureRaised;
    private int _stopInProgress;
    private WasapiLoopbackCapture? _capture;
    private WaveFileWriter? _waveWriter;
    private MMDevice? _device;
    private ManualResetEventSlim? _recordingStopped;
    private readonly ManualResetEventSlim _stopCompleted = new(true);

    /// <summary>Initializes system-audio capture and discovers the default playback endpoint.</summary>
    public WasapiLoopbackCaptureService() => RefreshAvailability();

    /// <inheritdoc />
    public bool IsSupported => !string.IsNullOrWhiteSpace(_selectedDeviceId);

    /// <inheritdoc />
    public bool IsCapturing => _capture is not null && _recordingStopped?.IsSet != true;

    /// <summary>Gets the active playback endpoint names, with the default endpoint first.</summary>
    public IReadOnlyList<string> AvailableDevices => _availableDevices;

    /// <summary>Gets the selected playback endpoint name.</summary>
    public string? SelectedDeviceName => _selectedDeviceName;

    /// <summary>Gets the selected Windows endpoint identifier.</summary>
    public string? SelectedDeviceId => _selectedDeviceId;

    /// <inheritdoc />
    public IReadOnlyList<AudioCaptureSegment> OutputSegments => _outputSegments;

    /// <inheritdoc />
    public event EventHandler<AudioCaptureFailedEventArgs>? CaptureFailed;

    /// <inheritdoc />
    public string AvailabilityMessage => IsSupported
        ? "系统声音将使用 WASAPI 回环采集，无需启用立体声混音。"
        : $"WASAPI 回环采集不可用：{_availabilityError ?? "Windows 未检测到活动的音频输出设备。"}";

    /// <inheritdoc />
    public void RefreshAvailability()
    {
        _availableDevices.Clear();
        _selectedDeviceId = null;
        _selectedDeviceName = null;
        _availabilityError = null;

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
            using var defaultEndpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            _selectedDeviceId = defaultEndpoint.ID;
            _selectedDeviceName = defaultEndpoint.FriendlyName;
            _availableDevices.Add(_selectedDeviceName);
            _availableDevices.AddRange(endpoints
                .Where(endpoint => !string.Equals(endpoint.ID, _selectedDeviceId, StringComparison.OrdinalIgnoreCase))
                .Select(endpoint => endpoint.FriendlyName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase));
            foreach (var endpoint in endpoints)
                endpoint.Dispose();
        }
        catch (Exception ex)
        {
            _availabilityError = ex.Message;
            _availableDevices.Clear();
            _selectedDeviceId = null;
            _selectedDeviceName = null;
        }
    }

    /// <inheritdoc />
    public void Start(string outputPath, TimeSpan timelineOffset = default)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("An output path is required.", nameof(outputPath));
        if (_capture is not null)
            throw new InvalidOperationException("系统声音采集已经启动。");

        if (!IsSupported)
            RefreshAvailability();
        if (!IsSupported)
            throw new InvalidOperationException(AvailabilityMessage);

        _baseOutputPath = Path.GetFullPath(outputPath);
        _outputSegments.Clear();
        _segmentIndex = 0;
        _isPaused = false;
        Interlocked.Exchange(ref _failureRaised, 0);
        StartSegment(timelineOffset);
    }

    /// <inheritdoc />
    public void Pause()
    {
        if (_capture is null)
        {
            _isPaused = true;
            return;
        }

        StopCurrentSegment();
        _isPaused = true;
    }

    /// <inheritdoc />
    public void Resume(TimeSpan timelineOffset = default)
    {
        if (!_isPaused || _capture is not null || string.IsNullOrWhiteSpace(_baseOutputPath))
            return;

        RefreshAvailability();
        if (!IsSupported)
            throw new InvalidOperationException(AvailabilityMessage);

        _segmentIndex++;
        Interlocked.Exchange(ref _failureRaised, 0);
        try
        {
            StartSegment(timelineOffset);
            _isPaused = false;
        }
        catch
        {
            _isPaused = true;
            throw;
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        StopCurrentSegment();
        _isPaused = false;
        _baseOutputPath = null;
    }

    /// <summary>Releases capture threads, endpoint handles, and file resources.</summary>
    public void Dispose()
    {
        try
        {
            Stop();
        }
        finally
        {
            _recordingStopped?.Dispose();
            _recordingStopped = null;
            _stopCompleted.Dispose();
        }
    }

    private void StartSegment(TimeSpan timelineOffset)
    {
        if (string.IsNullOrWhiteSpace(_baseOutputPath) || string.IsNullOrWhiteSpace(_selectedDeviceId))
            throw new InvalidOperationException("系统声音输出路径或播放设备尚未初始化。");

        var outputPath = BuildSegmentOutputPath(_baseOutputPath, _segmentIndex);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? AppContext.BaseDirectory);

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            try
            {
                _device = enumerator.GetDevice(_selectedDeviceId);
            }
            catch
            {
                _device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _selectedDeviceId = _device.ID;
                _selectedDeviceName = _device.FriendlyName;
            }
            if (_device.State != DeviceState.Active)
            {
                _device.Dispose();
                _device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _selectedDeviceId = _device.ID;
                _selectedDeviceName = _device.FriendlyName;
            }

            _capture = new WasapiLoopbackCapture(_device);
            _waveWriter = new WaveFileWriter(outputPath, _capture.WaveFormat);
            _recordingStopped?.Dispose();
            _recordingStopped = new ManualResetEventSlim(false);
            _stopRequested = false;
            _capture.DataAvailable += OnDataAvailable;
            _capture.RecordingStopped += OnRecordingStopped;
            _capture.StartRecording();
            _outputSegments.Add(new AudioCaptureSegment(outputPath, timelineOffset < TimeSpan.Zero ? TimeSpan.Zero : timelineOffset));
        }
        catch
        {
            try { ReleaseCaptureResources(); }
            finally { TryDeleteFile(outputPath); }
            throw;
        }
    }

    private void StopCurrentSegment()
    {
        _stopCompleted.Reset();
        if (Interlocked.CompareExchange(ref _stopInProgress, 1, 0) != 0)
        {
            _stopCompleted.Wait(StopTimeout);
            return;
        }

        var capture = _capture;
        if (capture is null)
        {
            try { ReleaseCaptureResources(); }
            finally
            {
                Interlocked.Exchange(ref _stopInProgress, 0);
                _stopCompleted.Set();
            }
            return;
        }

        _stopRequested = true;
        try
        {
            if (_recordingStopped?.IsSet != true)
                capture.StopRecording();
            if (_recordingStopped is not null && !_recordingStopped.Wait(StopTimeout))
                throw new TimeoutException("WASAPI 回环采集在五秒内未能确认停止。");
        }
        finally
        {
            try
            {
                ReleaseCaptureResources();
            }
            finally
            {
                _stopRequested = false;
                _stopCompleted.Set();
                Interlocked.Exchange(ref _stopInProgress, 0);
            }
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            if (e.BytesRecorded > 0)
                _waveWriter?.Write(e.Buffer, 0, e.BytesRecorded);
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
            if (_capture is not null)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try { StopCurrentSegment(); }
                    catch (Exception stopException) { ReportFailure(stopException); }
                });
            }
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _recordingStopped?.Set();
        if (e.Exception is not null)
        {
            ReportFailure(e.Exception);
            return;
        }

        if (!_stopRequested)
            ReportFailure(new InvalidOperationException("WASAPI 回环采集意外停止，视频录制将继续，但不再包含系统声音。"));
    }

    private void ReportFailure(Exception exception)
    {
        if (Interlocked.Exchange(ref _failureRaised, 1) != 0)
            return;

        try
        {
            CaptureFailed?.Invoke(this, new AudioCaptureFailedEventArgs("系统声音", exception));
        }
        catch
        {
            // An observer must not be allowed to crash the native audio callback thread.
        }
    }

    private void ReleaseCaptureResources()
    {
        var capture = _capture;
        _capture = null;
        try
        {
            if (capture is not null)
            {
                capture.DataAvailable -= OnDataAvailable;
                capture.RecordingStopped -= OnRecordingStopped;
                capture.Dispose();
            }
        }
        finally
        {
            try
            {
                _waveWriter?.Dispose();
            }
            finally
            {
                _waveWriter = null;
                _device?.Dispose();
                _device = null;
            }
        }
    }

    private static string BuildSegmentOutputPath(string outputPath, int segmentIndex)
    {
        if (segmentIndex == 0)
            return outputPath;

        var directory = Path.GetDirectoryName(outputPath) ?? AppContext.BaseDirectory;
        var fileName = Path.GetFileNameWithoutExtension(outputPath);
        var extension = Path.GetExtension(outputPath);
        return Path.Combine(directory, $"{fileName}-{segmentIndex:D2}{extension}");
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}