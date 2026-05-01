# Smart Toolbox Windows MSI 安装包编译说明

本文档详细说明如何为 Smart Toolbox 项目编译 Windows MSI 安装包。

## 目录

- [准备工作](#准备工作)
- [编译步骤](#编译步骤)
- [使用 WiX Toolset 创建 MSI](#使用-wix-toolset-创建-msi)
- [使用简化方法创建 MSI](#使用简化方法创建-msi)
- [测试安装包](#测试安装包)
- [故障排除](#故障排除)

## 准备工作

### 系统要求

- Windows 10 或更高版本
- .NET 8.0 SDK
- PowerShell 7+
- WiX Toolset 4.0+（推荐）
- Inno Setup（备用方案）

### 安装 WiX Toolset

```powershell
# 安装 WiX Toolset
dotnet tool install --global wix
```

### 安装 Inno Setup（备用方案）

1. 访问 [Inno Setup 官网](https://jrsoftware.org/isinfo.php)
2. 下载并安装最新版本

## 编译步骤

### 1. 构建跨平台发布包

首先需要构建应用程序的发布包：

```powershell
# 构建所有平台的发布包
.\build-packages.ps1 -Version "1.0.0"

# 或者仅构建 Windows 版本
.\build-packages.ps1 -Version "1.0.0" -WindowsOnly
```

这将在 `publish` 目录中生成各个平台的发布文件。

### 2. 创建 Windows MSI 安装包

有两种方法可以创建 MSI 安装包：

## 使用 WiX Toolset 创建 MSI

这是推荐的方法，使用微软官方推荐的工具创建 MSI 安装包。

```powershell
# 使用 WiX Toolset 创建 MSI
.\create-msi.ps1 -Version "1.0.0" -Platform "win-x64"
```

脚本会自动：
1. 检查并安装 WiX Toolset（如果未安装）
2. 创建 WiX 源文件
3. 构建 MSI 安装包
4. 在 `publish` 目录中生成 `.msi` 文件

生成的文件示例：
```
publish\SmartToolbox-1.0.0-win-x64.msi
```

### MSI 功能特性

- 标准 Windows 安装程序界面
- 开始菜单快捷方式
- 桌面快捷方式
- 完整的卸载支持
- 版本升级支持

## 使用简化方法创建 MSI

如果 WiX Toolset 不可用，脚本会自动回退到简化方法。

```powershell
# 使用简化方法创建 MSI
.\create-msi-simple.ps1 -Version "1.0.0" -Platform "win-x64"
```

此脚本会：
1. 尝试安装 `dotnet-msideploy` 工具
2. 如果安装失败，则创建 Inno Setup 脚本
3. 生成 MSI 安装包或 Inno Setup 脚本

## 测试安装包

### 静默安装测试

```powershell
# 静默安装
msiexec /i "publish\SmartToolbox-1.0.0-win-x64.msi" /quiet

# 交互式安装
msiexec /i "publish\SmartToolbox-1.0.0-win-x64.msi"

# 卸载
msiexec /x "publish\SmartToolbox-1.0.0-win-x64.msi" /quiet
```

### 使用测试脚本

项目包含一个测试脚本用于自动化测试：

```powershell
# 运行测试脚本
.\test-msi-install.ps1 -Version "1.0.0"
```

## 故障排除

### 常见问题

**1. WiX Toolset 未找到**
```
错误: 未找到 WiX 工具集
解决: 运行 dotnet tool install --global wix
```

**2. 发布文件夹不存在**
```
错误: 未找到发布文件夹
解决: 先运行 .\build-packages.ps1
```

**3. MSI 创建失败**
```
错误: MSI 创建失败
解决: 检查 WiX 版本或使用备用方法
```

**4. 权限不足**
```
错误: 访问被拒绝
解决: 以管理员身份运行 PowerShell
```

### 日志查看

```powershell
# 查看 WiX 构建日志
Get-Content "wix\SmartToolbox.wixpdb"
```

## 自定义配置

### 修改应用程序信息

在 `SmartToolbox.csproj` 文件中修改以下属性：

```xml
<!-- 应用程序信息 -->
<AssemblyTitle>Smart Toolbox</AssemblyTitle>
<AssemblyDescription>一个智能工具箱应用程序</AssemblyDescription>
<AssemblyCompany>Your Company</AssemblyCompany>
<AssemblyProduct>Smart Toolbox</AssemblyProduct>
```

### 修改发布配置

在 `SmartToolbox.csproj` 文件中修改发布配置：

```xml
<!-- 发布配置 -->
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<PublishTrimmed>true</PublishTrimmed>
<TrimMode>link</TrimMode>
<PublishReadyToRun>true</PublishReadyToRun>
```

### 自定义 MSI 属性

在 `create-msi.ps1` 脚本中修改以下参数：

```xml
<Package Name="Smart Toolbox" 
         Version="$Version" 
         Manufacturer="Your Company" 
         UpgradeCode="12345678-1234-1234-1234-123456789012">
```

## 文件结构

```
项目根目录/
├── publish/
│   ├── SmartToolbox-1.0.0-win-x64.msi      # MSI 安装包
│   ├── SmartToolbox-1.0.0-win-x64/         # Windows x64 发布文件
│   ├── SmartToolbox-1.0.0-win-x64.zip      # Windows x64 压缩包
│   └── ...                                 # 其他平台文件
├── wix/
│   └── SmartToolbox.wxs                    # WiX 源文件
├── create-msi.ps1                          # WiX MSI 创建脚本
├── create-msi-simple.ps1                   # 简化 MSI 创建脚本
├── build-packages.ps1                      # 跨平台发布脚本
├── build-all.ps1                           # 完整构建脚本
└── test-msi-install.ps1                    # MSI 测试脚本
```

## 相关文档

- [BUILD_INSTRUCTIONS.md](BUILD_INSTRUCTIONS.md) - 完整构建说明
- [README.md](README.md) - 项目说明