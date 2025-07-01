using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources;
using Helion.Resources.IWad;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class MidTex3D
{
    private static readonly string ResourceZip = "Resources/midtex3d.zip";
    private static readonly string MapName = "MAP01";

    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;
    private Entity Imp => GameActions.GetEntities(World, "DoomImp").First();

    public MidTex3D()
    {
        World = WorldAllocator.LoadMap(ResourceZip, "midtex3d.wad", MapName, GetType().Name, (world) => { }, IWadType.Doom2);

        // MidTex3D needs the texture to handle physics since it's based on its height
        var texture = World.TextureManager.GetTexture("STEPTOP", ResourceNamespace.Textures);
        texture.Image = CreateImage(64, 16);

        texture = World.TextureManager.GetTexture("BRNSMALC", ResourceNamespace.Textures);
        texture.Image = CreateImage(64, 64);

        // Entities with a z height aren't initialized correctly since the above texture sizes aren't populated when it loaded
        var entity = World.EntityManager.Head;
        while (entity != null)
        {
            entity.UnlinkFromWorld();
            World.Link(entity);
            entity = entity.Next;
        }
    }

    private static Helion.Graphics.Image CreateImage(int width, int height) =>
        new((width, height), Helion.Graphics.ImageType.Argb);

    [Fact(DisplayName = "Player walks on midtex")]
    public void PlayerWalksOnMidTex()
    {
        GameActions.SetEntityPosition(World, Player, (32, -368));
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        Player.Position.Z.Should().Be(256);
        GameActions.MoveEntity(World, Player, 64);
        Player.Position.ApproxEquals(new Vec3D(32, -304, 256)).Should().BeTrue();
        Player.OnGround.Should().BeTrue();
        Player.HighestFloorZ.Should().Be(256);

        Player.AngleRadians = GameActions.GetAngle(Bearing.South);
        GameActions.MoveEntity(World, Player, 64);
        Player.Position.ApproxEquals(new Vec3D(32, -368, 256)).Should().BeTrue();
        Player.OnGround.Should().BeTrue();
        Player.HighestFloorZ.Should().Be(256);
    }

    [Fact(DisplayName = "Monster walks on midtex")]
    public void MonsterWalksOnMidTex()
    {
        var imp = Imp;
        GameActions.SetEntityOutOfBounds(World, Player);
        GameActions.SetEntityPosition(World, imp, new Vec3D(32, 16, 256));
        imp.Position.Z.Should().Be(256);
        imp.OnGround.Should().BeTrue();

        imp.AngleRadians = GameActions.GetAngle(Bearing.South);
        imp.SetEnemyDirection(Entity.MoveDir.South);
        imp.MoveEnemy(out _).Should().Be(true);
        imp.Position.ApproxEquals(new Vec3D(32, 8, 256)).Should().BeTrue();
    }

    [Fact(DisplayName = "Monster steps up to midtex")]
    public void MonsterStepsUpAndDownToMidTex()
    {
        var imp = Imp;
        GameActions.SetEntityOutOfBounds(World, Player);
        GameActions.SetEntityPosition(World, imp, new Vec3D(196, -264, 0));

        imp.AngleRadians = GameActions.GetAngle(Bearing.South);
        imp.SetEnemyDirection(Entity.MoveDir.South);

        imp.MoveEnemy(out _).Should().Be(true);
        imp.Position.ApproxEquals(new Vec3D(196, -272, 16)).Should().BeTrue();

        imp.MoveEnemy(out _).Should().Be(true);
        imp.Position.ApproxEquals(new Vec3D(196, -280, 16)).Should().BeTrue();

        imp.MoveEnemy(out _).Should().Be(true);
        imp.Position.ApproxEquals(new Vec3D(196, -288, 32)).Should().BeTrue();

        imp.AngleRadians = GameActions.GetAngle(Bearing.North);
        imp.SetEnemyDirection(Entity.MoveDir.North);

        imp.MoveEnemy(out _).Should().Be(true);
        imp.Position.ApproxEquals(new Vec3D(196, -280, 32)).Should().BeTrue();

        imp.MoveEnemy(out _).Should().Be(true);
        imp.Position.ApproxEquals(new Vec3D(196, -272, 16)).Should().BeTrue();

        imp.MoveEnemy(out _).Should().Be(true);
        imp.Position.ApproxEquals(new Vec3D(196, -264, 16)).Should().BeTrue();

        imp.MoveEnemy(out _).Should().Be(true);
        imp.Position.ApproxEquals(new Vec3D(196, -256, 0)).Should().BeTrue();

        GameActions.SetEntityOutOfBounds(World, imp);
    }

    [Fact(DisplayName = "Player jumps on midtex")]
    public void PlayerJumpsOnMidTex()
    {
        GameActions.SetEntityPosition(World, Player, new Vec3D(32, -304, 256));
        Player.OnGround.Should().BeTrue();
        Player.Jump();
        GameActions.RunPlayerJump(World, Player);
    }

    [Fact(DisplayName = "Player jumps on midtex")]
    public void PlayerJumpBlockedByMidTex()
    {
        GameActions.SetEntityPosition(World, Player, new Vec2D(32, 64));
        Player.OnGround.Should().BeTrue();
        Player.Jump();

        int[] positions = [8, 8, 8, 6, 3, 0];
        for (int i = 0; i < positions.Length; i++)
        {
            World.Tick();
            Player.Position.Z.Should().Be(positions[i]);
        }

        GameActions.TickWorld(World, 35);
    }

    [Fact(DisplayName = "Player fits under midtex")]
    public void PlayerFitsUnderMidTex()
    {
        GameActions.SetEntityPosition(World, Player, new Vec3D(76, 64, 128));
        Player.OnGround.Should().BeTrue();
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        GameActions.MoveEntity(World, Player, 32);
        Player.BlockingBlockLineIndex.Should().Be(-1);
        Player.HighestFloorZ.Should().Be(128);
        Player.LowestCeilingZ.Should().Be(184);
    }

    [Fact(DisplayName = "Player blocked by midtex")]
    public void PlayerBlockedByMidTex()
    {
        GameActions.SetEntityPosition(World, Player, new Vec3D(112, 64, 128));
        Player.OnGround.Should().BeTrue();
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);
        GameActions.MoveEntity(World, Player, 32);
        Player.BlockingBlockLineIndex.Should().NotBe(-1);
        Player.HighestFloorZ.Should().Be(128);
        Player.LowestCeilingZ.Should().Be(184);
    }


    [Fact(DisplayName = "Player slides on midtex wall")]
    public void PlayerSlideWallMidTex()
    {
        GameActions.SetEntityPosition(World, Player, new Vec2D(224, 32));
        Player.AngleRadians = GameActions.GetAngle(Bearing.SouthEast);
        Player.BlockingBlockLineIndex.Should().Be(-1);

        GameActions.MoveEntity(World, Player, 32);
        Player.Position.ApproxEquals(new Vec3D(224, 9.372583, 0)).Should().BeTrue();
        Player.BlockingBlockLineIndex.Should().NotBe(-1);
    }

    [Fact(DisplayName = "Player steps up to midtex")]
    public void PlayerStepsUpToMidTex()
    {
        GameActions.SetEntityPosition(World, Player, new Vec2D(192, -256));
        Player.OnGround.Should().BeTrue();
        Player.HighestFloorZ.Should().Be(0);
        Player.AngleRadians = GameActions.GetAngle(Bearing.South);

        GameActions.MoveEntity(World, Player, 32);
        Player.OnGround.Should().BeTrue();
        Player.HighestFloorZ.Should().Be(16);

        GameActions.MoveEntity(World, Player, 16);
        Player.OnGround.Should().BeTrue();
        Player.HighestFloorZ.Should().Be(32);

        GameActions.MoveEntity(World, Player, 16);
        Player.OnGround.Should().BeTrue();
        Player.HighestFloorZ.Should().Be(48);

        GameActions.MoveEntity(World, Player, 16);
        Player.OnGround.Should().BeTrue();
        Player.HighestFloorZ.Should().Be(64);
    }

    [Fact(DisplayName = "Player moves with moving midttex")]
    public void PlayerOnMovingMidTex()
    {
        GameActions.SetEntityPosition(World, Player, new Vec3D(512, -96, 192));
        Player.OnGround.Should().BeTrue();
        GameActions.ActivateLine(World, Player, 128, Helion.World.Physics.ActivationContext.CrossLine);
        var sector = GameActions.GetSectorByTag(World, 1);

        GameActions.RunSectorPlaneSpecial(World, sector, () =>
        {
            // Midtex is 64 above the floor
            var sector = GameActions.GetSectorByTag(World, 1);
            Player.Position.Z.Should().Be(sector.Floor.Z + 64);
            Player.OnGround.Should().BeTrue();
        });
    }

    [Fact(DisplayName = "Player blocks with moving midttex from ceiling")]
    public void PlayerBlocksMovingMidTex()
    {
        GameActions.SetEntityPosition(World, Player, new Vec2D(960, -96));
        Player.OnGround.Should().BeTrue();
        GameActions.ActivateLine(World, Player, 137, Helion.World.Physics.ActivationContext.CrossLine);
        var sector = GameActions.GetSectorByTag(World, 2);

        GameActions.TickWorld(World, 35);
        sector.Ceiling.Z.Should().Be(120);
        Player.LowestCeilingZ.Should().Be(56);
        sector.ActiveCeilingMove.Should().NotBeNull();

        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.MoveEntity(World, Player, 32);
        Player.Position.ApproxEquals(new Vec3D(960, -64, 0)).Should().BeTrue();

        // Midtex also shouldnt stop it from going to the floor
        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Ceiling.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Stacked midtex entity movement through ceiling")]
    public void StackedMidTexMovement()
    {
        GameActions.TickWorld(World, 1);
        var sector = GameActions.GetSectorByTag(World, 3);
        var demons = GameActions.GetEntities(World, "DEMON");
        var zombies = GameActions.GetEntities(World, "ZOMBIEMAN");
        demons.Count.Should().Be(4);
        demons.Count.Should().Be(4);
        AssertZ(demons, 288, true);
        AssertZ(zombies, 416, true);

        GameActions.ActivateLine(World, Player, 154, Helion.World.Physics.ActivationContext.UseLine);
        sector.ActiveCeilingMove.Should().NotBeNull();

        GameActions.TickWorld(World, () => { return demons[0].Position.Z > 0; }, () => { });

        AssertZ(demons, 0, true);
        AssertZ(zombies, 128, true);

        sector.Ceiling.Z.Should().Be(192);
        zombies[0].LowestCeilingZ.Should().Be(192);

        GameActions.TickWorld(World, () => { return demons[0].LowestCeilingZ > 56; }, () => { });
        GameActions.TickWorld(World, 5);

        sector.Ceiling.Z.Should().Be(184);
        zombies[0].LowestCeilingZ.Should().Be(184);
        demons[0].LowestCeilingZ.Should().Be(56);

        AssertZ(demons, 0, true);
        AssertZ(zombies, 120, true);

        foreach (var entity in demons)
            entity.Kill(null);

        GameActions.TickWorld(World, () => { return zombies[0].Position.Z > 0; }, () => { });

        sector.Ceiling.Z.Should().Be(64);
        zombies[0].LowestCeilingZ.Should().Be(64);

        GameActions.TickWorld(World, () => { return zombies[0].LowestCeilingZ > 56; }, () => { });
        GameActions.TickWorld(World, 5);

        zombies[0].LowestCeilingZ.Should().Be(56);

        foreach (var entity in zombies)
            entity.Kill(null);

        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Ceiling.Z.Should().Be(0);
        sector.ActiveCeilingMove.Should().BeNull();
    }

    private static void AssertZ(List<Entity> entities, double z, bool onGround)
    {
        foreach (var entity in entities)
        {
            entity.Position.Z.Should().Be(z);
            entity.OnGround.Should().Be(onGround);
        }
    }

    [Fact(DisplayName = "Projectile blocked by midtex")]
    public void ProjectileBlockedByMidTex()
    {
        GameActions.SetEntityPosition(World, Player, new Vec2D(416, 416));
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.PlayerFirePlasma(World, Player, out var plasma).Should().BeTrue();
        plasma.Should().NotBeNull();
        GameActions.TickWorld(World, () => { return plasma!.BlockingBlockLineIndex == -1; }, () => { });
        World.Blockmap.BlockLines[plasma!.BlockingBlockLineIndex].LineId.Should().Be(164);
    }

    [Fact(DisplayName = "Projectile passes over midtex")]
    public void ProjectilePassOverMidTex()
    {
        GameActions.SetEntityPosition(World, Player, new Vec2D(608, 416));
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.PlayerFirePlasma(World, Player, out var plasma).Should().BeTrue();
        plasma.Should().NotBeNull();
        GameActions.TickWorld(World, () => { return plasma!.BlockingBlockLineIndex == -1; }, () => { });
        World.Blockmap.BlockLines[plasma!.BlockingBlockLineIndex].LineId.Should().Be(162);
    }

    [Fact(DisplayName = "Projectile passes under midtex")]
    public void ProjectilePassUnderMidTex()
    {
        GameActions.SetEntityPosition(World, Player, new Vec2D(512, 416));
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.PlayerFirePlasma(World, Player, out var plasma).Should().BeTrue();
        plasma.Should().NotBeNull();
        GameActions.TickWorld(World, () => { return plasma!.BlockingBlockLineIndex == -1; }, () => { });
        World.Blockmap.BlockLines[plasma!.BlockingBlockLineIndex].LineId.Should().Be(162);
    }

    [Fact(DisplayName = "Projectile not blocked by midtex with BlockMissileMidTex3D flag")]
    public void ProjectileNotBlockedByMidTex()
    {
        GameActions.SetEntityPosition(World, Player, new Vec2D(416, 416));
        Player.AngleRadians = GameActions.GetAngle(Bearing.South);
        GameActions.PlayerFirePlasma(World, Player, out var plasma).Should().BeTrue();
        plasma.Should().NotBeNull();
        GameActions.TickWorld(World, () => { return plasma!.BlockingBlockLineIndex == -1; }, () => { });
        World.Blockmap.BlockLines[plasma!.BlockingBlockLineIndex].LineId.Should().Be(160);
    }
}
