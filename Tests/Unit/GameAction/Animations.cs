using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class Animations
    {
        private const string Resource = "Resources/animations.zip";
        private const string File = "animations.WAD";
        private const string Map = "MAP01";

        private class AnimationValues
        {
            public AnimationValues(int id, int ticks, string[] names)
            {
                StartId = id;
                Ticks = ticks;
                TextureNames = names;
            }

            public int StartId;
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

        private static readonly AnimationValues[] TextureAnimationValues = new[]
        {
            new AnimationValues(84, 8, new[] { "BFALL1", "BFALL2", "BFALL3", "BFALL4" }),
            new AnimationValues(88, 8, new[] { "DBRAIN1", "DBRAIN2", "DBRAIN3", "DBRAIN4" }),
            new AnimationValues(92, 8, new[] { "FIREBLU1", "FIREBLU2" }),
            new AnimationValues(94, 8, new[] { "FIRELAV3", "FIRELAVA" }),
            new AnimationValues(96, 8, new[] { "FIREMAG1", "FIREMAG2", "FIREMAG3" }),
            new AnimationValues(99, 8, new[] { "FIREWALA", "FIREWALB", "FIREWALL" }),
            new AnimationValues(102, 8, new[] { "GSTFONT1", "GSTFONT2", "GSTFONT3" }),
            new AnimationValues(105, 8, new[] { "SFALL1", "SFALL2", "SFALL3", "SFALL4" }),
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
                CheckFlatAnimations(world, animation);
                // Make sure the cycle repeats
                CheckFlatAnimations(world, animation);
            }
        }

        [Fact(DisplayName = "Texture animation cycles")]
        public void TextureAnimations()
        {
            var world = WorldAllocator.LoadMap(Resource, File, Map, GetType().Name, WorldInit, IWadType.Doom2);

            foreach (var animation in TextureAnimationValues)
            {
                world.TextureManager.ResetAnimations();
                CheckTextureAnimations(world, animation);
                // Make sure the cycle repeats
                CheckTextureAnimations(world, animation);
            }
        }

        private static void CheckTextureAnimations(SinglePlayerWorld world, AnimationValues animation)
        {
            for (int i = 0; i < animation.TextureNames.Length; i++)
            {
                CheckTextureAnimationCycle(world, animation, i);
                GameActions.TickWorld(world, animation.Ticks);
            }
        }

        private static void CheckTextureAnimationCycle(SinglePlayerWorld world, AnimationValues animation, int startIndex)
        {
            for (int i = 0; i < animation.TextureNames.Length; i++)
            {
                int textureIndex = (startIndex + i) % animation.TextureNames.Length;
                CheckWallTexture(world, animation.StartId + i, animation.TextureNames[textureIndex]);
            }
        }

        private static void CheckWallTexture(WorldBase world, int lineId, string name)
        {
            var line = GameActions.GetLine(world, lineId);
            world.TextureManager.GetTexture(line.Front.Middle.TextureHandle).Name.Equals(name, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        }

        private static void CheckFlatAnimations(SinglePlayerWorld world, AnimationValues animation)
        {
            for (int i = 0; i < animation.TextureNames.Length; i++)
            {
                CheckFlatAnimationCycle(world, animation, i);
                GameActions.TickWorld(world, animation.Ticks);
            }
        }

        private static void CheckFlatAnimationCycle(SinglePlayerWorld world, AnimationValues animation, int startIndex)
        {
            for (int i = 0; i < animation.TextureNames.Length; i++)
            {
                int textureIndex = (startIndex + i) % animation.TextureNames.Length;
                CheckFloorTexture(world, animation.StartId + i, animation.TextureNames[textureIndex]);
            }
        }

        private static void CheckFloorTexture(WorldBase world, int sectorId, string name)
        {
            var floor = GameActions.GetSector(world, sectorId).Floor;
            GameActions.CheckPlaneTexture(world, floor, name);
        }
    }
}
