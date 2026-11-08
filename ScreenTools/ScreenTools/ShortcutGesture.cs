using System;
using System.Collections.Generic;
using System.Linq;

namespace ScreenTools;

/// <summary>
/// Represents a keyboard shortcut as normalized key tokens.
/// </summary>
public sealed class ShortcutGesture
{
    private static readonly string[] ModifierOrder = ["Ctrl", "Alt", "Shift", "Win"];

    /// <summary>
    /// Gets or sets the normalized key tokens that make up this shortcut gesture.
    /// </summary>
    public List<string> Tokens { get; set; } = [];

    /// <summary>
    /// Gets whether this shortcut gesture contains no key tokens.
    /// </summary>
    public bool IsEmpty => Tokens.Count == 0;

    /// <summary>
    /// Gets a human-readable representation of the shortcut tokens.
    /// </summary>
    public string DisplayText => IsEmpty ? "未设置" : string.Join(" + ", Tokens);

    /// <summary>
    /// Determines whether the supplied pressed-key set matches this shortcut gesture.
    /// </summary>
    public bool Matches(IReadOnlySet<string> pressedTokens)
    {
        return Tokens.Count > 0 && Tokens.All(pressedTokens.Contains);
    }

    /// <summary>
    /// Creates an independent copy of this shortcut gesture and its token list.
    /// </summary>
    public ShortcutGesture Clone()
    {
        return new ShortcutGesture
        {
            Tokens = [.. Tokens]
        };
    }

    /// <summary>
    /// Creates the default shortcut gesture associated with the specified action.
    /// </summary>
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

    /// <summary>
    /// Creates a shortcut gesture from the supplied key tokens.
    /// </summary>
    public static ShortcutGesture FromTokens(params string[] tokens)
    {
        return FromEnumerable(tokens);
    }

    /// <summary>
    /// Creates a shortcut gesture from an enumerable sequence of key tokens.
    /// </summary>
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

    /// <summary>
    /// Reports whether a token represents a supported modifier key.
    /// </summary>
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
