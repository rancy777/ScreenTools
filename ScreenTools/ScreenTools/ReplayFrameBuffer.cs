using System;
using System.Collections.Generic;

namespace ScreenTools;

internal sealed class ReplayFrameBuffer
{
    /// <summary>
    /// Defines the DefaultMaxBytes value used by this component.
    /// </summary>
    public const long DefaultMaxBytes = 160L * 1024 * 1024;
    private readonly object _sync = new();
    private readonly LinkedList<ReplayBufferService.ReplayFrameSnapshot> _frames = [];
    private readonly long _maxBytes;
    private long _bufferedBytes;

    /// <summary>
    /// Creates a thread-safe frame buffer with the specified maximum retained byte count.
    /// </summary>
    public ReplayFrameBuffer(long maxBytes = DefaultMaxBytes)
    {
        if (maxBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        _maxBytes = maxBytes;
    }

    /// <summary>
    /// Gets the number of frames currently retained in the replay buffer.
    /// </summary>
    public int Count { get { lock (_sync) return _frames.Count; } }
    /// <summary>
    /// Gets the total JPEG payload size currently retained by the replay buffer.
    /// </summary>
    public long BufferedBytes { get { lock (_sync) return _bufferedBytes; } }

    /// <summary>
    /// Adds a timestamped JPEG frame and evicts the oldest frames until the configured memory limit is met.
    /// </summary>
    public void Add(ReplayBufferService.ReplayFrameSnapshot frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.JpegBytes is null || frame.JpegBytes.Length == 0) return;
        lock (_sync)
        {
            _frames.AddLast(frame);
            _bufferedBytes += frame.JpegBytes.LongLength;
            TrimToLimits(DateTimeOffset.MinValue);
        }
    }

    /// <summary>
    /// Returns a stable copy of all buffered frames at or after the supplied cutoff time.
    /// </summary>
    public ReplayBufferService.ReplayFrameSnapshot[] SnapshotAfter(DateTimeOffset cutoff)
    {
        lock (_sync)
        {
            var result = new List<ReplayBufferService.ReplayFrameSnapshot>(_frames.Count);
            for (var node = _frames.First; node is not null; node = node.Next)
                if (node.Value.CapturedAt >= cutoff) result.Add(node.Value);
            return result.ToArray();
        }
    }

    /// <summary>
    /// Removes buffered frames older than the supplied timestamp.
    /// </summary>
    public void TrimOlderThan(DateTimeOffset cutoff)
    {
        lock (_sync) TrimToLimits(cutoff);
    }

    /// <summary>
    /// Removes all buffered frames and resets the tracked memory usage.
    /// </summary>
    public void Clear()
    {
        lock (_sync)
        {
            _frames.Clear();
            _bufferedBytes = 0;
        }
    }

    private void TrimToLimits(DateTimeOffset cutoff)
    {
        while (_frames.First is not null && _frames.First.Value.CapturedAt < cutoff) RemoveOldest();
        while (_frames.First is not null && _bufferedBytes > _maxBytes) RemoveOldest();
    }

    private void RemoveOldest()
    {
        _bufferedBytes -= _frames.First!.Value.JpegBytes.LongLength;
        _frames.RemoveFirst();
    }
}
