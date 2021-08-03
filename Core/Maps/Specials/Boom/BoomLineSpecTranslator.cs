using Helion.Maps.Specials.Compatibility;
using Helion.Maps.Specials.ZDoom;
using Helion.World.Geometry.Lines;

namespace Helion.Maps.Specials.Boom
{
    public static class BoomLineSpecTranslator
    {
        private enum BoomKey
        {
            AnyKey = 0,
            AllSixKeys = 7
        }

        private const int MaxVanilla = 272;

        // Constants, including masks and shifts from WinMBF.
        // Credit to Lee Killough et al.
        private const int GenericEnd = 0x8000;
        private const int FloorBase = 0x6000;
        private const int CeilingBase = 0x4000;
        private const int DoorBase = 0x3c00;
        private const int LockedBase = 0x3800;
        private const int LiftBase = 0x3400;
        private const int StairsBase = 0x3000;
        private const int CrusherBase = 0x2F80;

        private const int SpecialActivationMask = 0x0007;
        private const int SpecialSpeedMask = 0x0018;

        private const int LiftTargetMask = 0x0300;
        private const int LiftDelayMask = 0x00c0;
        private const int LiftMonsterMask = 0x0020;

        private const int DoorMonsterMask = 0x0080;
        private const int DoorKindMask = 0x0060;
        private const int DoorDelayMask = 0x0300;
        private const int DoorLockKeyMask = 0x01c0;
        private const int DoorLockKindMask = 0x0020;

        private const int StairMonsterMask = 0x0020;
        private const int StairIgnoreMask = 0x0200;
        private const int StairDirectionMask = 0x0100;
        private const int StairStepMask = 0x00c0;

        private const int CrusherMonsterMask = 0x0020;
        private const int CrusherSilentMask = 0x0040;

        private const int PlatCrushMask = 0x1000;
        private const int PlatChangeMask = 0x0c00;
        private const int PlatTargetMask = 0x0380;
        private const int PlatDirectionMask = 0x0040;
        private const int PlatModelMask = 0x0020;

        private const int SpecialSpeedShift = 3;

        private const int LiftDelayShift = 6;

        private const int LiftTargetShift = 8;

        private const int CrusherSilentShift = 6;

        private const int StairIgnoreShift = 9;
        private const int StairDirectionShift = 8;
        private const int StairStepShift = 6;

        private const int DoorDelayShift = 8;
        private const int DoorKindShift = 5;
        private const int DoorLockKindShift = 5;
        private const int DoorLockKeyShift = 6;

        private const int PlatCrushShift = 12;
        private const int PlatChangeShift = 10;
        private const int PlatTargetShift = 7;
        private const int PlatDirectionShift = 6;
        private const int PlatModelShift = 5;

        public static bool IsBoomLineSpecial(ushort special) => special > MaxVanilla;

        public static ZDoomLineSpecialType Translate(LineFlags lineFlags, ushort special, int tag,
            ref SpecialArgs argsToMutate, out LineSpecialCompatibility? compatibility)
        {
            compatibility = null;

            if (special <= CrusherBase || special >= GenericEnd)
                return ZDoomLineSpecialType.None;

            ZDoomLineSpecialType type = ZDoomLineSpecialType.None;
            lineFlags.ActivationType = GetSpecialActivationType(special, out bool repeat);
            lineFlags.Repeat = repeat;

            if (lineFlags.ActivationType != ActivationType.PlayerPushesWall)
                argsToMutate.Arg0 = tag;

            if (special >= FloorBase)
            {
                type = ZDoomLineSpecialType.GenericFloor;
                SetGenericPlat(special, ref argsToMutate);
            }
            else if (special >= CeilingBase)
            {
                type = ZDoomLineSpecialType.GenericCeiling;
                SetGenericPlat(special, ref argsToMutate);
            }
            else if (special >= DoorBase)
            {
                type = ZDoomLineSpecialType.DoorGeneric;
                SetGenericDoor(special, ref argsToMutate, lineFlags);
            }
            else if (special >= LockedBase)
            {
                type = ZDoomLineSpecialType.DoorGeneric;
                SetGenericDoorLock(special, ref argsToMutate);
            }
            else if (special >= LiftBase)
            {
                type = ZDoomLineSpecialType.GenericLift;
                SetGenericLift(special, ref argsToMutate, lineFlags);
            }
            else if (special >= StairsBase)
            {
                type = ZDoomLineSpecialType.StairsGeneric;
                SetGenericStairs(special, ref argsToMutate, lineFlags);
            }
            else if (special >= CrusherBase)
            {
                type = ZDoomLineSpecialType.GenericCrusher;
                SetGenericCrusher(special, ref argsToMutate, lineFlags);
            }

            return type;
        }

