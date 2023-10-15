using Helion;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.RandomGenerators;
using Helion.World;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.States;
using Helion.World.Physics.Blockmap;
using Helion.World.Sound;
using System.Collections.Generic;

namespace Helion.World;

public static class WorldStatic
{
    public static DynamicArray<BlockmapIntersect> Intersections = new(1024);
    public static IWorld World;
    public static IRandom Random;
    public static DataCache DataCache;
    public static int CheckCounter;
    public static bool SlowTickEnabled;
    public static int SlowTickChaseFailureSkipCount;
    public static int SlowTickDistance;
    public static int SlowTickChaseMultiplier;
    public static int SlowTickLookMultiplier;
    public static int SlowTickTracerMultiplier;
    public static bool IsFastMonsters;
    public static bool IsSlowMonsters;
    public static bool InfinitelyTallThings;
    public static bool MissileClip;
    public static bool AllowItemDropoff;
    public static bool NoTossDrops;
    public static int RespawnTimeSeconds;
    public static int ClosetLookFrameIndex;
    public static int ClosetChaseFrameIndex;
    public static EntityManager EntityManager;
    public static WorldSoundManager SoundManager;
    public static List<EntityFrame> Frames;

    public static EntityDefinition? DoomImpBall;
    public static EntityDefinition? ArachnotronPlasma;
    public static EntityDefinition? Rocket;
    public static EntityDefinition? FatShot;
    public static EntityDefinition? CacodemonBall;
    public static EntityDefinition? RevenantTracer;
    public static EntityDefinition? BaronBall;
    public static EntityDefinition? SpawnShot;
    public static EntityDefinition? BFGBall;
    public static EntityDefinition? PlasmaBall;

    public static void FlushIntersectionReferences()
    {
        for (int i = 0; i < Intersections.Capacity; i++)
        {
            Intersections.Data[i].Entity = null;
            Intersections.Data[i].Line = null;
        }
    }
}
