using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Helion.World.Special;
using Helion.World.Special.Specials;
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

    [Fact(DisplayName = "Light fade to value")]
    public void LightFadeToValue()
    {
        var sector = GameActions.GetSectorByTag(World, 4);
        sector.LightLevel.Should().Be(255);
        GameActions.ActivateLine(World, Player, 28, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 10);
        sector.LightLevel.Should().Be(250);
        GameActions.TickWorld(World, 10);
        sector.LightLevel.Should().Be(245);
        GameActions.TickWorld(World, 330);
        sector.LightLevel.Should().Be(64);
        GameActions.TickWorld(World, 35);
        sector.LightLevel.Should().Be(64);

        GameActions.ActivateLine(World, Player, 28, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 35);
        sector.LightLevel.Should().Be(64);
    }

    [Fact(DisplayName = "Light glow")]
    public void LightGlow()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.LightLevel.Should().Be(128);
        GameActions.ActivateLine(World, Player, 36, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 1);
        sector.LightLevel.Should().Be(248);
        GameActions.TickWorld(World, 16);
        sector.LightLevel.Should().Be(132);
        GameActions.TickWorld(World, 18);
        sector.LightLevel.Should().Be(0);
        GameActions.TickWorld(World, 17);
        sector.LightLevel.Should().Be(123);
        GameActions.TickWorld(World, 18);
        sector.LightLevel.Should().Be(255);
    }

    [Fact(DisplayName = "Light flicker")]
    public void LightFlicker()
    {
        var sector = GameActions.GetSectorByTag(World, 6);
        GameActions.ActivateLine(World, Player, 44, ActivationContext.UseLine).Should().BeTrue();
        var special = World.SpecialManager.FindSpecialBySector(sector);
        special.Should().NotBeNull();

        var light = special as LightFlickerDoomSpecial;
        light.Should().NotBeNull();
        light.MaxBright.Should().Be(192);
        light.MinBright.Should().Be(64);
    }

    [Fact(DisplayName = "Light strobe")]
    public void LightStrobe()
    {
        var sector = GameActions.GetSectorByTag(World, 7);
        GameActions.ActivateLine(World, Player, 52, ActivationContext.UseLine).Should().BeTrue();
        var special = World.SpecialManager.FindSpecialBySector(sector);
        special.Should().NotBeNull();

        var light = special as LightStrobeSpecial;
        light.Should().NotBeNull();
        light.MaxBright.Should().Be(255);
        light.MinBright.Should().Be(128);
        light.BrightTicks.Should().Be(35);
        light.DarkTicks.Should().Be(70);
    }
}
