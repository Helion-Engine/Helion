using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction.Util;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Dehacked;

[Collection("GameActions")]
public class PlayerSpriteOffset
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public PlayerSpriteOffset()
    {
        World = WorldAllocator.LoadMap("Resources/playerspriteoffset.zip", "playerspriteoffset.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
        World.Player.TickCommand = new TestTickCommand();
    }

    private void WorldInit(IWorld world)
    {
        world.Player.WeaponOffset.X = 0;
        world.Player.WeaponOffset.Y = 0;
    }

    [Fact(DisplayName ="Non-weapon state doesn't modify weapon sprite offset")]
    public void NonWeaponStateOffset()
    {
        GameActions.TickWorld(World, 35);
        Player.WeaponOffset.X.Should().Be(0);
        Player.WeaponOffset.Y.Should().Be(32);

        Player.Damage(null, 5, true, DamageType.AlwaysApply);

        GameActions.TickWorld(World, 35);
        Player.WeaponOffset.X.Should().Be(0);
        Player.WeaponOffset.Y.Should().Be(32);
    }

    [Fact(DisplayName = "Weapon state modifies weapon sprite offset")]
    public void WeaponStateOffset()
    {
        GameActions.TickWorld(World, 35);
        Player.WeaponOffset.X.Should().Be(0);
        Player.WeaponOffset.Y.Should().Be(32);

        Player.FireWeapon();

        GameActions.TickWorld(World, 1);
        Player.WeaponOffset.X.Should().Be(69);
        Player.WeaponOffset.Y.Should().Be(420);

        // A_WeaponReady should reset back to normal
        GameActions.TickWorld(World, 35);
        Player.WeaponOffset.X.Should().Be(0);
        Player.WeaponOffset.Y.Should().Be(32);
    }
}
