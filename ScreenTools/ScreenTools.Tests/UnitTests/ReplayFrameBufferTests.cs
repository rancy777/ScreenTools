using System;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class ReplayFrameBufferTests
{
    [Fact]
    public void Add_EnforcesByteBudgetByEvictingOldestFrames()
    {
        var buffer = new ReplayFrameBuffer(maxBytes: 10);
        var now = DateTimeOffset.UtcNow;
        var first = new ReplayBufferService.ReplayFrameSnapshot(now, new byte[6]);
        var second = new ReplayBufferService.ReplayFrameSnapshot(now.AddSeconds(1), new byte[6]);

        buffer.Add(first);
        buffer.Add(second);

        Assert.Equal(1, buffer.Count);
        Assert.Equal(6, buffer.BufferedBytes);
        Assert.Same(second, Assert.Single(buffer.SnapshotAfter(DateTimeOffset.MinValue)));
    }

    [Fact]
    public void SnapshotAfter_ReturnsOnlyFramesInsideRequestedWindowWithoutCopyingPayloads()
    {
        var buffer = new ReplayFrameBuffer();
        var now = DateTimeOffset.UtcNow;
        var oldBytes = new byte[] { 1, 2 };
        var currentBytes = new byte[] { 3, 4 };
        buffer.Add(new ReplayBufferService.ReplayFrameSnapshot(now.AddSeconds(-10), oldBytes));
        buffer.Add(new ReplayBufferService.ReplayFrameSnapshot(now, currentBytes));

        var snapshot = Assert.Single(buffer.SnapshotAfter(now.AddSeconds(-1)));

        Assert.Same(currentBytes, snapshot.JpegBytes);
        Assert.Equal(4, buffer.BufferedBytes);
    }

    [Fact]
    public void TrimOlderThanAndClear_KeepByteAccountingConsistent()
    {
        var buffer = new ReplayFrameBuffer();
        var now = DateTimeOffset.UtcNow;
        buffer.Add(new ReplayBufferService.ReplayFrameSnapshot(now.AddSeconds(-2), new byte[5]));
        buffer.Add(new ReplayBufferService.ReplayFrameSnapshot(now, new byte[7]));

        buffer.TrimOlderThan(now.AddSeconds(-1));
        Assert.Equal(1, buffer.Count);
        Assert.Equal(7, buffer.BufferedBytes);

        buffer.Clear();
        Assert.Equal(0, buffer.Count);
        Assert.Equal(0, buffer.BufferedBytes);
    }
}