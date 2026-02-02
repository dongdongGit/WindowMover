---
name: win-dev
description: 专注于 Windows 平台的 .NET 8.0 WinForms 开发，精通 P/Invoke 和鼠标钩子。
---

# Windows 开发规范 (WindowMover)

## 技术栈
- **框架**: .NET 8.0 (Windows Forms)
- **核心逻辑**: 使用 `user32.dll` 进行 P/Invoke，依赖 `WH_MOUSE_LL` 低级鼠标钩子。
- **UI 逻辑**: 手写 `MainForm.cs`，不依赖设计器生成的代码。

## 代码风格 (严格执行)
- **缩进**: 4 个空格。
- **大括号**: **Allman 风格** (左大括号必须换行)。
  ```csharp
  // 正确示例
  if (condition)
  {
      DoSomething();
  }