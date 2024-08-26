using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Id24;

[Collection("GameActions")]
public class Scroll
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public Scroll()
    {
        World = WorldAllocator.LoadMap("Resources/id24scroll.zip", "id24scroll.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }


    [Fact(DisplayName = "2082 ScrollSidesToLinesLeftDirection")]
    public void Action2082_ScrollSidesToLinesLeftDirection()
    {
        var line = GameActions.GetLine(World, 44);
        AssertTwoSidedScroll(line, (0, 0));
        GameActions.TickWorld(World, 1);
        AssertTwoSidedScroll(line, (1, 0));
    }

    [Fact(DisplayName = "2083 ScrollSidesToLinesRightDirection")]
    public void Action2083_ScrollSidesToLinesRightDirection()
    {
        var line = GameActions.GetLine(World, 45);
        AssertTwoSidedScroll(line, (0, 0));
        GameActions.TickWorld(World, 1);
        AssertTwoSidedScroll(line, (-1, 0));
    }

    [Fact(DisplayName = "2084 ScrollSidesToSectorScrollValues")]
    public void Action2084_ScrollSidesToSectorScrollValues()
    {
        var line = GameActions.GetLine(World, 8);
        AssertTwoSidedScroll(line, (0, 0));
        GameActions.TickWorld(World, 1);
        AssertTwoSidedScroll(line, (-0.5, 1));
    }

    [Fact(DisplayName = "2085 ScrollSidesToSectorMovement")]
    public void Action2085_ScrollSidesToSectorMovement()
    {
        var line = GameActions.GetLine(World, 9);
        AssertTwoSidedScroll(line, (0, 0));
        GameActions.TickWorld(World, 1);

        // Only changes with sector movement
        AssertTwoSidedScroll(line, (0, 0));

        GameActions.ActivateLine(World, Player, 30, ActivationContext.UseLine);

        int count = 0;
        GameActions.RunSectorPlaneSpecial(World, GameActions.GetSectorByTag(World, 4), () =>
        {
            count++;
            AssertTwoSidedScroll(line, (-1 * count, 0.5 * count));
        });

        var offset = line.Front.ScrollData!.OffsetMiddle;
        GameActions.TickWorld(World, 1);
        // Sector has completed movement so scrolling stops
        AssertTwoSidedScroll(line, offset);
    }

    [Fact(DisplayName = "2086 ScrollSidesAccelerateToSectorMovement")]
    public void Action2086_ScrollSidesAccelerateToSectorMovement()
    {
        var line = GameActions.GetLine(World, 31);
        AssertTwoSidedScroll(line, (0, 0));
        GameActions.TickWorld(World, 1);

        // Only changes with sector movement
        AssertTwoSidedScroll(line, (0, 0));

        GameActions.ActivateLine(World, Player, 41, ActivationContext.UseLine);

        int count = 0;
        Vec2D current = Vec2D.Zero;
        GameActions.RunSectorPlaneSpecial(World, GameActions.GetSectorByTag(World, 5), () =>
        {
            count++;
            current += (-count, count * 0.5);
            AssertTwoSidedScroll(line, current);
        });

        var offset = line.Front.ScrollData!.OffsetMiddle;
        GameActions.TickWorld(World, 1);
        // Scroll keeps moving even though sector has stopped
        line.Front.ScrollData.OffsetMiddle.Should().NotBe(offset);

        GameActions.ActivateLine(World, Player, 37, ActivationContext.UseLine);
        GameActions.RunSectorPlaneSpecial(World, GameActions.GetSectorByTag(World, 5), () => { });
        offset = line.Front.ScrollData!.OffsetMiddle;
        GameActions.TickWorld(World, 1);
        // Scroll stops moving with sector back to start height
        line.Front.ScrollData.OffsetMiddle.Should().Be(offset);
    }

    private static void AssertTwoSidedScroll(Line line, Vec2D offset)
    { 
        line.Front.ScrollData.Should().NotBeNull();
        line.Front.ScrollData!.OffsetMiddle.Should().Be(offset);
  
        line.Back.Should().NotBeNull();
        line.Back!.ScrollData.Should().NotBeNull();
        line.Front.ScrollData!.OffsetMiddle.Should().Be(offset);
        line.Back.ScrollData!.OffsetMiddle.Should().Be(new Vec2D(-offset.X, offset.Y));
    }
}