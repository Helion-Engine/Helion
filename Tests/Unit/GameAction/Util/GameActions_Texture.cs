
using FluentAssertions;
using Helion.World;

namespace Helion.Tests.Unit.GameAction;

public static partial class GameActions
{
    public static void AssertTexture(WorldBase world, int textureHandle, string textureName)
    {
        var texture = world.TextureManager.GetNonAnimatedTexture(textureHandle);
        texture.Name.Should().Be(textureName);
    }
}
