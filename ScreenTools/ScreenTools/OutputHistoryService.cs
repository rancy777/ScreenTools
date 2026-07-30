using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ScreenTools;

public sealed class OutputHistoryService
{
    private readonly string _historyPath;
    private readonly List<RecentOutputEntry> _entries = [];

    public OutputHistoryService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LensSnap");
        Directory.CreateDirectory(appDataPath);
        _historyPath = Path.Combine(appDataPath, "output-history.json");
        Load();
    }

    public event EventHandler? HistoryChanged;

    public IReadOnlyList<RecentOutputEntry> Entries => _entries;

    public void Add(CaptureArtifact artifact)
    {
        _entries.RemoveAll(entry => string.Equals(entry.OutputPath, artifact.OutputPath, StringComparison.OrdinalIgnoreCase));
        _entries.Insert(0, new RecentOutputEntry(
            artifact.Kind,
            artifact.OutputPath,
            DateTimeOffset.Now,
            artifact.IsVideo,
            artifact.FrameCount));

        if (_entries.Count > 8)
        {
            _entries.RemoveRange(8, _entries.Count - 8);
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
            var entries = JsonSerializer.Deserialize<List<RecentOutputEntry>>(File.ReadAllText(_historyPath));
            if (entries is null)
            {
                return;
            }

            _entries.Clear();
            _entries.AddRange(entries.Where(entry => !string.IsNullOrWhiteSpace(entry.OutputPath)));
        }
        catch
        {
            _entries.Clear();
        }
    }

    private void Save()
    {
        File.WriteAllText(
            _historyPath,
            JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true }));
    }
}
