using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class OutputHistoryServiceTests
{
    [Fact]
    public void Add_InsertsEntryAtTop()
    {
        var service = new OutputHistoryService();
        var artifact = new CaptureArtifact("截图", "path1", 1, IsVideo: false);

        service.Add(artifact);

        Assert.Single(service.Entries);
        Assert.Equal("path1", service.Entries[0].OutputPath);
    }

    [Fact]
    public void Add_DuplicatePath_MovesToTop()
    {
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
        var service = new OutputHistoryService();
        var raised = false;
        service.HistoryChanged += (_, _) => raised = true;

        service.Add(new CaptureArtifact("截图", "path1", 1, IsVideo: false));

        Assert.True(raised);
    }
}