        private static void SetGenericCrusher(ushort special, ref SpecialArgs argsToMutate, LineFlags lineFlags)
        {
            if ((special & CrusherMonsterMask) != 0)
                lineFlags.MonsterCanActivate = true;

            argsToMutate.Arg1 = GetCrusherSpeed(special);
            argsToMutate.Arg2 = argsToMutate.Arg1;
            argsToMutate.Arg3 = (special & CrusherSilentMask) >> CrusherSilentShift;
            argsToMutate.Arg4 = 10;
        }

        private static void SetGenericStairs(ushort special, ref SpecialArgs argsToMutate, LineFlags lineFlags)
        {
            if ((special & StairMonsterMask) != 0)
                lineFlags.MonsterCanActivate = true;

            argsToMutate.Arg1 = GetStairSpeed(special);
            argsToMutate.Arg2 = GetStairHeight(special);
            // Flags (Down, Up)
            argsToMutate.Arg3 = (special & StairDirectionMask) >> StairDirectionShift;
            // Flags (Ignore Texture)
            if ((special & StairIgnoreMask) >> StairIgnoreShift != 0)
                argsToMutate.Arg3 |= 2;
        }

        private static void SetGenericPlat(ushort special, ref SpecialArgs argsToMutate)
        {
            argsToMutate.Arg1 = GetPlatSpeed(special);
            // Target
            argsToMutate.Arg3 = ((special & PlatTargetMask) >> PlatTargetShift) + 1;
            if (argsToMutate.Arg3 >= (int)ZDoomGenericDest.Max)
            {
                // 7 = move by 24, 8 = move by 32
                if (argsToMutate.Arg3 == 7)
                    argsToMutate.Arg2 = 24;
                else
                    argsToMutate.Arg2 = 32;
                // Move by height, clear target
                argsToMutate.Arg3 = 0;
            }

            // ZDoomGenericFlags - Include change, direction, model, and crush
            // Change lines up with ZDoomGenericFlags
            argsToMutate.Arg4 = (special & PlatChangeMask) >> PlatChangeShift;

            if ((special & PlatDirectionMask) >> PlatDirectionShift != 0)
                argsToMutate.Arg4 |= (int)ZDoomGenericFlags.Raise;
            if ((special & PlatModelMask) >> PlatModelShift != 0)
                argsToMutate.Arg4 |= (int)ZDoomGenericFlags.TriggerNumericModel;
            if ((special & PlatCrushMask) >> PlatCrushShift != 0)
                argsToMutate.Arg4 |= (int)ZDoomGenericFlags.Crush;
        }

        private static void SetGenericLift(ushort special, ref SpecialArgs argsToMutate, LineFlags lineFlags)
        {
            if ((special & LiftMonsterMask) != 0)
                lineFlags.MonsterCanActivate = true;

            argsToMutate.Arg1 = GetLiftSpeed(special);
            argsToMutate.Arg2 = GetLiftDelay(special);

            // ZDoomLiftType - ZDoom starts at Plat_UpByValue, while boom starts at Plat_DownWaitUpStay, increment to skip
            argsToMutate.Arg3 = ((special & LiftTargetMask) >> LiftTargetShift) + 1;
        }

