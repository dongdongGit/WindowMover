---
name: mac-dev-guide
description: MacOS native Swift development standards, Accessibility API usage, and build instructions for WindowMoverMac.
---

# macOS Development Rules

## Tech Stack
- **Language**: Native Swift (Cocoa, CoreGraphics, Accessibility API).
- **Core**: Uses `CGEvent.tapCreate` for input interception and `AXUIElement` for window control.

## Code Style (Strict)
- **Indentation**: 4 spaces.
- **Braces**: **1TBS / K&R Style** (Open brace on the same line).
  ```swift
  if condition {
      doSomething()
  }