---
name: window-detection
description: Smart window detection and title bar identification algorithms for Windows and macOS.
---

# Window Detection & Title Bar Identification

## Windows Title Bar Detection (Program.cs)
### Standard Windows
- Use `WM_NCHITTEST` message to check for `HTCAPTION`
- Sends test message to window, returns hit test code
- `HTCAPTION` indicates title bar area

### File Explorer Tabs
- Standard `WM_NCHITTEST` fails on tab controls
- Use UI Automation to detect tab elements
- Check if mouse is over `TabItem` control within Explorer

### VS Code & Modern Apps
- Use geometric bounds checking
- Get window title bar rect via `GetTitleBarInfo`
- Check if mouse position is within title bar bounds

### Task Manager & System Apps
- Fallback to window title text matching
- Get window text via `GetWindowText`
- Apply heuristic rules for specific apps

## macOS Title Bar Detection (AccessibilityHelper.swift)
### Standard Cocoa Windows
- Get `AXWindow` element's `AXTitle` attribute
- Use `AXSize` and `AXPosition` to determine title bar bounds
- Check for `AXSubrole` = `AXStandardWindow`

### Chrome/Safari Tabs
- Depth-first search Accessibility tree
- Look for `AXTabButton` role elements
- Exclude tab bars from title bar detection
- Only intercept when clicking outside tab buttons

### Special Cases
- **Full-screen windows**: No title bar, skip detection
- **Floating windows**: May have reduced title bar
- **Tool windows**: Smaller title bar area

## Title Bar Detection Algorithm
```csharp
// Windows approach
public bool IsOnTitleBar(IntPtr hwnd, POINT cursorPos)
{
    // Method 1: WM_NCHITTEST
    var result = SendMessage(hwnd, WM_NCHITTEST, 0, MakeLParam(cursorPos.X, cursorPos.Y));
    if (result == HTCAPTION)
        return true;

    // Method 2: GetTitleBarInfo for modern apps
    var titleBarInfo = new TITLEBARINFO();
    titleBarInfo.cbSize = Marshal.SizeOf(titleBarInfo);
    GetTitleBarInfo(hwnd, ref titleBarInfo);
    return PtInRect(ref titleBarInfo.rcTitleBar, cursorPos);

    // Method 3: UI Automation for Edge/Explorer
    var element = AutomationElement.FromHandle(hwnd);
    var tabItem = element.FindFirst(TreeScope.Descendants,
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));
    if (tabItem != null && tabItem.Current.BoundingRectangle.Contains(cursorPos))
        return false;
}
```

```swift
// macOS approach
func isOnTitleBar(position: CGPoint) -> Bool {
    guard let windowElement = getFocusedWindowElement() else { return false }

    guard let title: String = windowElement.value(forAXAttribute: .title) else {
        return false
    }

    guard let positionValue: AnyObject = windowElement.value(forAXAttribute: .position),
          let sizeValue: AnyObject = windowElement.value(forAXAttribute: .size) else {
        return false
    }

    var windowPos = pointFromValue(positionValue)
    var windowSize = sizeFromValue(sizeValue)
    let titleBarHeight: CGFloat = 22.0

    let titleBarRect = CGRect(x: windowPos.x, y: windowPos.y,
                               width: windowSize.width, height: titleBarHeight)
    return titleBarRect.contains(position)
}
```

## Detection Priority
1. Check Accessibility permission is granted
2. Use platform-specific APIs first (NCHITTEST / AXPosition)
3. Fall back to geometric bounds checking
4. For special apps, use app-specific heuristics
5. Log detection method for debugging
