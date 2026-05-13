using Helion.Audio;
using Helion.Geometry.Vectors;
using Helion.Graphics.Palettes;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Physics;
using System.Runtime.CompilerServices;

namespace Helion.World.Special.Specials;

public static class ActionSpecials
{
    const double SpeedFactor = 1 / 8.0;
    const int ProjectileOffsetZ = -31;

    private static readonly DynamicArray<int> EntitiesByIndex = new(64);

    public static void ExitNormal(IWorld world, in SpecialArgs args)
    {
        world.ExitLevel(ExitLevelArgs.NextMap(flags: LevelChangeFlags.None, playerSpawnArg0: args.Arg0));
    }

    public static void ExitSecret(IWorld world, in SpecialArgs args)
    {
        world.ExitLevel(ExitLevelArgs.NextSecretMap(flags: LevelChangeFlags.None, playerSpawnArg0: args.Arg0));
    }

    public static void TeleportNewMap(IWorld world, in SpecialArgs args)
    {
        world.ExitLevel(ExitLevelArgs.SpecificMap(LevelChangeFlags.None, args.Arg0, args.Arg1, args.Arg2 > 0));
    }

    public static void TeleportEndGame(IWorld world)
    {
        world.ExitLevel(ExitLevelArgs.EndGame());
    }

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

        var teleportFog = args.Arg2 == 0;
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
        var speedXY = new Vec2D(horizontalSpeed, horizontalSpeed);

