# Collects the active Windows display topology and effective DPI for capture-validation evidence.
[CmdletBinding()]
param(
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms

$typeName = 'LensSnapDisplayDiagnostics.NativeDpi'
if (-not ($typeName -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

namespace LensSnapDisplayDiagnostics {
    public static class NativeDpi {
        [StructLayout(LayoutKind.Sequential)]
        public struct Point { public int X; public int Y; public Point(int x, int y) { X = x; Y = y; } }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(Point point, uint flags);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);

        public static string GetScale(int x, int y) {
            var monitor = MonitorFromPoint(new Point(x, y), 2);
            if (monitor == IntPtr.Zero) return "Unavailable";
            uint dpiX, dpiY;
            var result = GetDpiForMonitor(monitor, 0, out dpiX, out dpiY);
            if (result != 0) return "HRESULT 0x" + result.ToString("X8");
            return dpiX + "x" + dpiY + " DPI (" + (dpiX / 96.0 * 100.0).ToString("F0") + "%)";
        }
    }
}
'@
}

$monitors = @(
    [System.Windows.Forms.Screen]::AllScreens | ForEach-Object {
        $bounds = $_.Bounds
        $workArea = $_.WorkingArea
        [pscustomobject]@{
            Device = $_.DeviceName
            Primary = $_.Primary
            Bounds = [pscustomobject]@{ X = $bounds.X; Y = $bounds.Y; Width = $bounds.Width; Height = $bounds.Height }
            WorkingArea = [pscustomobject]@{ X = $workArea.X; Y = $workArea.Y; Width = $workArea.Width; Height = $workArea.Height }
            EffectiveDpi = [LensSnapDisplayDiagnostics.NativeDpi]::GetScale(
                [int]($bounds.Left + $bounds.Width / 2),
                [int]($bounds.Top + $bounds.Height / 2))
        }
    }
)

$left = ($monitors | ForEach-Object { $_.Bounds.X } | Measure-Object -Minimum).Minimum
$top = ($monitors | ForEach-Object { $_.Bounds.Y } | Measure-Object -Minimum).Minimum
$right = ($monitors | ForEach-Object { $_.Bounds.X + $_.Bounds.Width } | Measure-Object -Maximum).Maximum
$bottom = ($monitors | ForEach-Object { $_.Bounds.Y + $_.Bounds.Height } | Measure-Object -Maximum).Maximum
$os = Get-CimInstance Win32_OperatingSystem

$report = [pscustomobject]@{
    CollectedAt = (Get-Date).ToString('o')
    ComputerName = $env:COMPUTERNAME
    OS = $os.Caption
    OSVersion = $os.Version
    OSBuild = $os.BuildNumber
    ProcessArchitecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
    MonitorCount = $monitors.Count
    VirtualDesktopBounds = [pscustomobject]@{ X = $left; Y = $top; Width = ($right - $left); Height = ($bottom - $top) }
    Monitors = $monitors
}

$report | ConvertTo-Json -Depth 8
if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $fullPath = [System.IO.Path]::GetFullPath($OutputPath)
    $directory = [System.IO.Path]::GetDirectoryName($fullPath)
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $fullPath -Encoding UTF8
    Write-Host "Display diagnostics saved to $fullPath"
}
