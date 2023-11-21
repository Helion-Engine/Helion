using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

public partial class BoomActions
{
    [Fact(DisplayName = "Boom Action 245 Scroll ceiling when sector height changes")]
    public void Action245()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 15);
        scrollSector.Floor.SectorScrollData.Should().BeNull();
        scrollSector.Ceiling.SectorScrollData.Should().NotBeNull();

        GameActions.ActivateLine(World, World.Player, 308, ActivationContext.UseLine);
        GameActions.RunSectorPlaneSpecial(World, scrollSector);
        GameActions.TickWorld(World, 1);
        scrollSector.Ceiling.SectorScrollData!.Offset.Should().Be(new Vec2D(0, -1));
    }

    [Fact(DisplayName = "Boom Action 246 Scroll floor when sector height changes")]
    public void Action246()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 16);
        scrollSector.Ceiling.SectorScrollData.Should().BeNull();
        scrollSector.Floor.SectorScrollData.Should().NotBeNull();

        GameActions.ActivateLine(World, World.Player, 316, ActivationContext.UseLine);
        GameActions.RunSectorPlaneSpecial(World, scrollSector);
        GameActions.TickWorld(World, 1);
        scrollSector.Floor.SectorScrollData!.Offset.Should().Be(new Vec2D(0, 1));

        var barrel = GameActions.GetSectorEntity(World, 58, "ExplosiveBarrel");
        barrel.Velocity.XY.Should().Be(Vec2D.Zero);
    }

    [Fact(DisplayName = "Boom Action 247 Scroll floor objects when sector height changes")]
    public void Action247()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 17);
        scrollSector.Ceiling.SectorScrollData.Should().BeNull();
        scrollSector.Floor.SectorScrollData.Should().BeNull();

        GameActions.ActivateLine(World, World.Player, 336, ActivationContext.UseLine);
        GameActions.RunSectorPlaneSpecial(World, scrollSector);
        GameActions.TickWorld(World, 1);

        var barrel = GameActions.GetSectorEntity(World, 60, "ExplosiveBarrel");
        barrel.Velocity.XY.Should().NotBe(Vec2D.Zero);
    }

    [Fact(DisplayName = "Boom Action 248 Scroll floor and objects when sector height changes")]
    public void Action248()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 18);
        scrollSector.Ceiling.SectorScrollData.Should().BeNull();
        scrollSector.Floor.SectorScrollData.Should().NotBeNull();

        GameActions.ActivateLine(World, World.Player, 341, ActivationContext.UseLine);
        GameActions.RunSectorPlaneSpecial(World, scrollSector);
        GameActions.TickWorld(World, 1);
        scrollSector.Floor.SectorScrollData!.Offset.Should().Be(new Vec2D(0, 1));

        var barrel = GameActions.GetSectorEntity(World, 64, "ExplosiveBarrel");
        barrel.Velocity.XY.Should().NotBe(Vec2D.Zero);
    }

    [Fact(DisplayName = "Boom Action 249 Scroll wall when sector height changes")]
    public void Action249()
    {
        var scrollLine1 = GameActions.GetLine(World, 352);
        var scrollLine2 = GameActions.GetLine(World, 362);
        var moveSector = GameActions.GetSectorByTag(World, 19);
        scrollLine1.Front.ScrollData.Should().NotBeNull();
        scrollLine2.Front.ScrollData.Should().NotBeNull();

        GameActions.ActivateLine(World, World.Player, 352, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, moveSector);
        GameActions.TickWorld(World, 1);
        AssertScrollOffsets(scrollLine1.Front.ScrollData!, new Vec2D(64, 0));
        AssertScrollOffsets(scrollLine2.Front.ScrollData!, new Vec2D(64, 0));

        GameActions.ActivateLine(World, World.Player, 362, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, moveSector);
        GameActions.TickWorld(World, 1);
        AssertScrollOffsets(scrollLine1.Front.ScrollData!, new Vec2D(0, 0));
        AssertScrollOffsets(scrollLine2.Front.ScrollData!, new Vec2D(0, 0));
    }

    [Fact(DisplayName = "Boom Action 250 Scroll ceiling")]
    public void Action250()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 11);
        scrollSector.Floor.SectorScrollData.Should().BeNull();
        scrollSector.Ceiling.SectorScrollData.Should().NotBeNull();
        GameActions.TickWorld(World, 1);
        var offset = scrollSector.Ceiling.SectorScrollData!.LastOffset - scrollSector.Ceiling.SectorScrollData!.Offset;
        offset.Should().Be(new Vec2D(0.03125, 0));
    }

    [Fact(DisplayName = "Boom Action 251 Scroll floor")]
    public void Action251()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 12);
        scrollSector.Floor.SectorScrollData.Should().NotBeNull();
        scrollSector.Ceiling.SectorScrollData.Should().BeNull();
        GameActions.TickWorld(World, 1);
        var offset = scrollSector.Floor.SectorScrollData!.LastOffset - scrollSector.Floor.SectorScrollData!.Offset;
        offset.Should().Be(new Vec2D(0.03125, 0));
    }

    [Fact(DisplayName = "Boom Action 252 Scroll objects on floor")]
    public void Action252()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 13);
        var barrel = GameActions.GetSectorEntity(World, 52, "ExplosiveBarrel");
        scrollSector.Floor.SectorScrollData.Should().BeNull();
        scrollSector.Ceiling.SectorScrollData.Should().BeNull();
        GameActions.TickWorld(World, 1);
        barrel.Velocity.XY.Should().NotBe(Vec2D.Zero);
    }

    [Fact(DisplayName = "Boom Action 252 Scroll floor and objects")]
    public void Action253()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 9);
        var barrel = GameActions.GetSectorEntity(World, 47, "ExplosiveBarrel");
        scrollSector.Floor.SectorScrollData.Should().NotBeNull();
        scrollSector.Ceiling.SectorScrollData.Should().BeNull();
        GameActions.TickWorld(World, 1);
        var offset = scrollSector.Floor.SectorScrollData!.LastOffset - scrollSector.Floor.SectorScrollData!.Offset;
        offset.Should().Be(new Vec2D(0.03125, 0));
        barrel.Velocity.XY.Should().NotBe(Vec2D.Zero);
    }

    [Fact(DisplayName = "Boom Action 254 Scroll tagged wall")]
    public void Action254()
    {
        var line1 = GameActions.GetLine(World, 291);
        var line2 = GameActions.GetLine(World, 292);
        var line3 = GameActions.GetLine(World, 293);
        var line4 = GameActions.GetLine(World, 294);

        GameActions.TickWorld(World, 1);
        line1.Front.ScrollData.Should().NotBeNull();
        line2.Front.ScrollData.Should().NotBeNull();
        line3.Front.ScrollData.Should().NotBeNull();
        line4.Front.ScrollData.Should().NotBeNull();

        AssertScrollDiffOffsets(line1.Front.ScrollData!, new Vec2D(2, 0));
        AssertScrollDiffOffsets(line2.Front.ScrollData!, new Vec2D(0, -2));
        AssertScrollDiffOffsets(line3.Front.ScrollData!, new Vec2D(0, 2));
        AssertScrollDiffOffsets(line4.Front.ScrollData!, new Vec2D(-2, 0));
    }

    [Fact(DisplayName = "Boom Action 254 Scroll tagged wall")]
    public void Action255()
    {
        var line1 = GameActions.GetLine(World, 296);
        var line2 = GameActions.GetLine(World, 298);
        var line3 = GameActions.GetLine(World, 299);

        GameActions.TickWorld(World, 1);
        line1.Front.ScrollData.Should().NotBeNull();
        line2.Front.ScrollData.Should().NotBeNull();
        line3.Front.ScrollData.Should().NotBeNull();

        AssertScrollDiffOffsets(line1.Front.ScrollData!, new Vec2D(4, -2));
        AssertScrollDiffOffsets(line2.Front.ScrollData!, new Vec2D(2, -4));
        AssertScrollDiffOffsets(line3.Front.ScrollData!, new Vec2D(4, 0));
    }

    private static void AssertScrollDiffOffsets(SideScrollData scrollData, Vec2D offset)
    {
        (scrollData.LastOffsetUpper - scrollData.OffsetUpper).Should().Be(offset);
        (scrollData.LastOffsetMiddle - scrollData.OffsetMiddle).Should().Be(offset);
        (scrollData.LastOffsetLower - scrollData.OffsetLower).Should().Be(offset);
    }

    private static void AssertScrollOffsets(SideScrollData scrollData, Vec2D offset)
    {
        (scrollData.OffsetUpper).Should().Be(offset);
        (scrollData.OffsetMiddle).Should().Be(offset);
        (scrollData.OffsetLower).Should().Be(offset);
    }
}