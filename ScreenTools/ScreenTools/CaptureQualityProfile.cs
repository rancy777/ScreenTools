using System;

namespace ScreenTools;

/// <summary>
/// Defines capture dimensions, frame rates, and JPEG quality for a named preset.
/// </summary>
public sealed record CaptureQualityProfile(string Name, int MaxWidth, int RecordingFrameRate, int ReplayFrameRate, long JpegQuality)
{
    /// <summary>
    /// Resolves a quality profile from a saved preset name, using the default profile for unknown values.
    /// </summary>
    public static CaptureQualityProfile FromPreset(string? preset)
    {
        var normalizedPreset = preset?.Trim() ?? string.Empty;
        return normalizedPreset.ToUpperInvariant() switch
        {
            "720P 30FPS" => new CaptureQualityProfile("720P 30FPS", 1280, 12, 4, 76L),
            "1440P 30FPS" => new CaptureQualityProfile("1440P 30FPS", 2560, 15, 5, 84L),
            _ => new CaptureQualityProfile("1080P 60FPS", 1920, 15, 5, 82L)
        };
    }
}
