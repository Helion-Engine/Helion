using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Helion.Strings;

public partial class StringBuffer
{
    internal const int UNICODE_PLANE00_END = 0x00ffff;
    // The starting codepoint for Unicode plane 1.  Plane 1 contains 0x010000 ~ 0x01ffff.
    internal const int UNICODE_PLANE01_START = 0x10000;
    // The end codepoint for Unicode plane 16.  This is the maximum code point value allowed for Unicode.
    // Plane 16 contains 0x100000 ~ 0x10ffff.
    internal const int UNICODE_PLANE16_END = 0x10ffff;

    internal const int HIGH_SURROGATE_START = 0x00d800;
    internal const int LOW_SURROGATE_END = 0x00dfff;

    private static class CharUnicodeInfo
    {
        internal const char HIGH_SURROGATE_START = '\ud800';
        internal const char HIGH_SURROGATE_END = '\udbff';
        internal const char LOW_SURROGATE_START = '\udc00';
        internal const char LOW_SURROGATE_END = '\udfff';
    }

    // Taken from https://referencesource.microsoft.com/#mscorlib/system/char.cs,0fe3da7070268ee9,references
    // Modified to use StringBuffer to not allocate a string each time a key is pressed.
    public static string ConvertFromUtf32(string str, int utf32)
    {
        Clear(str);
        // For UTF32 values from U+00D800 ~ U+00DFFF, we should throw.  They
        // are considered as irregular code unit sequence, but they are not illegal.
        if ((utf32 < 0 || utf32 > UNICODE_PLANE16_END) || (utf32 >= HIGH_SURROGATE_START && utf32 <= LOW_SURROGATE_END))
        {
            throw new ArgumentOutOfRangeException("utf32", "ArgumentOutOfRange_InvalidUTF32");
        }
        Contract.EndContractBlock();

        if (utf32 < UNICODE_PLANE01_START)
        {
            // This is a BMP character.
            return Append(str, (char)utf32);
        }
        // This is a sumplementary character.  Convert it to a surrogate pair in UTF-16.
        utf32 -= UNICODE_PLANE01_START;
        str = Append(str, (char)((utf32 / 0x400) + (int)CharUnicodeInfo.HIGH_SURROGATE_START));
        str = Append(str, (char)((utf32 % 0x400) + (int)CharUnicodeInfo.LOW_SURROGATE_START));
        return str;
    }
}
