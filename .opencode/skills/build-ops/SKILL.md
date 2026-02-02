#### 3. 构建与发布专家 (`.opencode/skills/build-ops/SKILL.md`)

此技能整合了 `README.md` 和 `.github/workflows/build.yml` 中的构建命令，方便 AI 帮你写发布脚本或排查 CI 问题。

```markdown
---
name: build-ops
description: 处理 WindowMover 的多平台构建、发布命令及 GitHub Actions 工作流。
---

# 构建与发布指南

## Windows 构建 (.NET 8.0)
- **开发运行**: `dotnet run --project WindowMover.csproj`
- **正式发布 (单文件)**:
  ```bash
  dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false