using System;
using System.IO;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class FrameSequenceEncoderTests
{
    [Fact]
    public void IsValidMp4_ValidMp4Header_ReturnsTrue()
    {
        // Create a temporary file with a valid MP4 ftyp header
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D, 0x00, 0x00, 0x00, 0x01, 0x69, 0x73, 0x6F, 0x6D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            var method = typeof(FrameSequenceEncoder).GetMethod("IsValidMp4",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);
            var result = (bool)method.Invoke(null, new object[] { tempFile })!;
            Assert.True(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void IsValidMp4_InvalidHeader_ReturnsFalse()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            var method = typeof(FrameSequenceEncoder).GetMethod("IsValidMp4",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);
            var result = (bool)method.Invoke(null, new object[] { tempFile })!;
            Assert.False(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void IsValidMp4_EmptyFile_ReturnsFalse()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, string.Empty);
            var method = typeof(FrameSequenceEncoder).GetMethod("IsValidMp4",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);
            var result = (bool)method.Invoke(null, new object[] { tempFile })!;
            Assert.False(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
