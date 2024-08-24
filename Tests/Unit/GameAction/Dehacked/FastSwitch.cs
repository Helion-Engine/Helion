using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction.Util;
using Helion.Util;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Dehacked;

[Collection("GameActions")]
public class FastSwitch : IDisposable
{
    private const int SwitchTicks = 8;
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public FastSwitch()
    {
        World = WorldAllocator.LoadMap("Resources/fastswitch.zip", "fastswitch.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
        World.Player.TickCommand = new TestTickCommand();
    }

    public void Dispose()
    {
        InventoryUtil.Reset(World, Player);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
        // Dehacked modifies offset to 52
        world.Player.WeaponOffset.Y.Should().Be(52 - Constants.WeaponRaiseSpeed);
        InventoryUtil.AssertWeapon(world.Player.Weapon, "Pistol");
        GameActions.TickWorld(world, 1);
        InventoryUtil.AssertWeapon(world.Player.AnimationWeapon, "Pistol");

        InventoryUtil.RunWeaponSwitch(world, world.Player, "Pistol");
        InventoryUtil.AssertWeapon(world.Player.Weapon, "Pistol");
    }

    [Fact(DisplayName = "Fast switch weapon")]
    public void FastSwitchWeapon()
    {
        int startTick = World.Gametick;
        Player.ChangeWeapon(InventoryUtil.GetWeapon(Player, "Fist"));
        InventoryUtil.RunWeaponSwitch(World, Player, "Fist");
        InventoryUtil.AssertWeapon(Player.Weapon, "Fist");
        int ticks = World.Gametick - startTick;
        ticks.Should().Be(SwitchTicks);
    }

    [Fact(DisplayName = "Fast switch weapon and fire")]
    public void FastSwitchWeaponAndFire()
    {
        int startTick = World.Gametick;
        Player.GiveItem(GameActions.GetEntityDefinition(World, "Shotgun"), null);
        InventoryUtil.RunWeaponSwitch(World, Player, "Shotgun");
        InventoryUtil.AssertWeapon(Player.Weapon, "Shotgun");
        int ticks = World.Gametick - startTick;
        ticks.Should().Be(SwitchTicks);

        for (int i = 0; i < 2; i++)
        {
            Player.FireWeapon().Should().BeTrue();
            InventoryUtil.RunWeaponFire(World, Player);
        }

        startTick = World.Gametick;
        Player.ChangeWeapon(InventoryUtil.GetWeapon(Player, "Pistol"));
        InventoryUtil.RunWeaponSwitch(World, Player, "Pistol");
        InventoryUtil.AssertWeapon(Player.Weapon, "Pistol");
        ticks = World.Gametick - startTick;
        ticks.Should().Be(SwitchTicks);
    }

    [Fact(DisplayName = "Fast switch weapon during fire")]
    public void FastSwitchWeaponDuringFire()
    {
        Player.GiveItem(GameActions.GetEntityDefinition(World, "Clip"), null);

        Player.FireWeapon().Should().BeTrue();
        Player.ChangeWeapon(InventoryUtil.GetWeapon(Player, "Fist"));
        InventoryUtil.RunWeaponFire(World, Player);

        int startTick = World.Gametick;
        InventoryUtil.RunWeaponSwitch(World, Player, "Fist");
        InventoryUtil.AssertWeapon(Player.Weapon, "Fist");
        int ticks = World.Gametick - startTick;
        ticks.Should().Be(1);
    }

    [Fact(DisplayName = "Fast switch weapon back and forth")]
    public void FastSwitchWeaponMultiple()
    {
        int startTick = World.Gametick;
        Player.ChangeWeapon(InventoryUtil.GetWeapon(Player, "Fist"));
        InventoryUtil.RunWeaponSwitch(World, Player, "Fist");
        InventoryUtil.AssertWeapon(Player.Weapon, "Fist");
        int ticks = World.Gametick - startTick;
        ticks.Should().Be(SwitchTicks);

        startTick = World.Gametick;
        Player.ChangeWeapon(InventoryUtil.GetWeapon(Player, "Pistol"));
        InventoryUtil.RunWeaponSwitch(World, Player, "Pistol");
        InventoryUtil.AssertWeapon(Player.Weapon, "Pistol");
        ticks = World.Gametick - startTick;
        ticks.Should().Be(SwitchTicks);
    }

    [Fact(DisplayName = "Fast switch weapon pickup")]
    public void FastSwitchWeaponPickup()
    {
        int startTick = World.Gametick;
        Player.GiveItem(GameActions.GetEntityDefinition(World, "Shotgun"), null);
        InventoryUtil.RunWeaponSwitch(World, Player, "Shotgun");
        InventoryUtil.AssertWeapon(Player.Weapon, "Shotgun");
        int ticks = World.Gametick - startTick;
        ticks.Should().Be(SwitchTicks);
    }
}
