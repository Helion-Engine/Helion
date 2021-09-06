using Helion.Maps.Hexen;
using Helion.Maps.Specials;

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
        public const ushort UseThroughMask = 0x0200;
        public const ushort RepeatSpecialMask = 0x0200;
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
        public bool PassThrough;
        public bool RepeatSpecial;
        public bool BlockPlayers;
        public bool BlockEverything;

        public LineActivations Activations;

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

        public static MapLineFlags Doom(ushort flags) =>
            new MapLineFlags(flags);
        
        public static MapLineFlags ZDoom(ushort flags)
        {
            var mapLineFlags = new MapLineFlags(flags)
            {
                RepeatSpecial = (flags & RepeatSpecialMask) == RepeatSpecialMask,
                BlockPlayers = (flags & BlockPlayersMask) == BlockPlayersMask,
                BlockEverything = (flags & BlockEverythingMask) == BlockEverythingMask,
            };

            mapLineFlags.Activations = GetActivations(flags, out mapLineFlags.PassThrough);
            return mapLineFlags;
        }

        private static LineActivations GetActivations(ushort flags, out bool passThrough)
        {
            passThrough = false;
            LineActivations activations = LineActivations.None;
            ActivationType type = (ActivationType)((flags & 0x1C00) >> 10);

            switch (type)
            {
                case ActivationType.PlayerLineCross:
                    activations = LineActivations.Player | LineActivations.CrossLine;
                    break;

                case ActivationType.PlayerUse:
                    activations = LineActivations.Player | LineActivations.UseLine;
                    break;

                case ActivationType.MonsterLineCross:
                    activations = LineActivations.Monster | LineActivations.CrossLine;
                    break;

                case ActivationType.ProjectileOrHitscanHitsOrCrossesLine:
                    activations = LineActivations.Projectile | LineActivations.Hitscan | LineActivations.ImpactLine | LineActivations.CrossLine;
                    break;

                case ActivationType.PlayerPushesWall:
                    activations = LineActivations.Player | LineActivations.ImpactLine;
                    break;

                case ActivationType.ProjectileCrossesLine:
                    activations = LineActivations.Projectile | LineActivations.CrossLine;
                    break;

                case ActivationType.PlayerUsePassThrough:
                    passThrough = true;
                    activations = LineActivations.Player | LineActivations.UseLine;
                    break;

                case ActivationType.PlayerLineCrossThrough:
                    passThrough = true;
                    activations = LineActivations.Player | LineActivations.CrossLine;
                    break;

                default:
                    break;
            }

            return activations;
        }
    }
}