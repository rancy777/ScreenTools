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

public sealed class ClipboardManagerService
{
    private const int MaxEntries = 8;
    private readonly string _historyPath;
    private readonly List<ClipboardEntry> _entries = [];

    public ClipboardManagerService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LensSnap");
        Directory.CreateDirectory(appDataPath);
        _historyPath = Path.Combine(appDataPath, "clipboard-history.json");
        Load();
    }

    public event EventHandler? HistoryChanged;

    public IReadOnlyList<ClipboardEntry> Entries => _entries;

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
        for (var attempt = 0; attempt < 5; attempt++)
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
            catch (Exception ex)
            {
                lastError = ex;
                break;
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
