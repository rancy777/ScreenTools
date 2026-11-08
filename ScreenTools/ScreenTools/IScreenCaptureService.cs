using System;
using System.Windows;

namespace ScreenTools;

/// <summary>
/// Defines screenshot and frame-capture operations.
/// </summary>
public interface IScreenCaptureService
{
    /// <summary>Captures the configured screenshot target and saves it as an artifact.</summary>
    /// <param name="session">Settings that determine format, output directory, and capture behavior.</param>
    /// <returns>Metadata describing the saved screenshot.</returns>
    CaptureArtifact CaptureScreenshot(RecordingSessionState session);

    /// <summary>Captures and saves a selected rectangle of the virtual desktop.</summary>
    /// <param name="session">Settings that determine format and output directory.</param>
    /// <param name="region">The screen rectangle to capture in desktop coordinates.</param>
    /// <returns>Metadata describing the saved region screenshot.</returns>
    CaptureArtifact CaptureScreenshotRegion(RecordingSessionState session, Rect region);

    /// <summary>Captures the current desktop and writes a JPEG frame to disk.</summary>
    /// <param name="outputPath">Destination path for the JPEG frame.</param>
    /// <param name="qualityPreset">Name of the capture-quality preset to apply.</param>
    void CaptureFrameJpeg(string outputPath, string qualityPreset);

    /// <summary>Captures the current desktop and returns its JPEG-encoded bytes.</summary>
    /// <param name="qualityPreset">Name of the capture-quality preset to apply.</param>
    /// <returns>The encoded JPEG image data.</returns>
    byte[] CaptureFrameJpegBytes(string qualityPreset);
}
