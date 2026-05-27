using FluentAssertions;
using Helion.World;
using System;

namespace Helion.Tests.Unit.GameAction;

public static partial class GameActions
{
    public static void AssertMusicChange(WorldBase world, string music, MusicFlags flags, Action action)
    {
        MusicChangeEvent? musicChangeEvent = null;
        bool changed = false;
        world.OnMusicChanged += World_OnMusicChanged;
        action();

        TickWorld(world, () => { return !changed; }, () => { });
        musicChangeEvent.Should().NotBeNull();
        musicChangeEvent.Value.Entry.Path.Name.Should().Be(music);
        musicChangeEvent.Value.MusicFlags.Should().Be(flags);

        void World_OnMusicChanged(object? sender, MusicChangeEvent e)
        {
            musicChangeEvent = e;
            changed = true;
        }
    }

    public static void AssertNoMusicChange(WorldBase world, Action action)
    {
        MusicChangeEvent? musicChangeEvent = null;
        bool changed = false;
        world.OnMusicChanged += World_OnMusicChanged;
        action();

        TickWorld(world, 10);
        changed.Should().BeFalse();
        musicChangeEvent.Should().BeNull();

        void World_OnMusicChanged(object? sender, MusicChangeEvent e)
        {
            musicChangeEvent = e;
            changed = true;
        }
    }
}
