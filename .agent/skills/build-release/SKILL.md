---
name: build-release
description: CI/CD workflows, build commands, and release procedures for Windows (dotnet) and macOS (make/swift).
---

# Build & Release Guide

## Windows Build
- **Run**: `cd WindowMoverWin && dotnet run`
- **Publish (Release)**:
  ```bash
  cd WindowMoverWin && dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false
  ```
- **Output**: `WindowMoverWin/bin/Release/net8.0-windows/win-x64/publish/WindowMover.exe`

## macOS Build
- **Run**: `cd WindowMoverMac && make run`
- **Release**: `cd WindowMoverMac && make release`

## GitHub Actions
- Windows builds are triggered on push to `main` branch
- macOS builds require Xcode on the runner
