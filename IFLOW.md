# Smart Toolbox 项目概述

## 项目简介

Smart Toolbox 是一个基于 Avalonia UI 的跨平台工具箱应用程序，使用 .NET 8 和 MVVM 模式构建。该项目提供了一系列实用工具，目前实现了文件移动和重命名功能以及系统设置管理。

## 技术栈

- **核心框架**: .NET 8
- **UI 框架**: Avalonia UI 11.3+
- **MVVM 框架**: CommunityToolkit.Mvvm 8.2+
- **响应式编程**: ReactiveUI (通过 Avalonia)
- **构建系统**: MSBuild (通过 .csproj 文件)
- **包管理**: NuGet

## 项目架构

### 核心组件

1. **Models** - 数据模型层
   - `FileFilterConfig.cs` - 文件过滤配置和大小写转换配置
2. **ViewModels** - 视图模型层 (MVVM 模式)
   - `MainWindowViewModel.cs` - 主窗口视图模型
   - `SystemSettingsViewModel.cs` - 系统设置视图模型
   - `ViewModelBase.cs` - 视图模型基类
3. **Views** - 视图层
   - `MainWindow.axaml` - 主窗口界面
   - `FileMoverView.axaml` - 文件移动工具界面
   - `SystemSettingsView.axaml` - 系统设置界面
4. **其他核心文件**
   - `App.axaml` - 应用程序入口点和资源定义
   - `Program.cs` - 程序启动入口
   - `ViewLocator.cs` - 视图定位器

### MVVM 模式实现

项目严格遵循 MVVM 模式：
- View (视图) 通过 XAML 定义界面
- ViewModel (视图模型) 处理业务逻辑和数据绑定
- Model (模型) 管理数据和业务规则

## 构建和运行

### 前置要求

- .NET 8 SDK
- Visual Studio 2022 或 VS Code（推荐安装 C# Dev Kit）

### 基本命令

```bash
# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行应用
dotnet run

# 发布应用 (以 win-x64 为例)
dotnet publish -c Release -r win-x64 -o ./publish --self-contained true
```

### 跨平台发布

项目提供了 PowerShell 脚本来构建跨平台发布包：

```powershell
# 构建所有平台的发布包
.\build-packages.ps1 -Version "1.0.0"
```

### Windows MSI 安装包

项目支持编译为 Windows MSI 安装包：

```powershell
# 创建 MSI 安装包
.\create-msi.ps1 -Version "1.0.0" -Platform "win-x64"
```

详细说明请参阅 [WINDOWS_MSI_BUILD.md](WINDOWS_MSI_BUILD.md)。

## 开发约定

### 代码规范

- 使用 C# 10+ 特性
- 遵循 .NET 命名约定
- 使用 CommunityToolkit.Mvvm 简化 MVVM 实现
- 使用 `async/await` 模式避免 UI 线程阻塞
- 实现数据绑定减少手动 UI 更新

### 添加新工具模块

要添加新的工具模块，请按以下步骤操作：

1. 在 `Views/` 目录下创建新的 UserControl 视图文件
2. 在 `ViewModels/` 目录下创建对应的视图模型（如需要）
3. 在 `MainWindowViewModel.cs` 中：
   - 在 `InitializeTools()` 方法中添加工具项
   - 在 `CreateToolContent()` 方法中添加对应的视图创建逻辑

## 性能优化

- 使用 `async/await` 模式避免 UI 线程阻塞
- 实现高效的文件操作算法
- 使用数据绑定减少手动 UI 更新
- 合理使用内存管理和资源释放
- 使用 Avalonia 的编译绑定 (`AvaloniaUseCompiledBindingsByDefault`)

## 目录结构

```
SmartToolbox/
├── Models/                 # 数据模型
│   └── FileFilterConfig.cs # 文件过滤配置
├── ViewModels/             # 视图模型 (MVVM模式)
│   ├── MainWindowViewModel.cs
│   ├── SystemSettingsViewModel.cs
│   └── ViewModelBase.cs
├── Views/                  # 视图 (XAML和代码隐藏)
│   ├── FileMoverView.axaml
│   ├── MainWindow.axaml
│   └── SystemSettingsView.axaml
├── App.axaml               # 应用程序入口XAML
├── Program.cs              # 程序入口点
└── ViewLocator.cs          # 视图定位器
```