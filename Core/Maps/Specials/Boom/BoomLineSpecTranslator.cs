using Helion.Maps.Specials.Compatibility;
using Helion.Maps.Specials.ZDoom;
using Helion.World.Geometry.Lines;
using NLog;
using System;

namespace Helion.Maps.Specials.Boom
{
    public static class BoomLineSpecTranslator
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const int MaxVanilla = 272;
        private const int StairsBase = 0x3000;
        private const int LiftBase = 0x3400;
        private const int LockedBase = 0x3800;
        private const int DoorBase = 0x3c00;
        private const int CeilingBase = 0x4000;
        private const int CrusherBase = 0x2F80;
        private const int FloorBase = 0x6000;

        private const int SpecialActivationMask = 0x0007;
        private const int SpecialSpeedMask = 0x0018;

        public static bool IsBoomLineSpecial(ushort special) => special > MaxVanilla;

        public static ZDoomLineSpecialType Translate(LineFlags lineFlags, ushort special, int tag,
            ref SpecialArgs argsToMutate, out LineSpecialCompatibility? compatibility)
        {
            compatibility = null;

            if (special <= CrusherBase)
                return ZDoomLineSpecialType.None;

            ZDoomLineSpecialType type = ZDoomLineSpecialType.None;
            lineFlags.ActivationType = GetSpecialActivationType(special, out bool repeat);
            lineFlags.Repeat = repeat;

            if (lineFlags.ActivationType != ActivationType.PlayerPushesWall)
                argsToMutate.Arg0 = tag;

            if (special <= StairsBase)
            {
                type = ZDoomLineSpecialType.GenericCrusher;
            }
            else if (special <= LiftBase)
            {
                type = ZDoomLineSpecialType.StairsGeneric;
            }
            else if (special <= LockedBase)
            {
                type = ZDoomLineSpecialType.GenericLift;
                SetGenericLift(special, lineFlags, ref argsToMutate);
            }
            else if (special <= DoorBase)
            {
                type = ZDoomLineSpecialType.DoorGeneric;
                SetGenericDoorLock(special, ref argsToMutate);
            }
            else if (special <= CeilingBase)
            {
                type = ZDoomLineSpecialType.DoorGeneric;
                SetGenericDoor(special, ref argsToMutate);
            }
            else
            {
                if (special >= FloorBase)
                    type = ZDoomLineSpecialType.GenericFloor;
                else if (special >= CeilingBase)
                    type = ZDoomLineSpecialType.GenericCeiling;
                else
                {
                    // bad type
                }

                // Arg1 = Speed, Arg2 = Height, Arg3 = Target, Arg4 = Flags
                SetGenericPlat(special, ref argsToMutate);
            }

            return type;
        }

        private static void SetGenericPlat(ushort special, ref SpecialArgs argsToMutate)
        {
            argsToMutate.Arg1 = GetPlatSpeed(special);
            argsToMutate.Arg3 = ((special & 0x0380) >> 7) + 1;
            if (argsToMutate.Arg3 > 6)
            {
                argsToMutate.Arg2 = 24 + (argsToMutate.Arg3 - 7) * 8;
                argsToMutate.Arg3 = 0;
            }
            
            argsToMutate.Arg4 = ((special & 0x0c00) >> 10) | ((special & 0x0060) >> 3) | ((special & 0x1000) >> 8);
        }

        private static void SetGenericLift(ushort special, LineFlags lineFlags, ref SpecialArgs argsToMutate)
        {
            // Allows monster activation
            //if (special & 0x20 != 0)

            switch (special & SpecialSpeedMask)
            {
                case 0:
                    argsToMutate.Arg1 = 16;
                    break;
                case 8:
                    argsToMutate.Arg1 = 32;
                    break;
                case 16:
                    argsToMutate.Arg1 = 64;
                    break;
                case 32:
                    argsToMutate.Arg1 = 128;
                    break;
            }

            switch (special & 0xc0)
            {
                case 0:
                    argsToMutate.Arg2 = 0;
                    break;
                case 64:
                    argsToMutate.Arg2 = 24;
                    break;
                case 128:
                    argsToMutate.Arg2 = 40;
                    break;
                case 192:
                    argsToMutate.Arg2 = 80;
                    break;
            }

            argsToMutate.Arg3 = ((special & 0x0300) >> 8) + 1;
        }

        private static void SetGenericDoor(ushort special, ref SpecialArgs argsToMutate)
        {
            // Allows monster activation
            //if (special & 0x80)

            // Arg1 = Speed, Arg2 = Kind, Arg3 = Delay
            argsToMutate.Arg1 = GetDoorSpeed(special);
            argsToMutate.Arg2 = (special & 0x0020) >> 5;

            switch (special & 0x0300)
            {
                case 0:
                    argsToMutate.Arg3 = 8;
                    break;
                case 256:
                    argsToMutate.Arg3 = 32;
                    break;
                case 512:
                    argsToMutate.Arg3 = 72;
                    break;
                case 768:
                    argsToMutate.Arg3 = 240;
                    break;
            }
        }

        private static void SetGenericDoorLock(ushort special, ref SpecialArgs argsToMutate)
        {
            // Arg1 = Speed, Arg2 = Kind, Arg3 = Delay, Arg4 = Lock
            argsToMutate.Arg1 = GetDoorSpeed(special);
            argsToMutate.Arg2 = (special & 0x0020) >> 5;
            if (argsToMutate.Arg2 == 0)
                argsToMutate.Arg3 = 34;
            argsToMutate.Arg4 = (special & 0x01c0) >> 6;

            if (argsToMutate.Arg4 == 0)
                argsToMutate.Arg4 = (int)ZDoomKeyType.AnyKey;
            else if (argsToMutate.Arg4 == 7)
                argsToMutate.Arg4 = (int)ZDoomKeyType.AllSixKeys;

            argsToMutate.Arg4 |= (special & 0x0020) >> 2;
        }

        private static int GetDoorSpeed(ushort special)
        {
            return (special & SpecialSpeedMask) switch
            {
                0 => 16,
                8 => 32,
                16 => 64,
                24 => 128,
                _ => 0,
            };
        }

        private static int GetPlatSpeed(ushort special)
        {
            return (special & SpecialSpeedMask) switch
            {
                0 => 8,
                8 => 16,
                16 => 32,
                24 => 64,
                _ => 0,
            };
        }

        private static ActivationType GetSpecialActivationType(ushort special, out bool repeat)
        {
            repeat = false;
            BoomActivationType boomActivationType = (BoomActivationType)(special & SpecialActivationMask);

            switch (boomActivationType)
            {
                case BoomActivationType.WalkOnce:
                    return ActivationType.PlayerOrMonsterLineCross;

                case BoomActivationType.WalkRepeat:
                    repeat = true;
                    return ActivationType.PlayerOrMonsterLineCross;

                case BoomActivationType.SwitchOnce:
                case BoomActivationType.PushOnce:
                    return ActivationType.PlayerUse;

                case BoomActivationType.SwitchRepeat:
                case BoomActivationType.PushRepeat:
                    repeat = true;
                    return ActivationType.PlayerUse;

                case BoomActivationType.ShootOnce:
                    return ActivationType.ProjectileHitsOrCrossesLine;

                case BoomActivationType.ShootRepeat:
                    repeat = true;
                    return ActivationType.ProjectileHitsOrCrossesLine;
            }

            return ActivationType.None;
        }
    }
}
