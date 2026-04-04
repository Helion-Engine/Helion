using Helion.Resources.IWad;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfTeleportSpecials
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public UdmfTeleportSpecials()
    {
        World = WorldAllocator.LoadMap("Resources/udmfteleportspecials.zip", "udmfteleportspecials.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Teleport in sector without tid")]
    public void TeleportInSectorWithoutTid()
    {

    }

    [Fact(DisplayName = "Teleport in sector with td")]
    public void TeleportInSectorWithTid()
    {

    }

    [Fact(DisplayName = "Teleport group")]
    public void TeleportGroup()
    {

    }
}
