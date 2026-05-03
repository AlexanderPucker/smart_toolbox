# Smart Toolbox

一个基于 Avalonia UI 的跨平台智能工具箱应用程序，集成多种实用工具和 AI 功能。

## ✨ 功能特性

### 🤖 AI 工具
- **AI 对话** - 通用 AI 聊天助手
- **AI 翻译** - 智能多语言翻译
- **AI 文本润色** - 文本优化和改写
- **AI 代码解释** - 自动解析代码逻辑
- **AI 摘要** - 文本智能摘要提取
- **AI 正则表达式生成器** - 自然语言生成正则表达式
- **Prompt 模板管理** - 管理和复用提示词模板
- **AI 设置** - 配置 AI 服务参数

### 🔧 开发工具
- **JSON 格式化** - JSON 数据格式化与校验
- **Base64 编解码** - Base64 编码与解码
- **Hash 计算器** - 文件和内容哈希计算
- **UUID 生成器** - 唯一标识符生成
- **时间戳转换** - Unix 时间戳与日期互转

### 📁 文件工具
- **文件移动工具** - 批量文件移动和重命名，支持大小写转换和过滤规则，实时预览更改

### ⚙️ 系统设置
- 应用程序主题切换
- 系统信息查看

## 🛠️ 技术栈

- [.NET 8](https://dotnet.microsoft.com/)
- [Avalonia UI 11.3+](https://avaloniaui.net/)
- [CommunityToolkit.Mvvm 8.2+](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)

## 📂 项目结构

```
smart_toolbox/
├── Models/                     # 数据模型
│   ├── FileFilterConfig.cs     # 文件过滤配置
│   ├── FileMoveInfo.cs         # 文件移动信息
│   ├── PromptTemplate.cs       # 提示词模板
│   └── SystemSettingsConfig.cs # 系统设置配置
├── ViewModels/                 # 视图模型 (MVVM)
│   ├── AIChatViewModel.cs          # AI 对话
│   ├── AICodeExplainViewModel.cs   # AI 代码解释
│   ├── AIRegexGeneratorViewModel.cs # AI 正则生成
│   ├── AISettingsViewModel.cs      # AI 设置
│   ├── AISummaryViewModel.cs       # AI 摘要
│   ├── AITextPolishViewModel.cs    # AI 文本润色
│   ├── AITranslatorViewModel.cs    # AI 翻译
│   ├── Base64ViewModel.cs          # Base64 工具
│   ├── FileMoverViewModel.cs       # 文件移动工具
│   ├── HashCalculatorViewModel.cs  # Hash 计算器
│   ├── JsonFormatterViewModel.cs   # JSON 格式化
│   ├── MainWindowViewModel.cs      # 主窗口
│   ├── PromptTemplateViewModel.cs  # Prompt 模板管理
│   ├── SystemSettingsViewModel.cs  # 系统设置
│   ├── TimestampViewModel.cs       # 时间戳转换
│   ├── UuidGeneratorViewModel.cs   # UUID 生成器
│   └── ViewModelBase.cs            # 视图模型基类
├── Views/                      # 视图界面
│   ├── MainWindow.axaml            # 主窗口
│   ├── AIChatView.axaml            # AI 对话视图
│   ├── AICodeExplainView.axaml     # AI 代码解释视图
│   ├── AIRegexGeneratorView.axaml  # AI 正则生成视图
│   ├── AISettingsView.axaml        # AI 设置视图
│   ├── AISummaryView.axaml         # AI 摘要视图
│   ├── AITextPolishView.axaml      # AI 文本润色视图
│   ├── AITranslatorView.axaml      # AI 翻译视图
│   ├── Base64View.axaml            # Base64 工具视图
│   ├── FileMoverView.axaml         # 文件移动工具视图
│   ├── HashCalculatorView.axaml    # Hash 计算器视图
│   ├── JsonFormatterView.axaml     # JSON 格式化视图
│   ├── PromptTemplateView.axaml    # Prompt 模板视图
│   ├── SystemSettingsView.axaml    # 系统设置视图
│   ├── TimestampView.axaml         # 时间戳视图
│   └── UuidGeneratorView.axaml     # UUID 生成器视图
├── Services/                   # 服务层
│   ├── AIConfigManager.cs          # AI 配置管理
│   ├── AIService.cs                # AI 服务
│   └── PromptTemplateManager.cs    # Prompt 模板管理
├── Converters/                 # 数据转换器
│   └── BoolToStringConverter.cs
├── App.axaml                 # 应用程序入口 XAML
├── Program.cs                # 程序入口点
├── ViewLocator.cs            # 视图定位器
└── SmartToolbox.csproj       # 项目文件
```

## 🚀 快速开始

### 前置要求
- .NET 8 SDK
- Visual Studio 2022 或 VS Code（推荐安装 C# Dev Kit）

### 构建和运行

1. 克隆项目
2. 进入项目目录
3. 还原依赖：
   ```bash
   dotnet restore
   ```
4. 构建项目：
   ```bash
   dotnet build
   ```
5. 运行应用：
   ```bash
   dotnet run
   ```

## 📦 打包发布

### Windows MSI 安装包

```powershell
.\build-packages.ps1 -Version "1.0.0"
.\create-msi.ps1 -Version "1.0.0" -Platform "win-x64"
```

### macOS DMG 安装包

```bash
./create-dmg.sh
```

### Linux DEB 安装包

```bash
./create-deb.sh
```

## 📝 添加新工具

1. 创建视图模型：在 `ViewModels/` 中添加新的 ViewModel
2. 创建视图界面：在 `Views/` 中添加对应的 `.axaml` 和 `.axaml.cs` 文件
3. 在主窗口注册：在 `MainWindowViewModel.cs` 中添加工具项

## 📄 许可证

MIT 许可证

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！
