using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction.Util;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Dehacked;

[Collection("GameActions")]
public class PerAmmoMaxAmmo
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public PerAmmoMaxAmmo()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2,
            dehackedPatch: Dehacked);
        InventoryUtil.Reset(World, Player);
    }

    [Fact(DisplayName = "PerAmmo/MaxAmmo Bullets")]
    public void Bullets()
    {
        GameActions.GiveItem(Player, "Clip").Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "Clip", 70);
        GameActions.GiveItem(Player, "ClipBox").Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "Clip", 170);

        GiveMax("Clip");
        InventoryUtil.AssertAmount(Player, "Clip", 500);
    }

    [Fact(DisplayName = "PerAmmo Shells")]
    public void Shells()
    {
        GameActions.GiveItem(Player, "Shell").Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "Shell", 10);
        GameActions.GiveItem(Player, "ShellBox").Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "Shell", 60);

        GiveMax("Shell");
        InventoryUtil.AssertAmount(Player, "Shell", 600);
    }

    [Fact(DisplayName = "PerAmmo/MaxAmmo Rockets")]
    public void Rockets()
    {
        GameActions.GiveItem(Player, "RocketAmmo").Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "RocketAmmo", 5);
        GameActions.GiveItem(Player, "RocketBox").Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "RocketAmmo", 30);

        GiveMax("RocketAmmo");
        InventoryUtil.AssertAmount(Player, "RocketAmmo", 700);
    }

    [Fact(DisplayName = "PerAmmo/MaxAmmo Cells")]
    public void Cells()
    {
        GameActions.GiveItem(Player, "Cell").Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "Cell", 44);
        GameActions.GiveItem(Player, "CellPack").Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "Cell", 264);

        GiveMax("Cell");
        InventoryUtil.AssertAmount(Player, "Cell", 800);
    }

    [Fact(DisplayName = "PerAmmo Backpack")]
    public void Backpack()
    {
        GameActions.GiveItem(Player, "Backpack").Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "Clip", 70);
        InventoryUtil.AssertAmount(Player, "Shell", 10);
        InventoryUtil.AssertAmount(Player, "RocketAmmo", 5);
        InventoryUtil.AssertAmount(Player, "Cell", 44);
    }

    [Fact(DisplayName = "PerAmmo Shotgun")]
    public void Shotgun()
    {
        GameActions.GiveItem(Player, "Shotgun").Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "Shell", 20);
    }

    [Fact(DisplayName = "PerAmmo Chaingun")]
    public void Chaingun()
    {
        GameActions.GiveItem(Player, "Chaingun").Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "Clip", 90);
    }

    [Fact(DisplayName = "PerAmmo Rocket Launcher")]
    public void RocketLauncher()
    {
        GameActions.GiveItem(Player, "RocketLauncher").Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "RocketAmmo", 10);
    }

    [Fact(DisplayName = "PerAmmo Plasma Rifle")]
    public void PlasmaRifle()
    {
        GameActions.GiveItem(Player, "PlasmaRifle").Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "Cell", 88);
    }

    [Fact(DisplayName = "PerAmmo BFG 9000")]
    public void BFG9000()
    {
        GameActions.GiveItem(Player, "BFG9000").Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "Cell", 88);
    }

    private void GiveMax(string item)
    {
        for (int i = 0; i < 200; i++)
            GameActions.GiveItem(Player, item);
    }

    private static readonly string Dehacked =
@"
Ammo 0 (Bullets)
Max ammo = 500
Per ammo = 20

Ammo 1 (Shells)
Max ammo = 600
Per ammo = 10

Ammo 2 (Cells)
Max ammo = 800
Per ammo = 44

Ammo 3 (Rockets)
Max ammo = 700
Per ammo = 5
";
}
