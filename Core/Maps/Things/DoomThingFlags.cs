namespace Helion.Maps.Things
{
    public class DoomThingFlags
    {
        public const ushort EasyFlag = 0x0001;
        public const ushort MediumFlag = 0x0002;
        public const ushort HardFlag = 0x0004;
        public const ushort AmbushFlag = 0x0008;
        public const ushort NotSinglePlayerFlag = 0x0010;
        public const ushort NotDeathmatchFlag = 0x0020;
        public const ushort NotCooperativeFlag = 0x0040;
        
        public readonly bool Easy;
        public readonly bool Medium;
        public readonly bool Hard;
        public readonly bool Ambush;
        public readonly bool SinglePlayer;
        public readonly bool Deathmatch;
        public readonly bool Cooperative;

        public DoomThingFlags(ushort flags)
        {
            Easy = (flags & EasyFlag) == EasyFlag;
            Medium = (flags & MediumFlag) == MediumFlag;
            Hard = (flags & HardFlag) == HardFlag;
            Ambush = (flags & AmbushFlag) == AmbushFlag;
            SinglePlayer = (flags & NotSinglePlayerFlag) != NotSinglePlayerFlag;
            Deathmatch = (flags & NotDeathmatchFlag) != NotDeathmatchFlag;
            Cooperative = (flags & NotCooperativeFlag) != NotCooperativeFlag;
        }
    }
}