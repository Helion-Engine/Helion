using FluentAssertions;
using Helion.Audio;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.Util.RandomGenerators;
using Helion.World.Entities;
using Xunit;

namespace Helion.Tests.Unit.GameAction.SoundInfo;

[Collection("GameActions")]
public class AmbientSounds
{
    [Fact(DisplayName = "Point Sound")]
    public void PointSound()
    {
        var world = WorldAllocator.LoadMap("Resources/ambientsound.zip", "ambientsound.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        var ambientSounds = GameActions.GetEntities(world, Constants.AmbientSound);
        ambientSounds.Count.Should().Be(1);

        var entity = (AmbientSound)ambientSounds[0];
        entity.AmbientSoundInfo.Should().NotBeNull();

        var ambientSound = entity.AmbientSoundInfo!;
        ambientSound.LogicalSound.Should().Be("counting");
        ambientSound.Attenuation.Should().Be(2f);
        ambientSound.Type.Should().Be(AmbientSoundType.Point);
        ambientSound.Mode.Should().Be(AmbientSoundMode.Continuous);
        ambientSound.Volume.Should().Be(0.5f);

        world.Tick();

        var sounds = world.SoundManager.GetPlayingSounds();
        sounds.Count.Should().Be(1);

        var sound = sounds.First!.Value;
        sound.AudioDataRef().Loop.Should().BeTrue();
        sound.AudioDataRef().Volume.Should().Be(0.5f);
        sound.AudioDataRef().AttenuationFactor.Should().Be(2f);
        sound.AudioDataRef().Attenuation.Should().Be(Attenuation.Default);

        GameActions.TickWorld(world, 35);
        sounds = world.SoundManager.GetPlayingSounds();
        sounds.Count.Should().Be(1);
    }

    [Fact(DisplayName = "World Sound")]
    public void VolumeSound()
    {
        var world = WorldAllocator.LoadMap("Resources/ambientsound.zip", "ambientsound.wad", "MAP04", GetType().Name, (world) => { }, IWadType.Doom2);
        var ambientSounds = GameActions.GetEntities(world, Constants.AmbientSound);
        ambientSounds.Count.Should().Be(1);

        var entity = (AmbientSound)ambientSounds[0];
        entity.AmbientSoundInfo.Should().NotBeNull();

        var ambientSound = entity.AmbientSoundInfo!;
        ambientSound.LogicalSound.Should().Be("ss2_loop");
        ambientSound.Attenuation.Should().Be(1);
        ambientSound.Type.Should().Be(AmbientSoundType.World);
        ambientSound.Mode.Should().Be(AmbientSoundMode.Continuous);
        ambientSound.Volume.Should().Be(1f);

        world.Tick();

        var sounds = world.SoundManager.GetPlayingSounds();
        sounds.Count.Should().Be(1);

        var sound = sounds.First!.Value;
        sound.AudioDataRef().Loop.Should().BeTrue();
        sound.AudioDataRef().Volume.Should().Be(1f);
        sound.AudioDataRef().AttenuationFactor.Should().Be(1f);
        sound.AudioDataRef().Attenuation.Should().Be(Attenuation.None);

        GameActions.TickWorld(world, 35);
        sounds = world.SoundManager.GetPlayingSounds();
        sounds.Count.Should().Be(1);
    }

    [Fact(DisplayName = "Random Sound")]
    public void RandomSound()
    {
        var world = WorldAllocator.LoadMap("Resources/ambientsound.zip", "ambientsound.wad", "MAP05", GetType().Name, (world) => { }, IWadType.Doom2);
        var ambientSounds = GameActions.GetEntities(world, Constants.AmbientSound);
        ambientSounds.Count.Should().Be(1);

        var random = new NoRandom();
        world.SetRandom(random);

        var entity = (AmbientSound)ambientSounds[0];
        entity.AmbientSoundInfo.Should().NotBeNull();

        var ambientSound = entity.AmbientSoundInfo!;
        ambientSound.LogicalSound.Should().Be("owl");
        ambientSound.Attenuation.Should().Be(1);
        ambientSound.Type.Should().Be(AmbientSoundType.Point);
        ambientSound.Mode.Should().Be(AmbientSoundMode.Random);
        ambientSound.Volume.Should().Be(1f);
        ambientSound.MinTicks.Should().Be(35);
        ambientSound.MaxTicks.Should().Be(175);

        // Sets the minimum and should play after 35 ticks
        random.RandomValue = 0;
        world.Tick();

        // Sets the minimum and should play after 175 ticks
        random.RandomValue = 255;

        var sounds = world.SoundManager.GetPlayingSounds();
        sounds.Count.Should().Be(0);

        GameActions.TickWorld(world, 35);

        sounds = world.SoundManager.GetPlayingSounds();
        sounds.Count.Should().Be(1);

        var sound = sounds.First!.Value;
        sound.AudioDataRef().Loop.Should().BeFalse();
        sound.AudioDataRef().Volume.Should().Be(1f);
        sound.AudioDataRef().AttenuationFactor.Should().Be(1f);
        sound.AudioDataRef().Attenuation.Should().Be(Attenuation.Default);

        GameActions.TickWorld(world, () => { return world.SoundManager.GetPlayingSounds().Count > 0; }, () => { });

        GameActions.TickWorld(world, 40);
        sounds = world.SoundManager.GetPlayingSounds();
        sounds.Count.Should().Be(0);

        GameActions.TickWorld(world, 175 - 40);
        sounds = world.SoundManager.GetPlayingSounds();
        sounds.Count.Should().Be(1);
    }

    [Fact(DisplayName = "Periodic Sound")]
    public void PeriodicSound()
    {
        var world = WorldAllocator.LoadMap("Resources/ambientsound.zip", "ambientsound.wad", "MAP06", GetType().Name, (world) => { }, IWadType.Doom2);
        var ambientSounds = GameActions.GetEntities(world, Constants.AmbientSound);

        var entity = (AmbientSound)ambientSounds[0];
        entity.AmbientSoundInfo.Should().NotBeNull();

        var ambientSound = entity.AmbientSoundInfo!;
        ambientSound.LogicalSound.Should().Be("bell_ring");
        ambientSound.Attenuation.Should().Be(1);
        ambientSound.Type.Should().Be(AmbientSoundType.Point);
        ambientSound.Mode.Should().Be(AmbientSoundMode.Periodic);
        ambientSound.Volume.Should().Be(1f);
        ambientSound.MinTicks.Should().Be(105);
        ambientSound.MaxTicks.Should().Be(0);

        world.Tick();
        GameActions.TickWorld(world, () => { return world.SoundManager.GetPlayingSounds().Count > 0; }, () => { });

        for (int i = 0; i < 3; i++)
        {
            GameActions.TickWorld(world, 105);
            var sounds = world.SoundManager.GetPlayingSounds();
            sounds.Count.Should().Be(1);
            GameActions.TickWorld(world, () => { return world.SoundManager.GetPlayingSounds().Count > 0; }, () => { });
            world.Tick();
        }
    }
}

