using System;
using System.IO;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class OutputHistoryServiceTests
{
    private static string HistoryPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LensSnap", "output-history.json");

    [Fact]
    public void Add_InsertsEntryAtTop()
    {
        try
        {
            File.Delete(HistoryPath);
        }
        catch
        {
        }

        var service = new OutputHistoryService();
        var uniquePath = $"unique-{Guid.NewGuid():N}";
        var artifact = new CaptureArtifact("截图", uniquePath, 1, IsVideo: false);

        service.Add(artifact);

        Assert.Equal(uniquePath, service.Entries[0].OutputPath);
    }

    [Fact]
    public void Add_DuplicatePath_MovesToTop()
    {
        try
        {
            File.Delete(HistoryPath);
        }
        catch
        {
        }

        var service = new OutputHistoryService();
        service.Add(new CaptureArtifact("截图", "path1", 1, IsVideo: false));
        service.Add(new CaptureArtifact("截图", "path2", 1, IsVideo: false));
        service.Add(new CaptureArtifact("截图", "path1", 1, IsVideo: false));

        Assert.Equal(2, service.Entries.Count);
        Assert.Equal("path1", service.Entries[0].OutputPath);
    }

    [Fact]
    public void Add_MoreThanMaxEntries_TrimsOldest()
    {
        try
        {
            File.Delete(HistoryPath);
        }
        catch
        {
        }

        var service = new OutputHistoryService();
        for (var i = 0; i < 10; i++)
        {
            service.Add(new CaptureArtifact("截图", $"path{i}", 1, IsVideo: false));
        }

        Assert.Equal(8, service.Entries.Count);
    }

    [Fact]
    public void Add_RaisesHistoryChanged()
    {
        try
        {
            File.Delete(HistoryPath);
        }
        catch
        {
        }

        var service = new OutputHistoryService();
        var raised = false;
        service.HistoryChanged += (_, _) => raised = true;

        service.Add(new CaptureArtifact("截图", "path1", 1, IsVideo: false));

        Assert.True(raised);
    }

    [Fact]
    public void Load_EmptyFile_ReturnsEmptyEntries()
    {
        try
        {
            File.WriteAllText(HistoryPath, string.Empty);
        }
        catch
        {
        }

        var service = new OutputHistoryService();
        Assert.Empty(service.Entries);
    }
}
