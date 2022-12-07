using FluentAssertions;
using Helion.Resources;
using Helion.Resources.IWad;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Impl.SinglePlayer;
using System.Drawing;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class Keen
    {
        private const string Resource = "Resources/keen.zip";
        private const string File = "keen.wad";

        private void WorldInit(SinglePlayerWorld world)
        {
        }


        [Fact(DisplayName = "Door opens when all keen entities are dead")]
        public void KeenDeath()
        {
            var world = WorldAllocator.LoadMap(Resource, File, "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
            var keen1 = GameActions.GetEntity(world, 0);
            var keen2 = GameActions.GetEntity(world, 1);

            var sector = GameActions.GetSectorByTag(world, 666);
            sector.ActiveCeilingMove.Should().BeNull();
            sector.Ceiling.Z.Should().Be(0);

            keen1.Kill(null);
            GameActions.TickWorld(world, 70);
            sector.ActiveCeilingMove.Should().BeNull();
            keen2.Kill(null);
            GameActions.TickWorld(world, 70);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.TickWorld(world, () => { return sector.ActiveCeilingMove != null; }, () => { });
            sector.Ceiling.Z.Should().Be(124);

            world.Dispose();
        }
    }
}
