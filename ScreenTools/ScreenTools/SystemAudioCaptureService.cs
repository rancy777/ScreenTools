using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ScreenTools;

public sealed class SystemAudioCaptureService : IDisposable
{
    private readonly string? _ffmpegPath;
    private Process? _captureProcess;
    private string? _baseOutputPath;
    private string? _selectedDeviceName;
    private List<string> _availableDevices = [];
    private readonly List<string> _outputPaths = [];
    private int _segmentIndex;
    private bool _isPaused;

    public SystemAudioCaptureService(string? ffmpegPath)
    {
        _ffmpegPath = ffmpegPath;
    }

    public bool IsSupported => !string.IsNullOrWhiteSpace(_selectedDeviceName);

    public IReadOnlyList<string> AvailableDevices => _availableDevices;

    public string? SelectedDeviceName => _selectedDeviceName;

    public IReadOnlyList<string> OutputPaths => _outputPaths;

    public string AvailabilityMessage =>
        string.IsNullOrWhiteSpace(_ffmpegPath)
            ? "未找到 ffmpeg，无法采集系统声音。"
            : string.IsNullOrWhiteSpace(_selectedDeviceName)
                ? BuildUnavailableMessage()
                : $"系统声音将通过 “{_selectedDeviceName}” 采集。";

    public void RefreshAvailability()
    {
        _selectedDeviceName = null;
        _availableDevices = [];

        if (string.IsNullOrWhiteSpace(_ffmpegPath) || !File.Exists(_ffmpegPath))
        {
            return;
        }

        var output = RunFfmpegDeviceEnumeration();
        _availableDevices = ParseAudioDevices(output).ToList();
        _selectedDeviceName = _availableDevices.FirstOrDefault(IsLikelyLoopbackDevice);
    }

    public void Start(string outputPath)
    {
        if (!IsSupported)
        {
            throw new InvalidOperationException(AvailabilityMessage);
        }

        if (_captureProcess is not null)
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
        if (_captureProcess is null)
        {
            return;
        }

        StopCaptureProcess();
        _isPaused = true;
    }

    public void Resume()
    {
        if (!_isPaused || _captureProcess is not null || string.IsNullOrWhiteSpace(_baseOutputPath))
        {
            return;
        }

        _segmentIndex++;
        _isPaused = false;
        StartSegment();
    }

    public void Stop()
    {
        StopCaptureProcess();
        _isPaused = false;
    }

    public void Dispose()
    {
        Stop();
    }

    private string RunFfmpegDeviceEnumeration()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = "-list_devices true -f dshow -i dummy",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };

        process.Start();
        var output = process.StandardError.ReadToEnd() + Environment.NewLine + process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }

    private void StartSegment()
    {
        if (string.IsNullOrWhiteSpace(_baseOutputPath))
        {
            throw new InvalidOperationException("系统声音输出路径未初始化。");
        }

        var outputPath = BuildSegmentOutputPath(_baseOutputPath, _segmentIndex);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? AppContext.BaseDirectory);
        _outputPaths.Add(outputPath);
        _captureProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-y -f dshow -i audio=\"{_selectedDeviceName}\" -acodec pcm_s16le -ar 44100 -ac 2 \"{outputPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };

        _captureProcess.Start();
    }

    private void StopCaptureProcess()
    {
        if (_captureProcess is null)
        {
            return;
        }

        try
        {
            if (!_captureProcess.HasExited)
            {
                _captureProcess.StandardInput.WriteLine("q");
                if (!_captureProcess.WaitForExit(5000))
                {
                    _captureProcess.Kill(entireProcessTree: true);
                }
            }
        }
        finally
        {
            _captureProcess.Dispose();
            _captureProcess = null;
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

    private static IReadOnlyList<string> ParseAudioDevices(string output)
    {
        var matches = Regex.Matches(output, "\"(?<name>[^\"]+)\" \\(audio\\)");
        return matches
            .Select(match => match.Groups["name"].Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsLikelyLoopbackDevice(string deviceName)
    {
        var name = deviceName.ToLowerInvariant();
        return name.Contains("stereo mix")
            || name.Contains("立体声混音")
            || name.Contains("wave out")
            || name.Contains("what u hear")
            || name.Contains("loopback")
            || name.Contains("mix");
    }

    private string BuildUnavailableMessage()
    {
        if (_availableDevices.Count == 0)
        {
            return "当前没有枚举到可用于 ffmpeg 的音频输入设备。";
        }

        var preview = string.Join("、", _availableDevices.Take(3));
        var suffix = _availableDevices.Count > 3 ? " 等设备，但没有发现回放/loopback 输入。" : "，但没有发现回放/loopback 输入。";
        return $"当前枚举到 {preview}{suffix}";
    }
}
