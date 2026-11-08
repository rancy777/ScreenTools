using System;
using System.IO;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class OutputHistoryServiceTests
{
    private static string CreateHistoryPath() => Path.Combine(Path.GetTempPath(), "ScreenTools.Tests", Guid.NewGuid().ToString("N"), "history.json");

    [Fact]
    public void Add_InsertsEntryAtTop_AndPersistsIt()
    {
        var path = CreateHistoryPath();
        var service = new OutputHistoryService(path);
        var uniquePath = $"unique-{Guid.NewGuid():N}";

        service.Add(new CaptureArtifact("Screenshot", uniquePath, 1, IsVideo: false));

        Assert.Equal(uniquePath, service.Entries[0].OutputPath);
        Assert.True(File.Exists(path));
        Assert.Equal(uniquePath, new OutputHistoryService(path).Entries[0].OutputPath);
    }

    [Fact]
    public void Add_DuplicatePath_MovesToTop()
    {
        var service = new OutputHistoryService(CreateHistoryPath());
        service.Add(new CaptureArtifact("Screenshot", "path1", 1, IsVideo: false));
        service.Add(new CaptureArtifact("Screenshot", "path2", 1, IsVideo: false));
        service.Add(new CaptureArtifact("Screenshot", "path1", 1, IsVideo: false));

        Assert.Equal(2, service.Entries.Count);
        Assert.Equal("path1", service.Entries[0].OutputPath);
    }

    [Fact]
    public void Add_MoreThanMaxEntries_TrimsOldest()
    {
        var service = new OutputHistoryService(CreateHistoryPath());
        for (var i = 0; i < 10; i++)
            service.Add(new CaptureArtifact("Screenshot", $"path{i}", 1, IsVideo: false));

        Assert.Equal(8, service.Entries.Count);
        Assert.Equal("path9", service.Entries[0].OutputPath);
        Assert.Equal("path2", service.Entries[^1].OutputPath);
    }

    [Fact]
    public void Add_RaisesHistoryChanged()
    {
        var service = new OutputHistoryService(CreateHistoryPath());
        var raised = false;
        service.HistoryChanged += (_, _) => raised = true;

        service.Add(new CaptureArtifact("Screenshot", "path1", 1, IsVideo: false));

        Assert.True(raised);
    }

    [Fact]
    public void Load_EmptyFile_ReturnsEmptyEntries()
    {
        var path = CreateHistoryPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, string.Empty);

        Assert.Empty(new OutputHistoryService(path).Entries);
    }

    [Fact]
    public void Load_CorruptJson_QuarantinesDamagedHistory()
    {
        var path = CreateHistoryPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ not valid json");

        var service = new OutputHistoryService(path);

        Assert.Empty(service.Entries);
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!, "history.json.corrupt-*"));
    }
}