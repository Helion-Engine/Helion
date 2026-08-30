using FluentAssertions;
using Helion.Render;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class RenderBlocking
{
    private readonly SinglePlayerWorld World;

    public RenderBlocking()
    {
        World = WorldAllocator.LoadMap("Resources/renderblock.zip", "renderblock.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Rendering blocked from front side")]
    public void RenderBlockFrontSide()
    {
        AssertLine(8, true);
        AssertLine(11, true);
    }

    [Fact(DisplayName = "Rendering blocked from back side")]
    public void RenderBlockBackSide()
    {
        AssertLine(15, true);
        AssertLine(12, true);
    }

    [Fact(DisplayName = "Rendering not blocked from front side")]
    public void RenderNotBlockedFrontSide()
    {
        AssertLine(51, false);
        AssertLine(50, false);
    }

    [Fact(DisplayName = "Rendering not blocked from front side")]
    public void RenderNotBlockedBackSide()
    {
        AssertLine(60, false);
        AssertLine(61, false);
    }

    [Fact(DisplayName = "Rendering not blocked using render hack with no upper")]
    public void RenderNotBlockedHack()
    {
        AssertLine(33, false);
    }

    [Fact(DisplayName = "Rendering blocked with ceiling lower than floor")]
    public void RenderBlockCeilingLowerThanFloor()
    {
        AssertLine(55, true);
        AssertLine(58, true);
    }


    [Fact(DisplayName = "Rendering blocked with floor higher than ceiling front side (lift)")]
    public void RenderBlockLiftFront()
    {
        AssertLine(84, true);
    }

    [Fact(DisplayName = "Rendering blocked with floor higher than ceiling back side (lift)")]
    public void RenderBlockLiftBack()
    {
        AssertLine(91, true);
    }

    [Fact(DisplayName = "Rendering not blocked when transfer heights is set")]
    public void RenderBlockTransferHeightsIgnored()
    {
        AssertLine(310, false);
        AssertLine(312, false);

        AssertLine(321, false);
        AssertLine(323, false);
    }

    private void AssertLine(int lineId, bool blocked)
    {
        var line = GameActions.GetLine(World, lineId);
        var isBlocked = RenderBlock.IsBlocked(line, true) || RenderBlock.IsBlocked(line, false);
        isBlocked.Should().Be(blocked);
    }
}
