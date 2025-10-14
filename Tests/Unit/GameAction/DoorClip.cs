using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class DoorClip
{
    private static readonly string ResourceZip = "Resources/doorclip.zip";
    private static readonly string MapName = "MAP01";

    private readonly SinglePlayerWorld World;

    private Player Player => World.Player;

    public DoorClip()
    {
        World = WorldAllocator.LoadMap(ResourceZip, "doorclip.WAD", MapName, GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Door ceiling clips floor")]
    public void DoorClipsFloor()
    {
        World.Config.Compatibility.VanillaSectorPhysics.Value.Should().BeFalse();
        var sector = GameActions.GetSector(World, 2);
        sector.Ceiling.Z.Should().Be(0);
        GameActions.ActivateLine(World, Player, 3, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Ceiling.Z.Should().Be(-4);
    }
}
