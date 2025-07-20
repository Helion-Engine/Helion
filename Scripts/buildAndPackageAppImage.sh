#!/bin/sh
startdir=$PWD;
scriptdir=$(dirname $0);
cd $scriptdir;

# TODO:  Install any other required tools and deps needed for the build

# Do build
cd ../Client
if ! (dotnet publish -c Release -r linux-x64 -p:AOT=true); then
    echo 'Build failed!';
    exit 1;
fi

# Remove any existing AppImage
rm -rf AppImage;
rm -rf Helion.AppImage;

# Copy executable and support files
cd ..
echo 'Copying files for AppImage';
mkdir -p AppImage/usr/bin;
cp -r Publish/linux-x64_AOT/* AppImage/usr/bin;

# Copy all transitive dependencies
mkdir -p AppImage/lib;
ldd Publish/linux-x64_AOT/Helion | awk 'NF == 4 { system("cp " $3 " AppImage/lib") }';

# Copy icon and .desktop file
cp -r Scripts/appImageResources/* AppImage;
chmod +x AppImage/AppRun;

# Download AppImage tool
if [ ! -f appimagetool-x86_64.AppImage ]; then
    echo 'Downloading AppImage tool from GitHub';
    wget https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage;
    chmod +x appimagetool-x86_64.AppImage;
fi

ARCH=x86_64;
if ! (./appimagetool-x86_64.AppImage AppImage Helion.AppImage); then
    echo 'AppImage pack failed!';
    exit 1;
else
    echo 'AppImage created: Helion.AppImage';
fi

# Clean up
rm -rf AppImage;

cd $startdir;
