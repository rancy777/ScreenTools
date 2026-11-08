[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputRoot,
    [string]$FfmpegPath = "",
    [string]$FfprobePath = "",
    [string]$ReportPath = ""
)

$ErrorActionPreference = "Stop"
$OutputRoot = (Resolve-Path -LiteralPath $OutputRoot).Path

if ([string]::IsNullOrWhiteSpace($FfmpegPath)) {
    $ffmpegCommand = Get-Command ffmpeg.exe -ErrorAction SilentlyContinue
    if ($ffmpegCommand) { $FfmpegPath = $ffmpegCommand.Source }
}
if ([string]::IsNullOrWhiteSpace($FfprobePath)) {
    $ffprobeCommand = Get-Command ffprobe.exe -ErrorAction SilentlyContinue
    if ($ffprobeCommand) { $FfprobePath = $ffprobeCommand.Source }
    elseif (-not [string]::IsNullOrWhiteSpace($FfmpegPath)) {
        $siblingProbe = Join-Path (Split-Path -Parent $FfmpegPath) "ffprobe.exe"
        if (Test-Path -LiteralPath $siblingProbe) { $FfprobePath = $siblingProbe }
    }
}

if (-not (Test-Path -LiteralPath $FfmpegPath -PathType Leaf)) { throw "ffmpeg.exe was not found. Pass -FfmpegPath." }
if (-not (Test-Path -LiteralPath $FfprobePath -PathType Leaf)) { throw "ffprobe.exe was not found. Pass -FfprobePath." }

$manifestFiles = @(Get-ChildItem -LiteralPath $OutputRoot -Filter recording.json -File -Recurse)
if ($manifestFiles.Count -eq 0) { throw "No recording.json manifests were found under '$OutputRoot'." }

$results = [System.Collections.Generic.List[object]]::new()
foreach ($manifestFile in $manifestFiles) {
    $artifactDirectory = $manifestFile.Directory.FullName
    $manifest = Get-Content -LiteralPath $manifestFile.FullName -Raw | ConvertFrom-Json
    $videoPath = Join-Path $artifactDirectory "recording.mp4"
    $status = "PASS"
    $checks = [System.Collections.Generic.List[string]]::new()
    $durationSeconds = $null
    $videoBytes = 0L
    $audioSegmentCount = @($manifest.AudioSegments).Count

    if (Test-Path -LiteralPath $videoPath -PathType Leaf) {
        $videoBytes = (Get-Item -LiteralPath $videoPath).Length
        $probeOutput = & $FfprobePath -v error -show_entries "format=duration:stream=codec_type" -of json $videoPath 2>&1
        if ($LASTEXITCODE -ne 0) {
            $status = "FAIL"
            $checks.Add("ffprobe failed: $probeOutput")
        }
        else {
            $probe = ($probeOutput -join [Environment]::NewLine) | ConvertFrom-Json
            $durationSeconds = [double]::Parse([string]$probe.format.duration, [Globalization.CultureInfo]::InvariantCulture)
            $streamTypes = @($probe.streams | ForEach-Object { $_.codec_type })
            if ($streamTypes -notcontains "video") { $status = "FAIL"; $checks.Add("MP4 has no video stream.") }
            else { $checks.Add("video stream present") }
            if ($audioSegmentCount -gt 0 -and $streamTypes -notcontains "audio") { $status = "FAIL"; $checks.Add("Audio segments exist in the manifest but MP4 has no audio stream.") }
            elseif ($streamTypes -contains "audio") { $checks.Add("audio stream present") }

            $expectedDuration = [double]$manifest.DurationSeconds
            $tolerance = [Math]::Max(1.0, $expectedDuration * 0.10)
            if ([Math]::Abs($durationSeconds - $expectedDuration) -gt $tolerance) {
                $status = "FAIL"
                $checks.Add(("duration mismatch: MP4 {0:F2}s vs manifest {1:F2}s (tolerance {2:F2}s)" -f $durationSeconds, $expectedDuration, $tolerance))
            }
            else { $checks.Add(("duration {0:F2}s within tolerance of manifest {1:F2}s" -f $durationSeconds, $expectedDuration)) }
        }

        $decodeOutput = & $FfmpegPath -v error -i $videoPath -f null - 2>&1
        if ($LASTEXITCODE -ne 0) {
            $status = "FAIL"
            $checks.Add("Full MP4 decode failed: $decodeOutput")
        }
        else { $checks.Add("full video/audio decode succeeded") }
    }
    elseif ($manifest.FrameSequencePreserved) {
        $frameFiles = @(Get-ChildItem -LiteralPath $artifactDirectory -Filter "frame-*.jpg" -File)
        $expectedFrames = [int]$manifest.CapturedFrameCount
        if ($frameFiles.Count -ne $expectedFrames -or $expectedFrames -eq 0) {
            $status = "FAIL"
            $checks.Add(("frame bundle incomplete: found {0}, manifest lists {1}" -f $frameFiles.Count, $expectedFrames))
        }
        else { $checks.Add(("frame bundle preserved: {0} JPEG frames" -f $frameFiles.Count)) }
    }
    else {
        $status = "FAIL"
        $checks.Add("Neither a playable MP4 nor a preserved frame sequence exists.")
    }

    foreach ($segment in @($manifest.AudioSegments)) {
        $audioPath = Join-Path $artifactDirectory ([string]$segment.FileName)
        if (-not (Test-Path -LiteralPath $audioPath -PathType Leaf) -or (Get-Item -LiteralPath $audioPath).Length -le 44) {
            $status = "FAIL"
            $checks.Add(("audio sidecar missing or empty: {0}" -f $segment.FileName))
        }
    }
    if ($audioSegmentCount -gt 0 -and $status -eq "PASS") { $checks.Add(("{0} audio sidecar(s) present" -f $audioSegmentCount)) }

    $results.Add([pscustomobject]@{
        Status = $status
        Artifact = $artifactDirectory
        DurationSeconds = $durationSeconds
        ManifestDurationSeconds = [double]$manifest.DurationSeconds
        FrameCount = [int]$manifest.CapturedFrameCount
        AudioSegments = $audioSegmentCount
        VideoBytes = $videoBytes
        Checks = ($checks -join " | ")
    })
}

$results | Format-Table Status, DurationSeconds, ManifestDurationSeconds, FrameCount, AudioSegments, VideoBytes, Artifact -AutoSize
if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
    $reportDirectory = Split-Path -Parent $ReportPath
    if (-not [string]::IsNullOrWhiteSpace($reportDirectory)) { New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null }
    $results | Export-Csv -LiteralPath $ReportPath -NoTypeInformation -Encoding UTF8
    Write-Host "Report written to $ReportPath"
}

$failed = @($results | Where-Object Status -ne "PASS")
Write-Host ("Validated {0} recording artifact(s): {1} passed, {2} failed." -f $results.Count, ($results.Count - $failed.Count), $failed.Count)
if ($failed.Count -gt 0) { exit 1 }