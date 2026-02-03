```markdown
---
name: cross-platform-architecture
description: Cross-platform architecture design, code sharing strategies, and platform abstraction for WindowMover.
---

# Cross-Platform Architecture

## Project Structure
```
WindowMover/
├── WindowMover.csproj          # Windows .NET 8.0 WinForms
├── Program.cs                   # Windows core logic
├── MainForm.cs                  # Windows UI
├── WindowMoverMac/              # macOS Swift
│   ├── Makefile
│   ├── main.swift
│   ├── AppDelegate.swift
│   ├── WindowMover.swift
│   ├── AccessibilityHelper.swift
│   └── LoginItemHelper.swift
└── README.md
```

## Platform-Specific Implementations
### Shared Functionality
- Middle-click window dragging
- Multi-monitor support
- Title bar detection
- Settings persistence
- Startup at login

### Windows (C# / WinForms)
- **Core**: `WH_MOUSE_LL` hook in separate thread
- **Window Detection**: P/Invoke with user32.dll
- **UI**: WinForms NotifyIcon + Settings dialog
- **Storage**: Registry + Settings.settings

### macOS (Swift / Cocoa)
- **Core**: `CGEventTap` in main run loop
- **Window Detection**: Accessibility API (AXUIElement)
- **UI**: NSStatusItem + NSPopover
- **Storage**: UserDefaults + SMCopyAllJobDictionaries

## Architecture Comparison

### Windows Architecture
```
┌─────────────────┐
│   MainForm.cs   │  ← UI, Settings, Tray Icon
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Program.cs    │  ← Mouse Hook, Window Detection
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  user32.dll     │  ← P/Invoke
└─────────────────┘
```

### macOS Architecture
```
┌─────────────────┐
│ AppDelegate.swift│  ← Menu Bar, Lifecycle
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ WindowMover.swift│  ← Event Tap, Drag Logic
└────────┬────────┘
         │
         ▼
┌─────────────────────────┐
│ AccessibilityHelper.swift│  ← AXUIElement APIs
└─────────────────────────┘
```

## Key Design Differences
| Aspect | Windows | macOS |
|--------|---------|-------|
| Input Capture | `WH_MOUSE_LL` hook | `CGEventTap` |
| Window API | user32.dll P/Invoke | Accessibility API |
| Title Bar Detection | `WM_NCHITTEST` + heuristics | AXPosition + geometric |
| UI Framework | WinForms | Cocoa |
| Settings | Registry | UserDefaults |
| Startup | Registry Run key | Login Items (SMLoginItemSetEnabled) |

## Synchronization Strategy
- **Features**: Both platforms implement same feature set
- **Hotkeys**: Ctrl+Shift+M for monitor cycling
- **Settings**: Equivalent UI for enable/disable, startup
- **Icons**: Same icon design across platforms

## Potential Code Sharing
### Protocol/Interface Approach (Future)
```csharp
// Define shared interface
public interface IWindowMoverCore
{
    bool StartDragging(IntPtr window);
    bool MoveToMonitor(IntPtr window, int monitorIndex);
    bool IsOnTitleBar(IntPtr window, Point position);
}

// Platform-specific implementations
public class WindowsCore : IWindowMoverCore { ... }
```

### Build Time Options
- Use preprocessor directives for platform-specific code
- `#if NETFRAMEWORK` for Windows-specific features
- Consider .NET MAUI for future UI unification

## Development Workflow
1. Implement features independently per platform
2. Test each implementation thoroughly
3. Maintain feature parity
4. Share testing strategies across platforms
5. Document platform-specific quirks
```
