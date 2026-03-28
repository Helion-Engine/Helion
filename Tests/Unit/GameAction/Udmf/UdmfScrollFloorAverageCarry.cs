using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfScrollFloorAverageCarry : IDisposable
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public UdmfScrollFloorAverageCarry()
    {
        World = WorldAllocator.LoadMap("Resources/udmfscrollfloor.zip", "udmfscrollfloor.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    public void Dispose()
    {
        Player.Velocity = Vec3D.Zero;
    }

    public void ScrollAverageSameY()
    {

    }

    public void ScrollAverageDifferentY()
    {

    }

    public void ScrollAverageOppositeY()
    {

    }
}
