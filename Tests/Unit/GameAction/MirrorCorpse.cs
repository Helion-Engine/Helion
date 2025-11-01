using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Util.RandomGenerators;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class MirrorCorpse
{
    private readonly NoRandom Random = new();
    private readonly SinglePlayerWorld World;

    public MirrorCorpse()
    {
        World = WorldAllocator.LoadMap("Resources/shoot.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        GameActions.DestroyCreatedEntities(World);
        World.SetSecondaryRandom(Random);
        World.Config.Game.MirrorCorpse.Set(true);
    }

    [Fact(DisplayName = "Mirror corspe set on death when random is 1")]
    public void SetMirrorCorpseDeath()
    {
        Random.RandomValue = 1;
        var entity = GameActions.CreateEntity(World, "Zombieman", default);
        entity.Flags.DontMirrorCorpse().Should().BeFalse();
        entity.Flags.Mirror().Should().BeFalse();

        entity.Kill(null);
        entity.Flags.Mirror().Should().BeTrue();
    }

    [Fact(DisplayName = "Mirror corspe not set on death when random is 0")]
    public void DontSetMirrorCorpseDeath()
    {
        Random.RandomValue = 0;
        var entity = GameActions.CreateEntity(World, "Zombieman", default);
        entity.Flags.DontMirrorCorpse().Should().BeFalse();
        entity.Flags.Mirror().Should().BeFalse();

        entity.Kill(null);
        entity.Flags.Mirror().Should().BeFalse();
    }

    [Fact(DisplayName = "Mirror corspe set on spawn with +CORPSE flag")]
    public void SetMirrorCorpseOnSpawn()
    {
        Random.RandomValue = 1;
        var entity = GameActions.CreateEntity(World, "DeadMarine", default);
        entity.Flags.DontMirrorCorpse().Should().BeFalse();
        entity.Flags.Mirror().Should().BeTrue();
    }

    [Fact(DisplayName = "Mirror corspe set on spawn with bullet puff")]
    public void SetMirrorCorpseOnSpawnBulletPuff()
    {
        Random.RandomValue = 1;
        var entity = GameActions.CreateEntity(World, "BulletPuff", default);
        entity.Flags.DontMirrorCorpse().Should().BeFalse();
        entity.Flags.Mirror().Should().BeTrue();
    }

    [Fact(DisplayName = "Mirror corspe not set on spawn with bullet puff when random is 0")]
    public void DontSetMirrorCorpseOnSpawnBulletPuff()
    {
        Random.RandomValue = 0;
        var entity = GameActions.CreateEntity(World, "BulletPuff", default);
        entity.Flags.DontMirrorCorpse().Should().BeFalse();
        entity.Flags.Mirror().Should().BeFalse();
    }

    [Fact(DisplayName = "Mirror corspe set on spawn with blood")]
    public void SetMirrorCorpseOnSpawnBlood()
    {
        Random.RandomValue = 1;
        var entity = GameActions.CreateEntity(World, "Blood", default);
        entity.Flags.DontMirrorCorpse().Should().BeFalse();
        entity.Flags.Mirror().Should().BeTrue();
    }

    [Fact(DisplayName = "DontMirrorCorpse set")]
    public void DontMirrorCorpseSet()
    {
        var entity = GameActions.CreateEntity(World, "Cyberdemon", default);
        entity.Flags.DontMirrorCorpse().Should().BeTrue();

        entity = GameActions.CreateEntity(World, "ChaingunGuy", default);
        entity.Flags.DontMirrorCorpse().Should().BeTrue();
    }
}
