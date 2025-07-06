#!/bin/bash
# Linux DEB 包创建脚本
VERSION="1.0.0"
APP_NAME="smart-toolbox"
DEB_NAME="SmartToolbox-$VERSION-linux-x64"

# 创建 DEB 包结构
mkdir -p deb-temp/DEBIAN
mkdir -p deb-temp/usr/bin
mkdir -p deb-temp/usr/share/applications
mkdir -p deb-temp/usr/share/pixmaps

# 复制应用程序文件
cp -r "publish/SmartToolbox-$VERSION-linux-x64"/* deb-temp/usr/bin/

# 创建控制文件
cat > deb-temp/DEBIAN/control << EOF
Package: $APP_NAME
Version: $VERSION
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
dpkg-deb --build deb-temp "publish/$DEB_NAME.deb"

# 清理
rm -rf deb-temp

echo "✓ Linux DEB 包创建完成: publish/$DEB_NAME.deb"
