namespace Helion.Maps.Shared
{
    public class MapLineFlags
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
        public const ushort PassUseMask = 0x0200;
        public const ushort RepeatSpecialMask = 0x0200;
        public const ushort ActivatedByUseMask = 0x0400;
        public const ushort ActivatedCrossedByMonsterMask = 0x0800;
        public const ushort ActivatedByImpactMask = 0x0C00;
        public const ushort ActivatedByPushMask = 0x1000;
        public const ushort ActivatedProjectileCrossMask = 0x1400;
        public const ushort ActivatedByUsePassThroughMask = 0x1800;
        public const ushort ActivatedByPlayersAndMonstersMask = 0x2000;
        public const ushort BlockPlayersMask = 0x4000;
        public const ushort BlockEverythingMask = 0x8000;
        
        public bool BlockPlayersAndMonsters;
        public bool BlockMonsters;
        public bool UpperUnpegged;
        public bool LowerUnpegged;
        public bool DrawAsOneSidedAutomap;
        public bool BlockSound;
        public bool NoDrawAutomap;
        public bool AlwaysDrawAutomap;
        public bool PassUse;
        public bool RepeatSpecial;
        public bool ActivatedByUse;
        public bool ActivatedCrossedByMonster;
        public bool ActivatedByImpact;
        public bool ActivatedByPush;
        public bool ActivatedProjectileCross;
        public bool ActivatedByUsePassThrough;
        public bool ActivatedByPlayersAndMonsters;
        public bool BlockPlayers;
        public bool BlockEverything;

        private MapLineFlags(ushort flags)
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

        public static MapLineFlags Doom(ushort flags)
        {
            return new MapLineFlags(flags)
            {
                PassUse = (flags & PassUseMask) == PassUseMask,
            };
        }
        
        public static MapLineFlags ZDoom(ushort flags)
        {
            return new MapLineFlags(flags)
            {
                RepeatSpecial = (flags & RepeatSpecialMask) == RepeatSpecialMask,
                ActivatedByUse = (flags & ActivatedByUseMask) == ActivatedByUseMask,
                ActivatedCrossedByMonster = (flags & ActivatedCrossedByMonsterMask) == ActivatedCrossedByMonsterMask,
                ActivatedByImpact = (flags & ActivatedByImpactMask) == ActivatedByImpactMask,
                ActivatedByPush = (flags & ActivatedByPushMask) == ActivatedByPushMask,
                ActivatedProjectileCross = (flags & ActivatedProjectileCrossMask) == ActivatedProjectileCrossMask,
                ActivatedByUsePassThrough = (flags & ActivatedByUsePassThroughMask) == ActivatedByUsePassThroughMask,
                ActivatedByPlayersAndMonsters = (flags & ActivatedByPlayersAndMonstersMask) == ActivatedByPlayersAndMonstersMask,
                BlockPlayers = (flags & BlockPlayersMask) == BlockPlayersMask,
                BlockEverything = (flags & BlockEverythingMask) == BlockEverythingMask,
            };
        }
    }
}