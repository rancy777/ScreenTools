using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ScreenTools;

/// <summary>
/// Creates and cleans temporary workspaces used while recording and exporting.
/// </summary>
public sealed class TempWorkspaceService
{
    private readonly string _recordingWorkspaceRoot;

    /// <summary>
    /// Initializes the workspace service with the application default or a caller-supplied root.
    /// </summary>
    public TempWorkspaceService(string? recordingWorkspaceRoot = null)
    {
        _recordingWorkspaceRoot = string.IsNullOrWhiteSpace(recordingWorkspaceRoot)
            ? Path.Combine(Path.GetTempPath(), "LensSnap", "recording")
            : Path.GetFullPath(recordingWorkspaceRoot);
    }

    /// <summary>
    /// Gets the root directory used for temporary recording workspaces.
    /// </summary>
    public string RecordingWorkspaceRoot => _recordingWorkspaceRoot;

    /// <summary>
    /// Creates the temporary workspace required for recording and export.
    /// </summary>
    public void EnsureWorkspace()
    {
        Directory.CreateDirectory(RecordingWorkspaceRoot);
        HardenDirectoryPermissions(RecordingWorkspaceRoot);
    }

    /// <summary>
    /// Deletes recording workspaces older than the supplied age threshold.
    /// </summary>
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
                    DeleteDirectoryIfNotLocked(directory);
                }
            }
            catch
            {
            }
        }
    }

    private static void DeleteDirectoryIfNotLocked(string directoryPath)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(directoryPath))
            {
                try
                {
                    using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
                }
                catch (IOException)
                {
                    return;
                }
            }

            Directory.Delete(directoryPath, recursive: true);
        }
        catch
        {
        }
    }

    private static void HardenDirectoryPermissions(string directoryPath)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var accessControl = directoryInfo.GetAccessControl();
            var currentUser = WindowsIdentity.GetCurrent().Name;
            accessControl.SetAccessRule(new FileSystemAccessRule(
                currentUser,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));
            directoryInfo.SetAccessControl(accessControl);
        }
        catch
        {
            // Best-effort hardening.
        }
    }
}
