using FluentAssertions;
using Helion.Util.Extensions;
using Helion.World.Entities;

namespace Helion.Tests.Unit.GameAction
{
    public partial class GameActions
    {
        public static void AssertFrameStateFunction(Entity entity, string name)
        {
            entity.Frame.ActionFunction.Should().NotBeNull();
            entity.Frame.ActionFunction!.Method.Name.EqualsIgnoreCase(name).Should().BeTrue();
        }

        public static void AssertNoFrameStateFunction(Entity entity)
        {
            entity.Frame.ActionFunction.Should().BeNull();
        }
    }
}
