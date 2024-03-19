using FluentAssertions;
using Helion.Dehacked;
using Xunit;

namespace Helion.Tests.Unit.Dehacked;

public class DehackedCheat
{
    [Fact(DisplayName="Dehacked cheats")]
    public void DehackedCheats()
    {
        string data = @"Cheat whocares
  Change music = music
  Chainsaw = saw
  God mode = god
  Ammo & Keys = keyz
  Ammo = ammo
  No Clipping 1 = clip1
  No Clipping 2 = clip2
  Invincibility = invincibility
  Berserk = berserk
  Invisibility = invisibility
  Radiation Suit = rad
  Auto-map = map
  Lite-amp Goggles = amp
  BEHOLD menu = behold
  Level Warp = levelwarp
  Player Position = playerposition";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);

        dehacked.Cheat.Should().NotBeNull();
        var cheat = dehacked.Cheat!;
        cheat.ChangeMusic.Should().Be("music");
        cheat.Chainsaw.Should().Be("saw");
        cheat.God.Should().Be("god");
        cheat.AmmoAndKeys.Should().Be("keyz");
        cheat.NoClip1.Should().Be("clip1");
        cheat.NoClip2.Should().Be("clip2");
        cheat.NoClip2.Should().Be("clip2");
        cheat.Invincibility.Should().Be("invincibility");
        cheat.Berserk.Should().Be("berserk");
        cheat.Invisibility.Should().Be("invisibility");
        cheat.RadSuit.Should().Be("rad");
        cheat.AutoMap.Should().Be("map");
        cheat.LiteAmp.Should().Be("amp");
        cheat.Behold.Should().Be("behold");
        cheat.LevelWarp.Should().Be("levelwarp");
        cheat.PlayerPos.Should().Be("playerposition");
    }
}
