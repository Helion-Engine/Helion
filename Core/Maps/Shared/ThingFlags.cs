namespace Helion.Maps.Shared
{
    public class ThingFlags
    {
        public static class DoomFlags
        {
            public const ushort EasyFlag = 0x0001;
            public const ushort MediumFlag = 0x0002;
            public const ushort HardFlag = 0x0004;
            public const ushort AmbushFlag = 0x0008;
            public const ushort MultiPlayerFlag = 0x0010;
            public const ushort NotDeathmatchFlag = 0x0020;
            public const ushort NotCooperativeFlag = 0x0040;   
        }
        
        public static class ZDoomFlags
        {
            public const ushort EasyFlag = 0x0001;
            public const ushort MediumFlag = 0x0002;
            public const ushort HardFlag = 0x0004;
            public const ushort AmbushFlag = 0x0008;
            public const ushort DormantFlag = 0x0010;
            public const ushort FighterFlag = 0x0020;
            public const ushort ClericFlag = 0x0040;
            public const ushort MageFlag = 0x0080;
            public const ushort SinglePlayerFlag = 0x0100;
            public const ushort CooperativeFlag = 0x0200;
            public const ushort DeathmatchFlag = 0x0400;
            public const ushort ShadowFlag = 0x0800;
            public const ushort AltShadowFlag = 0x1000;
            public const ushort FriendlyFlag = 0x2000;
            public const ushort StandStillFlag = 0x4000;
        }
        
        public static class StrifeFlags
        {
        }

        // Doom
        public bool Easy;
        public bool Medium;
        public bool Hard;
        public bool Ambush;
        public bool MultiPlayer;
        // ZDoom/Hexen
        public bool SinglePlayer;
        public bool Deathmatch;
        public bool Cooperative;
        public bool Dormant;
        public bool Fighter;
        public bool Cleric;
        public bool Mage;
        public bool Shadow;
        public bool AltShadow;
        public bool Friendly;
        public bool StandStill;

        public ThingFlags()
        {
        }

        public static ThingFlags Doom(ushort flags)
        {
            return new ThingFlags
            {
                Easy = (flags & DoomFlags.EasyFlag) == DoomFlags.EasyFlag,
                Medium = (flags & DoomFlags.MediumFlag) == DoomFlags.MediumFlag,
                Hard = (flags & DoomFlags.HardFlag) == DoomFlags.HardFlag,
                Ambush = (flags & DoomFlags.AmbushFlag) == DoomFlags.AmbushFlag,
                MultiPlayer = (flags & DoomFlags.MultiPlayerFlag) == DoomFlags.MultiPlayerFlag,
                Deathmatch = (flags & DoomFlags.NotDeathmatchFlag) != DoomFlags.NotDeathmatchFlag,
                Cooperative = (flags & DoomFlags.NotCooperativeFlag) != DoomFlags.NotCooperativeFlag,
            };
        }
        
        public static ThingFlags ZDoom(ushort flags)
        {
            return new ThingFlags
            {
                Easy = (flags & ZDoomFlags.EasyFlag) == ZDoomFlags.EasyFlag,
                Medium = (flags & ZDoomFlags.MediumFlag) == ZDoomFlags.MediumFlag,
                Hard = (flags & ZDoomFlags.HardFlag) == ZDoomFlags.HardFlag,
                Ambush = (flags & ZDoomFlags.AmbushFlag) == ZDoomFlags.AmbushFlag,
                Dormant = (flags & ZDoomFlags.DormantFlag) == ZDoomFlags.DormantFlag,
                Fighter = (flags & ZDoomFlags.FighterFlag) == ZDoomFlags.FighterFlag,
                Cleric = (flags & ZDoomFlags.ClericFlag) == ZDoomFlags.ClericFlag,
                Mage = (flags & ZDoomFlags.MageFlag) == ZDoomFlags.MageFlag,
                SinglePlayer = (flags & ZDoomFlags.SinglePlayerFlag) == ZDoomFlags.SinglePlayerFlag,
                Cooperative = (flags & ZDoomFlags.CooperativeFlag) == ZDoomFlags.CooperativeFlag,
                Deathmatch = (flags & ZDoomFlags.DeathmatchFlag) == ZDoomFlags.DeathmatchFlag,
                Shadow = (flags & ZDoomFlags.ShadowFlag) == ZDoomFlags.ShadowFlag,
                AltShadow = (flags & ZDoomFlags.AltShadowFlag) == ZDoomFlags.AltShadowFlag,
                Friendly = (flags & ZDoomFlags.FriendlyFlag) == ZDoomFlags.FriendlyFlag,
                StandStill = (flags & ZDoomFlags.StandStillFlag) == ZDoomFlags.StandStillFlag,
            };
        }
    }
}