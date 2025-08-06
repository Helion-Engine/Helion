using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Util;
using System.Runtime.CompilerServices;

namespace Helion.World.Special.Specials;

public static class ActionSpecials
{
    public static bool ThingSpawn(IWorld world, in SpecialArgs args, bool teleportFog)
    {
        if (!ThingSpawnTypes.Lookup.TryGetValue(args.Arg1, out var definitionName))
            return false;

        var angle = FromByteAngle(args.Arg2);
        var newTid = args.Arg3;
        var entityDef = world.EntityManager.DefinitionComposer.GetByName(definitionName);
        if (entityDef == null)
            return false;

        var success = false;
        var spots = world.FindByTid(args.Arg0);
        foreach (var spot in spots)
            success = world.SpawnEntity(entityDef, spot.Position, newTid, angle, default, teleportFog) != null || success;
        return success;
    }

    public static bool ThingSpawnFacing(IWorld world, in SpecialArgs args)
    {
        if (!ThingSpawnTypes.Lookup.TryGetValue(args.Arg1, out var definitionName))
            return false;

        var entityDef = world.EntityManager.DefinitionComposer.GetByName(definitionName);
        if (entityDef == null)
            return false;

        var teleportFog = args.Arg2 != 0;
        var newTid = args.Arg3;
        var success = false;
        var spots = world.FindByTid(args.Arg0);
        foreach (var spot in spots)
            success = world.SpawnEntity(entityDef, spot.Position, newTid, spot.AngleRadians, default, teleportFog) != null || success;
        return success;
    }

    public static bool ThingProjectile(IWorld world, in SpecialArgs args, bool gravity)
    {
        if (!ThingSpawnTypes.Lookup.TryGetValue(args.Arg1, out var definitionName))
            return false;

        var angle = FromByteAngle(args.Arg2);
        var newTid = args.Arg3;
        var entityDef = world.EntityManager.DefinitionComposer.GetByName(definitionName);
        if (entityDef == null)
            return false;

        var success = false;
        var spots = world.FindByTid(args.Arg0);
        foreach (var spot in spots)
        {
            var entity = world.SpawnEntity(entityDef, spot.Position, newTid, angle, default, false);
            if (entity == null)
                continue;

            success = true;
            const double SpeedFactor = 1 / 8.0;
            var horizontalSpeed = args.Arg3 * SpeedFactor;
            var verticalSpeed = args.Arg3 * SpeedFactor;

            var xy = Vec2D.UnitCircle(angle) * new Vec2D(horizontalSpeed, horizontalSpeed);
            entity.Velocity.X = xy.X;
            entity.Velocity.Y = xy.Y;
            entity.Velocity.Z = verticalSpeed;

            if (!gravity)
            {
                entity.Flags.NoGravity = false;
                entity.Gravity = SpeedFactor;
            }
        }

        return success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double FromByteAngle(int angle)
    {
        return angle / 32 * MathHelper.QuarterPi; 
    }
}
