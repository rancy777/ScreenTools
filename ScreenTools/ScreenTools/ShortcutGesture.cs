using System;
using System.Collections.Generic;
using System.Linq;

namespace ScreenTools;

public sealed class ShortcutGesture
{
    private static readonly string[] ModifierOrder = ["Ctrl", "Alt", "Shift", "Win"];

    public List<string> Tokens { get; set; } = [];

    public bool IsEmpty => Tokens.Count == 0;

    public string DisplayText => IsEmpty ? "未设置" : string.Join(" + ", Tokens);

    public bool Matches(IReadOnlySet<string> pressedTokens)
    {
        return Tokens.Count > 0 && Tokens.All(pressedTokens.Contains);
    }

    public ShortcutGesture Clone()
    {
        return new ShortcutGesture
        {
            Tokens = [.. Tokens]
        };
    }

    public static ShortcutGesture CreateDefault(ShortcutAction action)
    {
        return action switch
        {
            ShortcutAction.Screenshot => FromTokens("Alt", "A"),
            ShortcutAction.Recording => FromTokens("Alt", "R"),
            ShortcutAction.Replay => FromTokens("Alt", "S"),
            _ => new ShortcutGesture()
        };
    }

    public static ShortcutGesture FromTokens(params string[] tokens)
    {
        return FromEnumerable(tokens);
    }

    public static ShortcutGesture FromEnumerable(IEnumerable<string> tokens)
    {
        var normalized = tokens
            .Select(NormalizeToken)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(GetSortOrder)
            .ThenBy(token => token, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ShortcutGesture
        {
            Tokens = normalized
        };
    }

    public static bool IsModifier(string token)
    {
        return ModifierOrder.Contains(token, StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeToken(string token)
    {
        return token.Trim() switch
        {
            "Control" => "Ctrl",
            "LeftCtrl" => "Ctrl",
            "RightCtrl" => "Ctrl",
            "LeftAlt" => "Alt",
            "RightAlt" => "Alt",
            "LeftShift" => "Shift",
            "RightShift" => "Shift",
            "LWin" => "Win",
            "RWin" => "Win",
            var value => value
        };
    }

    private static int GetSortOrder(string token)
    {
        var index = Array.FindIndex(ModifierOrder, value => string.Equals(value, token, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : 100;
    }
}
