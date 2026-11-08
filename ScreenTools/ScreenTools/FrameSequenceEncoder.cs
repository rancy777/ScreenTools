using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenTools;

/// <summary>
/// Uses FFmpeg, when available, to encode captured frame sequences and audio into MP4.
/// </summary>
public sealed class FrameSequenceEncoder
{
    private static readonly TimeSpan DefaultEncodingTimeout = TimeSpan.FromMinutes(3);
    private readonly string? _ffmpegPath;

    /// <summary>
    /// Initializes the encoder and resolves an available trusted FFmpeg executable.
    /// </summary>
    public FrameSequenceEncoder()
        : this(ResolveFfmpegPath())
    {
    }

    /// <summary>
    /// Initializes the encoder and resolves an available trusted FFmpeg executable.
    /// </summary>
    public FrameSequenceEncoder(string? ffmpegPath)
    {
        _ffmpegPath = string.IsNullOrWhiteSpace(ffmpegPath) ? null : ffmpegPath;
    }

    /// <summary>
    /// Gets whether a trusted FFmpeg executable was found and video encoding is available.
    /// </summary>
    public bool CanEncodeVideo => !string.IsNullOrWhiteSpace(_ffmpegPath);

    /// <summary>
    /// Gets the resolved path to the FFmpeg executable, or null when unavailable.
    /// </summary>
    public string? FfmpegPath => _ffmpegPath;

    /// <summary>
    /// Encodes a numbered JPEG frame sequence and optional audio tracks into an MP4 file.
    /// </summary>
    public async Task<string?> TryEncodeMp4Async(
        string workingDirectory,
        string prefix,
        int frameRate,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null,
        params string[] audioPaths)
        => await TryEncodeMp4CoreAsync(
            workingDirectory,
            prefix,
            frameRate,
            audioPaths.Select(path => new AudioCaptureSegment(path, TimeSpan.Zero)).ToArray(),
            videoDuration: null,
            cancellationToken,
            timeout).ConfigureAwait(false);

    /// <summary>
    /// Encodes captured frames with audio segments placed on the shared recording timeline.
    /// </summary>
    public Task<string?> TryEncodeMp4WithAudioSegmentsAsync(
        string workingDirectory,
        string prefix,
        int frameRate,
        CancellationToken cancellationToken,
        IReadOnlyList<AudioCaptureSegment> audioSegments,
        TimeSpan videoDuration,
        TimeSpan? timeout = null)
    {
        if (audioSegments is null)
            throw new ArgumentNullException(nameof(audioSegments));
        if (videoDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(videoDuration));

        return TryEncodeMp4CoreAsync(
            workingDirectory,
            prefix,
            frameRate,
            audioSegments,
            videoDuration,
            cancellationToken,
            timeout);
    }

    private async Task<string?> TryEncodeMp4CoreAsync(
        string workingDirectory,
        string prefix,
        int frameRate,
        IReadOnlyList<AudioCaptureSegment> audioSegments,
        TimeSpan? videoDuration,
        CancellationToken cancellationToken,
        TimeSpan? timeout)
    {
        if (string.IsNullOrWhiteSpace(_ffmpegPath))
            return null;
        if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
            throw new DirectoryNotFoundException($"Frame workspace does not exist: {workingDirectory}");
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("An output prefix is required.", nameof(prefix));
        if (frameRate is < 1 or > 240)
            throw new ArgumentOutOfRangeException(nameof(frameRate));
        if (timeout is { } requestedTimeout && requestedTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "The FFmpeg timeout must be greater than zero.");

        cancellationToken.ThrowIfCancellationRequested();
        var outputPath = Path.Combine(workingDirectory, $"{prefix}.mp4");
        var validAudioSegments = audioSegments
            .Where(segment => !string.IsNullOrWhiteSpace(segment.Path) && File.Exists(segment.Path))
            .GroupBy(segment => segment.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var startInfo = BuildStartInfo(workingDirectory, outputPath, frameRate, validAudioSegments, videoDuration);

        try
        {
            var result = await RunProcessAsync(startInfo, timeout ?? DefaultEncodingTimeout, cancellationToken).ConfigureAwait(false);
            if (result.ExitCode != 0 || !File.Exists(outputPath) || !IsValidMp4(outputPath))
            {
                var output = string.Join(
                    Environment.NewLine,
                    new[] { result.StandardError, result.StandardOutput }.Where(value => !string.IsNullOrWhiteSpace(value)));
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(output)
                    ? $"ffmpeg failed to export MP4 (exit code {result.ExitCode})."
                    : output.Trim());
            }

            return outputPath;
        }
        catch
        {
            TryDeleteFile(outputPath);
            throw;
        }
    }

