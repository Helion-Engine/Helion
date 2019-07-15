using System;

namespace Helion.Maps
{
    /// <summary>
    /// An enumeration of all the flags on things.
    /// </summary>
    [Flags]
    public enum ThingFlag : ushort
    {
        Easy = 0x0001,
        Medium = 0x0002,
        Hard = 0x0004,
        Ambush = 0x0008,
        Dormant = 0x0010,
        Fighter = 0x0020,
        Cleric = 0x0040,
        Mage = 0x0090,
        SinglePlayer = 0x0100,
        Cooperative = 0x0200,
        Deathmatch = 0x0400,
        SlightlyTranslucent = 0x0800,
        FullyTranslucent = 0x1000,
        Friendly = 0x2000,
        StandStill = 0x4000,
        NotSinglePlayer = 0x0010,
        NotDeathmatch = 0x0020,
        NotCooperative = 0x0040,
        BadEditorCheck = 0x0100,
    }
}