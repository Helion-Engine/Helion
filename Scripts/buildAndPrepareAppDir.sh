#!/bin/sh

# This builds a generic app dir that might be a basis for distributing as a DEB or RPM, packaging into a tarball, making an AppImage, etc.
# Some of the contents will need to be _deleted_ for some of these scenarios--for example, the contents of /usr/lib are needed for AppImage,
# but should not be in a DEB or RPM.

startdir=$PWD;
scriptdir=$(dirname $0);
cd $scriptdir;

rm -rf AppDir;

# Do build
cd ../Client
if ! (dotnet publish -c Release -r linux-x64 -p:AOT=true); then
    echo 'Build failed!';
    exit 1;
fi

# Copy executable and support files
cd ..
echo 'Copying files to AppDir/';
mkdir -p AppDir/usr/bin;
cp Publish/linux-x64_AOT/Helion AppDir/usr/bin;

mkdir -p AppDir/opt/Helion;
cp -r Publish/linux-x64_AOT/* AppDir/opt/Helion;
rm AppDir/opt/Helion/Helion;

# Copy all transitive dependencies; patch Helion ELF to use copied lib dir
mkdir -p AppDir/usr/lib;
ldd Publish/linux-x64_AOT/Helion | awk 'NF == 4 { system("cp " $3 " AppDir/usr/lib") }';
rm AppDir/usr/lib/libm.so.*;
rm AppDir/usr/lib/libc.so.*;
patchelf --force-rpath --set-rpath '$ORIGIN'/../usr/lib AppDir/usr/bin/Helion

mkdir -p AppDir/usr/share/applications
cp Assets/Misc/Helion.desktop AppDir/usr/share/applications

mkdir -p AppDir/usr/share/pixmaps
cp Scripts/packageResources/Helion.png AppDir/usr/share/pixmaps

cd $startdir;
