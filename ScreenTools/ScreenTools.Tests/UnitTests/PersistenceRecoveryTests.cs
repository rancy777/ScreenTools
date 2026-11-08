using System;
using System.IO;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class PersistenceRecoveryTests
{
    private static string CreatePath(string fileName) => Path.Combine(Path.GetTempPath(), "ScreenTools.Tests", Guid.NewGuid().ToString("N"), fileName);

    [Fact]
    public void AtomicFilePersistence_ReplacesExistingFileAndCleansTemporaryFiles()
    {
        var path = CreatePath("state.json");

        AtomicFilePersistence.WriteAllText(path, "first");
        AtomicFilePersistence.WriteAllText(path, "second");

        Assert.Equal("second", File.ReadAllText(path));
        Assert.Equal(new[] { "state.json" }, Array.ConvertAll(Directory.GetFiles(Path.GetDirectoryName(path)!), Path.GetFileName));
    }

    [Fact]
    public void ClipboardHistory_CorruptJsonIsPreservedForRecovery()
    {
        var path = CreatePath("clipboard.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "not-json");

        var service = new ClipboardManagerService(path);

        Assert.Empty(service.Entries);
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!, "clipboard.json.corrupt-*"));
    }
}