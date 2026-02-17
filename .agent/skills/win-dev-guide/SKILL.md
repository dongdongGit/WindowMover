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
  ```
- **Naming**: PascalCase for public members, camelCase for locals, `_camelCase` for private fields.
- **Comments**: Use Chinese (中文) for user-facing strings and comments.

## Important Rules
- All P/Invoke declarations go in `Program.cs` under the `#region Core Hook Logic & P/Invoke` region.
- UI controls are created programmatically in `MainForm.cs` — do NOT use `.Designer.cs` files.
- Settings are stored in `Registry.CurrentUser` under `Software\WindowMover`.
- Auto-start uses Windows Task Scheduler (`schtasks.exe`) for UAC elevation support.
