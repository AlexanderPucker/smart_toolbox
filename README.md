# Smart Toolbox

A cross-platform intelligent toolbox application built with Avalonia UI, featuring various practical tools and advanced AI capabilities.

[中文文档](README-CN.md)

## ✨ Features

### 🤖 AI Tools
- **AI Chat** - Intelligent Q&A system with streaming output, conversation persistence, and multi-model switching
- **AI Translation** - Multi-language intelligent translation with automatic language detection
- **AI Summary** - Automatically extract core content from text
- **AI Text Polish** - Improve text quality and expression
- **Code Explanation** - Explain code functionality and logic
- **Regex Generator** - Generate regular expressions using natural language
- **Prompt Optimizer** - Analyze and optimize prompts with A/B testing support
- **Knowledge Base** - RAG document Q&A system with document upload support
- **Workflow** - AI workflow automation engine with multi-step task orchestration
- **AI Settings** - Configure AI API and parameters with multi-provider support

### 🔧 Developer Tools
- **JSON Formatter** - JSON data formatting and validation
- **Base64 Encoder/Decoder** - Base64 encoding and decoding
- **Hash Calculator** - File and content hash calculation (MD5, SHA1, SHA256, SHA512)
- **UUID Generator** - Generate unique identifiers
- **Timestamp Converter** - Convert between Unix timestamps and dates
- **Code Sandbox** - Run code online (supports Python, JavaScript, Go, etc.)

### 📁 File Tools
- **File Mover** - Batch file moving and renaming with case conversion and filter rules

### 📊 Statistics
- **Usage Statistics** - Token usage and cost tracking with budget management

### 📝 Prompt Management
- **Prompt Templates** - Manage and reuse prompt templates

## 🚀 Core Technical Features

### AI Service Architecture
- **Singleton Pattern** - Unified AIService management to avoid resource waste
- **Streaming Output** - SSE streaming response support for real-time content display
- **Request Retry** - Exponential backoff retry mechanism with automatic 429 error handling
- **Rate Limiting** - Built-in RateLimiter to prevent API overload
- **Multi-Provider Support** - OpenAI, Claude, Qwen, DeepSeek, Gemini, and more

### Token Management
- **Accurate Estimation** - Mixed Chinese-English token estimation
- **Cost Calculation** - Calculate actual costs based on model pricing
- **Usage Tracking** - Daily/monthly/total usage tracking
- **Budget Alerts** - Automatic warnings when exceeding budget

### Intelligent Routing
- **Task Type Recognition** - Automatically select the optimal model based on task
- **Cost Optimization** - Use cheaper models for simple tasks
- **Capability Matching** - Select models with strong coding capabilities for code tasks

### Conversation Management
- **Persistent Storage** - Conversations automatically saved locally
- **History Search** - Search through historical conversations
- **Context Management** - Intelligent compression and sliding window strategies
- **Message Pinning** - Important messages never lost

### Function Calling
- **Tool Registry** - 10+ built-in tools
- **Auto Invocation** - AI can automatically call tools to complete tasks
- **Extensible** - Easily register custom tools

### RAG Knowledge Base
- **Document Upload** - Support for PDF, Markdown, TXT, etc.
- **Intelligent Chunking** - Automatically split documents into semantic chunks
- **Semantic Search** - Keyword-based relevance retrieval
- **Contextual Q&A** - Answer questions using knowledge base

### Workflow Engine
- **Visual Orchestration** - Drag-and-drop workflow design
- **Multi-Node Support** - AI tasks, tool calls, conditional branches
- **Dependency Management** - Automatic topological sort execution
- **Preset Templates** - Code review, translation pipeline, etc.

### Code Sandbox
- **Multi-Language Support** - Python, JavaScript, Go, Ruby, PHP, etc.
- **Secure Execution** - Timeout control, resource isolation
- **AI Assistance** - Code explanation, error fix suggestions

### Multimodal Support
- **Image Input** - Support for URL and Base64 images
- **Vision Models** - Automatically select Vision-capable models
- **Image Analysis** - Screenshot analysis, UI review, etc.

## 🛠️ Tech Stack

