using FluentAssertions;
using Helion.Audio.Impl;
using Helion.Util.Extensions;
using Helion.World;
using System.Linq;

namespace Helion.Tests.Unit.GameAction;

public partial class GameActions
{
    public static void AssertSoundInfo(WorldBase world, object soundSource, string soundInfoName)
    {
        var sound = world.SoundManager.FindBySource(soundSource);
        sound.Should().NotBeNull();
        sound.AudioData.SoundInfo.Name.EqualsIgnoreCase(soundInfoName);
    }

    public static void AssertNoSoundInfo(WorldBase world, string soundInfoName)
    {
        world.SoundManager.GetPlayingSounds().Any(x => x.AudioData.SoundInfo.Name.EqualsIgnoreCase(soundInfoName)).Should().BeFalse();
        world.SoundManager.GetSoundsToPlay().Any(x => x.AudioData.SoundInfo.Name.EqualsIgnoreCase(soundInfoName)).Should().BeFalse();
    }

    public static void AssertAnySound(WorldBase world, object soundSource) => AssertSound(world, soundSource, null);

    public static void AssertSound(WorldBase world, object soundSource, string? entrySoundName)
    {
        var sound = world.SoundManager.FindBySource(soundSource);
        sound.Should().NotBeNull();
        if (entrySoundName != null)
            sound!.AudioData.SoundInfo.EntryName.EqualsIgnoreCase(entrySoundName).Should().BeTrue();

        ((MockAudioSourceManager)world.SoundManager.AudioManager).CreateSound = false;
        TickWorld(world, () => { return world.SoundManager.FindBySource(soundSource) != null; }, () => { });
        ((MockAudioSourceManager)world.SoundManager.AudioManager).CreateSound = true;
    }

    public static void AssertNoSound(WorldBase world, object soundSource)
    {
        var sound = world.SoundManager.FindBySource(soundSource);
        sound.Should().BeNull();
    }
}
