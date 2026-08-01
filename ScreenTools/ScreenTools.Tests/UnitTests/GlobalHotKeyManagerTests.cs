using System;
using System.IO;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class GlobalHotKeyManagerTests
{
    [Fact]
    public void IsSystemReservedGesture_CtrlAltDelete_ReturnsTrue()
    {
        var method = typeof(GlobalHotKeyManager).GetMethod("IsSystemReservedGesture",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var gesture = ShortcutGesture.FromTokens("Ctrl", "Alt", "Delete");
        var result = (bool)method.Invoke(null, new object[] { gesture })!;
        Assert.True(result);
    }

    [Fact]
    public void IsSystemReservedGesture_AltA_ReturnsFalse()
    {
        var method = typeof(GlobalHotKeyManager).GetMethod("IsSystemReservedGesture",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var gesture = ShortcutGesture.FromTokens("Alt", "A");
        var result = (bool)method.Invoke(null, new object[] { gesture })!;
        Assert.False(result);
    }
}
