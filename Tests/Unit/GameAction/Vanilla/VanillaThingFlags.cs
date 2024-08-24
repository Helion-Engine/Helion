using FluentAssertions;
using Helion.Maps.Shared;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Vanilla;

[Collection("GameActions")]
public class VanillaThingFlags
{
    const string ZombieMan = "ZombieMan"; // easy
    const string ShotgunGuy = "ShotgunGuy"; // medium
    const string Imp = "DoomImp"; // hard
    const string Spectre = "Spectre"; // easy, medium, hard
    const string Shotgun = "Shotgun"; // multiplayer

    const string HellKnight = "HellKnight"; // no ambush
    const string Baron = "BaronOfHell"; // ambush

    private SinglePlayerWorld GetWorld(SkillLevel skillLevel) =>
        WorldAllocator.LoadMap("Resources/vanillathingflags.zip", "vanillathingflags.WAD", "MAP01", GetType().Name,
            (world) => { }, IWadType.Doom2, skillLevel, cacheWorld: false);

    [Fact(DisplayName = "Skill easy")]
    public void SkillEasy()
    {
        var world = GetWorld(SkillLevel.Easy);
        GameActions.FindEntity(world, ZombieMan).Should().NotBeNull();
        GameActions.FindEntity(world, ShotgunGuy).Should().BeNull();
        GameActions.FindEntity(world, Imp).Should().BeNull();
        GameActions.FindEntity(world, Spectre).Should().NotBeNull();
        GameActions.FindEntity(world, Shotgun).Should().BeNull();
    }

    [Fact(DisplayName = "Skill medium")]
    public void SkillMedium()
    {
        var world = GetWorld(SkillLevel.Medium);
        GameActions.FindEntity(world, ZombieMan).Should().BeNull();
        GameActions.FindEntity(world, ShotgunGuy).Should().NotBeNull();
        GameActions.FindEntity(world, Imp).Should().BeNull();
        GameActions.FindEntity(world, Spectre).Should().NotBeNull();
        GameActions.FindEntity(world, Shotgun).Should().BeNull();
    }

    [Fact(DisplayName = "Skill hard")]
    public void SkillHard()
    {
        var world = GetWorld(SkillLevel.Hard);
        GameActions.FindEntity(world, ZombieMan).Should().BeNull();
        GameActions.FindEntity(world, ShotgunGuy).Should().BeNull();
        GameActions.FindEntity(world, Imp).Should().NotBeNull();
        GameActions.FindEntity(world, Spectre).Should().NotBeNull();
        GameActions.FindEntity(world, Shotgun).Should().BeNull();
    }

    [Fact(DisplayName = "Ambush")]
    public void Ambush()
    {
        var world = GetWorld(SkillLevel.Hard);
        var hellKnight = GameActions.GetEntity(world, HellKnight);
        var baron = GameActions.GetEntity(world, Baron);

        hellKnight.Flags.Ambush.Should().BeFalse();
        baron.Flags.Ambush.Should().BeTrue();
    }

    [Fact(DisplayName = "Ambush noise alert and line of sight")]
    public void AmbushLineOfSight()
    {
        var world = GetWorld(SkillLevel.Hard);
        var hellKnight = GameActions.GetEntity(world, HellKnight);
        var baron = GameActions.GetEntity(world, Baron);

        hellKnight.Target.Entity.Should().BeNull();
        baron.Target.Entity.Should().BeNull();

        GameActions.SetEntityPosition(world, world.Player, (-1376, -256, 0));
        GameActions.PlayerFirePistol(world, world.Player);

        // The ambush baron doesn't have line of sight to the player and will not wake up
        // Hellknight without ambush will wake up
        hellKnight.Target.Entity.Should().Be(world.Player);
        baron.Target.Entity.Should().BeNull();

        GameActions.SetEntityPosition(world, world.Player, (-832, -320, 0));
        GameActions.TickWorld(world, 12);
        // Line of sight checks are all around and not just in FOV
        baron.Target.Entity.Should().Be(world.Player);
    }
}
