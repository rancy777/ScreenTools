[CmdletBinding()]
param(
    [int]$ProcessId = 0,
    [string]$ProcessName = "ScreenTools",
    [ValidateRange(1, 1440)]
    [int]$DurationMinutes = 15,
    [ValidateRange(1, 60)]
    [int]$SampleIntervalSeconds = 2,
    [string]$OutputCsv = ""
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($OutputCsv)) {
    $resultsDirectory = Join-Path (Split-Path -Parent $PSScriptRoot) "BenchmarkResults"
    New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null
    $OutputCsv = Join-Path $resultsDirectory ("recording-stability-{0}.csv" -f (Get-Date -Format "yyyyMMdd-HHmmss"))
}

if ($ProcessId -gt 0) {
    $process = Get-Process -Id $ProcessId -ErrorAction Stop
}
else {
    $processMatches = @(Get-Process -Name $ProcessName -ErrorAction Stop)
    if ($processMatches.Count -ne 1) {
        throw "Expected exactly one '$ProcessName' process, found $($processMatches.Count). Pass -ProcessId to select one explicitly."
    }
    $process = $processMatches[0]
}

$tempRoot = Join-Path $env:TEMP "LensSnap\recording"
$samples = [System.Collections.Generic.List[object]]::new()
$stopAt = (Get-Date).AddMinutes($DurationMinutes)
Write-Host ("Sampling PID {0} ({1}) every {2}s for up to {3} minute(s). Press Ctrl+C to stop." -f $process.Id, $process.ProcessName, $SampleIntervalSeconds, $DurationMinutes)

while ((Get-Date) -lt $stopAt) {
    try {
        $process.Refresh()
        if ($process.HasExited) { break }
        $tempBytes = 0L
        if (Test-Path -LiteralPath $tempRoot) {
            $tempBytes = (Get-ChildItem -LiteralPath $tempRoot -File -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
            if ($null -eq $tempBytes) { $tempBytes = 0L }
        }
        $samples.Add([pscustomobject]@{
            TimestampUtc = [DateTime]::UtcNow.ToString("o")
            ProcessId = $process.Id
            WorkingSetBytes = $process.WorkingSet64
            PrivateBytes = $process.PrivateMemorySize64
            PagedBytes = $process.PagedMemorySize64
            CpuSeconds = $process.CPU
            ThreadCount = $process.Threads.Count
            HandleCount = $process.HandleCount
            RecordingTempBytes = [long]$tempBytes
        })
    }
    catch [System.InvalidOperationException] {
        break
    }
    Start-Sleep -Seconds $SampleIntervalSeconds
}

if ($samples.Count -eq 0) { throw "No samples were collected; the process may have exited before sampling began." }
$samples | Export-Csv -LiteralPath $OutputCsv -NoTypeInformation -Encoding UTF8
$first = $samples[0]
$last = $samples[$samples.Count - 1]
$peakWorkingSet = ($samples | Measure-Object -Property WorkingSetBytes -Maximum).Maximum
$peakPrivate = ($samples | Measure-Object -Property PrivateBytes -Maximum).Maximum
$growth = [long]$last.PrivateBytes - [long]$first.PrivateBytes

[pscustomobject]@{
    ProcessId = $process.Id
    Samples = $samples.Count
    ElapsedMinutes = [Math]::Round(($samples.Count - 1) * $SampleIntervalSeconds / 60, 2)
    InitialWorkingSetMB = [Math]::Round($first.WorkingSetBytes / 1MB, 1)
    PeakWorkingSetMB = [Math]::Round($peakWorkingSet / 1MB, 1)
    InitialPrivateMB = [Math]::Round($first.PrivateBytes / 1MB, 1)
    PeakPrivateMB = [Math]::Round($peakPrivate / 1MB, 1)
    PrivateMemoryGrowthMB = [Math]::Round($growth / 1MB, 1)
    PeakRecordingTempMB = [Math]::Round((($samples | Measure-Object -Property RecordingTempBytes -Maximum).Maximum) / 1MB, 1)
    Csv = $OutputCsv
} | Format-List