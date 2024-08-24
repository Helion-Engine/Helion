using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Helion.World.Sound;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class VanillaSectorSound
{
    private static readonly string ResourceZip = "Resources/vanillasectorsound.zip";
    private static readonly string MapName = "MAP01";

    private readonly SinglePlayerWorld World;

    private Player Player => World.Player;

    public VanillaSectorSound()
    {
        World = WorldAllocator.LoadMap(ResourceZip, "vanillasectorsound.WAD", MapName, GetType().Name,
            (world) => { world.Config.Compatibility.VanillaSectorSound.Set(true); }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Vanilla sector sound determined by bounding box")]
    public void VanillaSectorSoundSingleSector()
    {
        World.Config.Compatibility.VanillaSectorSound.Value.Should().BeTrue();
        var soundSector = GameActions.GetSectorByTag(World, 1);
        GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine);
        GameActions.TickWorld(World, 1);
        var sound = World.SoundManager.FindBySource(soundSector.Floor);
        sound.Should().NotBeNull();
        var pos = sound!.GetPosition();
        pos.X.Should().Be(-288);
        pos.Y.Should().Be(-288);
        pos.Z.Should().Be(0);

        Player.Position.Z = 64;
        GameActions.TickWorld(World, 1);
        sound = World.SoundManager.FindBySource(soundSector.Floor);
        sound.Should().NotBeNull();
        pos = sound!.GetPosition();
        pos.X.Should().Be(-288);
        pos.Y.Should().Be(-288);
        pos.Z.Should().Be(64);
    }

    [Fact(DisplayName = "Vanilla sector sound with unconnected sectors")]
    public void VanillaSectorSoundUnconnectedSectors()
    {
        World.Config.Compatibility.VanillaSectorSound.Value.Should().BeTrue();
        var soundSector = GameActions.GetSectorByTag(World, 2);
        GameActions.ActivateLine(World, Player, 10, ActivationContext.UseLine);
        GameActions.TickWorld(World, 1);
        var sound = World.SoundManager.FindBySource(soundSector.Floor);
        sound.Should().NotBeNull();
        var pos = sound!.GetPosition();
        pos.X.Should().Be(192);
        pos.Y.Should().Be(448);
        pos.Z.Should().Be(0);

        Player.Position.Z = 64;
        GameActions.TickWorld(World, 1);
        sound = World.SoundManager.FindBySource(soundSector.Floor);
        sound.Should().NotBeNull();
        pos = sound!.GetPosition();
        pos.X.Should().Be(192);
        pos.Y.Should().Be(448);
        pos.Z.Should().Be(64);
    }
}