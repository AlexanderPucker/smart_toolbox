# Smart Toolbox 完整构建和打包脚本
# 包含跨平台发布和 Windows MSI 创建

param(
    [string]$Version = "1.0.0",
    [switch]$SkipBuild,
    [switch]$MSIOnly,
    [switch]$WindowsOnly
)

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "Smart Toolbox 完整构建和打包流程 v$Version" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan

# 步骤1：构建跨平台发布包
if (-not $SkipBuild -and -not $MSIOnly) {
    Write-Host "`n步骤 1: 构建跨平台发布包..." -ForegroundColor Green
    
    if ($WindowsOnly) {
        # 仅构建 Windows 版本
        & .\build-packages.ps1 -Version $Version
    } else {
        # 构建所有平台
        & .\build-packages.ps1 -Version $Version
    }
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ 构建失败，停止执行" -ForegroundColor Red
        exit 1
    }
}

# 步骤2：创建 Windows MSI 安装包
Write-Host "`n步骤 2: 创建 Windows MSI 安装包..." -ForegroundColor Green

# 检查发布文件是否存在
$publishPath = "publish\SmartToolbox-$Version-win-x64"
if (-not (Test-Path $publishPath)) {
    Write-Host "✗ 未找到 Windows 发布文件: $publishPath" -ForegroundColor Red
    Write-Host "请先运行构建命令或使用 -SkipBuild 参数" -ForegroundColor Yellow
    exit 1
}

# 尝试使用不同的方法创建 MSI
Write-Host "正在尝试创建 MSI 安装包..." -ForegroundColor Yellow

# 方法1：使用 WiX Toolset
$wixAvailable = Get-Command "wix" -ErrorAction SilentlyContinue
if ($wixAvailable) {
    Write-Host "使用 WiX Toolset 创建 MSI..." -ForegroundColor Yellow
    & .\create-msi.ps1 -Version $Version -Platform "win-x64"
} else {
    # 方法2：使用简化方法
    Write-Host "使用简化方法创建安装包..." -ForegroundColor Yellow
    & .\create-msi-simple.ps1 -Version $Version -Platform "win-x64"
}

# 步骤3：创建其他平台的安装包脚本
if (-not $WindowsOnly -and -not $MSIOnly) {
    Write-Host "`n步骤 3: 创建其他平台的安装脚本..." -ForegroundColor Green
    
    # 为 macOS 创建 DMG 创建脚本
    $macScript = @"
#!/bin/bash
# macOS DMG 创建脚本
VERSION="$Version"
APP_NAME="Smart Toolbox"
DMG_NAME="SmartToolbox-`$VERSION-osx"

# 创建临时目录
mkdir -p dmg-temp
cp -r "publish/SmartToolbox-`$VERSION-osx-x64" "dmg-temp/`$APP_NAME.app"

# 创建 DMG
hdiutil create -volname "`$APP_NAME" -srcfolder dmg-temp -ov -format UDZO "publish/`$DMG_NAME.dmg"

# 清理
rm -rf dmg-temp

echo "✓ macOS DMG 创建完成: publish/`$DMG_NAME.dmg"
"@
    
    $macScript | Out-File -FilePath "create-dmg.sh" -Encoding UTF8
    Write-Host "✓ macOS DMG 创建脚本已生成: create-dmg.sh" -ForegroundColor Green
    
    # 为 Linux 创建 DEB 包创建脚本
    $linuxScript = @"
#!/bin/bash
# Linux DEB 包创建脚本
VERSION="$Version"
APP_NAME="smart-toolbox"
DEB_NAME="SmartToolbox-`$VERSION-linux-x64"

# 创建 DEB 包结构
mkdir -p deb-temp/DEBIAN
mkdir -p deb-temp/usr/bin
mkdir -p deb-temp/usr/share/applications
mkdir -p deb-temp/usr/share/pixmaps

# 复制应用程序文件
cp -r "publish/SmartToolbox-`$VERSION-linux-x64"/* deb-temp/usr/bin/

# 创建控制文件
cat > deb-temp/DEBIAN/control << EOF
Package: `$APP_NAME
Version: `$VERSION
Section: utils
Priority: optional
Architecture: amd64
Depends: 
Maintainer: Your Company <your-email@company.com>
Description: Smart Toolbox - 一个智能工具箱应用程序
 Smart Toolbox 是一个功能丰富的工具箱应用程序。
EOF

# 创建桌面文件
cat > deb-temp/usr/share/applications/smart-toolbox.desktop << EOF
[Desktop Entry]
Name=Smart Toolbox
Comment=智能工具箱
Exec=/usr/bin/SmartToolbox
Icon=smart-toolbox
Terminal=false
Type=Application
Categories=Utility;
EOF

# 构建 DEB 包
dpkg-deb --build deb-temp "publish/`$DEB_NAME.deb"

# 清理
rm -rf deb-temp

echo "✓ Linux DEB 包创建完成: publish/`$DEB_NAME.deb"
"@
    
    $linuxScript | Out-File -FilePath "create-deb.sh" -Encoding UTF8
    Write-Host "✓ Linux DEB 创建脚本已生成: create-deb.sh" -ForegroundColor Green
}

# 步骤4：显示结果
Write-Host "`n===========================================" -ForegroundColor Cyan
Write-Host "构建和打包完成！" -ForegroundColor Green
Write-Host "===========================================" -ForegroundColor Cyan

if (Test-Path "publish") {
    Write-Host "`n生成的文件:" -ForegroundColor Yellow
    Get-ChildItem "publish" | ForEach-Object {
        $size = if ($_.PSIsContainer) { "文件夹" } else { "{0:N2} MB" -f ($_.Length / 1MB) }
        Write-Host "  $($_.Name) ($size)" -ForegroundColor White
    }
}

Write-Host "`n使用说明:" -ForegroundColor Cyan
Write-Host "- Windows: 运行 .msi 文件进行安装" -ForegroundColor White
Write-Host "- macOS: 在 macOS 上运行 create-dmg.sh 创建 DMG" -ForegroundColor White
Write-Host "- Linux: 在 Linux 上运行 create-deb.sh 创建 DEB 包" -ForegroundColor White
Write-Host "- 其他平台: 直接运行对应的 .zip 文件中的可执行文件" -ForegroundColor White 