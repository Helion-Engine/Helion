using FluentAssertions;
using Helion.Resources;
using Helion.Resources.IWad;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class Boss
    {
        private const string Resource = "Resources/boss.zip";
        private const string File = "boss.wad";

        public Boss()
        {
            
        }

        private void WorldInit(SinglePlayerWorld world)
        {
            // MAP07 arachnotron special uses raise by shortest lower which is dependent on the texture image height
            world.CheatManager.ActivateCheat(world.Player, CheatType.God);
            var texture = world.TextureManager.GetTexture("METAL", ResourceNamespace.Textures);
            texture.Image = CreateImage(64, 128);
        }

        private static Helion.Graphics.Image CreateImage(int width, int height) =>
            new((width, height), Helion.Graphics.ImageType.Argb);

        [Fact(DisplayName = "E1M8 baron boss death")]
        public void E1M8()
        {
            var world = WorldAllocator.LoadMap(Resource, File, "E1M8", GetType().Name, WorldInit, IWadType.UltimateDoom);
            var baron1 = GameActions.GetEntity(world, 0);
            var baron2 = GameActions.GetEntity(world, 1);
            baron1.Definition.Name.EqualsIgnoreCase("BaronOfHell").Should().BeTrue();
            baron2.Definition.Name.EqualsIgnoreCase("BaronOfHell").Should().BeTrue();

            var sector = GameActions.GetSectorByTag(world, 666);
            sector.ActiveFloorMove.Should().BeNull();

            baron1.Kill(null);
            GameActions.TickWorld(world, 70);
            sector.ActiveFloorMove.Should().BeNull();
            baron2.Kill(null);
            GameActions.TickWorld(world, 70);
            sector.ActiveFloorMove.Should().NotBeNull();
            world.Dispose();
        }

        [Fact(DisplayName = "E2M8 Cyberdemon boss death")]
        public void E2M8()
        {
            bool exited = false;
            var world = WorldAllocator.LoadMap(Resource, File, "E2M8", GetType().Name, WorldInit, IWadType.UltimateDoom);
            world.LevelExit += World_LevelExit;
            var cyber = GameActions.GetEntity(world, 0);
            cyber.Definition.Name.EqualsIgnoreCase("Cyberdemon").Should().BeTrue();
            cyber.Kill(null);
            GameActions.TickWorld(world, 200);

            void World_LevelExit(object? sender, LevelChangeEvent e)
            {
                e.Cancel = true;
                exited = true;
            }

            exited.Should().BeTrue();
        }

        [Fact(DisplayName = "E3M8 Spider Mastermind boss death")]
        public void E3M8()
        {
            bool exited = false;
            var world = WorldAllocator.LoadMap(Resource, File, "E3M8", GetType().Name, WorldInit, IWadType.UltimateDoom);
            world.LevelExit += World_LevelExit;
            var spider = GameActions.GetEntity(world, 0);
            spider.Definition.Name.EqualsIgnoreCase("SpiderMasterMind").Should().BeTrue();
            spider.Kill(null);
            GameActions.TickWorld(world, 300);

            void World_LevelExit(object? sender, LevelChangeEvent e)
            {
                e.Cancel = true;
                exited = true;
            }

            exited.Should().BeTrue();
        }

        [Fact(DisplayName = "E4M6 Cyberdemon boss death")]
        public void E4M6()
        {
            var world = WorldAllocator.LoadMap(Resource, File, "E4M6", GetType().Name, WorldInit, IWadType.UltimateDoom);
            var cyber = GameActions.GetEntity(world, 0);
            cyber.Definition.Name.EqualsIgnoreCase("Cyberdemon").Should().BeTrue();

            var sector = GameActions.GetSectorByTag(world, 666);
            sector.ActiveCeilingMove.Should().BeNull();

            cyber.Kill(null);
            GameActions.TickWorld(world, 120);
            
            sector.ActiveCeilingMove.Should().NotBeNull();
        }

        [Fact(DisplayName = "E4M8 Spider Mastermind boss death")]
        public void E4M8()
        {
            var world = WorldAllocator.LoadMap(Resource, File, "E4M8", GetType().Name, WorldInit, IWadType.UltimateDoom);
            var spider = GameActions.GetEntity(world, 0);
            spider.Definition.Name.EqualsIgnoreCase("SpiderMasterMind").Should().BeTrue();

            var sector = GameActions.GetSectorByTag(world, 666);
            sector.ActiveFloorMove.Should().BeNull();

            spider.Kill(null);
            GameActions.TickWorld(world, 200);

            sector.ActiveFloorMove.Should().NotBeNull();
        }

        [Fact(DisplayName = "MAP07 Mancubus and arachnotron boss death")]
        public void MAP07()
        {
            var world = WorldAllocator.LoadMap(Resource, File, "MAP07", GetType().Name, WorldInit, IWadType.Doom2);
            var manc1 = GameActions.GetEntity(world, 0);
            var manc2 = GameActions.GetEntity(world, 1);
            manc1.Definition.Name.EqualsIgnoreCase("Fatso").Should().BeTrue();
            manc2.Definition.Name.EqualsIgnoreCase("Fatso").Should().BeTrue();

            var arach1 = GameActions.GetEntity(world, 2);
            var arach2 = GameActions.GetEntity(world, 3);
            arach1.Definition.Name.EqualsIgnoreCase("Arachnotron").Should().BeTrue();
            arach2.Definition.Name.EqualsIgnoreCase("Arachnotron").Should().BeTrue();

            var mancSector = GameActions.GetSectorByTag(world, 666);
            mancSector.ActiveFloorMove.Should().BeNull();

            var arachSector = GameActions.GetSectorByTag(world, 667);
            arachSector.ActiveFloorMove.Should().BeNull();

            manc1.Kill(null);
            GameActions.TickWorld(world, 70);
            mancSector.ActiveFloorMove.Should().BeNull();
            manc2.Kill(null);
            GameActions.TickWorld(world, 70);
            mancSector.ActiveFloorMove.Should().NotBeNull();

            arach1.Kill(null);
            GameActions.TickWorld(world, 70);
            arachSector.ActiveFloorMove.Should().BeNull();
            arach2.Kill(null);
            GameActions.TickWorld(world, 70);
            arachSector.ActiveFloorMove.Should().NotBeNull();
        }
    }
}
