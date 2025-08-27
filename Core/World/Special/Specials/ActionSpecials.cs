using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Util;
using Helion.World.Entities;
using System.Runtime.CompilerServices;

namespace Helion.World.Special.Specials;

public static class ActionSpecials
{
    const double SpeedFactor = 1 / 8.0;
    const int ProjectileOffsetZ = -31;

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
        var horizontalSpeed = args.Arg3 * SpeedFactor;
        var verticalSpeed = args.Arg4 * SpeedFactor;

        foreach (var spot in spots)
        {
            var entity = world.FireProjectile(spot, angle, 0, 0, false, entityDef, out _, zOffset: ProjectileOffsetZ);
            if (entity == null)
                continue;

            entity.ThingId = newTid;
            var xy = Vec2D.UnitCircle(angle) * new Vec2D(horizontalSpeed, horizontalSpeed);
            entity.Velocity.X = xy.X;
            entity.Velocity.Y = xy.Y;
            entity.Velocity.Z = verticalSpeed;

            if (gravity)
            {
                entity.Flags.NoGravity = false;
                entity.Gravity = SpeedFactor;
            }

            success = true;
        }

        return success;
    }

    public static bool ThingProjectileAimed(Entity activator, IWorld world, in SpecialArgs args)
    {
        if (!ThingSpawnTypes.Lookup.TryGetValue(args.Arg1, out var definitionName))
            return false;

        var newTid = args.Arg4;
        var entityDef = world.EntityManager.DefinitionComposer.GetByName(definitionName);
        if (entityDef == null)
            return false;

        var success = false;
        var spots = world.FindByTid(args.Arg0);
        var target = GetActivator(activator, world, args.Arg3);
        if (target == null)
            return false;

        var speed = args.Arg2 * SpeedFactor;
        foreach (var spot in spots)
        {
            var angle = spot.Position.Angle(target.Position);
            var pitch = spot.Position.Pitch(target.Position.Z + target.Height / 2, spot.Position.XY.Distance(target.Position.XY));
            var entity = world.FireProjectile(spot, angle, pitch, 0, false, entityDef, out _, zOffset: ProjectileOffsetZ);
            if (entity == null)
                continue;

            entity.Velocity = Vec3D.UnitSphere(angle, pitch) * speed;
            entity.ThingId = newTid;
            success = true;
        }

        return success;
    }

    public static bool ThingDestroy(IWorld world, in SpecialArgs args)
    {
        var gib = args.Arg1 != 0;
        var tag = args.Arg2;
        if (args.Arg0 == 0)
        {
            world.KillAllMonsters(tag);
            return true;
        }

        var destroyEntities = world.FindByTid(args.Arg0);
        var success = false;

        foreach (var entity in destroyEntities)
        {
            if (tag != 0 && entity.Sector.Tag != tag)
                continue;

            if (gib)
                entity.ForceGib();
            else
                entity.Kill(null);

            success = true;
        }

        return success;
    }

    public static bool ThingRemove(IWorld world, in SpecialArgs args)
    {
        var removeEntities = world.FindByTid(args.Arg0);
        if (removeEntities.First != null)
        {
            world.EntityManager.Destroy(removeEntities);
            return true;
        }

        return false;
    }

    private static Entity? GetActivator(Entity activator, IWorld world, int tid)
    {
        if (tid == 0)
            return activator;

        return world.FindByTid(tid).First?.Value;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double FromByteAngle(int angle)
    {
        return angle / 32 * MathHelper.QuarterPi; 
    }
}
