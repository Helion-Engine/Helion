using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction._3DSector;

[Collection("GameActions")]
public class Sector3D_Physics : IDisposable
{
    private readonly SinglePlayerWorld World;
    private readonly Entity Imp;

    public Sector3D_Physics()
    {
        World = WorldAllocator.LoadMap("Resources/sector3d-physics.zip", "sector3d-physics.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        Imp = GameActions.CreateEntity(World, "DoomImp", default, frozen: false);
    }

    public void Dispose()
    {
        GameActions.DestroyCreatedEntities(World);
    }

    [Fact(DisplayName = "Monster walks on 3D sector")]
    public void MonsterWalkSector3D()
    {
        GameActions.SetEntityPosition(World, Imp, (-576, -96, 96));
        Imp.OnGround.Should().BeTrue();
        Imp.AngleRadians = GameActions.GetAngle(Bearing.North);
        Imp.SetMoveDirection(Entity.MoveDir.North);
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-576, -88, 96));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-576, -80, 96));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-576, -72, 96));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-576, -64, 96));
    }

    [Fact(DisplayName = "Monster walks to same 3D sector")]
    public void MonsterWalksToSameSector3D()
    {
        GameActions.SetEntityPosition(World, Imp, (-616, -64, 96));
        Imp.OnGround.Should().BeTrue();
        Imp.AngleRadians = GameActions.GetAngle(Bearing.West);
        Imp.SetMoveDirection(Entity.MoveDir.West);
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-624, -64, 96));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-632, -64, 96));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-640, -64, 96));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-648, -64, 96));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-656, -64, 96));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-664, -64, 96));
    }

    [Fact(DisplayName = "Monster walks on 3D sector drop off with line front to back")]
    public void MonsterWalkDropOff3DFrontToBack()
    {
        GameActions.SetEntityPosition(World, Imp, (-608, -32, 96));
        Imp.OnGround.Should().BeTrue();
        Imp.AngleRadians = GameActions.GetAngle(Bearing.North);
        Imp.SetMoveDirection(Entity.MoveDir.North);
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        GameActions.MoveEnemy(Imp).Should().BeFalse();
    }

    [Fact(DisplayName = "Monster walks on 3D sector drop off with line back to front")]
    public void MonsterWalkDropOff3DBackToFront()
    {
        GameActions.SetEntityPosition(World, Imp, (-544, -32, 96));
        Imp.OnGround.Should().BeTrue();
        Imp.AngleRadians = GameActions.GetAngle(Bearing.North);
        Imp.SetMoveDirection(Entity.MoveDir.North);
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        GameActions.MoveEnemy(Imp).Should().BeFalse();
    }

    [Fact(DisplayName = "Monster blocked by normal sector dropoff when below 3D sector with line back to front")]
    public void MonsterDropOffBlockWithNormalSectorBelowSector3DBackToFront()
    {
        GameActions.SetEntityPosition(World, Imp, (-160, -156, 0));
        Imp.OnGround.Should().BeTrue();
        Imp.AngleRadians = GameActions.GetAngle(Bearing.North);
        Imp.SetMoveDirection(Entity.MoveDir.North);
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        GameActions.MoveEnemy(Imp).Should().BeFalse();
    }

    [Fact(DisplayName = "Monster blocked by normal sector dropoff when below 3D sector with line front to back")]
    public void MonsterDropOffBlockWithNormalSectorBelowSector3DFrontToBack()
    {
        GameActions.SetEntityPosition(World, Imp, (-160, -36, 0));
        Imp.OnGround.Should().BeTrue();
        Imp.AngleRadians = GameActions.GetAngle(Bearing.South);
        Imp.SetMoveDirection(Entity.MoveDir.South);
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        GameActions.MoveEnemy(Imp).Should().BeFalse();
    }

    [Fact(DisplayName = "Monster not blocked by normal sector dropoff 3D when below 3D sector with line front to back")]
    public void MonsterDropOff3DNoBlockWithSectorAboveAndBelow3DFrontToBack()
    {
        GameActions.SetEntityPosition(World, Imp, (-160, 92, 0));
        Imp.OnGround.Should().BeTrue();
        Imp.AngleRadians = GameActions.GetAngle(Bearing.South);
        Imp.SetMoveDirection(Entity.MoveDir.South);

        for (int i = 0; i < 6; i++)
            GameActions.MoveEnemy(Imp).Should().BeTrue();

        Imp.Position.Should().Be(new Vec3D(-160, 44, -16));
    }

    [Fact(DisplayName = "Monster not blocked by normal sector dropoff 3D when below 3D sector with line back to front")]
    public void MonsterDropOff3DNoBlockWithSectorAboveAndBelow3DBackToFront()
    {
        GameActions.SetEntityPosition(World, Imp, (-160, -28, 0));
        Imp.OnGround.Should().BeTrue();
        Imp.AngleRadians = GameActions.GetAngle(Bearing.North);
        Imp.SetMoveDirection(Entity.MoveDir.North);

        for (int i = 0; i < 6; i++)
            GameActions.MoveEnemy(Imp).Should().BeTrue();

        Imp.Position.Should().Be(new Vec3D(-160, 20, -16));
    }

    [Fact(DisplayName = "Monster walks on 3D sector drop off stair line")]
    public void MonsterWalkOnDropOffStairLine3D()
    {
        GameActions.SetEntityPosition(World, Imp, (-296, 0, 16));
        Imp.OnGround.Should().BeTrue();

        // Can't walk up the stairs since drop off would be 32
        Imp.AngleRadians = GameActions.GetAngle(Bearing.West);
        Imp.SetMoveDirection(Entity.MoveDir.West);
        GameActions.MoveEnemy(Imp).Should().BeFalse();

        // Can walk back down to floor
        Imp.AngleRadians = GameActions.GetAngle(Bearing.East);
        Imp.SetMoveDirection(Entity.MoveDir.East);
        GameActions.MoveEnemy(Imp).Should().BeTrue();

        // Can walk on same stair
        GameActions.SetEntityPosition(World, Imp, (-296, 0, 16));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.South);
        Imp.SetMoveDirection(Entity.MoveDir.South);
        GameActions.MoveEnemy(Imp).Should().BeTrue();
    }

    [Fact(DisplayName = "Monster falls on 3D sector")]
    public void MonsterFallSector3D()
    {
        GameActions.SetEntityPosition(World, Imp, (-576, -96, 128));
        Imp.OnGround.Should().BeFalse();
        GameActions.TickWorld(World, () => Imp.OnGround == false, () => { });
        Imp.OnGround.Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-576, -96, 96));
    }

    [Fact(DisplayName = "Monster walks up 3D sector stairs with lines front to back")]
    public void MonsterWalksUpStairSectors3DFrontToBack()
    {
        GameActions.SetEntityPosition(World, Imp, (-248, -32, 0));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.West);
        Imp.SetMoveDirection(Entity.MoveDir.West);
        Imp.OnGround.Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-248, -32, 0));

        var height = 16.0;
        for (int step = 0; step < 6; step++)
        {
            for (int move = 0; move < 6; move++)
            {
                GameActions.MoveEnemy(Imp).Should().BeTrue();
                Imp.Position.Z.Should().Be(height);
            }
            height += 16.0;
        }
    }

    [Fact(DisplayName = "Monster walks up 3D sector stairs with lines back to front")]
    public void MonsterWalksUpStairSectors3DBackToFront()
    {
        GameActions.SetEntityPosition(World, Imp, (-248, -104, 0));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.West);
        Imp.SetMoveDirection(Entity.MoveDir.West);
        Imp.OnGround.Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-248, -104, 0));

        var height = 16.0;
        for (int step = 0; step < 6; step++)
        {
            for (int move = 0; move < 6; move++)
            {
                GameActions.MoveEnemy(Imp).Should().BeTrue();
                Imp.Position.Z.Should().Be(height);
            }
            height += 16.0;
        }
    }

    [Fact(DisplayName = "Monster walks down 3D sector stairs with lines back to front")]
    public void MonsterWalksDownStairSectors3DBackToFront()
    {
        GameActions.SetEntityPosition(World, Imp, (-544, -32, 96));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.East);
        Imp.SetMoveDirection(Entity.MoveDir.East);
        Imp.OnGround.Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-544, -32, 96));

        var height = 96.0;
        for (int step = 0; step < 6; step++)
        {
            for (int move = 0; move < 6; move++)
            {
                GameActions.MoveEnemy(Imp).Should().BeTrue();
                Imp.Position.Z.Should().Be(height);
            }
            height -= 16.0;
        }
    }

    [Fact(DisplayName = "Monster walks down 3D sector stairs with lines front to back")]
    public void MonsterWalksDownStairSectors3DFrontToBack()
    {
        GameActions.SetEntityPosition(World, Imp, (-544, -104, 96));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.East);
        Imp.SetMoveDirection(Entity.MoveDir.East);
        Imp.OnGround.Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-544, -104, 96));

        var height = 96.0;
        for (int step = 0; step < 6; step++)
        {
            for (int move = 0; move < 6; move++)
            {
                GameActions.MoveEnemy(Imp).Should().BeTrue();
                Imp.Position.Z.Should().Be(height);
            }
            height -= 16.0;
        }
    }

    [Fact(DisplayName = "Monster steps up to 3D sector with 3D sector ceiling exact height")]
    public void MonsterStepUpCeilingExact()
    {        
        GameActions.SetEntityPosition(World, Imp, (-576, 232, 16));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.North);
        Imp.SetMoveDirection(Entity.MoveDir.North);
        Imp.LowestCeilingZ.Should().Be(1024);
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-576, 240, 32));
        Imp.LowestCeilingZ.Should().Be(88);
    }

    [Fact(DisplayName = "Monster doesn't step up to 3D sector with 3D sector ceiling too low with line front to back")]
    public void MonsterNoStepUpCeilingLowFrontToBack()
    {
        GameActions.SetEntityPosition(World, Imp, (-416, 232, 16));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.North);
        Imp.SetMoveDirection(Entity.MoveDir.North);
        Imp.LowestCeilingZ.Should().Be(1024);
        GameActions.MoveEnemy(Imp).Should().BeFalse();
        Imp.Position.Should().Be(new Vec3D(-416, 232, 16));
        Imp.LowestCeilingZ.Should().Be(1024);
    }

    [Fact(DisplayName = "Monster doesn't step up to 3D sector with 3D sector ceiling too low with line back to front")]
    public void MonsterNoStepUpCeilingLowBackToFront()
    {
        GameActions.SetEntityPosition(World, Imp, (-352, 232, 16));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.North);
        Imp.SetMoveDirection(Entity.MoveDir.North);
        Imp.LowestCeilingZ.Should().Be(1024);
        GameActions.MoveEnemy(Imp).Should().BeFalse();
        Imp.Position.Should().Be(new Vec3D(-352, 232, 16));
        Imp.LowestCeilingZ.Should().Be(1024);
    }

    [Fact(DisplayName = "Monster blocked by two 3D sectors that are closed with line front to back")]
    public void MonsterBlockedByTwoClosedSectors3DFrontToBack()
    {
        GameActions.SetEntityPosition(World, Imp, (-184, 296, 0));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.North);
        Imp.SetMoveDirection(Entity.MoveDir.North);
        GameActions.MoveEnemy(Imp).Should().BeFalse();
    }

    [Fact(DisplayName = "Monster blocked by two 3D sectors that are closed with line back to front")]
    public void MonsterBlockedByTwoClosedSectors3DBackToFront()
    {
        GameActions.SetEntityPosition(World, Imp, (-120, 296, 0));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.North);
        Imp.SetMoveDirection(Entity.MoveDir.North);
        GameActions.MoveEnemy(Imp).Should().BeFalse();
    }

    [Fact(DisplayName = "Monster blocked my middle sector in between z and height")]
    public void MonsterBlockedByMiddleSector3D()
    {
        GameActions.SetEntityPosition(World, Imp, (64, 296, 0));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.North);
        Imp.SetMoveDirection(Entity.MoveDir.North);
        GameActions.MoveEnemy(Imp).Should().BeFalse();
    }

    [Fact(DisplayName = "Monster walks over 3D sectors with multiple lines and exactly on line")]
    public void MonsterWalkMultiple3DSectorLinesOnLine()
    {
        GameActions.SetEntityPosition(World, Imp, (-440, 640, 4));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.West);
        Imp.SetMoveDirection(Entity.MoveDir.West);
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-448, 640, 4));
    }

    [Fact(DisplayName = "Monster walks over 3D sectors with multiple lines and crosses line")]
    public void MonsterWalkMultiple3DSectorLinesAndCrossesLine()
    {
        GameActions.SetEntityPosition(World, Imp, (-444, 640, 4));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.West);
        Imp.SetMoveDirection(Entity.MoveDir.West);
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-452, 640, 4));
    }

    [Fact(DisplayName = "Monster walks over 3D sectors crossing many lines east")]
    public void MonsterWalkMultiple3DSectorLinesAndCrossesManyLinesEast()
    {
        GameActions.SetEntityPosition(World, Imp, (-450, 640, 4));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.East);
        Imp.SetMoveDirection(Entity.MoveDir.East);
        GameActions.MoveEnemy(Imp, 19).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-431, 640, 4));
    }

    [Fact(DisplayName = "Monster walks over 3D sectors crossing many line west")]
    public void MonsterWalkMultiple3DSectorLinesAndCrossingManyLinesWest()
    {
        GameActions.SetEntityPosition(World, Imp, (-430, 640, 4));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.West);
        Imp.SetMoveDirection(Entity.MoveDir.West);
        GameActions.MoveEnemy(Imp, 19).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-449, 640, 4));
    }

    [Fact(DisplayName = "Monster can't walk over multiple lines because of hole in 3D sector")]
    public void MonsterCantWalkMultipleLinesHoleInSector3D()
    {
        GameActions.SetEntityPosition(World, Imp, (-450, 712, 4));
        Imp.AngleRadians = GameActions.GetAngle(Bearing.East);
        Imp.SetMoveDirection(Entity.MoveDir.East);
        GameActions.MoveEnemy(Imp, 19).Should().BeFalse();
        Imp.Position.Should().Be(new Vec3D(-450, 712, 4));
    }

    [Fact(DisplayName = "Monster walks under 3D sectors crossing many lines")]
    public void MonsterWalksUnderCrossingManyLines()
    {
        GameActions.SetEntityPosition(World, Imp, (-404, 712, -64));
        Imp.OnGround.Should().BeTrue();
        Imp.AngleRadians = GameActions.GetAngle(Bearing.West);
        Imp.SetMoveDirection(Entity.MoveDir.West);
        GameActions.MoveEnemy(Imp, 19).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-423, 712, -64));
    }

    [Fact(DisplayName = "Monster walks on higher 3D sector crossing many lines")]
    public void MonsterWalksOnHigher3DSectorCrossingManyLines()
    {
        GameActions.SetEntityPosition(World, Imp, (-404, 712, 80));
        Imp.OnGround.Should().BeTrue();
        Imp.AngleRadians = GameActions.GetAngle(Bearing.West);
        Imp.SetMoveDirection(Entity.MoveDir.West);
        GameActions.MoveEnemy(Imp, 19).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-423, 712, 80));
    }

    [Fact(DisplayName = "Monster walks on multiple normal sectors and 3D sector lines back to front")]
    public void MonsterWalksMultipleNormalAndSector3DLinesBackToFront()
    {
        GameActions.SetEntityPosition(World, Imp, (-64, 704, 0));
        Imp.OnGround.Should().BeTrue();
        Imp.AngleRadians = GameActions.GetAngle(Bearing.West);
        Imp.SetMoveDirection(Entity.MoveDir.West);
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-72, 704, 0));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-80, 704, 0));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-88, 704, 0));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-96, 704, 0));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-104, 704, 0));
    }

    [Fact(DisplayName = "Monster walks on multiple normal sectors and 3D sector lines front to back")]
    public void MonsterWalksMultipleNormalAndSector3DLinesFrontToBack()
    {
        GameActions.SetEntityPosition(World, Imp, (-256, 704, 0));
        Imp.OnGround.Should().BeTrue();
        Imp.AngleRadians = GameActions.GetAngle(Bearing.East);
        Imp.SetMoveDirection(Entity.MoveDir.East);
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-248, 704, 0));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-240, 704, 0));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-232, 704, 0));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-224, 704, 0));
        GameActions.MoveEnemy(Imp).Should().BeTrue();
        Imp.Position.Should().Be(new Vec3D(-216, 704, 0));
    }
}
