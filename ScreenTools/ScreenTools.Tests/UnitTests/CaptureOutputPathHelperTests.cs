using System.IO;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class CaptureOutputPathHelperTests
{
    [Theory]
    [InlineData("C:\\Output", "PNG", ".PNG")]
    [InlineData("C:\\Output", "JPG", ".JPG")]
    [InlineData("C:\\Output", "BMP", ".BMP")]
    public void CreateScreenshotPath_FormatsExpected(string outputDirectory, string format, string expectedExtension)
    {
        var path = CaptureOutputPathHelper.CreateScreenshotPath(outputDirectory, format);
        Assert.Equal(expectedExtension, Path.GetExtension(path).ToUpperInvariant());
        Assert.StartsWith(outputDirectory, path);
    }
}
