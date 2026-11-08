using System;
using System.IO;
using System.Text;

namespace ScreenTools;

internal static class AtomicFilePersistence
{
    /// <summary>
    /// Writes text through a temporary file and replaces the destination to avoid partial updates.
    /// </summary>
    public static void WriteAllText(string path, string contents)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A destination path is required.", nameof(path));

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The destination directory could not be resolved.");
        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            var bytes = Encoding.UTF8.GetBytes(contents);
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 16 * 1024, FileOptions.WriteThrough))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
