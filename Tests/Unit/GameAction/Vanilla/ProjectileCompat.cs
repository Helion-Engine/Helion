using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction;
using Helion.Util;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class ProjectileCompat
{
    private static readonly string ResourceZip = "Resources/projectilecompat.zip";
    private static readonly string MapName = "MAP01";

    private readonly SinglePlayerWorld World;

    private Player Player => World.Player;

    public ProjectileCompat()
    {
        World = WorldAllocator.LoadMap(ResourceZip, "projectilecompat.wad", MapName, GetType().Name, 
            (world) => { world.CheatManager.ActivateCheat(world.Player, CheatType.God); }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "ArachnotronPlasma does not specials")]
    public void ArachnotronPlasmaDoesNotActivate()
    {
        Run("ArachnotronPlasma", false);
    }

    [Fact(DisplayName = "ArachnotronPlasma activates specials")]
    public void ArachnotronPlasmaActivate()
    {
        Run("ArachnotronPlasma", true);
    }

    [Fact(DisplayName = "RevenantTracer does not specials")]
    public void RevenantTracerDoesNotActivate()
    {
        Run("RevenantTracer", false);
    }

    [Fact(DisplayName = "RevenantTracer activates specials")]
    public void RevenantTracerActivate()
    {
        Run("RevenantTracer", true);
    }

    [Fact(DisplayName = "FatShot does not activate specials")]
    public void FatShotDoesNotActivate()
    {
        Run("FatShot", false);
    }

    [Fact(DisplayName = "FatShot activates specials")]
    public void FatShotTracerActivate()
    {
        Run("FatShot", true);
    }

    [Fact(DisplayName = "DoomImpBall does not specials")]
    public void DoomImpBallDoesNotActivate()
    {
        Run("DoomImpBall", false);
    }

    [Fact(DisplayName = "CacodemonBall does not activate specials")]
    public void CacodemonBallDoesNotActivate()
    {
        Run("CacodemonBall", false);
    }

    [Fact(DisplayName = "BaronBall does not activate specials")]
    public void BaronBallDoesNotActivate()
    {
        Run("BaronBall", false);
    }

    [Fact(DisplayName = "Rocket does not activate specials")]
    public void RocketDoesNotActivate()
    {
        Run("Rocket", false);
    }

    private void Run(string projectileName, bool shouldTrigger)
    {
        World.Config.Compatibility.Doom2ProjectileWalkTriggers.Set(shouldTrigger);
        var sector1 = GameActions.GetSectorByTag(World, 1);
        var sector2 = GameActions.GetSectorByTag(World, 2);
        var sector3 = GameActions.GetSectorByTag(World, 3);
        var sector4 = GameActions.GetSectorByTag(World, 4);
        var source = GameActions.GetEntity(World, 1);
        sector1.IsMoving.Should().BeFalse();
        sector2.IsMoving.Should().BeFalse();
        sector3.IsMoving.Should().BeFalse();
        FireProjectile(source, projectileName);
        sector1.IsMoving.Should().Be(shouldTrigger);
        sector2.IsMoving.Should().Be(shouldTrigger);
        sector3.IsMoving.Should().Be(shouldTrigger);
        // This special should never trigger
        sector4.IsMoving.Should().Be(false);
    }

    private void FireProjectile(Entity source, string projectileName)
    {
        var def = World.EntityManager.DefinitionComposer.GetByName(projectileName);
        def.Should().NotBeNull();
        var angle = source.Position.Angle(Player.Position);
        var projectile = World.FireProjectile(source, angle, source.PitchTo(source.Position, Player),
            Constants.EntityShootDistance, false, def!, out _);
        projectile.Should().NotBeNull();

        GameActions.TickWorld(World, () => projectile!.Position.Y > -320, () => { });
    }
}