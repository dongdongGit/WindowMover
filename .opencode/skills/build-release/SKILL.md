```markdown
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