using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class CaptureOutputPathHelperTests
{
    [Theory]
    [InlineData("C:\\Output", "PNG", "C:\\Output\\Screenshot_20240101_120000.png")]
    [InlineData("C:\\Output", "JPG", "C:\\Output\\Screenshot_20240101_120000.jpg")]
    [InlineData("C:\\Output", "BMP", "C:\\Output\\Screenshot_20240101_120000.bmp")]
    public void CreateScreenshotPath_FormatsExpected(string outputDirectory, string format, string expectedContainment)
    {
        var path = CaptureOutputPathHelper.CreateScreenshotPath(outputDirectory, format);
        Assert.Contains(Path.GetExtension(path).ToUpperInvariant(), expectedContainment.ToUpperInvariant());
        Assert.StartsWith(outputDirectory, path);
    }
}
