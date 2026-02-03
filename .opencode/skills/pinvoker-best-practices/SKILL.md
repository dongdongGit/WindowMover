```markdown
---
name: pinvoker-best-practices
description: Windows P/Invoke patterns, marshaling, and best practices for user32.dll interop in WindowMover.
---

# P/Invoke Best Practices

## P/Invoke Declarations (Program.cs)
### Struct Definitions
```csharp
[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width() => Right - Left;
    public int Height() => Bottom - Top;
}

[StructLayout(LayoutKind.Sequential)]
public struct TITLEBARINFO
{
    public int cbSize;
    public RECT rcTitleBar;
    public uint rgstate;
}

[StructLayout(LayoutKind.Sequential)]
public struct MONITORINFO
{
    public int cbSize;
    public RECT rcMonitor;
    public RECT rcWork;
    public uint dwFlags;
}
```

### DLL Import Patterns
```csharp
public static class NativeMethods
{
    private const string User32 = "user32.dll";

    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport(User32, CharSet = CharSet.Auto)]
    public static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetTitleBarInfo(IntPtr hWnd, ref TITLEBARINFO pti);

    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hHook);

    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport(User32, SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport(User32, CharSet = CharSet.Auto)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport(User32, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
}
```

## Marshaling Best Practices
- **SetLastError = true**: Always set for functions that set errors
- **out parameters**: Use `out` for output structs (RECT, POINT)
- **ref parameters**: Use `ref` for input/output structs (TITLEBARINFO)
- **CharSet**: Use `CharSet.Auto` for automatic ANSI/Unicode
- **return types**: Use `[return: MarshalAs(...)]` for bool returns
- **Handle types**: Use `IntPtr` for HWND, HMODULE, etc.
- **StringBuilder**: Use for output string buffers

## Error Handling
```csharp
[DllImport("user32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

public static bool SafeSetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags)
{
    if (!SetWindowPos(hWnd, hWndInsertAfter, X, Y, cx, cy, uFlags))
    {
        int error = Marshal.GetLastWin32Error();
        Debug.WriteLine($"SetWindowPos failed: {error}");
        return false;
    }
    return true;
}
```

## Common P/Invoke Patterns
- **P/Invoke in static class**: Group all native methods
- **Constants**: Define constants in separate static class
- **Helper methods**: Wrap complex P/Invoke calls
- **GCHandle**: Prevent GC of delegate passed to SetWindowsHookEx
```
