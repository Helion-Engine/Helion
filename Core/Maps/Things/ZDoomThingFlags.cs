namespace Helion.Maps.Things
{
    public class ZDoomThingFlags
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
        
        public readonly bool Easy;
        public readonly bool Medium;
        public readonly bool Hard;
        public readonly bool Ambush;
        public readonly bool Dormant;
        public readonly bool Fighter;
        public readonly bool Cleric;
        public readonly bool Mage;
        public readonly bool SinglePlayer;
        public readonly bool Cooperative;
        public readonly bool Deathmatch;
        public readonly bool Shadow;
        public readonly bool AltShadow;
        public readonly bool Friendly;
        public readonly bool StandStill;

        public ZDoomThingFlags(ushort flags)
        {
            Easy = (flags & EasyFlag) == EasyFlag;
            Medium = (flags & MediumFlag) == MediumFlag;
            Hard = (flags & HardFlag) == HardFlag;
            Ambush = (flags & AmbushFlag) == AmbushFlag;
            Dormant = (flags & DormantFlag) == DormantFlag;
            Fighter = (flags & FighterFlag) == FighterFlag;
            Cleric = (flags & ClericFlag) == ClericFlag;
            Mage = (flags & MageFlag) == MageFlag;
            SinglePlayer = (flags & SinglePlayerFlag) == SinglePlayerFlag;
            Cooperative = (flags & CooperativeFlag) == CooperativeFlag;
            Deathmatch = (flags & DeathmatchFlag) == DeathmatchFlag;
            Shadow = (flags & ShadowFlag) == ShadowFlag;
            AltShadow = (flags & AltShadowFlag) == AltShadowFlag;
            Friendly = (flags & FriendlyFlag) == FriendlyFlag;
            StandStill = (flags & StandStillFlag) == StandStillFlag;
        }
    }
}