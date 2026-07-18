using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction._3DSector;

[Collection("GameActions")]
public class Sector3D_EntityLightSector
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public Sector3D_EntityLightSector()
    {
        World = WorldAllocator.LoadMap("Resources/sector3d-map.zip", "sector3d-map.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Entity light sector 1")]
    public void LightSector1()
    {
        var sector = GameActions.GetSector(World, 191);
        var yellowSector = GameActions.GetSector(World, 193);
        GameActions.SetEntityPosition(World, Player, (-2848, 1488));
        Player.LightSector3D.Should().NotBeNull();
        Player.LightSector3D.Should().Be(yellowSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out var lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(yellowSector);

        GameActions.SetEntityPosition(World, Player, (-2848, 1488, 96));
        Player.LightSector3D.Should().NotBeNull();
        Player.LightSector3D.Should().Be(yellowSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(sector);

        GameActions.SetEntityPosition(World, Player, (-2848, 1488, 128));
        Player.LightSector3D.Should().NotBeNull();
        Player.LightSector3D.Should().Be(sector);
        Sector3D.TryGetValidViewLightSector3D(Player, out lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(sector);
    }

    [Fact(DisplayName = "Entity light sector 2")]
    public void LightSector2()
    {
        var yellowSector = GameActions.GetSector(World, 193);
        var redSector = GameActions.GetSector(World, 195);
        GameActions.SetEntityPosition(World, Player, (-2720, 1488));
        Player.LightSector3D.Should().NotBeNull();
        Player.LightSector3D.Should().Be(redSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out var lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(redSector);

        GameActions.SetEntityPosition(World, Player, (-2720, 1488, 32));
        Player.LightSector3D.Should().NotBeNull();
        Player.LightSector3D.Should().Be(redSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(yellowSector);
    }

    [Fact(DisplayName = "Entity light sector 3")]
    public void LightSector3()
    {
        var yellowSector = GameActions.GetSector(World, 217);
        var redSector = GameActions.GetSector(World, 218);
        GameActions.SetEntityPosition(World, Player, (-2000, 1632));
        Player.LightSector3D.Should().NotBeNull();
        Player.LightSector3D.Should().Be(yellowSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out var lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(yellowSector);

        GameActions.SetEntityPosition(World, Player, (-2000, 1632, 72));
        Player.LightSector3D.Should().NotBeNull();
        Player.LightSector3D.Should().Be(yellowSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(redSector);
    }

    [Fact(DisplayName = "Entity light sector 4")]
    public void LightSector4()
    {
        var yellowSector = GameActions.GetSector(World, 220);
        var redSector = GameActions.GetSector(World, 221);
        GameActions.SetEntityPosition(World, Player, (-2000, 1760));
        Player.LightSector3D.Should().NotBeNull();
        Player.LightSector3D.Should().Be(redSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out var lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(redSector);

        GameActions.SetEntityPosition(World, Player, (-2000, 1760, 32));
        Player.LightSector3D.Should().NotBeNull();
        Player.LightSector3D.Should().Be(redSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(yellowSector);

        // Light sector needs to overlap at least 16 map units
        GameActions.SetEntityPosition(World, Player, (-2000, 1760, 39));
        Player.Sector.Id.Should().Be(219);
        Player.LightSector3D.Should().NotBeNull();
        Player.LightSector3D.Should().Be(redSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(yellowSector);
    }

    [Fact(DisplayName = "Entity light sector 5")]
    public void LightSector5()
    {
        var darkSector = GameActions.GetSector(World, 277);
        var lightSector = GameActions.GetSector(World, 276);
        var sector = GameActions.GetSector(World, 272);

        GameActions.SetEntityPosition(World, Player, (-80, 2192, 0));
        Player.LightSector3D.Should().Be(darkSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out var lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(darkSector);

        GameActions.SetEntityPosition(World, Player, (-80, 2192, 32));
        Player.LightSector3D.Should().Be(darkSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(darkSector);

        GameActions.SetEntityPosition(World, Player, (-80, 2192, 96));
        Player.LightSector3D.Should().Be(lightSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(lightSector);

        GameActions.SetEntityPosition(World, Player, (-80, 2192, 128));
        Player.LightSector3D.Should().Be(lightSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(lightSector);

        GameActions.SetEntityPosition(World, Player, (-80, 2192, 192));
        Player.LightSector3D.Should().Be(sector);
        Sector3D.TryGetValidViewLightSector3D(Player, out lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(sector);

        GameActions.SetEntityPosition(World, Player, (-80, 2192, 224));
        Player.LightSector3D.Should().Be(sector);
        Sector3D.TryGetValidViewLightSector3D(Player, out lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(sector);
    }

    [Fact(DisplayName = "Entity light sector 6")]
    public void LightSector6()
    {
        var yellowSector = GameActions.GetSector(World, 299);
        GameActions.SetEntityPosition(World, Player, (16, 2232, 96));
        Player.LightSector3D.Should().Be(yellowSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out var lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(yellowSector);
    }

    [Fact(DisplayName = "Entity light sector 7")]
    public void LightSector7()
    {
        var redSector = GameActions.GetSector(World, 300);
        GameActions.SetEntityPosition(World, Player, (16, 2232, 0));
        Player.LightSector3D.Should().Be(redSector);
        Sector3D.TryGetValidViewLightSector3D(Player, out var lightSector3D).Should().BeTrue();
        lightSector3D.Should().Be(redSector);
    }
}
