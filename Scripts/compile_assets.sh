#!/bin/bash

if [ $# -lt 1 ]; then
    echo "Expecting output directory argument"
    exit 1
fi

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
ASSETS_DIR=$SCRIPT_DIR/../Assets/Assets
OUT_DIR=$1
OUTPUT_FILE_NAME="assets.pk3"

if [ ! -d $ASSETS_DIR ]; then
    echo "Cannot find assets directory at $ASSETS_DIR"
    exit 1
fi

echo "Writing $OUTPUT_FILE_NAME to $OUT_DIR"

cd $ASSETS_DIR
rm -f $OUTPUT_FILE_NAME
zip -qq -r $OUT_DIR/$OUTPUT_FILE_NAME .
cd - > /dev/null

