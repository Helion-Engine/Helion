using FluentAssertions;
using Helion.Util.Extensions;
using Helion.World;

namespace Helion.Tests.Unit.GameAction
{
    public partial class GameActions
    {
        public static void AssertSound(WorldBase world, object soundSource, string soundName)
        {
            var sound = world.SoundManager.FindBySource(soundSource);
            sound.Should().NotBeNull();
            sound!.AudioData.SoundInfo.EntryName.EqualsIgnoreCase(soundName).Should().BeTrue();

            TickWorld(world, () => { return world.SoundManager.FindBySource(soundSource) != null; }, () => { });
        }
    }
}
