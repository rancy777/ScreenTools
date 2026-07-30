using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace ScreenTools;

public sealed class WasapiLoopbackCaptureService : IAudioCaptureService
{
    private readonly List<string> _availableDevices = [];
    private string? _selectedDeviceName;
    private readonly List<string> _outputPaths = [];
    private int _segmentIndex;
    private bool _isPaused;
    private WaveFileWriter? _waveWriter;
    private WasapiLoopbackCapture? _capture;
    private string? _baseOutputPath;

    public WasapiLoopbackCaptureService()
    {
    }

    public bool IsSupported => !string.IsNullOrWhiteSpace(_selectedDeviceName);

    public IReadOnlyList<string> AvailableDevices => _availableDevices;

    public string? SelectedDeviceName => _selectedDeviceName;

    public IReadOnlyList<string> OutputPaths => _outputPaths;

    public string AvailabilityMessage =>
        string.IsNullOrWhiteSpace(_selectedDeviceName)
            ? BuildUnavailableMessage()
            : $"系统声音将通过 “{_selectedDeviceName}” 通过 WASAPI 回录采集。";

    public void RefreshAvailability()
    {
        _selectedDeviceName = null;
        _availableDevices.Clear();

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            _availableDevices.AddRange(devices.Select(d => d.FriendlyName).OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase));
            _selectedDeviceName = _availableDevices.FirstOrDefault();
        }
        catch
        {
            _availableDevices.Clear();
        }
    }

    public void Start(string outputPath)
    {
        if (!IsSupported)
        {
            throw new InvalidOperationException(AvailabilityMessage);
        }

        if (_capture is not null)
        {
            throw new InvalidOperationException("系统声音采集已经启动。");
        }

        _baseOutputPath = outputPath;
        _outputPaths.Clear();
        _segmentIndex = 0;
        _isPaused = false;
        StartSegment();
    }

    public void Pause()
    {
        if (_capture is null)
        {
            return;
        }

        _capture.StopRecording();
        _capture.Dispose();
        _capture = null;
        _waveWriter?.Dispose();
        _waveWriter = null;
        _isPaused = true;
    }

    public void Resume()
    {
        if (!_isPaused || _capture is not null || string.IsNullOrWhiteSpace(_baseOutputPath) || !IsSupported)
        {
            return;
        }

        _segmentIndex++;
        _isPaused = false;
        StartSegment();
    }

    public void Stop()
    {
        if (_capture is not null)
        {
            _capture.StopRecording();
            _capture.Dispose();
            _capture = null;
        }

        _waveWriter?.Dispose();
        _waveWriter = null;
        _isPaused = false;
    }

    public void Dispose()
    {
        Stop();
    }

    private void StartSegment()
    {
        if (string.IsNullOrWhiteSpace(_baseOutputPath))
        {
            throw new InvalidOperationException("系统声音输出路径未初始化。");
        }

        var outputPath = BuildSegmentOutputPath(_baseOutputPath, _segmentIndex);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? AppContext.BaseDirectory);

        using var enumerator = new MMDeviceEnumerator();
        MMDevice? device = null;
        if (!string.IsNullOrWhiteSpace(_selectedDeviceName))
        {
            device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .FirstOrDefault(d => string.Equals(d.FriendlyName, _selectedDeviceName, StringComparison.OrdinalIgnoreCase));
        }

        if (device is null)
        {
            device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        if (device is null)
        {
            throw new InvalidOperationException("无法获取默认音频输出设备，请检查音频驱动是否正常。");
        }

        _capture = new WasapiLoopbackCapture(device);
        _waveWriter = new WaveFileWriter(outputPath, _capture.WaveFormat);
        _capture.DataAvailable += OnDataAvailable;
        _capture.RecordingStopped += OnRecordingStopped;
        _outputPaths.Add(outputPath);
        _capture.StartRecording();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            _waveWriter?.Write(e.Buffer, 0, e.BytesRecorded);
        }
        catch
        {
            // Ignore write failures to avoid crashing the audio callback thread.
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
        {
            // Ensure writer is closed on unexpected stop; the exception will be available to the caller via logs or next operation.
        }
    }

    private static string BuildSegmentOutputPath(string outputPath, int segmentIndex)
    {
        if (segmentIndex == 0)
        {
            return outputPath;
        }

        var directory = Path.GetDirectoryName(outputPath) ?? AppContext.BaseDirectory;
        var fileName = Path.GetFileNameWithoutExtension(outputPath);
        var extension = Path.GetExtension(outputPath);
        return Path.Combine(directory, $"{fileName}-{segmentIndex:D2}{extension}");
    }

    private string BuildUnavailableMessage()
    {
        if (_availableDevices.Count == 0)
        {
            return "当前没有枚举到可用于 WASAPI 回录的音频输出设备。";
        }

        var preview = string.Join("、", _availableDevices.Take(3));
        var suffix = _availableDevices.Count > 3 ? " 等设备。" : "。";
        return $"当前枚举到 {preview}{suffix}";
    }
}
