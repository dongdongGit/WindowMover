#### 2. macOS 开发专家 (`.opencode/skills/mac-dev/SKILL.md`)

此技能提取了 macOS 原生开发的要求和 Accessibility API 的使用细节。

```markdown
---
name: mac-dev
description: 专注于 macOS 平台的 Native Swift 开发，精通 Cocoa、CoreGraphics 和 Accessibility API。
---

# macOS 开发规范 (WindowMoverMac)

## 技术栈
- **语言**: Native Swift。
- **核心框架**: Cocoa, CoreGraphics (`CGEventTap`), Accessibility API (`AXUIElement`)。

## 代码风格 (严格执行)
- **缩进**: 4 个空格。
- **大括号**: **K&R / 1TBS 风格** (左大括号不换行)。
  ```swift
  // 正确示例
  if condition {
      doSomething()
  }