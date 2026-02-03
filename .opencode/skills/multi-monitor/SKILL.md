```markdown
---
name: multi-monitor
description: Multi-monitor management and relative window position preservation across displays.
---

# Multi-Monitor Management

## Windows Monitor API (Program.cs)
### Enumerate Monitors
- Use `EnumDisplayMonitors` to get all monitors
- Get monitor info via `GetMonitorInfo`
- Store monitor bounds in list for calculations

### Key APIs
- `EnumDisplayMonitors`
- `GetMonitorInfo`
- `MonitorFromWindow`
- `MonitorFromPoint`

### Calculate Relative Position
```csharp
public void MoveToMonitor(IntPtr hwnd, IntPtr targetMonitor)
{
    var currentMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
    var targetInfo = new MONITORINFO();
    GetMonitorInfo(targetMonitor, ref targetInfo);

    var windowRect = new RECT();
    GetWindowRect(hwnd, ref windowRect);
    var windowWidth = windowRect.Right - windowRect.Left;
    var windowHeight = windowRect.Bottom - windowRect.Top;

    var currentInfo = new MONITORINFO();
    GetMonitorInfo(currentMonitor, ref currentInfo);

    // Calculate relative position (0.0 = left/top, 1.0 = right/bottom)
    double relativeX = (windowRect.Left - currentInfo.rcWork.Left) /
                       (double)(currentInfo.rcWork.Right - currentInfo.rcWork.Left);
    double relativeY = (windowRect.Top - currentInfo.rcWork.Top) /
                       (double)(currentInfo.rcWork.Bottom - currentInfo.rcWork.Top);

    // Apply to new monitor
    int newX = targetInfo.rcWork.Left + (int)(relativeX * targetInfo.rcWork.Width());
    int newY = targetInfo.rcWork.Top + (int)(relativeY * targetInfo.rcWork.Height());

    MoveWindow(hwnd, newX, newY, windowWidth, windowHeight, true);
}
```

## macOS Monitor API (WindowMover.swift)
### Enumerate Screens
- Use `NSScreen.screens` to get all displays
- Each `NSScreen` has `frame` and `visibleFrame`
- Convert between coordinate systems

### Key APIs
- `NSScreen.screens`
- `NSScreen.frame`
- `CGGetOnlineDisplayList`
- `CGDisplayBounds`

### Coordinate Conversion
```swift
// NSScreen: Cocoa (Y increases downward, origin at bottom-left)
// CGDisplay/AX: Quartz (Y increases upward, origin at bottom-left)
func convertToQuartz(point: NSPoint, screen: NSScreen) -> CGPoint {
    let screenFrame = screen.frame
    let quartzY = screenFrame.height - point.y
    return CGPoint(x: point.x, y: quartzY)
}

func convertToCocoa(point: CGPoint, screen: NSScreen) -> NSPoint {
    let screenFrame = screen.frame
    let cocoaY = screenFrame.height - point.y
    return NSPoint(x: point.x, y: cocoaY)
}

func moveToNextMonitor(window: AXUIElement) {
    let currentScreen = NSScreen.main ?? NSScreen.screens.first!
    let allScreens = NSScreen.screens
    let currentIndex = allScreens.firstIndex(of: currentScreen) ?? 0
    let nextIndex = (currentIndex + 1) % allScreens.count
    let targetScreen = allScreens[nextIndex]

    guard let currentPosition = window.value(forAXAttribute: .position) as? CGPoint,
          let currentSize = window.value(forAXAttribute: .size) as? CGSize else { return }

    let currentScreenFrame = currentScreen.visibleFrame
    let targetScreenFrame = targetScreen.visibleFrame

    let relativeX = (currentPosition.x - currentScreenFrame.minX) / currentScreenFrame.width
    let relativeY = (currentPosition.y - currentScreenFrame.minY) / currentScreenFrame.height

    let newX = targetScreenFrame.minX + relativeX * targetScreenFrame.width
    let newY = targetScreenFrame.minY + relativeY * targetScreenFrame.height

    window.setValue(CGPoint(x: newX, y: newY), forAXAttribute: .position)
}
```

## Multi-Monitor Best Practices
- **Relative positioning**: Maintain window's relative position (e.g., 25% from left edge)
- **DPI awareness**: Account for different scaling between monitors on Windows
- **Taskbar avoidance**: Use `rcWork` instead of `rcMonitor` to avoid taskbar
- **Cursor position**: Use cursor position to determine target monitor
- **Edge cases**: Handle single monitor gracefully, validate bounds
- **Animation**: Consider smooth transition between monitors
```
