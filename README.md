# Window Mover

一个简单实用的 Windows 多显示器窗口移动工具，通过鼠标中键快速将窗口移动到下一个显示器。

## 功能特性

- **快速移动窗口** - 在任意窗口标题栏点击鼠标中键，即可将窗口移动到下一个显示器
- **多显示器支持** - 自动检测所有连接的显示器，循环切换窗口位置
- **位置记忆** - 保留窗口在显示器上的相对位置，移动体验更自然
- **系统托盘** - 程序最小化至系统托盘，不占用任务栏空间
- **开机自启** - 可选开机自动启动功能
- **静默启动** - 可选最小化启动，启动时隐藏主窗口
- **功能开关** - 可随时禁用鼠标中键移动功能
- **强制置顶** - 移动窗口后自动将其置顶，确保窗口可见
- **DPI 感知** - 支持高 DPI 显示器，界面清晰锐利

## 使用方法

### 基本操作

1. 运行程序后，程序将在后台运行（系统托盘中显示图标）
2. 在任意窗口的标题栏区域点击**鼠标中键**
3. 窗口将自动移动到下一个显示器

### 主界面设置

双击系统托盘图标或右键菜单选择"显示主界面"可打开设置面板：

- **禁用鼠标中键移动功能** - 暂时关闭窗口移动功能
- **开机自动启动** - 程序随系统启动自动运行
- **总是最小化启动** - 启动时隐藏主窗口，直接运行在后台

### 系统托盘菜单

- **显示主界面** - 打开设置面板
- **退出程序** - 完全退出程序

## 系统要求

- Windows 10 或更高版本
- .NET 8.0 Runtime（或更高版本）

## 构建说明

### 前置要求

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 构建步骤

```bash
# 克隆仓库
git clone git@github.com:dongdongGit/WindowMover.git
cd WindowMover

# 构建项目
dotnet build

# 运行程序
dotnet run
```

### 发布为可执行文件

```bash
# 发布为单文件可执行程序
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# 发布后的文件位于：bin/Release/net8.0-windows/win-x64/publish/
```

## GitHub Actions 自动构建与发布

项目配置了 GitHub Actions 工作流，可自动构建并发布多平台版本。

### 工作流说明

- **触发方式**：推送以 `v` 开头的标签（如 `v1.0.0`）时自动触发
- **支持平台**：win-x64（64位）、win-x86（32位）、win-arm64（ARM64）
- **输出格式**：单文件可执行程序（.exe），打包为 ZIP 压缩包

### 发布新版本

1. **创建并推送标签**：
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. **等待构建完成**：GitHub Actions 会自动开始构建并创建 Release

3. **下载发布文件**：在项目的 Releases 页面下载对应的 ZIP 文件

### 手动触发构建

如果需要手动触发构建（不创建 Release），可以在 GitHub 仓库的 Actions 页面点击 "Build and Release" 工作流右侧的 "Run workflow" 按钮。

### 工作流配置文件

工作流配置位于 [`.github/workflows/build.yml`](.github/workflows/build.yml)，包含以下步骤：

1. 检出代码
2. 设置 .NET 8.0 环境
3. 恢复依赖
4. 构建项目
5. 发布为自包含单文件（三个平台）
6. 创建 ZIP 压缩包
7. 创建 GitHub Release 并上传文件

## 技术细节

### 核心技术

- **Windows API Hook** - 使用低级鼠标钩子（`WH_MOUSE_LL`）捕获鼠标中键事件
- **P/Invoke** - 调用 Windows API 进行窗口操作
- **强制置顶** - 使用 `AttachThreadInput` 技术确保窗口正确置顶

### 支持的应用程序

程序通过多种方式检测窗口标题栏区域，支持包括但不限于：
- 标准的 Win32 应用程序
- VS Code / Electron 应用
- Chrome / 浏览器
- 大多数现代桌面应用程序

## 项目结构

```
WindowMover/
├── Program.cs           # 主程序入口和核心钩子逻辑
├── MainForm.cs          # 主窗体和用户界面
├── WindowMover.csproj   # 项目配置文件
├── appicon.png          # 应用图标
└── .gitignore           # Git 忽略文件
```

## 许可证

本项目采用 MIT 许可证。

## 贡献

欢迎提交 Issue 和 Pull Request！

## 更新日志

### 当前版本
- 支持鼠标中键移动窗口
- 多显示器循环切换
- 系统托盘支持
- 开机自启动选项
- DPI 感知支持
