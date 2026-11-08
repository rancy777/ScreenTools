using System.Diagnostics;
using System.IO;
using ScreenTools;

var mode = args.FirstOrDefault()?.Trim().ToLowerInvariant() ?? "capture";
var iterations = ReadIntArgument(args, "--iterations", 60, 1, 10000);
var csvPath = ReadStringArgument(args, "--csv");

Console.WriteLine("ScreenTools performance harness");
Console.WriteLine($"Mode: {mode}; iterations: {iterations}; runtime: {Environment.Version}; OS: {Environment.OSVersion}");

var rows = mode switch
{
    "capture" => RunCaptureBenchmark(iterations),
    "buffer" => RunReplayBufferBenchmark(iterations),
    "all" => RunCaptureBenchmark(iterations).Concat(RunReplayBufferBenchmark(iterations)).ToList(),
    _ => throw new ArgumentException("Mode must be 'capture', 'buffer', or 'all'.")
};

if (!string.IsNullOrWhiteSpace(csvPath))
{
    var fullPath = Path.GetFullPath(csvPath);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    File.WriteAllText(fullPath, "scenario,iteration,elapsed_ms,allocated_bytes,output_bytes,working_set_bytes" + Environment.NewLine +
        string.Join(Environment.NewLine, rows.Select(row => string.Join(',', row.Scenario, row.Iteration, row.ElapsedMs.ToString("F3"), row.AllocatedBytes, row.OutputBytes, row.WorkingSetBytes))));
    Console.WriteLine($"CSV: {fullPath}");
}

static List<BenchmarkRow> RunCaptureBenchmark(int iterations)
{
    Console.WriteLine("\nCapture + resize + JPEG encode (real desktop capture)");
    var service = new ScreenCaptureService();
    for (var i = 0; i < Math.Min(5, iterations); i++)
        _ = service.CaptureFrameJpegBytes("1080P 60FPS");

    GC.Collect();
    GC.WaitForPendingFinalizers();
    using var process = Process.GetCurrentProcess();
    var rows = new List<BenchmarkRow>(iterations);
    for (var i = 0; i < iterations; i++)
    {
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        process.Refresh();
        var workingSetBefore = process.WorkingSet64;
        var stopwatch = Stopwatch.StartNew();
        var bytes = service.CaptureFrameJpegBytes("1080P 60FPS");
        stopwatch.Stop();
        var allocated = Math.Max(0, GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore);
        process.Refresh();
        rows.Add(new BenchmarkRow("capture-jpeg", i + 1, stopwatch.Elapsed.TotalMilliseconds, allocated, bytes.LongLength, process.WorkingSet64));
        if (i == 0) Console.WriteLine($"JPEG payload: {bytes.Length:N0} bytes; working-set baseline: {workingSetBefore:N0} bytes");
    }

    PrintSummary(rows);
    return rows;
}

static List<BenchmarkRow> RunReplayBufferBenchmark(int iterations)
{
    Console.WriteLine("\nReplay buffer insertion + bounded-memory behavior (synthetic 512 KiB JPEG payloads)");
    var buffer = new ReplayFrameBuffer();
    var payload = new byte[512 * 1024];
    Random.Shared.NextBytes(payload);
    using var process = Process.GetCurrentProcess();
    var rows = new List<BenchmarkRow>(iterations);
    var started = DateTimeOffset.UtcNow;

    for (var i = 0; i < iterations; i++)
    {
        var stopwatch = Stopwatch.StartNew();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        buffer.Add(new ReplayBufferService.ReplayFrameSnapshot(started.AddMilliseconds(i * 200), payload.ToArray()));
        stopwatch.Stop();
        process.Refresh();
        rows.Add(new BenchmarkRow("replay-buffer-add", i + 1, stopwatch.Elapsed.TotalMilliseconds,
            Math.Max(0, GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore), buffer.BufferedBytes, process.WorkingSet64));
    }

    Console.WriteLine($"Retained frames: {buffer.Count:N0}; retained JPEG bytes: {buffer.BufferedBytes:N0}; configured cap: {ReplayFrameBuffer.DefaultMaxBytes:N0}");
    PrintSummary(rows);
    return rows;
}

static void PrintSummary(IReadOnlyList<BenchmarkRow> rows)
{
    var ordered = rows.Select(row => row.ElapsedMs).Order().ToArray();
    if (ordered.Length == 0) return;
    Console.WriteLine($"Samples: {ordered.Length}; mean: {ordered.Average():F2} ms; median: {Percentile(ordered, 0.50):F2} ms; p95: {Percentile(ordered, 0.95):F2} ms; max: {ordered[^1]:F2} ms");
}

static double Percentile(double[] sorted, double percentile)
{
    var index = Math.Clamp((int)Math.Ceiling(percentile * sorted.Length) - 1, 0, sorted.Length - 1);
    return sorted[index];
}

static int ReadIntArgument(string[] values, string name, int fallback, int minimum, int maximum)
{
    var index = Array.IndexOf(values, name);
    return index >= 0 && index + 1 < values.Length && int.TryParse(values[index + 1], out var parsed)
        ? Math.Clamp(parsed, minimum, maximum)
        : fallback;
}

static string? ReadStringArgument(string[] values, string name)
{
    var index = Array.IndexOf(values, name);
    return index >= 0 && index + 1 < values.Length ? values[index + 1] : null;
}

internal sealed record BenchmarkRow(string Scenario, int Iteration, double ElapsedMs, long AllocatedBytes, long OutputBytes, long WorkingSetBytes);