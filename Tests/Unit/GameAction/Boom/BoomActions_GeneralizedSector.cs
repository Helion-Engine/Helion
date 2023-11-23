using FluentAssertions;
using Helion.World;
using Helion.World.Entities.Players;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

public partial class BoomActions
{
    [Fact(DisplayName = "Kill unprotected player")]
    public void KillUnprotectedPlayer()
    {
        var monster = GameActions.GetEntity(World, 15);
        GameActions.TickWorld(World, 1);
        monster.IsDead.Should().BeFalse();

        Player.GiveItem(GameActions.GetEntityDefinition(World, "Radsuit"), null);

        GameActions.SetEntityToLine(World, Player, 413, 64);
        GameActions.TickWorld(World, 1);
        Player.IsDead.Should().BeFalse();

        Player.Inventory.Remove("RadSuit", 1);
        Player.Inventory.Powerups.Clear();
        GameActions.TickWorld(World, 1);
        Player.IsDead.Should().BeTrue();
    }

    [Fact(DisplayName = "Kill player")]
    public void KillPlayer()
    {
        var monster = GameActions.GetEntity(World, 20);
        GameActions.TickWorld(World, 1);
        monster.IsDead.Should().BeFalse();

        Player.GiveItem(GameActions.GetEntityDefinition(World, "Radsuit"), null);

        GameActions.SetEntityToLine(World, Player, 429, 64);
        GameActions.TickWorld(World, 1);
        Player.IsDead.Should().BeTrue();

        Player.Inventory.Remove("RadSuit", 1);
    }

    [Fact(DisplayName = "Kill player and exit")]
    public void KillPlayerAndExit()
    {
        bool exited = false;
        World.LevelExit += World_LevelExit;
        var monster = GameActions.GetEntity(World, 16);
        GameActions.TickWorld(World, 1);
        monster.IsDead.Should().BeFalse();

        Player.GiveItem(GameActions.GetEntityDefinition(World, "Radsuit"), null);

        GameActions.SetEntityToLine(World, Player, 417, 64);
        GameActions.TickWorld(World, () => { return !exited; }, () => { });
        Player.IsDead.Should().BeTrue();
        exited.Should().BeTrue();

        Player.Inventory.Remove("RadSuit", 1);
        World.LevelExit -= World_LevelExit;

        void World_LevelExit(object? sender, LevelChangeEvent e)
        {
            e.Cancel = true;
            exited = true;
            e.ChangeType.Should().Be(LevelChangeType.Next);
        }
    }

    [Fact(DisplayName = "Kill player and secret exit")]
    public void KillPlayerAndSecretExit()
    {
        bool exited = false;
        World.LevelExit += World_LevelExit;
        var monster = GameActions.GetEntity(World, 17);
        GameActions.TickWorld(World, 1);
        monster.IsDead.Should().BeFalse();

        Player.GiveItem(GameActions.GetEntityDefinition(World, "Radsuit"), null);

        GameActions.SetEntityToLine(World, Player, 420, 64);
        GameActions.TickWorld(World, () => { return !exited; }, () => { });
        Player.IsDead.Should().BeTrue();
        exited.Should().BeTrue();

        Player.Inventory.Remove("RadSuit", 1);
        World.LevelExit -= World_LevelExit;

        void World_LevelExit(object? sender, LevelChangeEvent e)
        {
            e.Cancel = true;
            exited = true;
            e.ChangeType.Should().Be(LevelChangeType.SecretNext);
        }
    }

    [Fact(DisplayName = "Kill grounded monsters")]
    public void KillGroundedMonsters()
    {
        var monsters = GameActions.GetSectorEntities(World, 81);
        GameActions.TickWorld(World, 1);
        foreach (var monster in monsters)
            monster.IsDead.Should().BeTrue();

        var lostSoul = GameActions.CreateEntity(World, "LostSoul", (704, 1472, 128));
        GameActions.TickWorld(World, 1);
        lostSoul.IsDead.Should().BeFalse();

        GameActions.SetEntityToLine(World, Player, 424, 64);
        GameActions.TickWorld(World, 1);
        Player.IsDead.Should().BeFalse();
    }
}
