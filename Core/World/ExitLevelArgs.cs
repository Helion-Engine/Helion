using Helion.Maps.Shared;

namespace Helion.World;

public struct ExitLevelArgs
{
    public static ExitLevelArgs NextMap(LevelChangeFlags flags = LevelChangeFlags.None, int playerSpawnArg0 = 0, double? angle = null)
    {
        return new ExitLevelArgs()
        {
            Type = LevelChangeType.Next,
            Flags = flags,
            PlayerSpawnArg0 = playerSpawnArg0,
            Angle = angle
        };
    }

    public static ExitLevelArgs NextSecretMap(LevelChangeFlags flags = LevelChangeFlags.None, int playerSpawnArg0 = 0, double? angle = null)
    {
        return new ExitLevelArgs()
        {
            Type = LevelChangeType.SecretNext,
            Flags = flags,
            PlayerSpawnArg0 = playerSpawnArg0,
            Angle = angle
        };
    }

    public static ExitLevelArgs SpecificMap(LevelChangeFlags flags, int levelNumber, int playerSpawnArg0, double? angle)
    {
        return new ExitLevelArgs()
        {
            Type = LevelChangeType.SpecificMap,
            Flags = flags,
            LevelNumber = levelNumber,
            PlayerSpawnArg0 = playerSpawnArg0,
            Angle = angle
        };
    }

    public static ExitLevelArgs SpecificMapName(LevelChangeFlags flags, string mapName, int playerSpawnArg0, SkillLevel? skillLevel, double? angle)
    {
        return new ExitLevelArgs()
        {
            Type = LevelChangeType.SpecificMapName,
            Flags = flags,
            MapName = mapName,
            PlayerSpawnArg0 = playerSpawnArg0,
            SkillLevel = skillLevel,
            Angle = angle
        };
    }

    public static ExitLevelArgs LoadNewest()
    {
        return new ExitLevelArgs()
        {
            Type = LevelChangeType.LoadNewest
        };
    }

    public static ExitLevelArgs EndGame()
    {
        return new ExitLevelArgs()
        {
            Type = LevelChangeType.EndGame
        };
    }

    public LevelChangeType Type;
    public LevelChangeFlags Flags;
    public int LevelNumber;
    public int PlayerSpawnArg0;
    public bool RetainFace;
    public string? MapName;
    public SkillLevel? SkillLevel;
    public double? Angle;
}
