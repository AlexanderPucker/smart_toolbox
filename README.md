# 智能工具箱 - SmartToolbox

一个基于 Avalonia UI 框架的跨平台个人工具箱应用程序，提供日常开发和办公中常用的实用工具。

## 功能特性

- 🖥️ **跨平台支持** - 基于 Avalonia UI，支持 Windows、macOS 和 Linux
- 🎨 **现代化界面** - 清洁、直观的用户界面设计
- 🧩 **模块化架构** - 基于 MVVM 模式，易于扩展和维护
- ⚡ **高性能** - 基于 .NET 8 和 Avalonia 11.3+

## 当前工具模块

### 📄 文本工具
- 文本大小写转换（大写、小写、首字母大写）
- 去除多余空格和空行
- 文本统计（字符数、单词数、行数）
- 实时统计显示

### 🔢 计算器 *(规划中)*
- 基础数学运算
- 科学计算
- 单位转换
- 进制转换

### 🎨 颜色工具 *(规划中)*
- 颜色选择器
- RGB/HEX 格式转换
- 调色板生成
- 颜色对比度检查

### 🌐 网络工具 *(规划中)*
- URL 编码/解码
- IP 地址查询
- 端口状态检查
- 网络延迟测试

### 📊 数据工具 *(规划中)*
- JSON 格式化和验证
- Base64 编码/解码
- MD5/SHA 哈希计算
- 时间戳转换

### ⚙️ 系统信息 *(规划中)*
- CPU 和内存信息
- 磁盘使用情况
- 网络接口信息
- 环境变量查看

## 技术栈

- **.NET 8** - 运行时框架
- **Avalonia UI 11.3+** - 跨平台 UI 框架
- **CommunityToolkit.Mvvm** - MVVM 框架支持
- **C#** - 主要开发语言

## 快速开始

### 前置要求
- .NET 8 SDK
- Visual Studio 2022 或 VS Code（推荐安装 C# Dev Kit）

### 构建和运行

1. 克隆项目到本地
2. 进入项目目录：
   ```bash
   cd smart_toolbox
   ```

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

## 项目结构

```
smart_toolbox/
├── Views/              # 视图文件
│   ├── MainWindow.axaml           # 主窗口
│   ├── MainWindow.axaml.cs
│   ├── FileMoverView.axaml        # 文件移动工具页面
│   └── FileMoverView.axaml.cs
├── ViewModels/         # 视图模型
│   ├── ViewModelBase.cs
│   └── MainWindowViewModel.cs     # 主窗口视图模型
├── Models/             # 数据模型（待扩展）
├── Assets/             # 资源文件
│   └── avalonia-logo.ico
├── App.axaml           # 应用程序定义
├── App.axaml.cs
├── Program.cs          # 程序入口点
├── ViewLocator.cs      # 视图定位器
└── SmartToolbox.csproj # 项目文件
```

## 添加新工具

要添加新的工具模块，请按以下步骤操作：

### 1. 创建视图文件
在 `Views/` 目录下创建新的 UserControl：
```xml
<!-- Views/YourToolView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             x:Class="SmartToolbox.Views.YourToolView">
    <!-- 您的工具界面 -->
</UserControl>
```

### 2. 创建代码逻辑
```csharp
// Views/YourToolView.axaml.cs
using Avalonia.Controls;

namespace SmartToolbox.Views;

public partial class YourToolView : UserControl
{
    public YourToolView()
    {
        InitializeComponent();
    }
    
    // 添加您的业务逻辑
}
```

### 3. 注册到主窗口
在 `MainWindowViewModel.cs` 中：

1. 在 `InitializeTools()` 方法中添加工具项：
   ```csharp
   Tools.Add(new ToolItem("🔧", "您的工具", "工具描述"));
   ```

2. 在 `CreateToolContent()` 方法中添加对应的视图：
   ```csharp
   "您的工具" => new SmartToolbox.Views.YourToolView(),
   ```

## 许可证

本项目采用 MIT 许可证 - 详见 LICENSE 文件

## 贡献

欢迎提交 Issue 和 Pull Request 来改进这个项目！

## 联系方式

如果您有任何问题或建议，请创建 Issue 或通过以下方式联系我：

- GitHub Issues: [项目地址]/issues 