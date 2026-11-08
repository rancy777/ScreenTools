using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace ScreenTools;

/// <summary>
/// Registers and manages application-wide keyboard shortcuts and low-level input hooks.
/// </summary>
public sealed class GlobalHotKeyManager : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmRButtonDown = 0x0204;
    private const int WmRButtonUp = 0x0205;
    private const int WmMButtonDown = 0x0207;
    private const int WmMButtonUp = 0x0208;
    private const int WmXButtonDown = 0x020B;
    private const int WmXButtonUp = 0x020C;
    private const int XButton1 = 0x0001;
    private const int XButton2 = 0x0002;

    private readonly Window _window;
    private readonly RecordingSessionState _session;
    private readonly HashSet<string> _pressedTokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<ShortcutAction> _armedActions = [];
    private readonly LowLevelProc _keyboardProc;
    private readonly LowLevelProc _mouseProc;
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private bool _disposed;

    /// <summary>
    /// Initializes global shortcut hooks for the supplied window and session settings.
    /// </summary>
    public GlobalHotKeyManager(Window window, RecordingSessionState session)
    {
        _window = window;
        _session = session;
        _keyboardProc = KeyboardHookCallback;
        _mouseProc = MouseHookCallback;
        _window.SourceInitialized += OnSourceInitialized;
        _window.Closed += (_, _) => Dispose();
    }

    /// <summary>
    /// Raised when a configured global shortcut is activated.
    /// </summary>
    public event EventHandler<ShortcutAction>? HotKeyPressed;
    /// <summary>
    /// Raised when Windows refuses to install a required low-level input hook.
    /// </summary>
    public event EventHandler<HookInstallationFailedEventArgs>? HookInstallationFailed;
    /// <summary>
    /// Raised when a configured shortcut conflicts with a system-reserved gesture.
    /// </summary>
    public event EventHandler<ShortcutAction>? HotKeyConflictDetected;

    /// <summary>
    /// Gets or sets whether global shortcut handling is temporarily suspended.
    /// </summary>
    public bool IsSuspended { get; set; }

    /// <summary>
    /// Releases native hooks, event subscriptions, and other resources owned by this component.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        UnhookWindowsHookEx(_keyboardHook);
        UnhookWindowsHookEx(_mouseHook);
        _window.SourceInitialized -= OnSourceInitialized;
        _pressedTokens.Clear();
        _armedActions.Clear();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (_keyboardHook != IntPtr.Zero || _mouseHook != IntPtr.Zero)
        {
            return;
        }

        _keyboardHook = SetHook(WhKeyboardLl, _keyboardProc);
        if (_keyboardHook == IntPtr.Zero)
        {
            HookInstallationFailed?.Invoke(this, new HookInstallationFailedEventArgs("keyboard", Marshal.GetLastWin32Error()));
        }

        _mouseHook = SetHook(WhMouseLl, _mouseProc);
        if (_mouseHook == IntPtr.Zero)
        {
            HookInstallationFailed?.Invoke(this, new HookInstallationFailedEventArgs("mouse", Marshal.GetLastWin32Error()));
        }
    }

    /// <summary>
    /// Describes a failed low-level keyboard or mouse hook installation.
    /// </summary>
    public sealed record HookInstallationFailedEventArgs(string HookType, int ErrorCode);

    private IntPtr KeyboardHookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            var message = wParam.ToInt32();
            var hookStruct = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            var token = NormalizeKeyToken(KeyInterop.KeyFromVirtualKey((int)hookStruct.vkCode));

            if (!string.IsNullOrWhiteSpace(token))
            {
                if (message is WmKeyDown or WmSysKeyDown)
                {
                    _pressedTokens.Add(token);
                    EvaluateBindings();
                }
                else if (message is WmKeyUp or WmSysKeyUp)
                {
                    _pressedTokens.Remove(token);
                    DisarmReleasedBindings();
                }
            }
        }

        return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            var message = wParam.ToInt32();
            var hookStruct = Marshal.PtrToStructure<MsLlHookStruct>(lParam);
            var token = message switch
            {
                WmLButtonDown or WmLButtonUp => "LeftMouse",
                WmRButtonDown or WmRButtonUp => "RightMouse",
                WmMButtonDown or WmMButtonUp => "MiddleMouse",
                WmXButtonDown or WmXButtonUp when HiWord(hookStruct.mouseData) == XButton1 => "XButton1",
                WmXButtonDown or WmXButtonUp when HiWord(hookStruct.mouseData) == XButton2 => "XButton2",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(token))
            {
                if (message is WmLButtonDown or WmRButtonDown or WmMButtonDown or WmXButtonDown)
                {
                    _pressedTokens.Add(token);
                    EvaluateBindings();
                }
                else
                {
                    _pressedTokens.Remove(token);
                    DisarmReleasedBindings();
                }
            }
        }

        return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }

    private void EvaluateBindings()
    {
        if (IsSuspended)
        {
            return;
        }

        foreach (var action in Enum.GetValues<ShortcutAction>())
        {
            var gesture = GetGesture(action);
            if (!gesture.Matches(_pressedTokens))
            {
                _armedActions.Remove(action);
                continue;
            }

            if (_armedActions.Contains(action))
            {
                continue;
            }

            _armedActions.Add(action);

            if (IsSystemReservedGesture(gesture))
            {
                Application.Current.Dispatcher.BeginInvoke(() => HotKeyConflictDetected?.Invoke(this, action));
                continue;
            }

            Application.Current.Dispatcher.BeginInvoke(() => HotKeyPressed?.Invoke(this, action));
        }
    }

    private static bool IsSystemReservedGesture(ShortcutGesture gesture)
    {
        var tokens = gesture.Tokens;
        return tokens.Any(token => token.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)) &&
               tokens.Any(token => token.Equals("Alt", StringComparison.OrdinalIgnoreCase)) &&
               tokens.Any(token => token.Equals("Delete", StringComparison.OrdinalIgnoreCase));
    }

    private void DisarmReleasedBindings()
    {
        foreach (var action in Enum.GetValues<ShortcutAction>())
        {
            if (!GetGesture(action).Matches(_pressedTokens))
            {
                _armedActions.Remove(action);
            }
        }
    }

    private ShortcutGesture GetGesture(ShortcutAction action)
    {
        return action switch
        {
            ShortcutAction.Screenshot => _session.ScreenshotShortcut,
            ShortcutAction.Recording => _session.RecordingShortcut,
            ShortcutAction.Replay => _session.ReplayShortcut,
            _ => new ShortcutGesture()
        };
    }

    private static string NormalizeKeyToken(Key key)
    {
        return key switch
        {
            Key.LeftCtrl or Key.RightCtrl => "Ctrl",
            Key.LeftAlt or Key.RightAlt => "Alt",
            Key.LeftShift or Key.RightShift => "Shift",
            Key.LWin or Key.RWin => "Win",
            Key.System => string.Empty,
            Key.None => string.Empty,
            _ => GetKeyLabel(key)
        };
    }

    private static string GetKeyLabel(Key key)
    {
        return key switch
        {
            >= Key.A and <= Key.Z => key.ToString().ToUpperInvariant(),
            >= Key.D0 and <= Key.D9 => key.ToString()[1..],
            >= Key.NumPad0 and <= Key.NumPad9 => key.ToString().Replace("NumPad", "Num"),
            Key.OemPlus => "=",
            Key.OemMinus => "-",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemQuestion => "/",
            Key.OemSemicolon => ";",
            Key.OemQuotes => "'",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            Key.OemPipe => "\\",
            Key.OemTilde => "`",
            Key.Space => "Space",
            Key.Return => "Enter",
            Key.Escape => "Esc",
            Key.Back => "Backspace",
            Key.Tab => "Tab",
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Left => "Left",
            Key.Right => "Right",
            _ when key >= Key.F1 && key <= Key.F24 => key.ToString().ToUpperInvariant(),
            _ => key.ToString()
        };
    }

    private static IntPtr SetHook(int hookId, LowLevelProc callback)
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        return SetWindowsHookEx(hookId, callback, GetModuleHandle(module?.ModuleName), 0);
    }

    private static int HiWord(uint value)
    {
        return (int)((value >> 16) & 0xffff);
    }

    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        /// <summary>
        /// Contains the virtual-key code reported by the Windows low-level keyboard hook.
        /// </summary>
        public uint vkCode;
        /// <summary>
        /// Contains the hardware scan code reported by the Windows low-level keyboard hook.
        /// </summary>
        public uint scanCode;
        /// <summary>
        /// Contains the native hook flags associated with this input event.
        /// </summary>
        public uint flags;
        /// <summary>
        /// Contains the timestamp supplied by the Windows input hook.
        /// </summary>
        public uint time;
        /// <summary>
        /// Contains the additional application-defined information supplied by the Windows hook.
        /// </summary>
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        /// <summary>
        /// Contains the horizontal coordinate in the native Windows hook point structure.
        /// </summary>
        public int X;
        /// <summary>
        /// Contains the vertical coordinate in the native Windows hook point structure.
        /// </summary>
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MsLlHookStruct
    {
        /// <summary>
        /// Contains the desktop coordinates reported by the Windows low-level mouse hook.
        /// </summary>
        public Point pt;
        /// <summary>
        /// Contains mouse-wheel or extended-button data reported by the Windows low-level mouse hook.
        /// </summary>
        public uint mouseData;
        /// <summary>
        /// Contains the native hook flags associated with this input event.
        /// </summary>
        public uint flags;
        /// <summary>
        /// Contains the timestamp supplied by the Windows input hook.
        /// </summary>
        public uint time;
        /// <summary>
        /// Contains the additional application-defined information supplied by the Windows hook.
        /// </summary>
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
