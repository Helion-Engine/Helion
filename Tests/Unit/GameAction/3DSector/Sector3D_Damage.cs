using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Cheats;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction._3DSector;


[Collection("GameActions")]
public class Sector3D_Damage
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public Sector3D_Damage()
    {
        World = WorldAllocator.LoadMap("Resources/sector3d-damage.zip", "sector3d-damage.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);

        World.CheatManager.ActivateCheat(Player, CheatType.Fly);
    }

    [Fact(DisplayName = "non-solid 3D sector transfers damage")]
    public void DamageTransferNonSolid()
    {
        var sector = GameActions.GetSector(World, 3);
        var sector3D = GameActions.GetSector(World, 4);

        sector3D.SectorDamageSpecial.Should().NotBeNull();
        sector3D.SectorDamageSpecial.Damage.Should().Be(5);

        GameActions.SetEntityPosition(World, Player, (-832, 832, -64));
        Player.Sector.Should().Be(sector);
        Player.Health.Should().Be(100);

        GameActions.TickWorld(World, 32);
        Player.Health.Should().Be(95);
        Player.Health = 100;

        GameActions.SetEntityPosition(World, Player, new Vec3D(-832, 832, -32));
        GameActions.TickWorld(World, 32);
        Player.Health.Should().Be(95);
        Player.Health = 100;

        GameActions.SetEntityPosition(World, Player, new Vec3D(-832, 832, -16));
        GameActions.TickWorld(World, 32);
        Player.Health.Should().Be(95);
        Player.Health = 100;

        GameActions.SetEntityPosition(World, Player, new Vec3D(-832, 832, 0));
        GameActions.TickWorld(World, 32);
        Player.Health.Should().Be(100);

        GameActions.SetEntityPosition(World, Player, new Vec3D(-832, 832, 16));
        GameActions.TickWorld(World, 32);
        Player.Health.Should().Be(100);

        GameActions.SetEntityPosition(World, Player, (0, 0));
    }

    [Fact(DisplayName = "non-solid 3D sector transfers damage exactly under and over")]
    public void DamageTransferNonSolidExactlyUnderOver()
    {
        var sector = GameActions.GetSector(World, 2);
        var sector3D = GameActions.GetSector(World, 1);

        sector3D.SectorDamageSpecial.Should().NotBeNull();
        sector3D.SectorDamageSpecial.Damage.Should().Be(5);

        GameActions.SetEntityPosition(World, Player, (-576, 832, 8));
        Player.Sector.Should().Be(sector);

        GameActions.TickWorld(World, 32);
        Player.Health.Should().Be(95);
        Player.Health = 100;

        GameActions.SetEntityPosition(World, Player, (-576, 832, 0));
        GameActions.TickWorld(World, 32);
        Player.Health.Should().Be(100);

        GameActions.SetEntityPosition(World, Player, (-576, 832, 192));
        GameActions.TickWorld(World, 32);
        Player.Health.Should().Be(95);
        Player.Health = 100;

        GameActions.SetEntityPosition(World, Player, (-576, 832, 200));
        GameActions.TickWorld(World, 32);
        Player.Health.Should().Be(100);
    }

    [Fact(DisplayName = "solid 3D sector transfers damage through control top")]
    public void DamageTransferSolid()
    {
        var sector = GameActions.GetSector(World, 5);
        var sector3D = GameActions.GetSector(World, 6);

        sector3D.SectorDamageSpecial.Should().NotBeNull();
        sector3D.SectorDamageSpecial.Damage.Should().Be(5);

        GameActions.SetEntityPosition(World, Player, (-320, 832, 128));
        Player.Sector.Should().Be(sector);
        Player.OnGround.Should().BeTrue();

        GameActions.TickWorld(World, 32);
        Player.Health.Should().Be(95);
        Player.Health = 100;

        GameActions.SetEntityPosition(World, Player, (352, 832, 129));
        GameActions.TickWorld(World, 32);
        Player.Health.Should().Be(100);

        GameActions.SetEntityPosition(World, Player, (352, 832, 0));
        GameActions.TickWorld(World, 32);
        Player.Health.Should().Be(100);
    }
}
