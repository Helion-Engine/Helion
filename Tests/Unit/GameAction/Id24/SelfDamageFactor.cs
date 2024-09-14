using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Id24;

[Collection("GameActions")]
public class SelfDamageFactor
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;
    private readonly Vec3D ItemPos = new(0, 0, 0);

    public SelfDamageFactor()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (World) => { }, IWadType.Doom2,
            dehackedPatch: Dehacked, cacheWorld: false);
    }

    [Fact(DisplayName = "Damage self with self default damage factor")]
    public void DamageSelfWithDefaultSelfDamageFactor()
    {
        Player.Health = 100;
        GameActions.SetEntityPosition(World, Player, ItemPos);
        var rocket = GameActions.CreateEntity(World, "Rocket", ItemPos);
        rocket.SetOwner(Player);
        rocket.Properties.SelfDamageFactor.Should().Be(1);
        // Will deal max 128 damage, self damage factor reduces to 64
        EntityActionFunctions.A_Explode(rocket);
        Player.Health.Should().Be(-28);
    }

    [Fact(DisplayName = "Damage self with self damage factor")]
    public void DamageSelfWithSelfDamageFactor()
    {
        Player.Health = 100;
        GameActions.SetEntityPosition(World, Player, ItemPos);
        var rocket = GameActions.CreateEntity(World, "*deh/entity42068", ItemPos);
        rocket.SetOwner(Player);
        rocket.Properties.SelfDamageFactor.Should().Be(0.5);
        // Will deal max 128 damage, self damage factor reduces to 64
        EntityActionFunctions.A_Explode(rocket);
        Player.Health.Should().Be(36);
    }

    [Fact(DisplayName = "Damage other with self damage factor")]
    public void DamageOtherWithSelfDamageFactor()
    {
        Player.Health = 100;
        GameActions.SetEntityPosition(World, Player, ItemPos);
        var rocket = GameActions.CreateEntity(World, "*deh/entity42068", ItemPos);
        rocket.SetOwner(rocket);
        rocket.Properties.SelfDamageFactor.Should().Be(0.5);
        EntityActionFunctions.A_Explode(rocket);
        Player.Health.Should().Be(-28);
    }

    private static readonly string Dehacked =
@"Thing 42069 (Rocket)
Speed = 524288
Speed = 983040
Width = 2097152
Height = 2097152
Self damage factor = 32768
";
}