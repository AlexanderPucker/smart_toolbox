# Smart Toolbox 跨平台发布脚本
# 支持 Windows, macOS, Linux 平台

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)

Write-Host "开始构建 Smart Toolbox v$Version" -ForegroundColor Green

# 清理之前的发布文件
if (Test-Path "publish") {
    Remove-Item -Recurse -Force "publish"
}
New-Item -ItemType Directory -Path "publish" | Out-Null

# 定义目标平台
$platforms = @(
    @{ Name = "Windows x64"; RID = "win-x64"; Extension = ".exe" },
    @{ Name = "Windows x86"; RID = "win-x86"; Extension = ".exe" },
    @{ Name = "Windows ARM64"; RID = "win-arm64"; Extension = ".exe" },
    @{ Name = "macOS x64"; RID = "osx-x64"; Extension = "" },
    @{ Name = "macOS ARM64"; RID = "osx-arm64"; Extension = "" },
    @{ Name = "Linux x64"; RID = "linux-x64"; Extension = "" },
    @{ Name = "Linux ARM64"; RID = "linux-arm64"; Extension = "" }
)

foreach ($platform in $platforms) {
    Write-Host "正在构建 $($platform.Name)..." -ForegroundColor Yellow
    
    $outputPath = "publish\SmartToolbox-$Version-$($platform.RID)"
    
    # 发布应用程序
    dotnet publish -c $Configuration -r $platform.RID -o $outputPath --self-contained true
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ $($platform.Name) 构建成功" -ForegroundColor Green
        
        # 创建压缩包
        $zipName = "SmartToolbox-$Version-$($platform.RID).zip"
        Compress-Archive -Path "$outputPath\*" -DestinationPath "publish\$zipName" -Force
        Write-Host "✓ 已创建压缩包: $zipName" -ForegroundColor Green
    } else {
        Write-Host "✗ $($platform.Name) 构建失败" -ForegroundColor Red
    }
}

Write-Host "`n发布完成！文件位于 publish 目录中。" -ForegroundColor Green
Write-Host "要创建 Windows MSI 安装包，请运行: .\create-msi.ps1" -ForegroundColor Cyan 