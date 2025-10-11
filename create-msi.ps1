# Smart Toolbox MSI 安装包创建脚本
# 需要预先安装 WiX Toolset v4

param(
    [string]$Version = "1.0.0",
    [string]$Platform = "win-x64"
)

Write-Host "创建 Smart Toolbox MSI 安装包..." -ForegroundColor Green

# 检查是否安装了 WiX
$wixPath = Get-Command "wix" -ErrorAction SilentlyContinue
if (-not $wixPath) {
    Write-Host "未找到 WiX 工具集。正在安装..." -ForegroundColor Yellow
    
    # 安装 WiX
    try {
        dotnet tool install --global wix
        Write-Host "✓ WiX 工具集安装成功" -ForegroundColor Green
    } catch {
        Write-Host "✗ WiX 工具集安装失败。请手动安装：dotnet tool install --global wix" -ForegroundColor Red
        exit 1
    }
}

# 创建 WiX 配置目录
$wixDir = "wix"
if (-not (Test-Path $wixDir)) {
    New-Item -ItemType Directory -Path $wixDir | Out-Null
}

# 创建 WiX 源文件
$wxsContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Package Name="Smart Toolbox" 
           Version="$Version" 
           Manufacturer="Your Company" 
           UpgradeCode="12345678-1234-1234-1234-123456789012">
    
    <MajorUpgrade DowngradeErrorMessage="A newer version of Smart Toolbox is already installed." />
    
    <Media Id="1" Cabinet="SmartToolbox.cab" EmbedCab="yes" />
    
    <Feature Id="ProductFeature" Title="Smart Toolbox" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
    </Feature>
    
    <StandardDirectory Id="ProgramFiles64Folder">
      <Directory Id="INSTALLFOLDER" Name="Smart Toolbox" />
    </StandardDirectory>
    
    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
      <Component Id="SmartToolboxExe">
        <File Id="SmartToolboxExe" Source="publish\SmartToolbox-$Version-$Platform\SmartToolbox.exe" KeyPath="yes">
          <Shortcut Id="StartMenuShortcut" 
                    Directory="ProgramMenuFolder" 
                    Name="Smart Toolbox" 
                    WorkingDirectory="INSTALLFOLDER" />
          <Shortcut Id="DesktopShortcut" 
                    Directory="DesktopFolder" 
                    Name="Smart Toolbox" 
                    WorkingDirectory="INSTALLFOLDER" />
        </File>
      </Component>
      
      <!-- 添加其他必要的文件 -->
      <Component Id="AppConfig">
        <File Id="AppConfig" Source="publish\SmartToolbox-$Version-$Platform\SmartToolbox.dll" />
      </Component>
      
      <!-- 运行时文件 -->
      <Component Id="RuntimeFiles">
        <File Id="RuntimeFiles" Source="publish\SmartToolbox-$Version-$Platform\*" />
      </Component>
    </ComponentGroup>
    
    <StandardDirectory Id="ProgramMenuFolder" />
    <StandardDirectory Id="DesktopFolder" />
  </Package>
</Wix>
"@

$wxsPath = "$wixDir\SmartToolbox.wxs"
$wxsContent | Out-File -FilePath $wxsPath -Encoding UTF8

Write-Host "✓ WiX 源文件已创建: $wxsPath" -ForegroundColor Green

# 构建 MSI
try {
    $msiPath = "publish\SmartToolbox-$Version-$Platform.msi"
    
    Write-Host "正在构建 MSI 安装包..." -ForegroundColor Yellow
    
    # 使用 WiX 构建 MSI
    & wix build $wxsPath -out $msiPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ MSI 安装包创建成功: $msiPath" -ForegroundColor Green
    } else {
        Write-Host "✗ MSI 安装包创建失败" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ 构建过程中发生错误: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n要测试安装包，请运行: msiexec /i $msiPath" -ForegroundColor Cyan 