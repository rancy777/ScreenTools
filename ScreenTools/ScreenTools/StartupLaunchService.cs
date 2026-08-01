using System;
using Microsoft.Win32;

namespace ScreenTools;

public sealed class StartupLaunchService
{
    private const string RegistryRunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "LensSnap";

    public bool IsEnabled()
    {
        try
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunPath, writable: false);
            return runKey?.GetValue(AppName) is string value && !string.IsNullOrWhiteSpace(value);
        }
        catch
        {
            return false;
        }
    }

    public void SetEnabled(bool enabled)
    {
        try
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RegistryRunPath);

            if (enabled)
            {
                var executablePath = Environment.ProcessPath ?? throw new InvalidOperationException("找不到应用程序路径。");
                runKey.SetValue(AppName, $"\"{executablePath}\"");
                return;
            }

            runKey.DeleteValue(AppName, throwOnMissingValue: false);
        }
        catch
        {
            // Startup registry access is best-effort; do not crash the application if the registry is inaccessible.
        }
    }
}
