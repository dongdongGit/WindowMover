# WindowMover Development Guide

This repository contains the WindowMover application with separate implementations for Windows (WinForms/.NET) and macOS (Native Swift).

## Project Structure

### Windows (Root)
- **Framework**: .NET 8.0 (Windows Forms)
- **Files**:
  - `WindowMover.csproj`: SDK-style project file
  - `MainForm.cs`: UI logic (Settings, Tray Icon)
  - `Program.cs`: Core logic (Low-level Mouse Hook `WH_MOUSE_LL`, P/Invoke definitions)

### macOS (/WindowMoverMac)
- **Framework**: Native Swift (Cocoa, CoreGraphics, Accessibility API)
- **Files**:
  - `Makefile`: Build script for creating the `.app` bundle
  - `WindowMover.swift`: Core logic (CGEventTap, Window manipulation)
  - `AppDelegate.swift`: App lifecycle and Menu Bar management
  - `AccessibilityHelper.swift`: wrappers for AXUIElement APIs

## Build & Run

### Windows
Run these commands from the repository root:
- **Build**: `dotnet build WindowMover.csproj`
- **Run**: `dotnet run --project WindowMover.csproj`
- **Publish**: `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false`

### macOS
Run these commands from the `WindowMoverMac` directory:
- **Build (Make)**: 
  ```bash
  cd WindowMoverMac
  make
  # Creates WindowMover.app in the current directory
  ```
- **Build (SPM)**:
  ```bash
  cd WindowMoverMac
  swift build
  ```

## Code Style & Conventions

### C# (Windows)
- **Indentation**: 4 spaces
- **Braces**: **Allman style** (Open brace on a new line)
  ```csharp
  if (condition)
  {
      DoSomething();
  }
  ```
- **Naming**:
  - Classes/Methods/Properties: `PascalCase`
  - Parameters/Locals: `camelCase`
  - Private Fields: `camelCase` (or `_camelCase`)
- **Specifics**: 
  - Heavy use of `P/Invoke` (`user32.dll`) located in `Program.cs`.
  - UI code is manually written in `MainForm.cs` (not designer-generated).

### Swift (macOS)
- **Indentation**: 4 spaces
- **Braces**: **1TBS / K&R style** (Open brace on the same line)
  ```swift
  if condition {
      doSomething()
  }
  ```
- **Naming**:
  - Types: `PascalCase`
  - Functions/Variables: `camelCase`
- **Specifics**:
  - Uses `CGEvent.tapCreate` for input interception.
  - Uses `AXUIElement` for window control.
  - Requires Accessibility permissions to function.

## Testing
*No automated tests are currently present in the repository.*

## CI/CD
- GitHub Actions workflows are located in `.github/workflows/`.
- The project uses tags (e.g., `v1.0.0`) to trigger release builds.
