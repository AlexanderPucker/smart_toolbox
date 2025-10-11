# Smart Toolbox 构建和打包说明

本文档描述如何为 Smart Toolbox 项目生成跨平台安装包，包括 Windows MSI 格式。

## 目录

- [快速开始](#快速开始)
- [详细说明](#详细说明)
- [Windows MSI 安装包](#windows-msi-安装包)
- [其他平台安装包](#其他平台安装包)
- [故障排除](#故障排除)

## 快速开始

### 生成所有平台的安装包

```powershell
# 构建所有平台并创建 Windows MSI
.\build-all.ps1 -Version "1.0.0"

# 仅构建 Windows 版本
.\build-all.ps1 -Version "1.0.0" -WindowsOnly

# 仅创建 MSI（跳过构建）
.\build-all.ps1 -Version "1.0.0" -MSIOnly
```

### 仅生成跨平台发布包

```powershell
.\build-packages.ps1 -Version "1.0.0"
```

### 仅创建 Windows MSI

```powershell
# 方法1：使用 WiX Toolset
.\create-msi.ps1 -Version "1.0.0"

# 方法2：使用简化方法
.\create-msi-simple.ps1 -Version "1.0.0"
```

## 详细说明

### 项目配置

项目已配置为支持以下发布选项：

- **单文件发布**: `PublishSingleFile=true`
- **自包含**: `SelfContained=true`
- **代码裁剪**: `PublishTrimmed=true`
- **AOT 编译**: `PublishReadyToRun=true`

### 支持的平台

- Windows x64/x86/ARM64
- macOS x64/ARM64
- Linux x64/ARM64

### 生成的文件结构

```
publish/
├── SmartToolbox-1.0.0-win-x64/          # Windows x64 发布文件
├── SmartToolbox-1.0.0-win-x64.zip       # Windows x64 压缩包
├── SmartToolbox-1.0.0-win-x64.msi       # Windows x64 MSI 安装包
├── SmartToolbox-1.0.0-osx-x64/          # macOS x64 发布文件
├── SmartToolbox-1.0.0-osx-x64.zip       # macOS x64 压缩包
├── SmartToolbox-1.0.0-linux-x64/        # Linux x64 发布文件
├── SmartToolbox-1.0.0-linux-x64.zip     # Linux x64 压缩包
└── ...                                  # 其他平台文件
```

## Windows MSI 安装包

### 方法1：使用 WiX Toolset（推荐）

WiX Toolset 是微软官方推荐的 MSI 创建工具。

**安装 WiX:**
```powershell
dotnet tool install --global wix
```

**创建 MSI:**
```powershell
.\create-msi.ps1 -Version "1.0.0" -Platform "win-x64"
```

**MSI 功能:**
- 标准 Windows 安装程序界面
- 开始菜单快捷方式
- 桌面快捷方式（可选）
- 完整的卸载支持
- 版本升级支持

### 方法2：使用简化工具

如果 WiX 不可用，脚本会自动回退到 Inno Setup。

**自动安装工具:**
```powershell
.\create-msi-simple.ps1 -Version "1.0.0"
```

**手动安装 Inno Setup:**
1. 下载 [Inno Setup](https://jrsoftware.org/isinfo.php)
2. 运行生成的 `SmartToolbox.iss` 脚本

### MSI 安装测试

```powershell
# 静默安装
msiexec /i "publish\SmartToolbox-1.0.0-win-x64.msi" /quiet

# 交互式安装
msiexec /i "publish\SmartToolbox-1.0.0-win-x64.msi"

# 卸载
msiexec /x "publish\SmartToolbox-1.0.0-win-x64.msi" /quiet
```

## 其他平台安装包

### macOS DMG

在 macOS 上运行：
```bash
chmod +x create-dmg.sh
./create-dmg.sh
```

### Linux DEB

在 Linux 上运行：
```bash
chmod +x create-deb.sh
./create-deb.sh
```

**安装 DEB 包:**
```bash
sudo dpkg -i SmartToolbox-1.0.0-linux-x64.deb
```

## 故障排除

### 常见问题

**1. 构建失败**
```
错误: 无法找到 .NET SDK
解决: 确保安装了 .NET 8.0 SDK
```

**2. WiX 工具未找到**
```
错误: 未找到 WiX 工具集
解决: 运行 dotnet tool install --global wix
```

**3. 发布文件夹不存在**
```
错误: 未找到发布文件夹
解决: 先运行 .\build-packages.ps1
```

**4. MSI 创建失败**
```
错误: MSI 创建失败
解决: 检查 WiX 版本，或使用备用方法
```

### 环境要求

- .NET 8.0 SDK
- PowerShell 7+（Windows）
- WiX Toolset 4.0+（可选）
- Inno Setup（备用方案）

### 构建优化

**减少文件大小:**
```xml
<PublishTrimmed>true</PublishTrimmed>
<TrimMode>link</TrimMode>
```

**提高启动速度:**
```xml
<PublishReadyToRun>true</PublishReadyToRun>
```

**单文件部署:**
```xml
<PublishSingleFile>true</PublishSingleFile>
```

## 自定义配置

### 修改应用程序信息

在 `SmartToolbox.csproj` 中修改：

```xml
<AssemblyTitle>你的应用名称</AssemblyTitle>
<AssemblyDescription>你的应用描述</AssemblyDescription>
<AssemblyCompany>你的公司名称</AssemblyCompany>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
```

### 修改 MSI 配置

在 `create-msi.ps1` 或 `create-msi-simple.ps1` 中修改相关参数。

### 添加新平台

在 `build-packages.ps1` 的 `$platforms` 数组中添加新的目标平台。

## 持续集成

可以将这些脚本集成到 CI/CD 流水线中：

```yaml
# GitHub Actions 示例
- name: Build and Package
  run: |
    .\build-all.ps1 -Version ${{ github.ref_name }}
  shell: pwsh
```

## 许可证和分发

生成的安装包可以自由分发。确保：

1. 更新版权信息
2. 包含必要的许可证文件
3. 配置代码签名（生产环境）

---

如有问题，请查看项目的 GitHub Issues 或联系维护者。 