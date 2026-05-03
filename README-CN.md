# Smart Toolbox

一个基于 Avalonia UI 的跨平台智能工具箱应用程序，集成多种实用工具和深度 AI 功能。

## ✨ 功能特性

### 🤖 AI 工具
- **AI 对话** - 智能对话问答系统，支持流式输出、对话持久化、多模型切换
- **AI 翻译** - 多语言智能翻译，自动语言检测
- **AI 摘要** - 自动提取文本核心内容
- **AI 润色** - 改善文本质量和表达
- **代码解释** - 解释代码功能和逻辑
- **正则生成** - 用自然语言生成正则表达式
- **Prompt优化** - 分析和优化提示词，支持 A/B 测试
- **知识库** - RAG 文档问答系统，支持上传文档进行智能问答
- **工作流** - AI 工作流自动化引擎，支持多步骤任务编排
- **AI 设置** - 配置 AI API 和参数，支持多 Provider

### 🔧 开发工具
- **JSON格式化** - JSON 数据格式化与校验
- **Base64 编解码** - Base64 编码与解码
- **Hash 计算器** - 文件和内容哈希计算（MD5、SHA1、SHA256、SHA512）
- **UUID 生成器** - 唯一标识符生成
- **时间戳转换** - Unix 时间戳与日期互转
- **代码沙盒** - 在线运行代码（支持 Python、JavaScript、Go 等）

### 📁 文件工具
- **文件移动工具** - 批量文件移动和重命名，支持大小写转换和过滤规则

### 📊 统计分析
- **用量统计** - Token 使用量和费用统计，支持预算管理

### 📝 Prompt 管理
- **Prompt 模板** - 管理和复用提示词模板

## 🚀 核心技术特性

### AI 服务架构
- **单例模式** - 统一的 AIService 管理，避免资源浪费
- **流式输出** - 支持 SSE 流式响应，实时显示生成内容
- **请求重试** - 指数退避重试机制，自动处理 429 错误
- **限流控制** - 内置 RateLimiter，防止 API 过载
- **多 Provider 支持** - OpenAI、Claude、Qwen、DeepSeek、Gemini 等

### Token 管理
- **精确估算** - 中英文混合 Token 估算
- **费用计算** - 按模型定价计算实际费用
- **用量统计** - 日/月/总用量追踪
- **预算预警** - 超预算自动提醒

### 智能路由
- **任务类型识别** - 根据任务自动选择最优模型
- **成本优化** - 简单任务使用便宜模型
- **能力匹配** - 代码任务选择代码能力强的模型

### 对话管理
- **持久化存储** - 对话自动保存到本地
- **历史搜索** - 支持搜索历史对话
- **上下文管理** - 智能压缩和滑动窗口策略
- **消息置顶** - 重要消息永不丢失

### Function Calling
- **工具注册** - 内置 10+ 常用工具
- **自动调用** - AI 可自动调用工具完成任务
- **可扩展** - 轻松注册自定义工具

### RAG 知识库
- **文档上传** - 支持 PDF、Markdown、TXT 等
- **智能分块** - 自动切分文档为语义块
- **语义搜索** - 基于关键词的相关性检索
- **上下文问答** - 结合知识库回答问题

### 工作流引擎
- **可视化编排** - 拖拽式工作流设计
- **多节点支持** - AI 任务、工具调用、条件分支
- **依赖管理** - 自动拓扑排序执行
- **预置模板** - 代码审查、翻译流水线等

### 代码沙盒
- **多语言支持** - Python、JavaScript、Go、Ruby、PHP 等
- **安全执行** - 超时控制、资源隔离
- **AI 辅助** - 代码解释、错误修复建议

### 多模态支持
- **图片输入** - 支持 URL 和 Base64 图片
- **视觉模型** - 自动选择支持 Vision 的模型
- **图片分析** - 截图分析、UI 审查等

## 🛠️ 技术栈

- [.NET 8](https://dotnet.microsoft.com/)
- [Avalonia UI 11.3+](https://avaloniaui.net/)
- [CommunityToolkit.Mvvm 8.2+](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)

## 📂 项目结构

```
smart_toolbox/
├── Models/                     # 数据模型
│   ├── AIModels.cs             # AI 相关模型定义
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
│   ├── CodeSandboxViewModel.cs     # 代码沙盒
│   ├── FileMoverViewModel.cs       # 文件移动工具
│   ├── HashCalculatorViewModel.cs  # Hash 计算器
│   ├── JsonFormatterViewModel.cs   # JSON 格式化
│   ├── KnowledgeBaseViewModel.cs   # 知识库
│   ├── MainWindowViewModel.cs      # 主窗口
│   ├── PromptOptimizerViewModel.cs # Prompt 优化器
│   ├── PromptTemplateViewModel.cs  # Prompt 模板管理
│   ├── SystemSettingsViewModel.cs  # 系统设置
│   ├── TimestampViewModel.cs       # 时间戳转换
│   ├── UsageStatsViewModel.cs      # 用量统计
│   ├── UuidGeneratorViewModel.cs   # UUID 生成器
│   ├── ViewModelBase.cs            # 视图模型基类
│   └── WorkflowViewModel.cs        # 工作流
├── Views/                      # 视图界面
├── Services/                   # 服务层
│   ├── AIService.cs                # AI 服务（核心）
│   ├── AIConfigManager.cs          # AI 配置管理
│   ├── CodeSandboxService.cs       # 代码沙盒服务
│   ├── ContextWindowManager.cs     # 上下文窗口管理
│   ├── ConversationExportService.cs # 对话导出服务
│   ├── ConversationManager.cs      # 对话管理
│   ├── KnowledgeBaseService.cs     # 知识库服务
│   ├── ModelRouter.cs              # 模型路由
│   ├── PromptOptimizerService.cs   # Prompt 优化服务
│   ├── PromptTemplateManager.cs    # Prompt 模板管理
│   ├── RateLimiter.cs              # 限流器
│   ├── ServiceLocator.cs           # 服务定位器
│   ├── TokenCounterService.cs      # Token 计数服务
│   ├── ToolRegistry.cs             # 工具注册表
│   ├── UsageStatisticsService.cs   # 用量统计服务
│   └── WorkflowEngine.cs           # 工作流引擎
├── Converters/                 # 数据转换器
├── App.axaml                   # 应用程序入口 XAML
├── Program.cs                  # 程序入口点
└── SmartToolbox.csproj         # 项目文件
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
