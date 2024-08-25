using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Id24;

[Collection("GameActions")]
public class Music
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    const string DefaultMusic = "D_RUNNIN";
    const string ChangeMusic1 = "D_STALKS";
    const string ChangeMusic2 = "D_COUNTD";

    public Music()
    {
        World = WorldAllocator.LoadMap("Resources/id24music.zip", "id24music.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
    }

    private static void WorldInit(IWorld world)
    {
        world.PlayLevelMusic(DefaultMusic, null);
    }

    [Fact(DisplayName = "2057 - W1 ChangeMusicAndLoop")]
    public void Action2057_ChangeMusicAndLoop()
    {
        GameActions.GetLine(World, 4).Flags.Repeat.Should().BeFalse();
        AssertMusicChange(ChangeMusic1, MusicFlags.Loop, () =>
        {
            GameActions.ActivateLine(World, Player, 4, ActivationContext.CrossLine).Should().BeTrue();
        });

        AssertMusicChange(ChangeMusic2, MusicFlags.Loop, () =>
        {
            GameActions.ActivateLine(World, Player, 4, ActivationContext.CrossLine, fromFront: false).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2058 - WR ChangeMusicAndLoop")]
    public void Action2058_ChangeMusicAndLoop()
    {
        GameActions.GetLine(World, 5).Flags.Repeat.Should().BeTrue();
        AssertMusicChange(ChangeMusic1, MusicFlags.Loop, () =>
        {
            GameActions.ActivateLine(World, Player, 5, ActivationContext.CrossLine).Should().BeTrue();
        });

        AssertMusicChange(ChangeMusic2, MusicFlags.Loop, () =>
        {
            GameActions.ActivateLine(World, Player, 5, ActivationContext.CrossLine, fromFront: false).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2059 - S1 ChangeMusicAndLoop")]
    public void Action2059_ChangeMusicAndLoop()
    {
        GameActions.GetLine(World, 2).Flags.Repeat.Should().BeFalse();
        AssertMusicChange(ChangeMusic1, MusicFlags.Loop, () =>
        {
            GameActions.ActivateLine(World, Player, 2, ActivationContext.UseLine).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2060 - SR ChangeMusicAndLoop")]
    public void Action2060_ChangeMusicAndLoop()
    {
        GameActions.GetLine(World, 6).Flags.Repeat.Should().BeTrue();
        AssertMusicChange(ChangeMusic1, MusicFlags.Loop, () =>
        {
            GameActions.ActivateLine(World, Player, 6, ActivationContext.UseLine).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2061 - G1 ChangeMusicAndLoop")]
    public void Action2061_ChangeMusicAndLoop()
    {
        GameActions.GetLine(World, 7).Flags.Repeat.Should().BeFalse();
        AssertMusicChange(ChangeMusic1, MusicFlags.Loop, () =>
        {
            GameActions.SetEntityToLine(World, Player, 7, Player.Radius * 2).Should().BeTrue();
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2062 - GR ChangeMusicAndLoop")]
    public void Action2062_ChangeMusicAndLoop()
    {
        GameActions.GetLine(World, 8).Flags.Repeat.Should().BeTrue();
        AssertMusicChange(ChangeMusic1, MusicFlags.Loop, () =>
        {
            GameActions.SetEntityToLine(World, Player, 8, Player.Radius * 2).Should().BeTrue();
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2063 - W1 ChangeMusicPlayOnce")]
    public void Action2063_ChangeMusicAndLoop()
    {
        GameActions.GetLine(World, 10).Flags.Repeat.Should().BeFalse();
        AssertMusicChange(ChangeMusic1, MusicFlags.None, () =>
        {
            GameActions.ActivateLine(World, Player, 10, ActivationContext.CrossLine).Should().BeTrue();
        });

        AssertMusicChange(ChangeMusic2, MusicFlags.None, () =>
        {
            GameActions.ActivateLine(World, Player, 10, ActivationContext.CrossLine, fromFront: false).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2064 - WR ChangeMusicPlayOnce")]
    public void Action2064_ChangeMusicAndLoop()
    {
        GameActions.GetLine(World, 11).Flags.Repeat.Should().BeTrue();
        AssertMusicChange(ChangeMusic1, MusicFlags.None, () =>
        {
            GameActions.ActivateLine(World, Player, 11, ActivationContext.CrossLine).Should().BeTrue();
        });

        AssertMusicChange(ChangeMusic2, MusicFlags.None, () =>
        {
            GameActions.ActivateLine(World, Player, 11, ActivationContext.CrossLine, fromFront: false).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2065 - S1 ChangeMusicPlayOnce")]
    public void Action2065_ChangeMusicPlayOnce()
    {
        GameActions.GetLine(World, 12).Flags.Repeat.Should().BeFalse();
        AssertMusicChange(ChangeMusic1, MusicFlags.None, () =>
        {
            GameActions.ActivateLine(World, Player, 12, ActivationContext.UseLine).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2066 - SR ChangeMusicAndPlayOnce")]
    public void Action2066_ChangeMusicPlayOnce()
    {
        GameActions.GetLine(World, 13).Flags.Repeat.Should().BeTrue();
        AssertMusicChange(ChangeMusic1, MusicFlags.None, () =>
        {
            GameActions.ActivateLine(World, Player, 13, ActivationContext.UseLine).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2067 - G1 ChangeMusicPlayOnce")]
    public void Action2067_ChangeMusicPlayOnce()
    {
        GameActions.GetLine(World, 14).Flags.Repeat.Should().BeFalse();
        AssertMusicChange(ChangeMusic1, MusicFlags.None, () =>
        {
            GameActions.SetEntityToLine(World, Player, 14, Player.Radius * 2).Should().BeTrue();
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2068 - GR ChangeMusicPlayOnce")]
    public void Action2068_ChangeMusicPlayOnce()
    {
        GameActions.GetLine(World, 15).Flags.Repeat.Should().BeTrue();
        AssertMusicChange(ChangeMusic1, MusicFlags.None, () =>
        {
            GameActions.SetEntityToLine(World, Player, 15, Player.Radius * 2).Should().BeTrue();
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2087 - W1 ChangeMusicAndLoopDefault")]
    public void Action2087_ChangeMusicAndLoopDefault()
    {
        GameActions.GetLine(World, 18).Flags.Repeat.Should().BeFalse();
        AssertMusicChange(ChangeMusic1, MusicFlags.Loop | MusicFlags.ResetToDefault, () =>
        {
            GameActions.ActivateLine(World, Player, 18, ActivationContext.CrossLine).Should().BeTrue();
        });

        AssertMusicChange(DefaultMusic, MusicFlags.Loop | MusicFlags.ResetToDefault, () =>
        {
            GameActions.ActivateLine(World, Player, 18, ActivationContext.CrossLine, fromFront: false).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2088 - WR ChangeMusicAndLoopDefault")]
    public void Action2088_ChangeMusicAndLoopDefault()
    {
        GameActions.GetLine(World, 17).Flags.Repeat.Should().BeTrue();
        AssertMusicChange(ChangeMusic1, MusicFlags.Loop | MusicFlags.ResetToDefault, () =>
        {
            GameActions.ActivateLine(World, Player, 17, ActivationContext.CrossLine).Should().BeTrue();
        });

        AssertMusicChange(DefaultMusic, MusicFlags.Loop | MusicFlags.ResetToDefault, () =>
        {
            GameActions.ActivateLine(World, Player, 17, ActivationContext.CrossLine, fromFront: false).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2089 - S1 ChangeMusicAndLoopDefault")]
    public void Action2089_ChangeMusicAndLoopDefault()
    {
        GameActions.GetLine(World, 19).Flags.Repeat.Should().BeFalse();
        AssertMusicChange(ChangeMusic1, MusicFlags.Loop | MusicFlags.ResetToDefault, () =>
        {
            GameActions.ActivateLine(World, Player, 19, ActivationContext.UseLine).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2090 - SR ChangeMusicAndLoopDefault")]
    public void Action2090_ChangeMusicAndLoopDefault()
    {
        GameActions.GetLine(World, 20).Flags.Repeat.Should().BeTrue();
        AssertMusicChange(ChangeMusic1, MusicFlags.Loop | MusicFlags.ResetToDefault, () =>
        {
            GameActions.ActivateLine(World, Player, 20, ActivationContext.UseLine).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2091 - G1 ChangeMusicAndLoopDefault")]
    public void Action2091_ChangeMusicAndLoopDefault()
    {
        GameActions.GetLine(World, 21).Flags.Repeat.Should().BeFalse();
        AssertMusicChange(ChangeMusic1, MusicFlags.Loop | MusicFlags.ResetToDefault, () =>
        {
            GameActions.SetEntityToLine(World, Player, 21, Player.Radius * 2).Should().BeTrue();
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2092 - GR ChangeMusicAndLoopDefault")]
    public void Action2092_ChangeMusicAndLoopDefault()
    {
        GameActions.GetLine(World, 22).Flags.Repeat.Should().BeTrue();
        AssertMusicChange(ChangeMusic1, MusicFlags.Loop | MusicFlags.ResetToDefault, () =>
        {
            GameActions.SetEntityToLine(World, Player, 22, Player.Radius * 2).Should().BeTrue();
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2093 - W1 ChangeMusicAndLoopDefault")]
    public void Action2093_ChangeMusicPlayOnceDefault()
    {
        GameActions.GetLine(World, 28).Flags.Repeat.Should().BeFalse();
        AssertMusicChange(ChangeMusic1, MusicFlags.ResetToDefault, () =>
        {
            GameActions.ActivateLine(World, Player, 28, ActivationContext.CrossLine).Should().BeTrue();
        });

        AssertMusicChange(DefaultMusic, MusicFlags.Loop | MusicFlags.ResetToDefault, () =>
        {
            GameActions.ActivateLine(World, Player, 28, ActivationContext.CrossLine, fromFront: false).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2094 - WR ChangeMusicPlayOnceDefault")]
    public void Action2094_ChangeMusicPlayOnceDefault()
    {
        GameActions.GetLine(World, 29).Flags.Repeat.Should().BeTrue();
        AssertMusicChange(ChangeMusic1, MusicFlags.ResetToDefault, () =>
        {
            GameActions.ActivateLine(World, Player, 29, ActivationContext.CrossLine).Should().BeTrue();
        });

        AssertMusicChange(DefaultMusic, MusicFlags.Loop | MusicFlags.ResetToDefault, () =>
        {
            GameActions.ActivateLine(World, Player, 29, ActivationContext.CrossLine, fromFront: false).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2095 - S1 ChangeMusicPlayOnceDefault")]
    public void Action2095_ChangeMusicPlayOnceDefault()
    {
        GameActions.GetLine(World, 24).Flags.Repeat.Should().BeFalse();
        AssertMusicChange(ChangeMusic1, MusicFlags.ResetToDefault, () =>
        {
            GameActions.ActivateLine(World, Player, 24, ActivationContext.UseLine).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2096 - SR ChangeMusicAndLoopDefault")]
    public void Action2096_ChangeMusicPlayOnceDefault()
    {
        GameActions.GetLine(World, 25).Flags.Repeat.Should().BeTrue();
        AssertMusicChange(ChangeMusic1, MusicFlags.ResetToDefault, () =>
        {
            GameActions.ActivateLine(World, Player, 25, ActivationContext.UseLine).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2097 - G1 ChangeMusicPlayOnceDefault")]
    public void Action2097_ChangeMusicPlayOnceDefault()
    {
        GameActions.GetLine(World, 26).Flags.Repeat.Should().BeFalse();
        AssertMusicChange(ChangeMusic1, MusicFlags.ResetToDefault, () =>
        {
            GameActions.SetEntityToLine(World, Player, 26, Player.Radius * 2).Should().BeTrue();
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2098 - GR ChangeMusicPlayOnceDefault")]
    public void Action2098_ChangeMusicPlayOnceDefault()
    {
        GameActions.GetLine(World, 27).Flags.Repeat.Should().BeTrue();
        AssertMusicChange(ChangeMusic1, MusicFlags.ResetToDefault, () =>
        {
            GameActions.SetEntityToLine(World, Player, 27, Player.Radius * 2).Should().BeTrue();
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        });
    }

    private void AssertMusicChange(string music, MusicFlags flags, Action action)
    {
        MusicChangeEvent? musicChangeEvent = null;
        bool changed = false;
        World.OnMusicChanged += World_OnMusicChanged;
        action();

        GameActions.TickWorld(World, () => { return !changed; }, () => { });
        musicChangeEvent.Should().NotBeNull();
        musicChangeEvent!.Value.Entry.Path.Name.Should().Be(music);
        musicChangeEvent!.Value.MusicFlags.Should().Be(flags);

        void World_OnMusicChanged(object? sender, MusicChangeEvent e)
        {
            musicChangeEvent = e;
            changed = true;
        }
    }
}