using System;
using System.IO;

namespace ScreenTools;

/// <summary>
/// Creates and validates the output locations used for captured artifacts.
/// </summary>
public static class CaptureOutputPathHelper
{
    /// <summary>
    /// Returns the default directory used for saved captures.
    /// </summary>
    public static string GetDefaultOutputDirectory()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var fallback = string.IsNullOrWhiteSpace(pictures)
            ? AppContext.BaseDirectory
            : pictures;
        return Path.Combine(fallback, "LensSnap");
    }

    /// <summary>
    /// Creates or resolves a valid output directory, falling back to the default when needed.
    /// </summary>
    public static string EnsureOutputDirectory(string? outputDirectory)
    {
        var resolvedPath = string.IsNullOrWhiteSpace(outputDirectory)
            ? GetDefaultOutputDirectory()
            : outputDirectory;

        Directory.CreateDirectory(resolvedPath);
        return resolvedPath;
    }

    /// <summary>
    /// Creates a unique file path for a screenshot with the requested extension.
    /// </summary>
    public static string CreateScreenshotPath(string? outputDirectory, string extension)
    {
        var baseDirectory = EnsureOutputDirectory(outputDirectory);
        var dayDirectory = Path.Combine(baseDirectory, "Screenshots", DateTime.Now.ToString("yyyyMMdd"));
        Directory.CreateDirectory(dayDirectory);
        return Path.Combine(dayDirectory, $"Screenshot-{DateTime.Now:yyyyMMdd-HHmmss}.{extension.ToLowerInvariant()}");
    }

    /// <summary>
    /// Creates a unique output directory for a recording or replay artifact.
    /// </summary>
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
