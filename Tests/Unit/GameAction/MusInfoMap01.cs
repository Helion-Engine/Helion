using FluentAssertions;
using Helion.Resources.Archives.Entries;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class MusInfoMap01
{
    private readonly SinglePlayerWorld World;

    public MusInfoMap01()
    {
        World = WorldAllocator.LoadMap("Resources/musinfo.zip", "musinfo.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName= "Change music MAP01 #1")]
    public void ChangeMusic1()
    {
        Entry? musicEntry = null;
        World.OnMusicChanged += World_OnMusicChanged;

        GameActions.SetEntityPosition(World, World.Player, (-384, -96));
        musicEntry.Should().BeNull();
        GameActions.TickWorld(World, 30);
        musicEntry.Should().NotBeNull();
        musicEntry!.Path.Name.Should().Be("D_STALKS");

        musicEntry = null;
        GameActions.SetEntityPosition(World, World.Player, (-256, -192));
        musicEntry.Should().BeNull();
        GameActions.TickWorld(World, 30);
        musicEntry.Should().BeNull();

        // Going back to the same music changer should not invoke a new change
        musicEntry = null;
        GameActions.SetEntityPosition(World, World.Player, (-384, -96));
        GameActions.TickWorld(World, 30);
        musicEntry.Should().BeNull();

        void World_OnMusicChanged(object? sender, Entry e)
        {
            musicEntry = e;
        }
    }

    [Fact(DisplayName = "Change music MAP01 #2")]
    public void ChangeMusic2()
    {
        Entry? musicEntry = null;
        World.OnMusicChanged += World_OnMusicChanged;

        GameActions.SetEntityPosition(World, World.Player, (-128, -96));
        musicEntry.Should().BeNull();
        GameActions.TickWorld(World, 30);
        musicEntry.Should().NotBeNull();
        musicEntry!.Path.Name.Should().Be("D_COUNTD");

        musicEntry = null;
        GameActions.SetEntityPosition(World, World.Player, (-256, -192));
        musicEntry.Should().BeNull();
        GameActions.TickWorld(World, 30);
        musicEntry.Should().BeNull();

        // Going back to the same music changer should not invoke a new change
        musicEntry = null;
        GameActions.SetEntityPosition(World, World.Player, (-128, -96));
        GameActions.TickWorld(World, 30);
        musicEntry.Should().BeNull();

        void World_OnMusicChanged(object? sender, Entry e)
        {
            musicEntry = e;
        }
    }

    [Fact(DisplayName = "Change music MAP01 #1 then #2")]
    public void ChangeMusic1And2()
    {
        Entry? musicEntry = null;
        World.OnMusicChanged += World_OnMusicChanged;

        GameActions.SetEntityPosition(World, World.Player, (-384, -96));
        musicEntry.Should().BeNull();
        GameActions.TickWorld(World, 30);
        musicEntry.Should().NotBeNull();
        musicEntry!.Path.Name.Should().Be("D_STALKS");

        musicEntry = null;

        GameActions.SetEntityPosition(World, World.Player, (-128, -96));
        musicEntry.Should().BeNull();
        GameActions.TickWorld(World, 30);
        musicEntry.Should().NotBeNull();
        musicEntry!.Path.Name.Should().Be("D_COUNTD");

        void World_OnMusicChanged(object? sender, Entry e)
        {
            musicEntry = e;
        }
    }

    [Fact(DisplayName = "Change music MAP01 #1 timing")]
    public void ChangeMusic1Timing()
    {
        Entry? musicEntry = null;
        World.OnMusicChanged += World_OnMusicChanged;

        // Only stand on the sector for 1 gametick, then move off to sector with music changer that should be ignored.
        GameActions.SetEntityPosition(World, World.Player, (-384, -96));
        GameActions.TickWorld(World, 1);
        musicEntry.Should().BeNull();
        GameActions.SetEntityPosition(World, World.Player, (-128, -96));
        GameActions.TickWorld(World, 1);
        musicEntry.Should().BeNull();
        GameActions.TickWorld(World, 29);
        musicEntry.Should().NotBeNull();
        musicEntry!.Path.Name.Should().Be("D_COUNTD");

        void World_OnMusicChanged(object? sender, Entry e)
        {
            musicEntry = e;
        }
    }
}
