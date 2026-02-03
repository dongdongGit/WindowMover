---
name: win-dev-guide
description: Detailed Windows .NET 8.0 WinForms development guidelines, P/Invoke patterns, and UI rules for WindowMover.
---

# Windows Development Rules

## Tech Stack & Core Logic
- **Framework**: .NET 8.0 (Windows Forms).
- **Core**: Uses `user32.dll` P/Invoke and `WH_MOUSE_LL` (Low-level Mouse Hook) in `Program.cs`.
- **UI**: UI logic is manually written in `MainForm.cs`, avoid using the Designer for core logic.

## Code Style (Strict)
- **Indentation**: 4 spaces.
- **Braces**: **Allman Style** (Open brace must be on a new line).
  ```csharp
  if (condition)
  {
      DoSomething();
  }