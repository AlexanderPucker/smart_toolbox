# Smart Toolbox - AI 编程助手指南

## 项目概述
Smart Toolbox 是一个基于 .NET 8.0 和 MVVM 模式的跨平台 Avalonia UI 应用程序。它提供了一个模块化的工具箱界面，其中每个工具都作为独立的视图实现，并拥有自己的视图模型。

## 架构与设计模式

### MVVM 实现
- **基类**: 所有视图模型都继承自 `ViewModelBase`（继承自 CommunityToolkit.Mvvm 的 `ObservableObject`）
- **属性绑定**: 使用 `[ObservableProperty]` 属性为自动生成的 getter/setter
- **命令**: 使用 `[RelayCommand]` 属性为可绑定到 UI 操作的方法
- **视图解析**: `ViewLocator` 通过将视图模型名称中的 "ViewModel" 替换为 "View" 来自动映射视图模型到视图

### 工具系统架构
- 工具在 `MainWindowViewModel.InitializeTools()` 中注册
- 每个工具都是一个带有图标、名称和描述的 `ToolItem`
- 工具内容在 `CreateToolContent()` 方法中动态创建
- 工具可以拥有自己的视图模型或作为自包含的视图

### 关键架构决策
- **模块化设计**: 每个工具都是独立的 UserControl 以提高可维护性
- **异步优先**: 所有文件操作都使用 async/await 以防止 UI 阻塞
- **取消支持**: 长时间运行的操作通过 `CancellationTokenSource` 支持取消
- **性能优化**: 大文件操作使用批处理和进度报告
- **内存管理**: 内存密集型操作使用显式的 GC 调用

## 编码规范

### 文件组织
```
Views/           # XAML 视图 (UserControls/Windows)
ViewModels/      # 视图模型 (继承 ViewModelBase)
Models/          # 数据模型和配置类
Services/        # 业务逻辑服务 (计划中)
```

### 命名规范
- **视图**: `{Feature}View.axaml` 和 `{Feature}View.axaml.cs`
- **视图模型**: `{Feature}ViewModel.cs`
- **模型**: `{Feature}Config.cs` 用于配置类
- **方法**: PascalCase，异步方法以 `Async` 结尾
- **私有字段**: camelCase 并以下划线前缀 (`_fieldName`)

### Avalonia UI 模式
- **控件查找**: 使用 `this.FindControl<T>("ControlName")` 来访问 XAML 元素
- **事件处理器**: UI 事件处理器使用 async void 方法
- **数据绑定**: 利用编译绑定 (`AvaloniaUseCompiledBindingsByDefault=true`)
- **存储提供者**: 使用 `TopLevel.GetTopLevel(this).StorageProvider` 进行文件/文件夹选择器

### 异步编程模式
```csharp
// 带取消的标准异步操作
private async Task ProcessFilesAsync()
{
    _cancellationTokenSource = new CancellationTokenSource();
    var token = _cancellationTokenSource.Token;

    try
    {
        // 长时间运行的工作，包含令牌检查
        await Task.Run(() => {
            token.ThrowIfCancellationRequested();
            // ... 工作内容 ...
        }, token);
    }
    catch (OperationCanceledException)
    {
        // 处理取消
    }
}
```

### 错误处理
- 在所有异步操作周围使用 try/catch 块
- 为成功和失败情况都更新 UI 状态
- 使用 `Debug.WriteLine()` 记录开发中的错误
- 当操作失败时优雅地降级功能

## 构建与部署

### 开发工作流
```bash
# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行应用程序
dotnet run
```

### 跨平台发布
- **单文件**: `PublishSingleFile=true` 以便于分发
- **自包含**: `SelfContained=true` 包含 .NET 运行时
- **裁剪**: `PublishTrimmed=true` 减少文件大小
- **ReadyToRun**: `PublishReadyToRun=true` 提高启动性能

### 构建脚本
- `build-packages.ps1`: 创建跨平台发布包
- `build-all.ps1`: 包含 MSI 创建的完整构建流水线
- `create-msi.ps1`: 使用 WiX Toolset 创建 Windows MSI 安装程序
- macOS DMG 和 Linux DEB 包的平台特定脚本

