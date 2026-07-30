using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ScreenTools;

public sealed class FrameSequenceEncoder
{
    private readonly string? _ffmpegPath;

    public FrameSequenceEncoder()
    {
        _ffmpegPath = ResolveFfmpegPath();
    }

    public bool CanEncodeVideo => !string.IsNullOrWhiteSpace(_ffmpegPath);

    public string? FfmpegPath => _ffmpegPath;

    public string? TryEncodeMp4(string workingDirectory, string prefix, int frameRate, params string[] audioPaths)
    {
        if (string.IsNullOrWhiteSpace(_ffmpegPath))
        {
            return null;
        }

        var outputPath = Path.Combine(workingDirectory, $"{prefix}.mp4");
        var validAudioPaths = audioPaths
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var audioArguments = BuildAudioArguments(validAudioPaths);
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-y -framerate {frameRate} -i \"{Path.Combine(workingDirectory, "frame-%04d.jpg")}\" -c:v libx264 -pix_fmt yuv420p{audioArguments} \"{outputPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };

        process.Start();
        var stdErrTask = process.StandardError.ReadToEndAsync();
        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(stdErrTask, stdOutTask);

        if (process.ExitCode != 0 || !File.Exists(outputPath) || !IsValidMp4(outputPath))
        {
            var output = string.Join(
                Environment.NewLine,
                new[] { stdErrTask.Result, stdOutTask.Result }.Where(text => !string.IsNullOrWhiteSpace(text)));
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            throw new InvalidOperationException(string.IsNullOrWhiteSpace(output) ? "ffmpeg 导出 MP4 失败。" : output.Trim());
        }

        return outputPath;
    }

    private static bool IsValidMp4(string filePath)
    {
        try
        {
            Span<byte> header = stackalloc byte[12];
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Read(header) != 12)
            {
                return false;
            }

            // MP4 files start with an ftyp box: 4-byte size + "ftyp" + brand
            return header[4] == (byte)'f' &&
                   header[5] == (byte)'t' &&
                   header[6] == (byte)'y' &&
                   header[7] == (byte)'p';
        }
        catch
        {
            return false;
        }
    }

    private static string BuildAudioArguments(IReadOnlyList<string> audioPaths)
    {
        if (audioPaths.Count == 0)
        {
            return string.Empty;
        }

        if (audioPaths.Count == 1)
        {
            return $" -i \"{audioPaths[0]}\" -c:a aac -shortest";
        }

        var inputArguments = string.Join(" ", audioPaths.Select(path => $"-i \"{path}\""));
        var mixInputs = string.Concat(Enumerable.Range(1, audioPaths.Count).Select(index => $"[{index}:a]"));
        return $" {inputArguments} -filter_complex \"{mixInputs}amix=inputs={audioPaths.Count}:normalize=0[aout]\" -map \"[aout]\" -c:a aac -shortest";
    }

    private static string? ResolveFfmpegPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe"),
            @"C:\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe"
        };

        var directMatch = candidates.FirstOrDefault(File.Exists);
        if (!string.IsNullOrWhiteSpace(directMatch))
        {
            return directMatch;
        }

        var wingetRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "WinGet",
            "Packages");

        if (!Directory.Exists(wingetRoot))
        {
            return null;
        }

        var wingetMatch = Directory
            .EnumerateFiles(wingetRoot, "ffmpeg.exe", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(wingetMatch) && !IsPathInUserWritableDirectory(wingetMatch))
        {
            return wingetMatch;
        }

        return null;
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
            return true; // Treat as unsafe if we cannot determine
        }
    }
}
