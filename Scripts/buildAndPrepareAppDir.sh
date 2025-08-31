#!/bin/sh
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

cd $startdir;
