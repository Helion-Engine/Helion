#!/bin/sh
startdir=$PWD;
scriptdir=$(dirname $0);
cd $scriptdir;
cd ../;

# TODO:  Install any other required tools and deps needed for the build

# Remove any existing AppImage

rm -rf Helion.AppImage;

if ! (./Scripts/buildAndPrepareAppDir.sh);
then
    echo "Failed to build and create app dir";
    exit 1;
fi

# Copy icon and .desktop file
cp -r Scripts/appImageResources/* AppDir;
chmod +x AppDir/AppRun;
cp Assets/Misc/Helion.desktop AppDir;

# Download AppImage tool
if [ ! -f appimagetool-x86_64.AppImage ]; then
    echo 'Downloading AppImage tool from GitHub';
    wget https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage;
    chmod +x appimagetool-x86_64.AppImage;
fi

ARCH=x86_64;
if ! (./appimagetool-x86_64.AppImage AppDir Helion.AppImage); then
    echo 'AppImage pack failed!';
    exit 2;
else
    echo 'AppImage created: Helion.AppImage';
fi

# Clean up
rm -rf AppDir;

cd $startdir;
