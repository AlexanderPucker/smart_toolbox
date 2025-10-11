# Smart Toolbox 简化 MSI 创建脚本
# 使用 dotnet-msideploy 工具

param(
    [string]$Version = "1.0.0",
    [string]$Platform = "win-x64"
)

Write-Host "使用简化方法创建 Smart Toolbox MSI 安装包..." -ForegroundColor Green

# 检查并安装 dotnet-msideploy
$msiDeployPath = Get-Command "dotnet-msideploy" -ErrorAction SilentlyContinue
if (-not $msiDeployPath) {
    Write-Host "正在安装 dotnet-msideploy..." -ForegroundColor Yellow
    try {
        dotnet tool install --global dotnet-msideploy
        Write-Host "✓ dotnet-msideploy 安装成功" -ForegroundColor Green
    } catch {
        Write-Host "✗ dotnet-msideploy 安装失败，将使用备用方法" -ForegroundColor Yellow
        
        # 备用方法：使用 NSIS 或 Inno Setup
        Write-Host "正在创建 Inno Setup 脚本..." -ForegroundColor Yellow
        
        # 创建 Inno Setup 脚本
        $innoScript = @"
[Setup]
AppName=Smart Toolbox
AppVersion=$Version
AppPublisher=Your Company
AppPublisherURL=https://yourcompany.com
AppSupportURL=https://yourcompany.com/support
AppUpdatesURL=https://yourcompany.com/updates
DefaultDirName={autopf}\Smart Toolbox
DefaultGroupName=Smart Toolbox
AllowNoIcons=yes
LicenseFile=
OutputDir=publish
OutputBaseFilename=SmartToolbox-$Version-Setup
SetupIconFile=Assets\avalonia-logo.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimp.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish\SmartToolbox-$Version-$Platform\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Smart Toolbox"; Filename: "{app}\SmartToolbox.exe"
Name: "{group}\{cm:UninstallProgram,Smart Toolbox}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Smart Toolbox"; Filename: "{app}\SmartToolbox.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\SmartToolbox.exe"; Description: "{cm:LaunchProgram,Smart Toolbox}"; Flags: nowait postinstall skipifsilent
"@
        
        $innoPath = "SmartToolbox.iss"
        $innoScript | Out-File -FilePath $innoPath -Encoding UTF8
        Write-Host "✓ Inno Setup 脚本已创建: $innoPath" -ForegroundColor Green
        Write-Host "要创建安装包，请安装 Inno Setup 并运行此脚本" -ForegroundColor Cyan
        return
    }
}

# 使用 dotnet-msideploy 创建 MSI
try {
    $publishPath = "publish\SmartToolbox-$Version-$Platform"
    $msiPath = "publish\SmartToolbox-$Version-$Platform.msi"
    
    if (-not (Test-Path $publishPath)) {
        Write-Host "✗ 未找到发布文件夹: $publishPath" -ForegroundColor Red
        Write-Host "请先运行: .\build-packages.ps1" -ForegroundColor Yellow
        return
    }
    
    Write-Host "正在创建 MSI 安装包..." -ForegroundColor Yellow
    
    # 使用 dotnet-msideploy 创建 MSI
    & dotnet-msideploy -s $publishPath -o $msiPath -n "Smart Toolbox" -v $Version -p "Your Company"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ MSI 安装包创建成功: $msiPath" -ForegroundColor Green
    } else {
        Write-Host "✗ MSI 安装包创建失败" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ 创建过程中发生错误: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n安装包创建完成！" -ForegroundColor Green
Write-Host "要测试安装，请运行: msiexec /i publish\SmartToolbox-$Version-$Platform.msi" -ForegroundColor Cyan 