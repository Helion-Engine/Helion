using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UmdfLineFlags
{
    private static readonly string ResourceZip = "Resources/udmflineflags.zip";
    private static readonly string MapName = "MAP01";

    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;
    private Entity Imp => GameActions.GetEntity(World, 1);
    private Entity ZombieMan => GameActions.GetEntity(World, 2);
    private Entity LostSoul => GameActions.GetEntity(World, 3);
    private Entity Cacodemon => GameActions.GetEntity(World, 4);

    public UmdfLineFlags()
    {
        World = WorldAllocator.LoadMap(ResourceZip, "udmflineflags.wad", MapName, GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Udmf block everything")]
    public void BlockEverything()
    {
        ZombieMan.Target().Should().BeNull();
        GameActions.GetSector(World, 1).SoundTarget.Get().Should().BeNull();

        AssertLineBlockHitscan(5, true);
        AssertLineBlockProjectile(5, true);

        // Doesn't block sound
        ZombieMan.Target().Should().Be(Player);
        GameActions.GetSector(World, 1).SoundTarget.Get().Should().Be(Player);

        World.CheckLineOfSight(ZombieMan, Player).Should().BeFalse();

        GameActions.EntityUseLine(World, Player, 13).Should().BeFalse();
        GameActions.ActivateLine(World, Player, 13, ActivationContext.UseLine).Should().BeTrue();
    }

    [Fact(DisplayName = "Udmf block everything movement check")]
    public void BlockEverythingMovement()
    {
        AssertLineMoveBlocking(18, BlockFlags.All, true);
    }

    [Fact(DisplayName = "Udmf block players")]
    public void BlockPlayers()
    {
        AssertLineMoveBlocking(23, BlockFlags.Monsters, false);
        AssertLineMoveBlocking(23, BlockFlags.Player, true);
    }

    [Fact(DisplayName = "Udmf block monsters")]
    public void BlockMonsters()
    {
        AssertLineMoveBlocking(28, BlockFlags.Monsters, true);
        AssertLineMoveBlocking(28, BlockFlags.Player, false);
    }

    [Fact(DisplayName = "Udmf block land monsters")]
    public void BlockLandMonsters()
    {
        AssertLineMoveBlocking(37, BlockFlags.LandMonsters, true);
        AssertLineMoveBlocking(37, BlockFlags.FloatingMonsters | BlockFlags.Player, false);
    }

    [Fact(DisplayName = "Udmf block floating monsters")]
    public void BlockFloatingMonsters()
    {
        AssertLineMoveBlocking(38, BlockFlags.FloatingMonsters, true);
        AssertLineMoveBlocking(38, BlockFlags.LandMonsters | BlockFlags.Player, false);
    }

    [Fact(DisplayName = "Udmf block projectiles")]
    public void BlockProjectiles()
    {
        AssertLineBlockProjectile(47, true);
        AssertLineBlockHitscan(47, false);
    }

    [Fact(DisplayName = "Udmf block hitscans")]
    public void BlockHitscans()
    {
        AssertLineBlockProjectile(48, false);
        AssertLineBlockHitscan(48, true);
    }

    [Fact(DisplayName = "Udmf block use")]
    public void BlockUse()
    {
        GameActions.EntityUseLine(World, Player, 57).Should().BeFalse();
        GameActions.ActivateLine(World, Player, 57, ActivationContext.UseLine).Should().BeTrue();
    }

    [Fact(DisplayName = "Udmf block monster line of sight")]
    public void BlockMonsterLineOfSight()
    {
        GameActions.SetEntityToLine(World, Player, 62, 128);
        World.CheckLineOfSight(Cacodemon, Player).Should().BeFalse();
        GameActions.SetEntityToLine(World, Player, 62, -64);
        World.CheckLineOfSight(Cacodemon, Player).Should().BeTrue();
    }

    [Flags]
    enum BlockFlags
    {
        Player = 1,
        Monsters = 2,
        LandMonsters = 4,
        FloatingMonsters = 8,
        All = -1
    }

    private void AssertLineBlockProjectile(int lineId, bool blocks)
    {
        var line = GameActions.GetLine(World, lineId);
        GameActions.SetEntityToLine(World, Player, lineId, 128);

        GameActions.PlayerFirePlasma(World, Player, out var plasma).Should().BeTrue();
        plasma.Should().NotBeNull();
        GameActions.TickWorld(World, () => { return plasma!.BlockingBlockLineIndex == -1; }, () => { });
        GameActions.AssertBlockingLine(plasma!, lineId, blocks);
    }

    private void AssertLineBlockHitscan(int lineId, bool blocks)
    {
        var line = GameActions.GetLine(World, lineId);
        GameActions.SetEntityToLine(World, Player, lineId, 128);

        Vec3D? bulletPuffPos = null;
        World.OnTick += World_OnTick;
        GameActions.PlayerFirePistol(World, Player).Should().BeTrue();

        bulletPuffPos.HasValue.Should().BeTrue();
        var pos = bulletPuffPos!.Value;

        line.Segment.OnRight(pos).Should().Be(blocks);

        void World_OnTick(object? sender, EventArgs e)
        {
            if (bulletPuffPos == null)
            {
                var bulletPuff = GameActions.FindEntity(World, "BulletPuff");
                if (bulletPuff != null)
                    bulletPuffPos = bulletPuff.Position;
            }
        }
    }

    private void AssertLineMoveBlocking(int lineId, BlockFlags blockFlags, bool blocks)
    {
        if ((blockFlags & BlockFlags.Player) != 0)
        {
            GameActions.SetEntityToLine(World, Player, lineId, Player.Radius);
            Player.AngleRadians = GameActions.GetAngle(Bearing.North);
            var pos = Player.Position;
            GameActions.MoveEntity(World, Player, 32);
            AssertBlocks(Player, pos);
            GameActions.AssertBlockingLine(Player, lineId, blocks);
            GameActions.SetEntityOutOfBounds(World, Player);
        }

        if ((blockFlags & BlockFlags.Monsters) != 0 || (blockFlags & BlockFlags.LandMonsters) != 0)
        {
            GameActions.SetEntityToLine(World, Imp, lineId, Player.Radius);
            Imp.AngleRadians = GameActions.GetAngle(Bearing.North);
            var pos = Imp.Position;
            GameActions.MoveEntity(World, Imp, 32);
            AssertBlocks(Imp, pos);
            GameActions.AssertBlockingLine(Imp, lineId, blocks);
            GameActions.SetEntityOutOfBounds(World, Imp);
        }

        if ((blockFlags & BlockFlags.Monsters) != 0 || (blockFlags & BlockFlags.FloatingMonsters) != 0)
        {
            GameActions.SetEntityToLine(World, LostSoul, lineId, LostSoul.Radius);
            LostSoul.AngleRadians = GameActions.GetAngle(Bearing.North);
            var pos = LostSoul.Position;
            GameActions.MoveEntity(World, LostSoul, 32);
            AssertBlocks(LostSoul, pos);
            GameActions.AssertBlockingLine(LostSoul, lineId, blocks);
            GameActions.SetEntityOutOfBounds(World, LostSoul);
        }

        void AssertBlocks(Entity entity, Vec3D startPos)
        {
            if (blocks)
            {
                entity.Position.Should().Be(startPos);
                GameActions.AssertBlockingLine(entity, lineId, blocks);
            }
            else
            {
                entity.Position.Should().NotBe(startPos);
                entity.BlockingBlockLineIndex.Should().Be(-1);
            }
        }
    }
}
