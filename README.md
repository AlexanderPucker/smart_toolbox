# Smart Toolbox

一个基于 Avalonia UI 的跨平台工具箱应用程序，使用 .NET 8 和 MVVM 模式构建。

## 功能特性

### 已实现的功能

- 文件移动和重命名工具
  - 批量文件移动和重命名
  - 大小写转换选项
  - 文件过滤规则
  - 实时预览更改
- 系统设置管理
  - 应用程序主题切换
  - 系统信息查看

### 规划中的功能

- 文本处理工具
- 图像处理工具
- 计算器工具
- 密码生成器
- 网络工具

## 技术栈

- .NET 8
- Avalonia UI 11.3+
- CommunityToolkit.Mvvm 8.2+
- ReactiveUI (通过 Avalonia)

## 代码结构

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

## 性能优化

- 使用`async/await`模式避免UI线程阻塞
- 实现高效的文件操作算法
- 使用数据绑定减少手动UI更新
- 合理使用内存管理和资源释放

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