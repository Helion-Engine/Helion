using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction.Util;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class SoloNet
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public SoloNet()
    {
        var config = WorldAllocator.CreateConfig();
        config.Game.SoloNet.Set(true);
        World = WorldAllocator.LoadMap("Resources/solonet.zip", "solonet.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, 
            cacheWorld: false,  config: config);
    }

    [Fact(DisplayName = "Solo-net player spawn")]
    public void PlayerSpawn()
    {
        World.Config.Game.SoloNet.Value.Should().BeTrue();
        World.EntityManager.Players.Count.Should().Be(1);
        var player = World.Player;
        player.Position.Should().Be(new Vec3D(64, -96, 0));
    }


    [Fact(DisplayName = "Solo-net player respawn")]
    public void PlayerRespawn()
    {
        Player.PlayerStats.DeathCount.Should().Be(0);
        var imp = GameActions.GetEntity(World, "DoomImp");
        imp.Kill(null);
        imp.IsDead().Should().BeTrue();
        GameActions.TickWorld(World, 35);

        World.EntityManager.Players.Count.Should().Be(1);
        var originalPlayer = Player;
        World.PerformItemPickup(originalPlayer, GameActions.FindEntity(World, "RedCard")!);
        originalPlayer.Inventory.HasItem("RedCard").Should().BeTrue();
        originalPlayer.Kill(null);
        originalPlayer.IsDead().Should().BeTrue();
        originalPlayer.TickCommand.Add(TickCommands.Use);

        GameActions.TickWorld(World, 1);
        originalPlayer.PlayerState.Should().Be(PlayerState.Ignore);
        Player.PlayerState.Should().Be(PlayerState.Normal);
        ReferenceEquals(Player, originalPlayer).Should().BeFalse();
        ReferenceEquals(Player, World.EntityManager.GetRealPlayer(0)).Should().BeTrue();
        GameActions.FindEntity(World, "TeleportFog").Should().NotBeNull();
        Player.Health.Should().Be(100);
        Player.Inventory.HasItem("RedCard").Should().BeFalse();
        imp.IsDead().Should().BeTrue();

        World.EntityManager.Players.Count.Should().Be(1);
        Player.PlayerStats.DeathCount.Should().Be(1);
    }

    [Fact(DisplayName = "Solo-net player pickup weapon stay")]
    public void PlayerPickupWeaponStay()
    {
        World.PerformItemPickup(Player, GameActions.FindEntity(World, "Chaingun")!);
        World.PerformItemPickup(Player, GameActions.FindEntity(World, "SuperShotgun")!);
        InventoryUtil.AssertHasWeapon(Player, "Chaingun");
        InventoryUtil.AssertHasWeapon(Player, "SuperShotgun");
        GameActions.FindEntity(World, "Chaingun").Should().NotBeNull();
        GameActions.FindEntity(World, "SuperShotgun").Should().NotBeNull();
        InventoryUtil.AssertAmount(Player, "Clip", 70);
        // Chaingun stays but can't pickup twice
        World.PerformItemPickup(Player, GameActions.FindEntity(World, "Chaingun")!);
        InventoryUtil.AssertHasWeapon(Player, "Chaingun");
        InventoryUtil.AssertAmount(Player, "Clip", 70);
    }

    [Fact(DisplayName = "Solo-net player pickup key stay")]
    public void PlayerPickupKeyStay()
    {
        World.PerformItemPickup(Player, GameActions.FindEntity(World, "RedCard")!);
        World.PerformItemPickup(Player, GameActions.FindEntity(World, "BlueSkull")!);
        InventoryUtil.AssertAmount(Player, "RedCard", 1);
        InventoryUtil.AssertAmount(Player, "BlueSkull", 1);
        GameActions.FindEntity(World, "RedCard").Should().NotBeNull();
        GameActions.FindEntity(World, "BlueSkull").Should().NotBeNull();
        // Keys stay but can't pickup twice
        World.PerformItemPickup(Player, GameActions.FindEntity(World, "RedCard")!);
        World.PerformItemPickup(Player, GameActions.FindEntity(World, "BlueSkull")!);
        GameActions.FindEntity(World, "RedCard").Should().NotBeNull();
        GameActions.FindEntity(World, "BlueSkull").Should().NotBeNull();
    }


    [Fact(DisplayName = "Solo-net player pickup items remove")]
    public void PlayerPickupItemRemove()
    {
        World.PerformItemPickup(Player, GameActions.FindEntity(World, "GreenArmor")!);
        World.PerformItemPickup(Player, GameActions.FindEntity(World, "HealthBonus")!);
        Player.Armor.Should().Be(100);
        Player.Health.Should().Be(101);
        GameActions.FindEntity(World, "GreenArmor").Should().BeNull();
        GameActions.FindEntity(World, "HealthBonus").Should().BeNull();
    }

    [Fact(DisplayName = "Solo-net player pickup dropped weapon remove")]
    public void PlayerPickupDroppedWeaponRemove()
    {
        var shotgunGuy = GameActions.GetEntity(World, "ShotgunGuy");
        shotgunGuy.Kill(null);
        GameActions.TickWorld(World, 35);
        var shotgun = GameActions.GetEntity(World, "Shotgun");
        shotgun.Flags.Dropped().Should().BeTrue();
        World.PerformItemPickup(Player, shotgun);
        InventoryUtil.AssertHasWeapon(Player, "Shotgun");
        GameActions.FindEntity(World, "Shotgun").Should().BeNull();
    }

    [Fact(DisplayName = "Solo-net object spawn")]
    public void SpawnObjectsSoloNet()
    {
        GameActions.FindEntity(World, "Chaingun").Should().NotBeNull();
        GameActions.FindEntity(World, "SuperShotgun").Should().NotBeNull();
        GameActions.FindEntity(World, "GreenArmor").Should().NotBeNull();
        GameActions.FindEntity(World, "HealthBonus").Should().NotBeNull();
        GameActions.FindEntity(World, "DoomImp").Should().NotBeNull();
        GameActions.FindEntity(World, "ZombieMan").Should().BeNull();
        GameActions.FindEntity(World, "ShotgunGuy").Should().NotBeNull();
        GameActions.FindEntity(World, "RedCard").Should().NotBeNull();
        GameActions.FindEntity(World, "BlueSkull").Should().NotBeNull();
        GameActions.FindEntity(World, "HellKnight").Should().BeNull();
    }
}

