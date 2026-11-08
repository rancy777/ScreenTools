using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ScreenTools;

/// <summary>
/// Maintains a bounded, persisted history of screenshot, recording, and replay outputs.
/// </summary>
public sealed class OutputHistoryService : IOutputHistoryService
{
    private const int MaxEntries = 8;
    private readonly string _historyPath;
    private readonly List<RecentOutputEntry> _entries = [];

    /// <summary>
    /// Initializes recent-output history and loads previously persisted entries.
    /// </summary>
    public OutputHistoryService(string? historyPath = null)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LensSnap");
        _historyPath = Path.GetFullPath(historyPath ?? Path.Combine(appDataPath, "output-history.json"));
        Load();
    }

    /// <summary>
    /// Raised when the corresponding history collection changes.
    /// </summary>
    public event EventHandler? HistoryChanged;
    /// <summary>
    /// Raised when updated history or settings could not be persisted.
    /// </summary>
    public event EventHandler<Exception>? PersistenceFailed;

    /// <summary>
    /// Gets the current history entries in newest-first order.
    /// </summary>
    public IReadOnlyList<RecentOutputEntry> Entries => _entries;

    /// <summary>
    /// Adds a capture artifact to the recent-output history and persists the updated list.
    /// </summary>
    public void Add(CaptureArtifact artifact)
    {
        _entries.RemoveAll(entry => string.Equals(entry.OutputPath, artifact.OutputPath, StringComparison.OrdinalIgnoreCase));
        _entries.Insert(0, new RecentOutputEntry(
            artifact.Kind,
            artifact.OutputPath,
            DateTimeOffset.Now,
            artifact.IsVideo,
            artifact.FrameCount));

        if (_entries.Count > MaxEntries)
        {
            _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);
        }

        Save();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Load()
    {
        if (!File.Exists(_historyPath))
        {
            return;
        }

        try
        {
            var content = File.ReadAllText(_historyPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            var entries = JsonSerializer.Deserialize<List<RecentOutputEntry>>(content);
            if (entries is null)
            {
                return;
            }

            _entries.Clear();
            _entries.AddRange(entries.Where(entry => entry is not null && !string.IsNullOrWhiteSpace(entry.OutputPath)).Take(MaxEntries));
        }
        catch (JsonException ex)
        {
            _entries.Clear();
            QuarantineCorruptedHistory();
            PersistenceFailed?.Invoke(this, ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _entries.Clear();
            PersistenceFailed?.Invoke(this, ex);
        }
    }

    private void Save()
    {
        try
        {
            AtomicFilePersistence.WriteAllText(
                _historyPath,
                JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            PersistenceFailed?.Invoke(this, ex);
        }
    }

    private void QuarantineCorruptedHistory()
    {
        try
        {
            if (File.Exists(_historyPath))
                File.Move(_historyPath, $"{_historyPath}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}", overwrite: false);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
