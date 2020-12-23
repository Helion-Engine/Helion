using System;

namespace Helion.Util.Sounds.Mus
{
    [Flags]
    public enum MidiEvent : byte
    {
        ReleaseKey = 0x80,
        PressKey = 0x90,
        ChangeController = 0xB0,
        ChangePatch = 0xC0,
        PitchWheel = 0xE0
    }
}
