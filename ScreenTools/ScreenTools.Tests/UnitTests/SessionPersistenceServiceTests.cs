using System;
using System.IO;
using System.Linq;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class SessionPersistenceServiceTests
{
    private static string CreateSettingsPath() => Path.Combine(Path.GetTempPath(), "ScreenTools.Tests", Guid.NewGuid().ToString("N"), "settings.json");

    [Fact]
    public void SaveAndLoad_RoundTripsUserPreferences()
    {
        var path = CreateSettingsPath();
        var service = new SessionPersistenceService(path);
        var state = new RecordingSessionState
        {
            ScreenshotFormat = "JPG",
            IncludeMicrophone = false,
            IncludeSystemAudio = true,
            ReplayBufferEnabled = false,
            ReplaySeconds = 60,
            OutputDirectory = Path.GetTempPath(),
            Language = LocalizationManager.French,
            QualityPreset = "720P 30FPS"
        };

        service.Save(state);
        var loaded = new SessionPersistenceService(path).Load();

        Assert.Equal("JPG", loaded.ScreenshotFormat);
        Assert.False(loaded.IncludeMicrophone);
        Assert.True(loaded.IncludeSystemAudio);
        Assert.False(loaded.ReplayBufferEnabled);
        Assert.Equal(60, loaded.ReplaySeconds);
        Assert.Equal(state.OutputDirectory, loaded.OutputDirectory);
        Assert.Equal(LocalizationManager.French, loaded.Language);
        Assert.Equal("720P 30FPS", loaded.QualityPreset);
    }

    [Fact]
    public void Load_CorruptJson_QuarantinesOriginalAndReturnsDefaults()
    {
        var path = CreateSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ this is not valid json");
        var corruptedRaised = false;
        var service = new SessionPersistenceService(path);
        service.SessionCorrupted += (_, _) => corruptedRaised = true;

        var loaded = service.Load();

        Assert.True(corruptedRaised);
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!, "settings.json.corrupt-*"));
        Assert.Equal("PNG", loaded.ScreenshotFormat);
    }

    [Fact]
    public void Load_ClampsInvalidReplayDurationAndNormalizesUnsupportedValues()
    {
        var path = CreateSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, """
            { "ScreenshotFormat": "GIF", "QualityPreset": "invalid", "ReplaySeconds": 9999 }
            """);

        var loaded = new SessionPersistenceService(path).Load();

        Assert.Equal("PNG", loaded.ScreenshotFormat);
        Assert.Equal("1080P 60FPS", loaded.QualityPreset);
        Assert.Equal(120, loaded.ReplaySeconds);
    }
}