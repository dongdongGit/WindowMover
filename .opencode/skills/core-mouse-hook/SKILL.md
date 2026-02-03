```markdown
---
name: core-mouse-hook
description: Cross-platform mouse middle-click capture and window dragging implementation for Windows (WH_MOUSE_LL) and macOS (CGEventTap).
---

# Core Mouse Hook Implementation

## Windows Implementation (Program.cs)
- **Hook Type**: `WH_MOUSE_LL` (Low-level Mouse Hook)
- **Setup**: Use `SetWindowsHookEx` with `WH_MOUSE_LL` flag
- **Callback**: Process `WM_MBUTTONDOWN` and `WM_MBUTTONUP` messages
- **Key APIs**:
  - `SetWindowsHookEx`
  - `CallNextHookEx`
  - `UnhookWindowsHookEx`
- **Thread Safety**: Use `GCHandle` to prevent garbage collection

## macOS Implementation (WindowMover.swift)
- **Tap Type**: `CGEventTapLocation.cgSessionEventTap`
- **Event Type**: `.maskMiddleMouseDown` and `.maskMiddleMouseUp`
- **Callback**: Process mouse events before they're delivered
- **Key APIs**:
  - `CGEvent.tapCreate`
  - `CGEvent.getIntegerValueField`
  - `CGEvent.setIntegerValueField`
- **RunLoop**: Register with `.commonModes` to work during drag operations

## Middle-Click Detection Logic
```csharp
// Windows
if (nCode == HC_ACTION && message == WM_MBUTTONDOWN)
{
    var hitTest = SendMessage(hwnd, WM_NCHITTEST, 0, lParam);
    if (hitTest == HTCAPTION)
    {
        StartDragging(hwnd);
    }
}
```

```swift
// macOS
if let event = CGEvent(tap: tap, eventSource: source) {
    let eventType = event.type
    if eventType == .middleMouseDown {
        if isOnTitleBar(mousePosition) {
            startDragging()
        }
    }
}
```

## Critical Implementation Notes
- **Windows**: Must pump messages correctly in separate thread
- **macOS**: Must restart tap if it becomes invalid (e.g., after sleep)
- **Both**: Only intercept when modifier keys are NOT pressed to avoid conflicts
- **Performance**: Keep callback processing minimal; post to main thread for heavy work
```