        private static void SetGenericDoor(ushort special, ref SpecialArgs argsToMutate, LineFlags lineFlags)
        {
            if ((special & DoorMonsterMask) != 0)
                lineFlags.MonsterCanActivate = true;

            // Arg1 = Speed, Arg2 = Kind, Arg3 = Delay
            argsToMutate.Arg1 = GetDoorSpeed(special);
            // ZDoomDoorKind
            argsToMutate.Arg2 = (special & DoorKindMask) >> DoorKindShift;
            argsToMutate.Arg3 = GetDoorDelay(special);
        }

        private static void SetGenericDoorLock(ushort special, ref SpecialArgs argsToMutate)
        {
            // Arg1 = Speed, Arg2 = Kind, Arg3 = Delay, Arg4 = Lock
            argsToMutate.Arg1 = GetDoorSpeed(special);
            // ZDoomDoorKind (OpenDelayClose[0] and OpenStay[1] supported)
            argsToMutate.Arg2 = (special & DoorLockKindMask) >> DoorLockKindShift;
            // Boom couldn't specify a specific delay for locked doors and only supports OpenDelayClose and OpenStay
            // If OpenDelayClose then the delay is 34
            if (argsToMutate.Arg2 == (int)ZDoomDoorKind.OpenDelayClose)
                argsToMutate.Arg3 = 34;

            // All color keys map directly to ZDoom, any and all six need to changed specifically
            argsToMutate.Arg4 = (special & DoorLockKeyMask) >> DoorLockKeyShift;
            if (argsToMutate.Arg4 == (int)BoomKey.AnyKey)
                argsToMutate.Arg4 = (int)ZDoomKeyType.AnyKey;
            else if (argsToMutate.Arg4 == (int)BoomKey.AllSixKeys)
                argsToMutate.Arg4 = (int)ZDoomKeyType.AllSixKeys;
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

        private static int GetDoorDelay(ushort special)
        {
            return ((special & DoorDelayMask) >> DoorDelayShift) switch
            {
                0 => 8,
                1 => 32,
                2 => 72,
                3 => 240,
                _ => 0,
            };
        }

        private static int GetDoorSpeed(ushort special)
        {
            return ((special & SpecialSpeedMask) >> SpecialSpeedShift) switch
            {
                0 => 16,
                1 => 32,
                2 => 64,
                3 => 128,
                _ => 0,
            };
        }

        private static int GetPlatSpeed(ushort special)
        {
            return ((special & SpecialSpeedMask) >> SpecialSpeedShift) switch
            {
                0 => 8,
                1 => 16,
                2 => 32,
                3 => 64,
                _ => 0,
            };
        }

        private static int GetStairSpeed(ushort special)
        {
            return ((special & SpecialSpeedMask) >> SpecialSpeedShift) switch
            {
                0 => 2,
                1 => 4,
                2 => 16,
                3 => 32,
                _ => 0,
            };
        }

        private static int GetStairHeight(ushort special)
        {
            return ((special & StairStepMask) >> StairStepShift) switch
            {
                0 => 4,
                1 => 8,
                2 => 16,
                3 => 24,
                _ => 0,
            };
        }

        private static int GetCrusherSpeed(ushort special)
        {
            return ((special & SpecialSpeedMask) >> SpecialSpeedShift) switch
            {
                0 => 8,
                1 => 16,
                2 => 32,
                3 => 64,
                _ => 0,
            };
        }

        private static int GetLiftDelay(ushort special)
        {
            return ((special & LiftDelayMask) >> LiftDelayShift) switch
            {
                0 => 0,
                1 => 24,
                2 => 40,
                3 => 80,
                _ => 0,
            };
        }

        private static int GetLiftSpeed(ushort special)
        {
            return ((special & SpecialSpeedMask) >> SpecialSpeedShift) switch
            {
                0 => 16,
                1 => 32,
                2 => 64,
                3 => 128,
                _ => 0,
            };
        }
    }
}