### Windows MSI 特性
- 带有开始菜单/桌面快捷方式的标准安装程序界面
- 正确的卸载支持和版本升级
- 使用 WiX Toolset 创建专业安装程序

## 添加新工具

### 1. 创建视图文件
```xml
<!-- Views/YourToolView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             x:Class="SmartToolbox.Views.YourToolView">
    <!-- 您的工具界面 -->
</UserControl>
```

```csharp
// Views/YourToolView.axaml.cs
public partial class YourToolView : UserControl
{
    public YourToolView()
    {
        InitializeComponent();
    }
}
```

### 2. 注册工具
在 `MainWindowViewModel.cs` 中：
```csharp
private void InitializeTools()
{
    Tools.Add(new ToolItem("🔧", "您的工具", "工具描述"));
}

private object CreateToolContent(string toolName)
{
    return toolName switch
    {
        "您的工具" => new YourToolView(),
        // ... 现有工具 ...
    };
}
```

### 3. 可选视图模型
如果工具需要复杂的状态管理，请创建 `YourToolViewModel.cs`：
```csharp
public partial class YourToolViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _someProperty;
    
    [RelayCommand]
    private void SomeAction() { /* ... */ }
}
```

然后在 `CreateToolContent()` 中实例化：
```csharp
"您的工具" => new YourToolView() { DataContext = new YourToolViewModel() }
```

## 性能考虑

### 文件操作
- **批处理**: 以块为单位处理文件 (BATCH_SIZE = 100) 以防止 UI 冻结
- **进度更新**: 在后台工作期间使用 `Dispatcher.UIThread.InvokeAsync()` 进行 UI 更新
- **内存限制**: 将预览限制为 MAX_PREVIEW_FILES (5000) 以防止内存问题
- **取消**: 始终支持长时间运行操作的取消

### UI 响应性
- **异步操作**: 永远不要用同步文件操作阻塞 UI 线程
- **状态更新**: 为所有操作提供清晰的进度反馈
- **错误恢复**: 优雅地处理异常而不使应用程序崩溃

## 配置模式

### 静态配置类
为预定义选项使用静态类：
```csharp
public static class YourConfig
{
    public static ObservableCollection<YourItem> Options { get; } = new()
    {
        new YourItem("选项 1", "value1"),
        new YourItem("选项 2", "value2")
    };
}
```

### 设置管理
- 将用户设置存储在专用配置类中
- 为 UI 控件数据绑定使用 ObservableCollection
- 为所有配置选项提供合理的默认值

## 测试与验证

### 构建验证
- 代码更改后运行 `dotnet build`
- 使用 `build-packages.ps1` 测试跨平台发布
- 在 Windows 构建上验证 MSI 创建

### 运行时测试
- 使用各种文件类型和大小测试文件操作
- 验证取消功能是否适用于长时间运行的操作
- 在重负载期间测试 UI 响应性

## 常见陷阱

1. **UI 线程阻塞**: 文件操作始终使用 async/await
2. **内存泄漏**: 释放 CancellationTokenSource 并清理事件处理器
3. **视图定位器问题**: 确保视图模型名称遵循 "ViewModel" → "View" 约定
4. **绑定错误**: 使用编译绑定并验证属性名称与 XAML 匹配
5. **平台差异**: 在 Windows/macOS/Linux 上测试文件操作

## 依赖项与库

- **Avalonia UI**: 跨平台 UI 框架
- **CommunityToolkit.Mvvm**: 带源码生成器的 MVVM 实现
- **ReactiveUI**: 响应式编程 (已引用但尚未大量使用)
- **WiX Toolset**: Windows 安装程序创建
- **PowerShell**: 构建自动化脚本

## 未来考虑

- **服务层**: 将业务逻辑提取到 Services/ 目录
- **插件架构**: 使工具可在运行时加载
- **设置持久化**: 添加基于文件的设置存储
- **本地化**: 实现多语言支持
- **可访问性**: 添加屏幕阅读器支持和键盘导航</content>
<parameter name="filePath">e:\projects\smart_toolbox\.github\copilot-instructions.md