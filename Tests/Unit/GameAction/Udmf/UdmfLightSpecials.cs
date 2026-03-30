using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfLightSpecials
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public UdmfLightSpecials()
    {
        World = WorldAllocator.LoadMap("Resources/udmflightspecials.zip", "udmflightspecials.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Light raise by value")]
    public void LightRaiseByValue()
    {
        var sector = GameActions.GetSectorByTag(World, 1);
        sector.LightLevel.Should().Be(64);
        GameActions.ActivateLine(World, Player, 12, ActivationContext.UseLine).Should().BeTrue();
        sector.LightLevel.Should().Be(128);
        GameActions.ActivateLine(World, Player, 12, ActivationContext.UseLine).Should().BeTrue();
        sector.LightLevel.Should().Be(192);
        GameActions.ActivateLine(World, Player, 12, ActivationContext.UseLine).Should().BeTrue();
        sector.LightLevel.Should().Be(256);
        GameActions.ActivateLine(World, Player, 12, ActivationContext.UseLine).Should().BeTrue();
        sector.LightLevel.Should().Be(320);
    }

    [Fact(DisplayName = "Light lower by value")]
    public void LightLowerByValue()
    {
        var sector = GameActions.GetSectorByTag(World, 2);
        sector.LightLevel.Should().Be(255);
        GameActions.ActivateLine(World, Player, 16, ActivationContext.UseLine).Should().BeTrue();
        sector.LightLevel.Should().Be(191);
        GameActions.ActivateLine(World, Player, 16, ActivationContext.UseLine).Should().BeTrue();
        sector.LightLevel.Should().Be(127);
        GameActions.ActivateLine(World, Player, 16, ActivationContext.UseLine).Should().BeTrue();
        sector.LightLevel.Should().Be(63);
        GameActions.ActivateLine(World, Player, 16, ActivationContext.UseLine).Should().BeTrue();
        sector.LightLevel.Should().Be(-1);
    }

    [Fact(DisplayName = "Light set to value")]
    public void LightSetToValue()
    {
        var sector = GameActions.GetSectorByTag(World, 3);
        sector.LightLevel.Should().Be(255);
        GameActions.ActivateLine(World, Player, 20, ActivationContext.UseLine).Should().BeTrue();
        sector.LightLevel.Should().Be(69);
        GameActions.ActivateLine(World, Player, 20, ActivationContext.UseLine).Should().BeTrue();
        sector.LightLevel.Should().Be(69);
    }
}