- [.NET 8](https://dotnet.microsoft.com/)
- [Avalonia UI 11.3+](https://avaloniaui.net/)
- [CommunityToolkit.Mvvm 8.2+](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)

## 📂 Project Structure

```
smart_toolbox/
├── Models/                     # Data Models
│   ├── AIModels.cs             # AI-related model definitions
│   ├── FileFilterConfig.cs     # File filter configuration
│   ├── FileMoveInfo.cs         # File move information
│   ├── PromptTemplate.cs       # Prompt templates
│   └── SystemSettingsConfig.cs # System settings configuration
├── ViewModels/                 # View Models (MVVM)
│   ├── AIChatViewModel.cs          # AI Chat
│   ├── AICodeExplainViewModel.cs   # AI Code Explanation
│   ├── AIRegexGeneratorViewModel.cs # AI Regex Generator
│   ├── AISettingsViewModel.cs      # AI Settings
│   ├── AISummaryViewModel.cs       # AI Summary
│   ├── AITextPolishViewModel.cs    # AI Text Polish
│   ├── AITranslatorViewModel.cs    # AI Translator
│   ├── Base64ViewModel.cs          # Base64 Tool
│   ├── CodeSandboxViewModel.cs     # Code Sandbox
│   ├── FileMoverViewModel.cs       # File Mover
│   ├── HashCalculatorViewModel.cs  # Hash Calculator
│   ├── JsonFormatterViewModel.cs   # JSON Formatter
│   ├── KnowledgeBaseViewModel.cs   # Knowledge Base
│   ├── MainWindowViewModel.cs      # Main Window
│   ├── PromptOptimizerViewModel.cs # Prompt Optimizer
│   ├── PromptTemplateViewModel.cs  # Prompt Template Manager
│   ├── SystemSettingsViewModel.cs  # System Settings
│   ├── TimestampViewModel.cs       # Timestamp Converter
│   ├── UsageStatsViewModel.cs      # Usage Statistics
│   ├── UuidGeneratorViewModel.cs   # UUID Generator
│   ├── ViewModelBase.cs            # View Model Base
│   └── WorkflowViewModel.cs        # Workflow
├── Views/                      # Views
├── Services/                   # Service Layer
│   ├── AIService.cs                # AI Service (Core)
│   ├── AIConfigManager.cs          # AI Config Manager
│   ├── CodeSandboxService.cs       # Code Sandbox Service
│   ├── ContextWindowManager.cs     # Context Window Manager
│   ├── ConversationExportService.cs # Conversation Export Service
│   ├── ConversationManager.cs      # Conversation Manager
│   ├── KnowledgeBaseService.cs     # Knowledge Base Service
│   ├── ModelRouter.cs              # Model Router
│   ├── PromptOptimizerService.cs   # Prompt Optimizer Service
│   ├── PromptTemplateManager.cs    # Prompt Template Manager
│   ├── RateLimiter.cs              # Rate Limiter
│   ├── ServiceLocator.cs           # Service Locator
│   ├── TokenCounterService.cs      # Token Counter Service
│   ├── ToolRegistry.cs             # Tool Registry
│   ├── UsageStatisticsService.cs   # Usage Statistics Service
│   └── WorkflowEngine.cs           # Workflow Engine
├── Converters/                 # Data Converters
├── App.axaml                   # Application Entry XAML
├── Program.cs                  # Program Entry Point
└── SmartToolbox.csproj         # Project File
```

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code (C# Dev Kit recommended)

### Build and Run

1. Clone the project
2. Navigate to project directory
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Build the project:
   ```bash
   dotnet build
   ```
5. Run the application:
   ```bash
   dotnet run
   ```

## 📦 Packaging & Distribution

### Windows MSI Installer

```powershell
.\build-packages.ps1 -Version "1.0.0"
.\create-msi.ps1 -Version "1.0.0" -Platform "win-x64"
```

### macOS DMG Installer

```bash
./create-dmg.sh
```

### Linux DEB Package

```bash
./create-deb.sh
```

## 📝 Adding New Tools

1. Create View Model: Add a new ViewModel in `ViewModels/`
2. Create View: Add corresponding `.axaml` and `.axaml.cs` files in `Views/`
3. Register in Main Window: Add tool item in `MainWindowViewModel.cs`

## 📄 License

MIT License

## 🤝 Contributing

Issues and Pull Requests are welcome!
