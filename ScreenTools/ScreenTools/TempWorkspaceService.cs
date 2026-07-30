using System;
using System.IO;

namespace ScreenTools;

public sealed class TempWorkspaceService
{
    public string RecordingWorkspaceRoot =>
        Path.Combine(Path.GetTempPath(), "LensSnap", "recording");

    public void EnsureWorkspace()
    {
        Directory.CreateDirectory(RecordingWorkspaceRoot);
    }

    public void CleanupStaleRecordingWorkspaces(TimeSpan maxAge)
    {
        if (!Directory.Exists(RecordingWorkspaceRoot))
        {
            return;
        }

        foreach (var directory in Directory.EnumerateDirectories(RecordingWorkspaceRoot))
        {
            try
            {
                var info = new DirectoryInfo(directory);
                if (DateTime.UtcNow - info.LastWriteTimeUtc > maxAge)
                {
                    info.Delete(recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
