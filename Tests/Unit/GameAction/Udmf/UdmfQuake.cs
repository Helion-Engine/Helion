using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util.RandomGenerators;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Helion.World.Special.Specials;
using System;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfQuake : IDisposable
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public UdmfQuake()
    {
        World = WorldAllocator.LoadMap("Resources/udmfquake.zip", "udmfquake.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        World.SetRandom(new NoRandom());
    }

    public void Dispose()
    {
        Player.Health = 100;
        Player.Velocity = Vec3D.Zero;
    }

    [Fact(DisplayName = "RadiusQuake focus on activator no damage")]
    public void RadiusQuakeFocusActivator()
    {
        GameActions.ActivateLine(World, Player, 5, ActivationContext.UseLine).Should().BeTrue();
        var quake = GetQuake();
        quake.Intensity.Should().Be(4);
        quake.TremorRadius.Should().Be(256);
        quake.DamageRadius.Should().Be(0);
        quake.Duration.Should().Be(70);
        quake.Location.Get().Should().Be(Player);
        quake.SoundSource.Should().NotBeNull();

        GameActions.TickWorld(World, 1);

        World.SpecialManager.GetQuakeIntensity(Player).Should().Be(new Vec3D(4, 4, 0));
        GameActions.AssertSoundInfo(World, quake.SoundSource, "world/quake");

        GameActions.TickWorld(World, 35);
        Player.Health.Should().Be(100);
        GetQuake().Duration.Should().Be(34);
        GameActions.TickWorld(World, 35);
        GetQuakes().Length.Should().Be(0);
    }

    [Fact(DisplayName = "RadiusQuake focus on tid with damage")]
    public void RadiusQuakeFocusTid()
    {
        var soul = GameActions.CreateEntity(World, "LostSoul", (0, 64, 0), frozen: false, tid: 1);
        soul.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.ActivateLine(World, Player, 11, ActivationContext.UseLine).Should().BeTrue();
        var quake = GetQuake();
        quake.Intensity.Should().Be(6);
        quake.TremorRadius.Should().Be(256);
        quake.DamageRadius.Should().Be(256);
        quake.Duration.Should().Be(350);
        quake.Location.Get().Should().Be(soul);
        quake.SoundSource.Should().NotBeNull();

        GameActions.AssertSoundInfo(World, quake.SoundSource, "world/quake");
        quake.SoundSource.Position.Should().Be(soul.Position);

        GameActions.MoveEntity(World, soul, 32);
        GameActions.TickWorld(World, 1);
        quake.SoundSource.Position.Should().Be(soul.Position);

        World.SpecialManager.GetQuakeIntensity(Player).Should().Be(new Vec3D(6, 6, 0));
        Player.Health.Should().Be(98);
        Player.Velocity.Should().Be(new Vec3D(11.4375, 0, 0));

        GameActions.TickWorld(World, 1);
        Player.Health.Should().Be(97);
        Player.Velocity.Should().Be(new Vec3D(16.365234375, 0, 0));

        GameActions.SetEntityPosition(World, soul, (0, 272));
        GameActions.TickWorld(World, 1);
        World.SpecialManager.GetQuakeIntensity(Player).Should().Be(Vec3D.Zero);
        Player.Health.Should().Be(97);

        World.EntityManager.Destroy(soul);
        GameActions.TickWorld(World, 1);
        GetQuakes().Length.Should().Be(0);
        GameActions.AssertNoSoundInfo(World, "world/quake");
        GameActions.TickWorld(World, 350);
    }

    [Fact(DisplayName = "Quake with different tremor and damage radius")]
    public void QuakeDifferentRadius()
    {
        var soul = GameActions.CreateEntity(World, "LostSoul", (0, 64, 0), frozen: false, tid: 1);
        GameActions.ActivateLine(World, Player, 13, ActivationContext.UseLine).Should().BeTrue();
        var quake = GetQuake();
        quake.Intensity.Should().Be(4);
        quake.TremorRadius.Should().Be(256);
        quake.DamageRadius.Should().Be(128);
        quake.Duration.Should().Be(70);
        quake.SoundSource.Should().NotBeNull();

        GameActions.TickWorld(World, 1);
        World.SpecialManager.GetQuakeIntensity(Player).Should().Be(new Vec3D(4, 4, 0));
        Player.Health.Should().Be(99);

        GameActions.SetEntityPosition(World, soul, (0, 129, 0));
        GameActions.TickWorld(World, 1);
        World.SpecialManager.GetQuakeIntensity(Player).Should().Be(new Vec3D(4, 4, 0));
        Player.Health.Should().Be(99);

        GameActions.SetEntityPosition(World, soul, (0, 257, 0));
        GameActions.TickWorld(World, 1);
        World.SpecialManager.GetQuakeIntensity(Player).Should().Be(new Vec3D(0, 0, 0));
        Player.Health.Should().Be(99);

        World.EntityManager.Destroy(soul);
        GameActions.TickWorld(World, 350);
    }

    [Fact(DisplayName = "Quake intensity based on highest quake intensity")]
    public void MultipleQuakes()
    {
        var soul = GameActions.CreateEntity(World, "LostSoul", (0, 64, 0), frozen: false, tid: 1);
        GameActions.ActivateLine(World, Player, 5, ActivationContext.UseLine).Should().BeTrue();
        GameActions.ActivateLine(World, Player, 11, ActivationContext.UseLine).Should().BeTrue();
        var quakes = GetQuakes();
        quakes.Length.Should().Be(2);
        quakes.Any(x => x.Intensity == 4).Should().BeTrue();
        quakes.Any(x => x.Intensity == 6).Should().BeTrue();
        GameActions.TickWorld(World, 1);
        World.SpecialManager.GetQuakeIntensity(Player).Should().Be(new Vec3D(6, 6, 0));
        World.EntityManager.Destroy(soul);
        GameActions.TickWorld(World, 1);
        World.SpecialManager.GetQuakeIntensity(Player).Should().Be(new Vec3D(4, 4, 0));
        GameActions.TickWorld(World, 350);
    }

    private QuakeSpecial GetQuake()
    {
        return GetQuakes().Single();
    }

    private QuakeSpecial[] GetQuakes()
    {
        return World.SpecialManager.GetSpecials().Where(x => x is QuakeSpecial).Cast<QuakeSpecial>().ToArray();
    }
}
