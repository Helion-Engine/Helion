using FluentAssertions;
using Helion.Models;
using Helion.Resources.IWad;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Geometry.Sides;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using System;
using System.Collections.Generic;
using Xunit;
using Helion.Geometry.Vectors;
using Helion.World.Special;
using Helion.World.Entities.Players;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class Serialization : IDisposable
{
    private SinglePlayerWorld PreviousWorld;
    private SinglePlayerWorld NewWorld;

    private static Entity Zombieman(SinglePlayerWorld world) => GameActions.GetEntity(world, 1);

    public Serialization()
    {
        PreviousWorld = LoadMap(null, WorldInit, disposeExistingWorld: true);
        NewWorld = PreviousWorld;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        PreviousWorld.ArchiveCollection.Dispose();
        PreviousWorld.Dispose();
    }

    [Fact(DisplayName = "Test serialization")]
    public void TestSerialization()
    {
        var model = PreviousWorld.ToWorldModel();
        NewWorld = LoadMap(model, WorldLoadInit);

        AssertWorldEqual(NewWorld);

        ChangeWorld(NewWorld);
        NewWorld = ReloadWorldFromModel(NewWorld);
        AssertWorldEqual(NewWorld);

        AddRevenantTracer(NewWorld);
        NewWorld = ReloadWorldFromModel(NewWorld);
        AssertWorldEqual(NewWorld);

        ChangeWorld2(NewWorld);
        NewWorld = ReloadWorldFromModel(NewWorld);
        AssertWorldEqual(NewWorld);
    }

    private SinglePlayerWorld LoadMap(WorldModel? worldModel, Action<SinglePlayerWorld> onInit, bool disposeExistingWorld = false)
    {
        // Need to dispose the first world's archive collection because it locks the file
        if (worldModel != null && !PreviousWorld.ArchiveCollection.IsDisposed)
            PreviousWorld.ArchiveCollection.Dispose();

        return WorldAllocator.LoadMap("Resources/serialization.zip", "serialization.WAD", "MAP01", Guid.NewGuid().ToString(), onInit, IWadType.Doom2,
            worldModel: worldModel, disposeExistingWorld: disposeExistingWorld);
    }

    private SinglePlayerWorld ReloadWorldFromModel(SinglePlayerWorld existingWorld)
    {
        var model = existingWorld.ToWorldModel();

        if (!ReferenceEquals(PreviousWorld, NewWorld))
            PreviousWorld.Dispose();

        existingWorld.ArchiveCollection.Dispose();
        PreviousWorld = existingWorld;
        return LoadMap(model, WorldLoadInit);
    }

    private static void ChangeWorld(SinglePlayerWorld world)
    {
        var txSector = GameActions.GetSector(world, 41);
        txSector.Floor.TextureHandle.Should().Be(world.TextureManager.GetTexture("NUKAGE1", Resources.ResourceNamespace.Global).Index);

        var transferSkySector = GameActions.GetSectorByTag(world, 99);
        transferSkySector.SkyTextureHandle.Should().NotBeNull();
        world.TextureManager.GetTexture(transferSkySector.SkyTextureHandle!.Value).Name.Should().Be("SKY3");

        GameActions.ActivateLine(world, world.Player, 15, ActivationContext.UseLine).Should().BeTrue();
        GameActions.ActivateLine(world, world.Player, 27, ActivationContext.UseLine).Should().BeTrue();
        GameActions.ActivateLine(world, world.Player, 36, ActivationContext.UseLine).Should().BeTrue();
        GameActions.ActivateLine(world, world.Player, 172, ActivationContext.UseLine).Should().BeTrue();
        GameActions.ActivateLine(world, world.Player, 178, ActivationContext.UseLine).Should().BeTrue();
        GameActions.ActivateLine(world, world.Player, 195, ActivationContext.UseLine).Should().BeTrue();

        GameActions.ActivateLine(world, world.Player, 124, ActivationContext.CrossLine).Should().BeTrue();
        world.Tick();
        GameActions.GetSector(world, 4).ActiveCeilingMove.Should().NotBeNull();
        GameActions.GetSector(world, 6).ActiveFloorMove.Should().NotBeNull();
        GameActions.GetSector(world, 8).ActiveCeilingMove.Should().NotBeNull();
        GameActions.GetSector(world, 36).ActiveCeilingMove.Should().NotBeNull();

        GameActions.GetSector(world, 24).ActiveFloorMove.Should().NotBeNull();
        GameActions.GetSector(world, 25).ActiveFloorMove.Should().NotBeNull();
        GameActions.GetSector(world, 26).ActiveFloorMove.Should().NotBeNull();
        GameActions.GetSector(world, 27).ActiveFloorMove.Should().NotBeNull();
        GameActions.GetSector(world, 28).ActiveFloorMove.Should().NotBeNull();
        GameActions.GetSector(world, 29).ActiveFloorMove.Should().NotBeNull();
        GameActions.GetSector(world, 39).ActiveFloorMove.Should().NotBeNull();

        txSector.ActiveFloorMove.Should().NotBeNull();
        txSector.Floor.TextureHandle.Should().Be(world.TextureManager.GetTexture("FLOOR5_1", Resources.ResourceNamespace.Global).Index);

        GameActions.PlayerRunForward(world, world.Player.AngleRadians, () => { return world.Gametick < 10; });
        GameActions.PlayerFirePistol(world, world.Player);
        GameActions.TickWorld(world, 10);

        GameActions.GetSector(world, 0).SoundTarget.Entity.Should().Be(world.Player);
        var zombieman = Zombieman(world);
        zombieman.Target.Entity.Should().Be(world.Player);
        world.Player.Velocity = new Vec3D(0, 16, 2);
    }

    private static void AddRevenantTracer(SinglePlayerWorld world)
    {
        var revenant = GameActions.CreateEntity(world, "Revenant", new Vec3D(-256, -16, 0), frozen: false);
        revenant.AngleRadians = GameActions.GetAngle(Bearing.South);
        GameActions.TickWorld(world, () => { return revenant.Target.Entity == null; }, () => { });
        revenant.Target.Entity.Should().Be(world.Player);
        
        GameActions.TickWorld(world, () => { return revenant.FrameState.Frame.ActionFunction?.Method.Name.Equals("A_SkelMissile") == false; },
            () => { });
        var tracer = GameActions.GetEntity(world, "RevenantTracer");
        tracer.SetTracer(world.Player);
        tracer.Tracer.Entity.Should().Be(world.Player);
    }

    private static void ChangeWorld2(SinglePlayerWorld world)
    {
        // TODO this broke for some reason
        world.LevelStats.SecretCount.Should().Be(0);
        Zombieman(world).Kill(null);
        GameActions.SetEntityPosition(world, world.Player, new Vec2D(-192, 808));
        GameActions.TickWorld(world, 100);
        world.LevelStats.SecretCount.Should().Be(1);
        world.LevelStats.KillCount.Should().Be(2);
        Zombieman(world).IsDead.Should().BeTrue();
    }

    private void AssertWorldEqual(SinglePlayerWorld world)
    {
        AssertEntitiesEqual(world);
        AssertSectorsEqual(world);
        AssertLinesEqual(world);
        AssertSpecials(world);
        AssertVoodooDolls(world);

        PreviousWorld.LevelTime.Should().Be(world.LevelTime);
        PreviousWorld.Gametick.Should().Be(world.Gametick);
        PreviousWorld.SkillDefinition.Key.Should().Be(world.SkillDefinition.Key);
        PreviousWorld.GlobalData.Equals(world.GlobalData).Should().BeTrue();
        PreviousWorld.LevelStats.Equals(world.LevelStats).Should().BeTrue();

        PreviousWorld.Random.RandomIndex.Should().Be(world.Random.RandomIndex);
    }

    private static void AssertVoodooDolls(SinglePlayerWorld world)
    {
        world.EntityManager.VoodooDolls.Count.Should().Be(2);
        world.EntityManager.Players.Count.Should().Be(1);

        world.EntityManager.VoodooDolls[0].PlayerNumber.Should().Be(0);
        world.EntityManager.VoodooDolls[1].PlayerNumber.Should().Be(0);

        world.EntityManager.Players[0].PlayerNumber.Should().Be(0);
        world.Player.Should().Be(world.EntityManager.Players[0]);
    }

    private void AssertSpecials(SinglePlayerWorld world)
    {
        var previousSpecials = PreviousWorld.SpecialManager.GetSpecials();
        var specials = world.SpecialManager.GetSpecials();
        previousSpecials.Count.Should().Be(specials.Count);

        foreach (var special in specials)
        {
            if (!special.OverrideEquals)
                continue;

            FindMatchingSpecial(special, previousSpecials).Should().BeTrue();
        }
    }

    private static bool FindMatchingSpecial(ISpecial special, IEnumerable<ISpecial> specials)
    {
        foreach (var checkSpecial in specials)
        {
            if (!checkSpecial.OverrideEquals)
                continue;

            if (checkSpecial.Equals(special))
                return true;
        }
        return false;
    }

    private void AssertLinesEqual(SinglePlayerWorld world)
    {
        PreviousWorld.Lines.Count.Should().Be(world.Lines.Count);

        foreach (var line in PreviousWorld.Lines)
        {
            var newLine = GameActions.GetLine(world, line.Id);

            line.LineId.Should().Be(newLine.LineId);
            line.Args.Should().Be(newLine.Args);
            line.Flags.Should().Be(newLine.Flags);
            line.Special.LineSpecialType.Should().Be(newLine.Special.LineSpecialType);
            line.Activated.Should().Be(newLine.Activated);
            line.DataChanges.Should().Be(newLine.DataChanges);
            line.Alpha.Should().Be(newLine.Alpha);

            AssertSide(line.Front, newLine.Front);
            if (line.Back == null)
                newLine.Back.Should().BeNull();

            if (line.Back != null && newLine.Back != null)
                AssertSide(line.Back, newLine.Back);
        }

        static void AssertSide(Side first, Side second)
        {
            first.Upper.TextureHandle.Should().Be(second.Upper.TextureHandle);
            first.Middle.TextureHandle.Should().Be(second.Middle.TextureHandle);
            first.Lower.TextureHandle.Should().Be(second.Lower.TextureHandle);

            first.Offset.Should().Be(second.Offset);
            first.DataChanges.Should().Be(second.DataChanges);
            first.ScrollData?.OffsetUpper.Should().Be(second.ScrollData?.OffsetUpper);
            first.ScrollData?.OffsetMiddle.Should().Be(second.ScrollData?.OffsetMiddle);
            first.ScrollData?.OffsetLower.Should().Be(second.ScrollData?.OffsetLower);

            first.LastRenderGametick.Should().Be(second.LastRenderGametick);
        }
    }

    private void AssertSectorsEqual(SinglePlayerWorld world)
    {
        PreviousWorld.Sectors.Count.Should().Be(world.Sectors.Count);

        foreach (var sector in PreviousWorld.Sectors)
        {
            var newSector = GameActions.GetSector(world, sector.Id);

            sector.LightLevel.Should().Be(newSector.LightLevel);
            sector.TransferHeights?.ControlSector.Id.Should().Be(newSector.TransferHeights?.ControlSector.Id);
            sector.TransferHeights?.ParentSector.Id.Should().Be(newSector.TransferHeights?.ParentSector.Id);
            sector.ActiveFloorMove?.Sector.Id.Should().Be(newSector.ActiveFloorMove?.Sector.Id);
            sector.ActiveCeilingMove?.Sector.Id.Should().Be(newSector.ActiveCeilingMove?.Sector.Id);
            sector.SectorSpecialType.Should().Be(newSector.SectorSpecialType);
            sector.Secret.Should().Be(newSector.Secret);
            sector.DamageAmount .Should().Be(newSector.DamageAmount);
            sector.SkyTextureHandle.Should().Be(newSector.SkyTextureHandle);
            sector.DataChanges.Should().Be(newSector.DataChanges);
            sector.SoundTarget.Entity?.Id.Should().Be(newSector.SoundTarget.Entity?.Id);
            sector.KillEffect.Should().Be(newSector.KillEffect);
            sector.SectorEffect.Should().Be(newSector.SectorEffect);
            sector.SkyTextureHandle.Should().Be(newSector.SkyTextureHandle);
            sector.Friction.Should().Be(newSector.Friction);

            if (sector.SectorDamageSpecial != null)
                sector.SectorDamageSpecial.Equals(newSector.SectorDamageSpecial).Should().BeTrue();

            if (sector.ActiveFloorMove != null)
                sector.ActiveFloorMove.Equals(newSector.ActiveFloorMove).Should().BeTrue();

            if (sector.ActiveCeilingMove != null)
                sector.ActiveCeilingMove.Equals(newSector.ActiveCeilingMove).Should().BeTrue();
        }
    }

    private void AssertEntitiesEqual(SinglePlayerWorld world)
    {
        EntitiesCount(PreviousWorld).Should().Be(EntitiesCount(world));

        for (var entity = PreviousWorld.EntityManager.Head; entity != null; entity = entity.Next)
        {
            var newEntity = GameActions.GetEntity(world, entity.Id);

            Player? player = entity as Player;
            Player? newPlayer = newEntity as Player;

            if (player != null || newPlayer != null)
            {
                entity.Should().NotBeNull();
                newEntity.Should().NotBeNull();
                player!.Equals(player).Should().BeTrue();
            }

            entity.Definition.Name.Should().Be(newEntity.Definition.Name);
            entity.Flags.Should().Be(newEntity.Flags);
            // TODO properties
            entity.FrameState.Should().Be(newEntity.FrameState);
            entity.AngleRadians.Should().Be(newEntity.AngleRadians);
            // Prev position is loaded as Postion when loading a serialized world.
            entity.Position.Should().Be(newEntity.PrevPosition);
            entity.SpawnPoint.Should().Be(newEntity.SpawnPoint);
            entity.Position.Should().Be(newEntity.Position);
            entity.Velocity.Should().Be(newEntity.Velocity);
            entity.FrozenTics.Should().Be(newEntity.FrozenTics);
            entity.MoveCount.Should().Be(newEntity.MoveCount);
            entity.Sector.Id.Should().Be(newEntity.Sector.Id);
            entity.HighestFloorSector.Should().Be(newEntity.HighestFloorSector);
            entity.LowestCeilingSector.Should().Be(newEntity.LowestCeilingSector);
            entity.LowestCeilingZ.Should().Be(newEntity.LowestCeilingZ);
            entity.HighestFloorZ.Should().Be(newEntity.HighestFloorZ);

            entity.IntersectSectors.Length.Should().Be(newEntity.IntersectSectors.Length);
            for (int i = 0; i < entity.IntersectSectors.Length; i++)
                entity.IntersectSectors[i].Id.Should().Be(newEntity.IntersectSectors[i].Id);

            entity.Target.Entity?.Id.Should().Be(newEntity.Target.Entity?.Id);
            entity.Tracer.Entity?.Id.Should().Be(newEntity.Tracer.Entity?.Id);
            entity.OnEntity.Entity?.Id.Should().Be(newEntity.OnEntity.Entity?.Id);
            entity.OverEntity.Entity?.Id.Should().Be(newEntity.OverEntity.Entity?.Id);
            entity.Owner.Entity?.Id.Should().Be(newEntity.Owner.Entity?.Id);

            entity.Threshold.Should().Be(newEntity.Threshold);
            entity.ReactionTime.Should().Be(newEntity.ReactionTime);
            entity.OnGround.Should().Be(newEntity.OnGround);
            entity.MoveLinked.Should().Be(newEntity.MoveLinked);
            entity.Respawn.Should().Be(newEntity.Respawn);
        }
    }

    private void WorldInit(SinglePlayerWorld world)
    {
        world.CheatManager.ActivateCheat(world.Player, CheatType.God);
    }

    private void WorldLoadInit(SinglePlayerWorld world)
    {

    }

    private static int EntitiesCount(SinglePlayerWorld world)
    {
        int count = 0;
        for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
            count++;
        return count;
    }
}
