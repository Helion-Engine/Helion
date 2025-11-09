namespace Helion.World;

public struct ExitLevelArgs
{
    public static ExitLevelArgs NextMap(LevelChangeFlags flags = LevelChangeFlags.None, int playerSpawnArg0 = 0, bool retainFace = false)
    {
        return new ExitLevelArgs()
        {
            Type = LevelChangeType.Next,
            Flags = flags,
            PlayerSpawnArg0 = playerSpawnArg0,
            RetainFace = retainFace
        };
    }

    public static ExitLevelArgs NextSecretMap(LevelChangeFlags flags = LevelChangeFlags.None, int playerSpawnArg0 = 0, bool retainFace = false)
    {
        return new ExitLevelArgs()
        {
            Type = LevelChangeType.SecretNext,
            Flags = flags,
            PlayerSpawnArg0 = playerSpawnArg0,
            RetainFace = retainFace
        };
    }

    public static ExitLevelArgs SpecificMap(LevelChangeFlags flags, int levelNumber, int playerSpawnArg0, bool retainFace)
    {
        return new ExitLevelArgs()
        {
            Type = LevelChangeType.SpecificMap,
            Flags = flags,
            LevelNumber = levelNumber,
            PlayerSpawnArg0 = playerSpawnArg0,
            RetainFace = retainFace
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
}
