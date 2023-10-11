using Helion.Util.RandomGenerators;

namespace Helion.World.Entities;

public static class EntityStatic
{
    public static IRandom Random;
    public static bool SlowTickEnabled;
    public static int SlowTickChaseFailureSkipCount;
    public static int SlowTickDistance;
    public static int SlowTickChaseMultiplier;
    public static int SlowTickLookMultiplier;
    public static int SlowTickTracerMultiplier;
    public static bool IsFastMonsters;
    public static bool IsSlowMonsters;
    public static int RespawnTimeSeconds;
}
