using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ScreenTools;

/// <summary>
/// Manages clipboard history, image copying, and durable history persistence.
/// </summary>
public sealed class ClipboardManagerService : IClipboardManagerService
{
    private const int MaxEntries = 8;
    private const string CustomFormatName = "LensSnap.ClipboardEntry.v1";
    private readonly string _historyPath;
    private readonly List<ClipboardEntry> _entries = [];

    /// <summary>
    /// Initializes clipboard history and loads any previously persisted entries.
    /// </summary>
    public ClipboardManagerService(string? historyPath = null)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LensSnap");
        _historyPath = Path.GetFullPath(historyPath ?? Path.Combine(appDataPath, "clipboard-history.json"));
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
    public IReadOnlyList<ClipboardEntry> Entries => _entries;

    /// <summary>
    /// Loads an image file and attempts to place its bitmap on the Windows clipboard.
    /// </summary>
    public ClipboardCopyResult CopyImageFromFile(string imagePath, string kind)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return Register(kind, imagePath, false, "源文件不存在，请重新截图后再试。");
        }

        BitmapSource bitmap;
        try
        {
            bitmap = LoadBitmap(imagePath);
        }
        catch (Exception ex)
        {
            return Register(kind, imagePath, false, $"图片读取失败：{ex.Message}");
        }

        Exception? lastError = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Clipboard.SetImage(bitmap);
                return Register(kind, imagePath, true, "已复制到系统剪贴板。");
            }
            catch (ExternalException ex)
            {
                lastError = ex;
                Thread.Sleep(60 * (attempt + 1));
            }
        }

        return Register(kind, imagePath, false, GetFailureDetail(lastError));
    }

    private static BitmapSource LoadBitmap(string imagePath)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private ClipboardCopyResult Register(string kind, string imagePath, bool success, string detail)
    {
        _entries.RemoveAll(entry => string.Equals(entry.OutputPath, imagePath, StringComparison.OrdinalIgnoreCase));
        var entry = new ClipboardEntry(
            kind,
            imagePath,
            DateTimeOffset.Now,
            success,
            detail);
        _entries.Insert(0, entry);

        if (_entries.Count > MaxEntries)
        {
            _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);
        }

        Save();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        return new ClipboardCopyResult(success, detail, entry);
    }

    private void Load()
    {
        if (!File.Exists(_historyPath))
        {
            return;
        }

        try
        {
            var entries = JsonSerializer.Deserialize<List<ClipboardEntry>>(File.ReadAllText(_historyPath));
            if (entries is null)
            {
                return;
            }

            _entries.Clear();
            _entries.AddRange(entries
                .Where(entry => entry is not null && !string.IsNullOrWhiteSpace(entry.OutputPath))
                .GroupBy(entry => entry.OutputPath, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Take(MaxEntries));
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

    private static string GetFailureDetail(Exception? error)
    {
        return error switch
        {
            ExternalException => "系统剪贴板正被其他程序占用，请重试一次。",
            not null when !string.IsNullOrWhiteSpace(error.Message) => error.Message,
            _ => "写入系统剪贴板失败。"
        };
    }
}
