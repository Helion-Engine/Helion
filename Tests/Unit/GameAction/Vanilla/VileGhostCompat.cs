using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class VileGhostCompat
{
    private static readonly string ResourceZip = "Resources/vileghost.zip";
    private static readonly string MapName = "MAP01";

    private readonly SinglePlayerWorld World;

    private Player Player => World.Player;

    public VileGhostCompat()
    {
        World = WorldAllocator.LoadMap(ResourceZip, "vileghost.wad", MapName, GetType().Name,
            (world) => { world.CheatManager.ActivateCheat(world.Player, CheatType.God); }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Monster is resurrected as ghost")]
    public void VileGhostResurrect()
    {
        World.Config.Compatibility.VileGhosts.Set(true);
        var imp = CrushAndRaiseImp();

        imp.Flags.Solid.Should().BeFalse();
        imp.Flags.Shootable.Should().BeTrue();
        imp.Radius.Should().Be(0);
        imp.Height.Should().Be(0);

        GameActions.SetEntityPosition(World, imp, (-352, -352));
        // One sided line shouldn't block
        World.PhysicsManager.TryMoveXY(imp, -352, -416).Success.Should().BeTrue();
    }

    [Fact(DisplayName = "Monster resurrected as ghost takes splash damage")]
    public void VileGhostDamage()
    {
        World.Config.Compatibility.VileGhosts.Set(true);
        var imp = CrushAndRaiseImp();
        imp.IsDead.Should().BeFalse();
        imp.Radius.Should().Be(0);
        var rocket = GameActions.CreateEntity(World, "Rocket", imp.Position);
        World.RadiusExplosion(rocket, rocket, 128, 128);
        imp.IsDead.Should().BeTrue();
    }

    [Fact(DisplayName = "Monster is resurrected normally")]
    public void VileNormalResurrect()
    {
        World.Config.Compatibility.VileGhosts.Value.Should().BeFalse();
        var imp = CrushAndRaiseImp();

        imp.Flags.Solid.Should().BeTrue();
        imp.Flags.Shootable.Should().BeTrue();
        imp.Radius.Should().Be(20);
        imp.Height.Should().Be(56);

        GameActions.SetEntityPosition(World, imp, (-352, -352));
        // One sided should block
        World.PhysicsManager.TryMoveXY(imp, -352, -416).Success.Should().BeFalse();
    }

    [Fact(DisplayName = "Monster normal resurrection with vile ghosts on")]
    public void VileNormalResurrectWithVileGhost()
    {
        World.Config.Compatibility.VileGhosts.Set(true);
        var imp = KillAndRaiseImp();

        imp.Flags.Solid.Should().BeTrue();
        imp.Flags.Shootable.Should().BeTrue();
        imp.Radius.Should().Be(20);
        imp.Height.Should().Be(56);

        GameActions.SetEntityPosition(World, imp, (-352, -352));
        // One sided should block
        World.PhysicsManager.TryMoveXY(imp, -352, -416).Success.Should().BeFalse();
    }


    private Entity CrushAndRaiseImp()
    {
        var crushSector = GameActions.GetSectorByTag(World, 1);
        var imp = GameActions.GetEntity(World, "DoomImp");
        GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, () => { return crushSector.Ceiling.Z > 8; }, () => { });
        GameActions.TickWorld(World, () => { return crushSector.Ceiling.Z < 100; }, () => { });
        GameActions.ActivateLine(World, Player, 5, ActivationContext.UseLine).Should().BeTrue();
        imp.IsDead.Should().BeTrue();
        imp.Flags.CrushGiblets.Should().BeTrue();

        GameActions.SetEntityPosition(World, Player, (-192, -448));
        World.NoiseAlert(Player, Player);
        GameActions.TickWorld(World, () => { return imp.IsDead; }, () => { });
        imp.Flags.CrushGiblets.Should().BeFalse();
        return imp;
    }

    private Entity KillAndRaiseImp()
    {
        var imp = GameActions.GetEntity(World, "DoomImp");
        imp.Kill(null);
        imp.IsDead.Should().BeTrue();

        GameActions.SetEntityPosition(World, Player, (-192, -448));
        World.NoiseAlert(Player, Player);
        GameActions.TickWorld(World, () => { return imp.IsDead; }, () => { });
        return imp;
    }
}