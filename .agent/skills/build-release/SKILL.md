---
name: build-release
description: CI/CD workflows, build commands, and release procedures for Windows (dotnet) and macOS (make/swift).
---

# Build & Release Guide

## Windows Build
- **Run**: `dotnet run --project WindowMover.csproj`
- **Publish (Release)**:
  ```bash
  dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false
  ```
- **Output**: `bin/Release/net8.0-windows/win-x64/publish/WindowMover.exe`

## macOS Build
- **Run**: `cd WindowMoverMac && make run`
- **Release**: `cd WindowMoverMac && make release`

## GitHub Actions
- Windows builds are triggered on push to `main` branch
- macOS builds require Xcode on the runner
