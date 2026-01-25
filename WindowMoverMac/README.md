# Window Mover (macOS)

A native Swift port of the Window Mover tool. This utility allows you to move windows between displays by simply clicking the **Middle Mouse Button** on the window's title bar.

## Architecture

The application is built using native macOS APIs:

1.  **Cocoa / AppKit**: For the Menu Bar icon (`NSStatusItem`) and application lifecycle management.
2.  **Core Graphics (`CGEvent`)**: To create a global event tap that intercepts mouse clicks (specifically `kCGEventOtherMouseDown`) before they reach other applications.
3.  **Accessibility API (`AXUIElement`)**: To determine which window is under the mouse, check if the click is within the title bar area, and move the window to a different display.

## Requirements

- macOS 10.13 or later
- **Accessibility Permissions**: Because this app controls other windows, you must grant it permission in `System Settings` -> `Privacy & Security` -> `Accessibility`.

## Project Structure

```text
WindowMoverMac/
├── main.swift              # Entry point, sets up NSApplication
├── AppDelegate.swift       # Manages the Menu Bar icon and app state
├── WindowMover.swift       # Core logic: Event Tap and Window Moving
├── AccessibilityHelper.swift # Wrappers for AXUIElement APIs
├── Info.plist              # App Bundle metadata
├── Makefile                # Build script for creating .app bundle
└── Package.swift           # Swift Package Manager manifest
```

## Build Instructions

### Option 1: Create Application Bundle (Recommended)

To create a proper macOS Application Bundle (`WindowMover.app`) that can be moved to your Applications folder:

1. Open Terminal.
2. Navigate to the `WindowMoverMac` directory.
3. Run `make`.

```bash
cd WindowMoverMac
make
```

This will create `WindowMover.app` in the current directory.

### Option 2: Swift Package Manager (For Development)

If you want to edit the project in Xcode or build using SPM:

```bash
swift build
```

To generate an Xcode project:

```bash
swift package generate-xcodeproj
```

## Usage

1.  **Launch**: Double-click `WindowMover.app`.
2.  **Grant Permissions**: On first run, macOS will prompt for Accessibility permissions. Grant them, and you may need to restart the app.
3.  **Move Window**: Click the **Middle Mouse Button** (Scroll Wheel Click) on any window's title bar.
4.  **Quit**: Click the icon in the menu bar and select "Quit".