[Collection("GameActions")]
public class SpawnMap
{
    private readonly SinglePlayerWorld World;

    public SpawnMap()
    {
        var config = WorldAllocator.CreateConfig();
        World = WorldAllocator.LoadMap("Resources/solonet.zip", "solonet.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2,
            cacheWorld: false, config: config);
    }

    [Fact(DisplayName = "Map spawn flags")]
    public void SpawnObjects()
    {
        GameActions.FindEntity(World, "Chaingun").Should().NotBeNull();
        GameActions.FindEntity(World, "SuperShotgun").Should().NotBeNull();
        GameActions.FindEntity(World, "GreenArmor").Should().NotBeNull();
        GameActions.FindEntity(World, "HealthBonus").Should().NotBeNull();
        GameActions.FindEntity(World, "DoomImp").Should().BeNull();
        GameActions.FindEntity(World, "ZombieMan").Should().BeNull();
        GameActions.FindEntity(World, "ShotgunGuy").Should().BeNull();
        GameActions.FindEntity(World, "RedCard").Should().NotBeNull();
        GameActions.FindEntity(World, "BlueSkull").Should().NotBeNull();
        GameActions.FindEntity(World, "HellKnight").Should().NotBeNull();
    }
}

[Collection("GameActions")]
public class SkillDefSpawnMulti
{
    private SinglePlayerWorld World = null!;
    private SpawnMulti m_set;

    public SkillDefSpawnMulti()
    {
        World = LoadWorld();
    }

    private SinglePlayerWorld LoadWorld()
    {
        var config = WorldAllocator.CreateConfig();
        return WorldAllocator.LoadMap("Resources/solonet.zip", "solonet.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2,
            cacheWorld: false, config: config, onBeforeInit: OnBeforeInit);
    }

    private void OnBeforeInit(ArchiveCollection archiveCollection)
    {
        foreach (var skill in archiveCollection.MapInfo.MapInfo.Skills)
            skill.SpawnMulti = m_set;
    }

    [Fact(DisplayName = "Skill Definition SpawnMulti")]
    public void SpawnObjects()
    {
        m_set = SpawnMulti.None;
        World = LoadWorld();
        GameActions.FindEntity(World, "Chaingun").Should().NotBeNull();
        GameActions.FindEntity(World, "SuperShotgun").Should().NotBeNull();
        GameActions.FindEntity(World, "GreenArmor").Should().NotBeNull();
        GameActions.FindEntity(World, "HealthBonus").Should().NotBeNull();
        GameActions.FindEntity(World, "DoomImp").Should().BeNull();
        GameActions.FindEntity(World, "ZombieMan").Should().BeNull();
        GameActions.FindEntity(World, "ShotgunGuy").Should().BeNull();
        GameActions.FindEntity(World, "RedCard").Should().NotBeNull();
        GameActions.FindEntity(World, "BlueSkull").Should().NotBeNull();
        GameActions.FindEntity(World, "HellKnight").Should().NotBeNull();

        m_set = SpawnMulti.SinglePlayerAndCoop;
        World = LoadWorld();
        GameActions.FindEntity(World, "Chaingun").Should().NotBeNull();
        GameActions.FindEntity(World, "SuperShotgun").Should().NotBeNull();
        GameActions.FindEntity(World, "GreenArmor").Should().NotBeNull();
        GameActions.FindEntity(World, "HealthBonus").Should().NotBeNull();
        GameActions.FindEntity(World, "DoomImp").Should().NotBeNull();
        GameActions.FindEntity(World, "ZombieMan").Should().BeNull();
        GameActions.FindEntity(World, "ShotgunGuy").Should().NotBeNull();
        GameActions.FindEntity(World, "RedCard").Should().NotBeNull();
        GameActions.FindEntity(World, "BlueSkull").Should().NotBeNull();
        GameActions.FindEntity(World, "HellKnight").Should().NotBeNull();

        m_set = SpawnMulti.CoopOnly;
        World = LoadWorld();
        GameActions.FindEntity(World, "Chaingun").Should().NotBeNull();
        GameActions.FindEntity(World, "SuperShotgun").Should().NotBeNull();
        GameActions.FindEntity(World, "GreenArmor").Should().NotBeNull();
        GameActions.FindEntity(World, "HealthBonus").Should().NotBeNull();
        GameActions.FindEntity(World, "DoomImp").Should().NotBeNull();
        GameActions.FindEntity(World, "ZombieMan").Should().BeNull();
        GameActions.FindEntity(World, "ShotgunGuy").Should().NotBeNull();
        GameActions.FindEntity(World, "RedCard").Should().NotBeNull();
        GameActions.FindEntity(World, "BlueSkull").Should().NotBeNull();
        GameActions.FindEntity(World, "HellKnight").Should().BeNull();
    }
}