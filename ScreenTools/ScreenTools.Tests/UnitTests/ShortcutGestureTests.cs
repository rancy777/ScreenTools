using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class ShortcutGestureTests
{
    [Fact]
    public void FromTokens_CreatesGesture()
    {
        var gesture = ShortcutGesture.FromTokens("Ctrl", "Shift", "S");
        Assert.NotNull(gesture);
        Assert.Equal(3, gesture.Tokens.Count);
        Assert.Equal("Ctrl + Shift + S", gesture.DisplayText);
    }

    [Fact]
    public void CreateDefault_ReturnsExpectedGesture()
    {
        var gesture = ShortcutGesture.CreateDefault(ShortcutAction.Screenshot);
        Assert.NotNull(gesture);
        Assert.Equal("Alt + A", gesture.DisplayText);
    }
}
