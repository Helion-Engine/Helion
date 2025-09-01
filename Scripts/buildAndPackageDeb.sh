#!/bin/sh

if [ $# -ne 1 ];
then
    echo "Illegal number of parameters: Expected ./buildAndPackageDeb.sh <versionNumber>";
    exit 1;
fi

startdir=$PWD;
scriptdir=$(dirname $0);
cd $scriptdir;
cd ../;

rm Helion-$1.deb;

if ! (./Scripts/buildAndPrepareAppDir.sh);
then
    echo "Failed to build and create app dir";
    exit 2;
fi

# Remove libs, since we're distributing a package
rm -rf AppDir/usr/lib;

# Get the estimated install size
size=`du -shk AppDir | awk '{split($0,a," "); print a[1];}'`

mkdir -p AppDir/DEBIAN;
touch AppDir/DEBIAN/control;
cat > AppDir/DEBIAN/control << EOF
Package: Helion
Version: $1
Section: games
Priority: optional
Depends: libsndfile1, libstdc++6, libglib2.0-0, libasound2, zlib1g
Architecture: amd64
Installed-Size: $size
Maintainer: Helion Team <helion@github.com>
Description: High-performance modern DOOM source port
EOF

dpkg-deb --root-owner-group --build AppDir Helion-$1.deb;

#rm -rf AppDir;

cd $startdir
