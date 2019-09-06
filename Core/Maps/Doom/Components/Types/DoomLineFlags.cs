namespace Helion.Maps.Doom.Components.Types
{
    public struct DoomLineFlags
    {
        public const ushort BlockPlayersAndMonstersMask = 0x0001;
        public const ushort BlockMonstersMask = 0x0002;
        // Note: We don't use the TwoSided flag of 0x0004.
        public const ushort UpperUnpeggedMask = 0x0008;
        public const ushort LowerUnpeggedMask = 0x0010;
        public const ushort DrawAsOneSidedAutomapMask = 0x0020;
        public const ushort BlockSoundMask = 0x0040;
        public const ushort NoDrawAutomapMask = 0x0080;
        public const ushort AlwaysDrawAutomapMask = 0x0100;

        public readonly bool BlockPlayersAndMonsters;
        public readonly bool BlockMonsters;
        public readonly bool UpperUnpegged;
        public readonly bool LowerUnpegged;
        public readonly bool DrawAsOneSidedAutomap;
        public readonly bool BlockSound;
        public readonly bool NoDrawAutomap;
        public readonly bool AlwaysDrawAutomap;
        
        public DoomLineFlags(ushort flags)
        {
            BlockPlayersAndMonsters = (flags & BlockPlayersAndMonstersMask) == BlockPlayersAndMonstersMask;
            BlockMonsters = (flags & BlockMonstersMask) == BlockMonstersMask;
            UpperUnpegged = (flags & UpperUnpeggedMask) == UpperUnpeggedMask;
            LowerUnpegged = (flags & LowerUnpeggedMask) == LowerUnpeggedMask;
            DrawAsOneSidedAutomap = (flags & DrawAsOneSidedAutomapMask) == DrawAsOneSidedAutomapMask;
            BlockSound = (flags & BlockSoundMask) == BlockSoundMask;
            NoDrawAutomap = (flags & NoDrawAutomapMask) == NoDrawAutomapMask;
            AlwaysDrawAutomap = (flags & AlwaysDrawAutomapMask) == AlwaysDrawAutomapMask;
        }
    }
}