    internal static async Task<(int ExitCode, string StandardError, string StandardOutput)> RunProcessAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "The process timeout must be greater than zero.");
        cancellationToken.ThrowIfCancellationRequested();

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        if (!process.Start())
            throw new InvalidOperationException($"The process could not be started: {startInfo.FileName}");

        var stderrTask = process.StandardError.ReadToEndAsync();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        try
        {
            await process.WaitForExitAsync(linkedSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKillProcessTree(process);
            await process.WaitForExitAsync().ConfigureAwait(false);
            await Task.WhenAll(stderrTask, stdoutTask).ConfigureAwait(false);
            if (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                throw new TimeoutException($"进程超时：{startInfo.FileName}，超时时间为 {timeout.TotalSeconds:0.###} 秒。");
            throw;
        }

        await Task.WhenAll(stderrTask, stdoutTask).ConfigureAwait(false);
        return (process.ExitCode, stderrTask.Result, stdoutTask.Result);
    }

    private ProcessStartInfo BuildStartInfo(
        string workingDirectory,
        string outputPath,
        int frameRate,
        IReadOnlyList<AudioCaptureSegment> audioSegments,
        TimeSpan? videoDuration)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath!,
            WorkingDirectory = workingDirectory,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        void Add(string argument) => startInfo.ArgumentList.Add(argument);

        Add("-y");
        Add("-nostdin");
        Add("-hide_banner");
        Add("-loglevel");
        Add("error");
        Add("-framerate");
        Add(frameRate.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Add("-i");
        Add(Path.Combine(workingDirectory, "frame-%04d.jpg"));

        foreach (var audioSegment in audioSegments)
        {
            Add("-i");
            Add(audioSegment.Path);
        }

        Add("-c:v");
        Add("libx264");
        Add("-pix_fmt");
        Add("yuv420p");

        if (audioSegments.Count > 0)
        {
            var audioLabels = new List<string>(audioSegments.Count);
            var filters = new List<string>(audioSegments.Count + 1);
            for (var index = 0; index < audioSegments.Count; index++)
            {
                var label = $"audio{index}";
                var offsetSeconds = audioSegments[index].StartOffset.TotalSeconds;
                var timelineFilter = offsetSeconds < 0
                    ? $"atrim=start={(-offsetSeconds).ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)},asetpts=PTS-STARTPTS"
                    : $"adelay={Math.Round(offsetSeconds * 1000).ToString("0", System.Globalization.CultureInfo.InvariantCulture)}:all=1";
                filters.Add($"[{index + 1}:a]aresample=async=1:first_pts=0,{timelineFilter}[{label}]");
                audioLabels.Add($"[{label}]");
            }

            if (audioSegments.Count > 1)
            {
                filters.Add($"{string.Concat(audioLabels)}amix=inputs={audioSegments.Count}:duration=longest:dropout_transition=0:normalize=1[aout]");
                Add("-filter_complex");
                Add(string.Join(';', filters));
                Add("-map");
                Add("0:v:0");
                Add("-map");
                Add("[aout]");
            }
            else
            {
                Add("-filter_complex");
                Add(filters[0]);
                Add("-map");
                Add("0:v:0");
                Add("-map");
                Add("[audio0]");
            }

            Add("-c:a");
            Add("aac");
            Add("-b:a");
            Add("192k");
        }

        if (videoDuration is { } duration && duration > TimeSpan.Zero)
        {
            Add("-t");
            Add(duration.TotalSeconds.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture));
        }

        Add("-movflags");
        Add("+faststart");

        Add(outputPath);
        return startInfo;
    }

    private static bool IsValidMp4(string filePath)
    {
        try
        {
            Span<byte> header = stackalloc byte[12];
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return stream.Read(header) == 12 &&
                   header[4] == (byte)'f' && header[5] == (byte)'t' &&
                   header[6] == (byte)'y' && header[7] == (byte)'p';
        }
        catch
        {
            return false;
        }
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
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

    private static string? ResolveFfmpegPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe"),
            @"C:\ProgramData\chocolatey\bin\ffmpeg.exe",
            @"C:\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe"
        };

        var directMatch = candidates.FirstOrDefault(File.Exists);
        if (!string.IsNullOrWhiteSpace(directMatch))
            return directMatch;

        var pathMatch = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(directory => Path.Combine(directory, "ffmpeg.exe"))
            .FirstOrDefault(path => File.Exists(path) && !IsPathInUserWritableDirectory(path));
        if (!string.IsNullOrWhiteSpace(pathMatch))
            return pathMatch;

        var wingetRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WinGet", "Packages");
        if (!Directory.Exists(wingetRoot))
            return null;

        var wingetMatch = Directory.EnumerateFiles(wingetRoot, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
        return !string.IsNullOrWhiteSpace(wingetMatch) && !IsPathInUserWritableDirectory(wingetMatch)
            ? wingetMatch
            : null;
    }

    private static bool IsPathInUserWritableDirectory(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var tempPath = Path.GetTempPath();
            return fullPath.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase) ||
                   fullPath.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase) ||
                   fullPath.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }
}
