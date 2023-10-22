#!/bin/bash

if [ $# -lt 1 ]; then
    echo "Expecting output directory argument for soundfont copying"
    exit 1
fi

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
SOUNDFONT_FILE_NAME=Default.sf2
SOUNDFONT_DIR=$SCRIPT_DIR/../Client/SoundFonts
OUTPUT_DIR=$1

mkdir -p $OUTPUT_DIR/SoundFonts
cp $SOUNDFONT_DIR/$SOUNDFONT_FILE_NAME $OUTPUT_DIR/SoundFonts/$SOUNDFONT_FILE_NAME
echo "Copied soundfont to $OUTPUT_DIR/SoundFonts/$SOUNDFONT_FILE_NAME"
