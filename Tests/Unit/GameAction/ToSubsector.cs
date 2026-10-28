using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class ToSubsector
{
    private readonly SinglePlayerWorld World;

    public ToSubsector()
    {
        World = WorldAllocator.LoadMap("Resources/tosubsector.zip", "tosubsector.wad", "E1M1", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact]
    public void ToSubsectorBlockMapMatch()
    {
        // Some simple coherency checks
        World.ToSubsector(1824, -3280).Sector.Id.Should().Be(0);
        World.ToSubsector(2080, -3152).Sector.Id.Should().Be(1);
        World.ToSubsector(1056, -3552).Sector.Id.Should().Be(29);
        World.ToSubsector(-224, -3232).Sector.Id.Should().Be(44);
        World.ToSubsector(2848, -3584).Sector.Id.Should().Be(52);
        World.ToSubsector(2240, -2480).Sector.Id.Should().Be(60);
        World.ToSubsector(2944, -4256).Sector.Id.Should().Be(64);

        var top = World.Blockmap.Bounds.TopLeft;
        var bottom = World.Blockmap.Bounds.BottomRight;

        top.X = Math.Max(World.Blockmap.Bounds.TopLeft.X, World.Blockmap.Bounds.BottomRight.X);
        top.Y = Math.Max(World.Blockmap.Bounds.TopLeft.Y, World.Blockmap.Bounds.BottomRight.Y);
        bottom.X = Math.Min(World.Blockmap.Bounds.TopLeft.X, World.Blockmap.Bounds.BottomRight.X);
        bottom.Y = Math.Min(World.Blockmap.Bounds.TopLeft.Y, World.Blockmap.Bounds.BottomRight.Y);

        for (int x = (int)bottom.X; x < top.X; x++)
        {
            for (int y = (int)bottom.Y; y < top.Y; y++)
            {
                var subsector = World.ToSubsector(x, y);
                var treeSubsector = World.BspTree.ToSubsector((uint)World.BspTree.Nodes.Length - 1, x, y);
                subsector.Should().Be(treeSubsector);
            }
        }
    }
}
