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

        GameActions.ActivateLine(World, World.Player, 308, ActivationContext.UseLine);
        GameActions.RunSectorPlaneSpecial(World, scrollSector);
        GameActions.TickWorld(World, 1);
        scrollSector.Floor.RenderOffsets.Offset.Should().Be(new Vec2D(0, 0));
        scrollSector.Ceiling.RenderOffsets.Offset.Should().Be(new Vec2D(0, -64));
    }

    [Fact(DisplayName = "Boom Action 246 Scroll floor when sector height changes")]
    public void Action246()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 16);

        GameActions.ActivateLine(World, World.Player, 316, ActivationContext.UseLine);
        GameActions.RunSectorPlaneSpecial(World, scrollSector);
        GameActions.TickWorld(World, 1);
        scrollSector.Ceiling.RenderOffsets.Offset.Should().Be(new Vec2D(0, 0));
        scrollSector.Floor.RenderOffsets.Offset.Should().Be(new Vec2D(0, 64));

        var barrel = GameActions.GetSectorEntity(World, 58, "ExplosiveBarrel");
        barrel.Velocity.XY.Should().Be(Vec2D.Zero);
    }

    [Fact(DisplayName = "Boom Action 247 Scroll floor objects when sector height changes")]
    public void Action247()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 17);

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

        GameActions.ActivateLine(World, World.Player, 341, ActivationContext.UseLine);
        GameActions.RunSectorPlaneSpecial(World, scrollSector);
        GameActions.TickWorld(World, 1);
        scrollSector.Ceiling.RenderOffsets.Offset.Should().Be(new Vec2D(0, 0));
        scrollSector.Floor.RenderOffsets.Offset.Should().Be(new Vec2D(0, 64));

        var barrel = GameActions.GetSectorEntity(World, 64, "ExplosiveBarrel");
        barrel.Velocity.XY.Should().NotBe(Vec2D.Zero);
    }

    [Fact(DisplayName = "Boom Action 249 Scroll wall when sector height changes")]
    public void Action249()
    {
        var scrollLine1 = GameActions.GetLine(World, 352);
        var scrollLine2 = GameActions.GetLine(World, 362);
        var moveSector = GameActions.GetSectorByTag(World, 19);

        GameActions.ActivateLine(World, World.Player, 352, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, moveSector);
        GameActions.TickWorld(World, 1);
        AssertScrollOffsets(scrollLine1.Front.ScrollData!, new Vec2D(64, 0));
        AssertScrollOffsets(scrollLine2.Front.ScrollData!, new Vec2D(64, 0));

        GameActions.ActivateLine(World, World.Player, 362, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, moveSector);
        GameActions.TickWorld(World, 1);
        AssertScrollOffsets(scrollLine1.Front.ScrollData!, Vec2D.Zero);
        AssertScrollOffsets(scrollLine2.Front.ScrollData!, Vec2D.Zero);
    }

    [Fact(DisplayName = "Boom Action 250 Scroll ceiling")]
    public void Action250()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 11);
        GameActions.TickWorld(World, 1);
        var offset = scrollSector.Ceiling.RenderOffsets.LastOffset - scrollSector.Ceiling.RenderOffsets.Offset;
        offset.Should().Be(new Vec2D(2, 0));
        scrollSector.Floor.RenderOffsets.Offset.Should().Be(new Vec2D(0, 0));
    }

    [Fact(DisplayName = "Boom Action 251 Scroll floor")]
    public void Action251()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 12);
        GameActions.TickWorld(World, 1);
        var offset = scrollSector.Floor.RenderOffsets.LastOffset - scrollSector.Floor.RenderOffsets.Offset;
        offset.Should().Be(new Vec2D(2, 0));
        scrollSector.Ceiling.RenderOffsets.Offset.Should().Be(new Vec2D(0, 0));
    }

    [Fact(DisplayName = "Boom Action 252 Scroll objects on floor")]
    public void Action252()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 13);
        var barrel = GameActions.GetSectorEntity(World, 52, "ExplosiveBarrel");
        GameActions.TickWorld(World, 1);
        barrel.Velocity.XY.Should().NotBe(Vec2D.Zero);
        scrollSector.Ceiling.RenderOffsets.Offset.Should().Be(new Vec2D(0, 0));
        scrollSector.Floor.RenderOffsets.Offset.Should().Be(new Vec2D(0, 0));
    }

    [Fact(DisplayName = "Boom Action 252 Scroll floor and objects")]
    public void Action253()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 9);
        var barrel = GameActions.GetSectorEntity(World, 47, "ExplosiveBarrel");
        GameActions.TickWorld(World, 1);
        var offset = scrollSector.Floor.RenderOffsets.LastOffset - scrollSector.Floor.RenderOffsets.Offset;
        offset.Should().Be(new Vec2D(2, 0));
        barrel.Velocity.XY.Should().NotBe(Vec2D.Zero);
        scrollSector.Ceiling.RenderOffsets.Offset.Should().Be(new Vec2D(0, 0));
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

    [Fact(DisplayName = "Boom Action 1024 Scroll tagged wall using offsets")]
    public void Action1024()
    {
        var line1 = GameActions.GetLine(World, 367);
        var line2 = GameActions.GetLine(World, 368);
        var line3 = GameActions.GetLine(World, 370);

        GameActions.TickWorld(World, 1);
        line1.Front.ScrollData.Should().NotBeNull();
        line2.Front.ScrollData.Should().NotBeNull();
        line3.Front.ScrollData.Should().NotBeNull();

        AssertScrollDiffOffsets(line1.Front.ScrollData!, new Vec2D(0.25, -0.125));
        AssertScrollDiffOffsets(line2.Front.ScrollData!, new Vec2D(0.125, -0.25));
        AssertScrollDiffOffsets(line3.Front.ScrollData!, new Vec2D(-0.25, 0.125));
    }

    [Fact(DisplayName = "Boom Action 1025 Scroll tagged wall using offsets displacement")]
    public void Action1025()
    {
        var line1 = GameActions.GetLine(World, 374);
        var line2 = GameActions.GetLine(World, 375);
        var line3 = GameActions.GetLine(World, 377);
        var moveSector = GameActions.GetSectorByTag(World, 24);

        GameActions.TickWorld(World, 1);
        line1.Front.ScrollData.Should().NotBeNull();
        line2.Front.ScrollData.Should().NotBeNull();
        line3.Front.ScrollData.Should().NotBeNull();

        GameActions.ActivateLine(World, World.Player, 379, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, moveSector);
        GameActions.TickWorld(World, 1);

        AssertScrollOffsets(line1.Front.ScrollData!, new Vec2D(-4, 8));
        AssertScrollOffsets(line2.Front.ScrollData!, new Vec2D(-4, 8));
        AssertScrollOffsets(line3.Front.ScrollData!, new Vec2D(-4, 8));

        GameActions.ActivateLine(World, World.Player, 389, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, moveSector);
        GameActions.TickWorld(World, 1);

        AssertScrollOffsets(line1.Front.ScrollData!, Vec2D.Zero);
        AssertScrollOffsets(line2.Front.ScrollData!, Vec2D.Zero);
        AssertScrollOffsets(line3.Front.ScrollData!, Vec2D.Zero);
    }

    [Fact(DisplayName = "Boom Action 1026 Scroll tagged wall using offsets accelerative")]
    public void Action1026()
    {
        var line1 = GameActions.GetLine(World, 402);
        var line2 = GameActions.GetLine(World, 403);
        var line3 = GameActions.GetLine(World, 404);
        var moveSector = GameActions.GetSectorByTag(World, 25);

        GameActions.TickWorld(World, 1);
        line1.Front.ScrollData.Should().NotBeNull();
        line2.Front.ScrollData.Should().NotBeNull();
        line3.Front.ScrollData.Should().NotBeNull();

        GameActions.ActivateLine(World, World.Player, 400, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, moveSector);
        GameActions.TickWorld(World, 1);

        AssertScrollDiffOffsets(line1.Front.ScrollData!, new Vec2D(4, -8));
        AssertScrollDiffOffsets(line2.Front.ScrollData!, new Vec2D(4, -8));
        AssertScrollDiffOffsets(line3.Front.ScrollData!, new Vec2D(4, -8));

        GameActions.ActivateLine(World, World.Player, 391, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, moveSector);
        GameActions.TickWorld(World, 1);

        AssertScrollDiffOffsets(line1.Front.ScrollData!, Vec2D.Zero);
        AssertScrollDiffOffsets(line2.Front.ScrollData!, Vec2D.Zero);
        AssertScrollDiffOffsets(line3.Front.ScrollData!, Vec2D.Zero);
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