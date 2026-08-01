using System;

namespace ScreenTools;

public interface IAudioCaptureService
{
    bool IsSupported { get; }
    string AvailabilityMessage { get; }
    void Start(string outputPath);
    void Pause();
    void Resume();
    void Stop();
}
