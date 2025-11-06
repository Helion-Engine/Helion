using Helion.Models;
using System;
using static Helion.Util.Assertion.Assert;

namespace Helion.World;

public sealed class LevelChangeEvent
{
    public static readonly LevelChangeEvent Default = new(LevelChangeType.Default, LevelChangeFlags.None);

    public readonly LevelChangeType ChangeType;
    public readonly LevelChangeFlags Flags;
    public readonly int LevelNumber = 1;
    public readonly WorldModel? WorldModel;
    public readonly bool IsCheat;
    public readonly int PlayerSpawnArg0;
    public readonly bool RetainFace;

    public bool Cancel { get; set; }

    public LevelChangeEvent(LevelChangeType levelChangeType, LevelChangeFlags flags)
    {
        Precondition(levelChangeType != LevelChangeType.SpecificLevel, "Wrong level change type constructor");

        ChangeType = levelChangeType;
        Flags = flags;
    }

    public LevelChangeEvent(int levelNumber, bool isCheat = false)
    {
        Precondition(levelNumber >= 0, "Cannot have a negative level number");

        ChangeType = LevelChangeType.SpecificLevel;
        LevelNumber = levelNumber;
        IsCheat = isCheat;
    }

    public LevelChangeEvent(ExitLevelArgs args)
    {
        ChangeType = args.Type;
        Flags = args.Flags;
        LevelNumber = args.LevelNumber;
        PlayerSpawnArg0 = args.PlayerSpawnArg0;
        RetainFace = args.RetainFace;
    }
}

public enum LevelChangeType
{
    Default,
    Next,
    SecretNext,
    SpecificLevel,
    Reset,
    ResetOrLoadLast,
    SpecificMap,
    EndGame
}

[Flags]
public enum LevelChangeFlags
{
    None,
    KillAllPlayers,
    ResetInventory
}