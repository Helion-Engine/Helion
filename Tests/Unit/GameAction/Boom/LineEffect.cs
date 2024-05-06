using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Special.SectorMovement;
using Helion.World.Special.Specials;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class LineEffect
{
    private readonly SinglePlayerWorld World;

    public LineEffect()
    {
        World = WorldAllocator.LoadMap("Resources/lineeffect.zip", "lineeffect.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2, cacheWorld: false);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
    }


    [Fact(DisplayName = "A_LineEffect lift and door")]
    public void LineEffectLiftAndDoor()
    {
        // Dehacked changes look frames to LineEffect with lift specal and then door special
        var liftSector = GameActions.GetSectorByTag(World, 1);
        var doorSector = GameActions.GetSectorByTag(World, 2);

        liftSector.Floor.Z.Should().Be(64);
        liftSector.Ceiling.Z.Should().Be(128);

        doorSector.Floor.Z.Should().Be(32);
        doorSector.Ceiling.Z.Should().Be(32);

        GameActions.TickWorld(World, () => { return !liftSector.IsMoving || !doorSector.IsMoving; }, () => { });

        var specials = World.SpecialManager.GetSpecials();
        specials.Count.Should().Be(2);

        var liftSpecial = (SectorMoveSpecial)specials.Single(x => x is SectorMoveSpecial special && special.Sector.Tag == 1);
        var doorSpecial = (SectorMoveSpecial)specials.Single(x => x is SectorMoveSpecial special && special.Sector.Tag == 2);

        liftSpecial.MoveDirection.Should().Be(MoveDirection.Down);
        liftSpecial.DestZ.Should().Be(0);

        doorSpecial.MoveDirection.Should().Be(MoveDirection.Up);
        doorSpecial.DestZ.Should().Be(124);
    }
}
