using System;

namespace Helion.Util.Sounds.Mus;

[Flags]
public enum MusEvent : byte
{
    ReleaseKey = 0x00,
    PressKey = 0x10,
    PitchWheel = 0x20,
    SystemEvent = 0x30,
    ChangeController = 0x40,
    ScoreEnd = 0x60
}
