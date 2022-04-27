using Helion.Resources.IWad;
using Helion.World;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class Animations
    {
        private const string Resource = "Resources/animations.zip";
        private const string File = "animations.wad";
        private const string Map = "MAP01";

        private class AnimationValues
        {
            public AnimationValues(int id, int ticks, string[] names)
            {
                SectorStartId = id;
                Ticks = ticks;
                TextureNames = names;
            }

            public int SectorStartId;
            public int Ticks;
            public string[] TextureNames;
        }

        private static readonly AnimationValues[] FlatAnimationValues = new[]
        {
            new AnimationValues(1, 8, new[] { "BLOOD1", "BLOOD2", "BLOOD3" }),
            new AnimationValues(4, 8, new[] { "FWATER1", "FWATER2", "FWATER3", "FWATER4" }),
            new AnimationValues(8, 8, new[] { "LAVA1", "LAVA2", "LAVA3", "LAVA4" }),
            new AnimationValues(12, 8, new[] { "NUKAGE1", "NUKAGE2", "NUKAGE3" }),
            new AnimationValues(15, 8, new[] { "RROCK05", "RROCK06", "RROCK07", "RROCK08" }),
            new AnimationValues(19, 8, new[] { "SLIME01", "SLIME02", "SLIME03", "SLIME04" }),
            new AnimationValues(23, 8, new[] { "SLIME09", "SLIME10", "SLIME11", "SLIME12" }),
        };

        public Animations()
        {

        }

        private void WorldInit(SinglePlayerWorld world)
        {

        }

        [Fact(DisplayName = "Flat animation cycles")]
        public void FlatAnimations()
        {
            var world = WorldAllocator.LoadMap(Resource, File, Map, GetType().Name, WorldInit, IWadType.Doom2);

            foreach (var animation in FlatAnimationValues)
            {
                world.TextureManager.ResetAnimations();
                CheckAnimations(world, animation);
                // Make sure the cycle repeats
                CheckAnimations(world, animation);
            }
        }

        private static void CheckAnimations(SinglePlayerWorld world, AnimationValues animation)
        {
            for (int i = 0; i < animation.TextureNames.Length; i++)
            {
                CheckAnimationCycle(world, animation, i);
                GameActions.TickWorld(world, animation.Ticks);
            }
        }

        private static void CheckAnimationCycle(SinglePlayerWorld world, AnimationValues animation, int startIndex)
        {
            for (int i = 0; i < animation.TextureNames.Length; i++)
            {
                int textureIndex = (startIndex + i) % animation.TextureNames.Length;
                CheckFloorTexture(world, animation.SectorStartId + i, animation.TextureNames[textureIndex]);
            }
        }

        private static void CheckFloorTexture(WorldBase world, int sectorId, string name)
        {
            var floor = GameActions.GetSector(world, sectorId).Floor;
            GameActions.CheckPlaneTexture(world, floor, name);
        }
    }
}
