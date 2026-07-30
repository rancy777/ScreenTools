using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace ScreenTools;

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

    public GlobalHotKeyManager(Window window, RecordingSessionState session)
    {
        _window = window;
        _session = session;
        _keyboardProc = KeyboardHookCallback;
        _mouseProc = MouseHookCallback;
        _window.SourceInitialized += OnSourceInitialized;
        _window.Closed += (_, _) => Dispose();
    }

    public event EventHandler<ShortcutAction>? HotKeyPressed;

    public bool IsSuspended { get; set; }

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
        _mouseHook = SetHook(WhMouseLl, _mouseProc);
    }

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
            Application.Current.Dispatcher.BeginInvoke(() => HotKeyPressed?.Invoke(this, action));
        }
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
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MsLlHookStruct
    {
        public Point pt;
        public uint mouseData;
        public uint flags;
        public uint time;
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
