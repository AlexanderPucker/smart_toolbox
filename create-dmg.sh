#!/bin/bash
# macOS DMG 创建脚本
VERSION="1.0.0"
APP_NAME="Smart Toolbox"
DMG_NAME="SmartToolbox-$VERSION-osx"

# 创建临时目录
mkdir -p dmg-temp
cp -r "publish/SmartToolbox-$VERSION-osx-x64" "dmg-temp/$APP_NAME.app"

# 创建 DMG
hdiutil create -volname "$APP_NAME" -srcfolder dmg-temp -ov -format UDZO "publish/$DMG_NAME.dmg"

# 清理
rm -rf dmg-temp

echo "✓ macOS DMG 创建完成: publish/$DMG_NAME.dmg"