        foreach (var spot in spots)
        {
            var entity = world.FireProjectile(spot, angle, 0, 0, false, entityDef, out _, zOffset: ProjectileOffsetZ);
            if (entity == null)
                continue;

            entity.ThingId = newTid;
            var xy = Vec2D.UnitCircle(angle) * speedXY;
            entity.Velocity.X = xy.X;
            entity.Velocity.Y = xy.Y;
            entity.Velocity.Z = verticalSpeed;

            if (gravity)
            {
                entity.Flags.ClearNoGravity();
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

    public static bool SectorSetColor(IWorld world, in SpecialArgs args)
    {
        var colormap = world.ArchiveCollection.Definitions.GetLevelSectorColormap(new((byte)args.Arg1, (byte)args.Arg2, (byte)args.Arg3));
        var sectors = world.FindBySectorTag(args.Arg0);
        for (int i = 0; i < sectors.Count; i++)
        {
            var sector = sectors[i];
            world.SetSectorColorMap(sector, colormap);
        }
        return true;
    }

    public static bool SectorSetFade(IWorld world, in SpecialArgs args)
    {
        var sectors = world.FindBySectorTag(args.Arg0);
        for (int i = 0; i < sectors.Count; i++)
        {
            var sector = sectors[i];
            world.SetSectorFogColor(sector, new((byte)args.Arg1, (byte)args.Arg2, (byte)args.Arg3), sector.FogDensity);
        }
        return true;
    }

    public static bool NoiseAlert(Entity activator, IWorld world, in SpecialArgs args)
    {
        var target = GetActivator(activator, world, args.Arg0);
        var source = GetActivator(activator, world, args.Arg1);
        if (target != null && source != null)
        {
            world.NoiseAlert(target, source);
            return true;
        }

        return false;
    }

    public static bool ThingActivate(Entity activator, IWorld world, in SpecialArgs args)
    {
        if (args.Arg0 == 0)
        {
            activator.Flags.ClearDormant();
            return true;
        }

        var targets = GetActivatorOrEntities(activator, world, args.Arg0);
        for (var target = targets.Current(); target != null; target = targets.Advance())
            target.Flags.ClearDormant();

        return true;
    }

    public static bool ThingDeactivate(Entity activator, IWorld world, in SpecialArgs args)
    {
        if (args.Arg0 == 0)
        {
            activator.Flags.SetDormant();
            return true;
        }

        var targets = GetActivatorOrEntities(activator, world, args.Arg0);
        for (var target = targets.Current(); target != null; target = targets.Advance())
            target.Flags.SetDormant();

        return true;
    }

    public static bool HealThing(Entity activator, IWorld world, in SpecialArgs args)
    {
        var max = args.Arg1;
        if (max == 0 || !activator.IsPlayer)
        {
            if (max == 0)
                max = activator.Properties.Health;

            activator.AddHealth(args.Arg0, max);
            return true;
        }
        else
        {
            if (max == 1)
                max = WorldStatic.MaxSoulsphere;

            activator.AddHealth(args.Arg0, max);
            return true;
        }
    }

    public static bool ThingHate(Entity activator, IWorld world, in SpecialArgs args)
    {
        var sources = GetActivatorOrEntities(activator, world, args.Arg0);
        var targets = GetActivatorOrEntities(activator, world, args.Arg1);

        Entity? findTarget = null;
        for (var target = targets.Current(); target != null; target = targets.Advance())
        {
            if (!target.Flags.Shootable() || target.Flags.Dormant() || target.IsDead())
                continue;

            findTarget = target;
            break;
        }

        if (findTarget == null)
            return true;

        for (var source = sources.Current(); source != null; source = sources.Advance())
        {
            if (!source.Definition.SeeState.HasValue || !source.Flags.Shootable() || source.IsDead() || source.IsPlayer)
                continue;

            source.SetTarget(findTarget);
            source.SetSeeState();
        }

        return true;
    }

    public static bool ThingRaise(Entity activator, IWorld world, in SpecialArgs args)
    {
        var targets = world.FindByTid(args.Arg0);
        for (var targetNode = targets.First; targetNode != null; targetNode = targetNode.Next)
        {
            var target = targetNode.Value;
            if (target.IsDead())
            {
                WorldStatic.SoundManager.CreateSoundOn(target, "vile/raise", new SoundParams(target));
                target.SetRaiseState();
            }
        }
        return true;
    }

    public static bool ThingStop(Entity activator, IWorld world, in SpecialArgs args)
    {
        var targets = GetActivatorOrEntities(activator, world, args.Arg0);
        for (var target = targets.Current(); target != null; target = targets.Advance())
            target.Velocity = Vec3D.Zero;
        return true;
    }

    public static bool ThingDamageTid(Entity activator, IWorld world, in SpecialArgs args)
    {

        var targets = GetActivatorOrEntities(activator, world, args.Arg0);
        for (var target = targets.Current(); target != null; target = targets.Advance())
            world.DamageEntity(target, activator, args.Arg1, DamageType.AlwaysApply, Thrust.None);
        return true;
    }

    public static bool ThingMove(Entity activator, IWorld world, in SpecialArgs args)
    {
        var source = GetActivator(activator, world, args.Arg0);
        var destination = GetActivator(activator, world, args.Arg1);

        if (source == null || destination == null)
            return true;

        var flags = args.Arg2 == 0 ? TeleportFog.Source | TeleportFog.Dest : TeleportFog.None;
        var teleport = new TeleportSpecial(source, null, world, 0, 0, flags);
        teleport.Teleport(destination);
        return true;
    }

    public static bool ThingThrustZ(Entity activator, IWorld world, in SpecialArgs args)
    {
        var force = args.Arg1 / 4.0;
        if (args.Arg2 != 0)
            force = -force;
        var set = args.Arg3 == 0;
        var success = false;
        var targets = GetActivatorOrEntities(activator, world, args.Arg0);
        for (var target = targets.Current(); target != null; target = targets.Advance())
        {
            if (set)
                target.Velocity.Z = force;
            else
                target.Velocity.Z += force;
            success = true;
        }

        return success;
    }

    public static bool ThingChangeTid(Entity activator, IWorld world, in SpecialArgs args)
    {
        var targets = GetActivatorOrEntities(activator, world, args.Arg0);
        for (var target = targets.Current(); target != null; target = targets.Advance())
            target.ThingId = args.Arg1;
        return true;
    }

    public static bool ThingSetSpecial(Entity activator, IWorld world, in SpecialArgs args)
    {
        var targets = GetActivatorOrEntities(activator, world, args.Arg0);
        for (var target = targets.Current(); target != null; target = targets.Advance())
        {
            target.Special = (ZDoomLineSpecialType)args.Arg1;
            target.Args.Arg0 = args.Arg2;
            target.Args.Arg1 = args.Arg3;
            target.Args.Arg2 = args.Arg4;
        }
        return true;
    }

    public static bool TeleportOther(IWorld world, in SpecialArgs args)
    {
        var sourceTid = args.Arg0;
        var targetTid = args.Arg1;
        var fog = args.Arg2 == 0 ? TeleportFog.None : TeleportFog.Source | TeleportFog.Dest;

        if (sourceTid == 0 || targetTid == 0)
            return false;

        return TeleportOther(world, sourceTid, targetTid, fog);
    }

    public static bool TeleportOther(IWorld world, int sourceTid, int targetTid, TeleportFog fog)
    {
        if (sourceTid == 0 || targetTid == 0)
            return false;

        var success = false;

        var teleportEntities = GetEntities(world, sourceTid);
        for (var entity = teleportEntities.Current(); entity != null; entity = teleportEntities.Advance())
        {
            var teleport = new TeleportSpecial(entity, null, world, targetTid, 0, fog);
            success |= teleport.Teleport();
        }

        return success;
    }

    public static bool TeleportInSector(IWorld world, in SpecialArgs args)
    {
        return TeleportInSector(world, args.Arg0, args.Arg1, args.Arg2, args.Arg3 == 0 ? TeleportFog.None : TeleportFog.Source | TeleportFog.Dest, args.Arg4);
    }

    public static bool TeleportInSector(IWorld world, int tag, int sourceTid, int targetTid, TeleportFog fog, int groupTid)
    {
        var sources = GetEntities(world, sourceTid);
        var source = sources.Current();
        if (source == null)
            return false;

        var targets = GetEntities(world, targetTid, Constants.TeleportDest);
        var target = targets.Current();
        if (target == null)
            return false;

        var success = false;
        var sectors = world.FindBySectorTag(tag);
        for (int i = 0; i < sectors.Count; i++)
        {
            var sector = sectors[i];

            // Teleport will modify the sector Entities linked list. Use a temporary storage array
            EntitiesByIndex.Clear();
            for (var entityNode = sector.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                if (entityNode.Value.Flags.NoBlockmap())
                    continue;
                EntitiesByIndex.Add(entityNode.Value.Index);
            }

            for (int j = 0; j < EntitiesByIndex.Length; j++)
            {
                var entity = world.DataCache.Entities[EntitiesByIndex[j]];
                if (groupTid == 0 || entity.ThingId == groupTid)
                    success |= DoTeleportGroup(world, entity, source, target, fog);
            }
        }

        return success;
    }

    public static bool TeleportGroup(Entity activator, IWorld world, in SpecialArgs args)
    {
        var sources = GetEntities(world, args.Arg1);
        var source = sources.Current();
        var fog = args.Arg4 == 0 ? TeleportFog.None : TeleportFog.Source | TeleportFog.Dest;
        if (source == null)
            return TeleportOther(world, args.Arg1, args.Arg2, fog);

        var targets = GetEntities(world, args.Arg2, Constants.TeleportDest);
        var target = targets.Current();
        if (target == null)
            return false;

        var success = false;
        if (args.Arg0 == 0)
        {
            success = DoTeleportGroup(world, activator, source, target, fog);
        }
        else
        {
            var teleportEntities = GetEntities(world, args.Arg0);
            for (var teleportEntity = teleportEntities.Current(); teleportEntity != null; teleportEntity = teleportEntities.Advance())
                success |= DoTeleportGroup(world, teleportEntity, source, target, fog);
        }

        if (success && args.Arg3 != 0)
        {
            var teleport = new TeleportSpecial(source, null, world, 0, 0, fog);
            teleport.Teleport(target);
        }

        return success;
    }

    private static bool DoTeleportGroup(IWorld world, Entity entity, Entity source, Entity teleportSpot, TeleportFog fog)
    {
        var angle = teleportSpot.AngleRadians - source.AngleRadians;
        var diff = entity.Position.XY - source.Position.XY;
        var teleportPosXY = Vec2D.Rotate(diff.X, diff.Y, angle);
        var teleportPos = new Vec3D(teleportSpot.Position.X + teleportPosXY.X, teleportSpot.Position.Y + teleportPosXY.Y, teleportSpot.Position.Z);
        var save = teleportSpot.Position;
        var originalEntityAngle = entity.AngleRadians;
        teleportSpot.Position = teleportPos;

        var teleport = new TeleportSpecial(entity, null, world, 0, 0, fog);
        var success = teleport.Teleport(teleportSpot);

        if (success)
        {
            entity.AngleRadians = originalEntityAngle + angle;
        }

        teleportSpot.Position = save;
        return success;
    }

    public static bool AcsExecute(Entity activator, IWorld world, in SpecialArgs args)
    {
        var threadInfo = new ACS.ThreadInfo { activator = activator };
        var mapId = (args.Arg1 == 0) ? (uint)world.MapInfo.LevelNumber : (uint)args.Arg1;
        var scriptArgs = (uint[])[(uint)args.Arg2, (uint)args.Arg3, (uint)args.Arg4];
        if (args.Arg0Str != null)
        {
            return world.AcsExecutor.ScriptStart( args.Arg0Str, 0, mapId, scriptArgs, threadInfo);
        }
        else
        {
            return world.AcsExecutor.ScriptStart( (uint)args.Arg0, 0, mapId, scriptArgs, threadInfo);
        }
    }

    private static EntityList GetActivatorOrEntities(Entity activator, IWorld world, int tid)
    {
        if (tid == 0)
            return new EntityList(activator);

        return new EntityList(world.FindByTid(tid));
    }

    private static EntityList GetEntities(IWorld world, int tid)
    {
        return new EntityList(world.FindByTid(tid));
    }

    private static EntityList GetEntities(IWorld world, int tid, string className)
    {
        return new EntityList(world.FindByTid(tid), className);
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
