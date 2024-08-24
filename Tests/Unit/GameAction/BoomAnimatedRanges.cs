using FluentAssertions;
using Helion.Resources.IWad;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class BoomAnimatedRanges
{
    [Fact(DisplayName = "Animated range with duplicate texture")]
    public void AnimatedRangeWithDuplicateTexture()
    {
        var world = WorldAllocator.LoadMap("Resources/boomanimated.zip", "boomanimated.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        var animations = world.TextureManager.GetAnimations();

        var animation = animations.First(x => x.AnimatedTexture.Name.Equals("KS_FLSG1")).AnimatedTexture;
        animation.Components.Count.Should().Be(6);

        for (int i = 0; i < animation.Components.Count; i++)
            animation.Components[i].Texture.Should().Be($"KS_FLSG{i + 1}");

        animation = animations.First(x => x.AnimatedTexture.Name.Equals("KS_FLSG6")).AnimatedTexture;
        animation.Components.Count.Should().Be(6);

        animation.Components[0].Texture.Should().Be($"KS_FLSG6");
        for (int i = 1; i < animation.Components.Count; i++)
            animation.Components[i].Texture.Should().Be($"KS_FLSG{i}");
    }
}
