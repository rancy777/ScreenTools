using System;
using System.IO;

namespace ScreenTools;

public static class CaptureOutputPathHelper
{
    public static string GetDefaultOutputDirectory()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var fallback = string.IsNullOrWhiteSpace(pictures)
            ? AppContext.BaseDirectory
            : pictures;
        return Path.Combine(fallback, "LensSnap");
    }

    public static string EnsureOutputDirectory(string? outputDirectory)
    {
        var resolvedPath = string.IsNullOrWhiteSpace(outputDirectory)
            ? GetDefaultOutputDirectory()
            : outputDirectory;

        Directory.CreateDirectory(resolvedPath);
        return resolvedPath;
    }

    public static string CreateScreenshotPath(string? outputDirectory, string extension)
    {
        var baseDirectory = EnsureOutputDirectory(outputDirectory);
        var dayDirectory = Path.Combine(baseDirectory, "Screenshots", DateTime.Now.ToString("yyyyMMdd"));
        Directory.CreateDirectory(dayDirectory);
        return Path.Combine(dayDirectory, $"Screenshot-{DateTime.Now:yyyyMMdd-HHmmss}.{extension.ToLowerInvariant()}");
    }

    public static string CreateArtifactDirectory(string? outputDirectory, string category, string prefix)
    {
        var baseDirectory = EnsureOutputDirectory(outputDirectory);
        var dayDirectory = Path.Combine(baseDirectory, category, DateTime.Now.ToString("yyyyMMdd"));
        var artifactDirectory = Path.Combine(
            dayDirectory,
            $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}".Substring(0, $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}-".Length + 8));
        Directory.CreateDirectory(artifactDirectory);
        return artifactDirectory;
    }
}
