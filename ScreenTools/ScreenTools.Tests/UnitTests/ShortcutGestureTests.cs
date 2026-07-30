using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class ShortcutGestureTests
{
    [Fact]
    public void Parse_ReturnsGesture_ForValidInput()
    {
        var gesture = ShortcutGesture.Parse("Ctrl+Shift+S");
        Assert.NotNull(gesture);
        Assert.Equal(ModifierKeys.Control | ModifierKeys.Shift, gesture.Modifiers);
        Assert.Equal(Key.S, gesture.Key);
    }

    [Fact]
    public void ToString_ReturnsReadableString()
    {
        var gesture = new ShortcutGesture(ModifierKeys.Control, Key.S);
        var text = gesture.ToString();
        Assert.Contains("Ctrl", text);
        Assert.Contains("S", text);
    }
}
