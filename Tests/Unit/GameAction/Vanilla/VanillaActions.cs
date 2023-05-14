using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials.Vanilla;
using Helion.Resources;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Sound;
using Helion.World.Special;
using Helion.World.Special.SectorMovement;
using Helion.World.Special.Specials;
using MoreLinq;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class VanillaActions
    {
        private static readonly string ResourceZip = "Resources/vandacts.zip";

        private static readonly string MapName = "MAP01";
        private readonly SinglePlayerWorld World;
        private Player Player => World.Player;
        private Entity Monster => GameActions.GetEntity(World, 1);

        public VanillaActions()
        {
            World = WorldAllocator.LoadMap(ResourceZip, "vandacts.wad", MapName, GetType().Name, WorldInit, IWadType.Doom2);
        }

        private void WorldInit(SinglePlayerWorld world)
        {
            SetUnitTestTextures(world);
            world.CheatManager.ActivateCheat(world.Player, CheatType.God);

            int[] pauseSectors = new int[] { 726, 643 };
            foreach (int sector in pauseSectors)
                GameActions.GetSector(world, sector).ActiveCeilingMove?.Pause();

            world.Config.Compatibility.VanillaSectorPhysics.Set(true);
        }

        private static void SetUnitTestTextures(WorldBase world)
        {
            // We're not loading doom2.wad so the texture images are empty. Load fake images to test action 30 (raise by shortest lower)

            // For raise by shortest lower compatibility testing            
            Texture texture = world.TextureManager.GetTexture("AASHITTY", ResourceNamespace.Textures);
            texture.Image = CreateImage(64, 64);

            world.TextureManager.NullCompatibilityTextureIndex = texture.Index;

            texture = world.TextureManager.GetTexture("NUKE24", ResourceNamespace.Textures);
            texture.Image = CreateImage(64, 24);

            texture = world.TextureManager.GetTexture("DBRAIN1", ResourceNamespace.Textures);
            texture.Image = CreateImage(64, 32);
            texture = world.TextureManager.GetTexture("DBRAIN2", ResourceNamespace.Textures);
            texture.Image = CreateImage(64, 32);
            texture = world.TextureManager.GetTexture("DBRAIN3", ResourceNamespace.Textures);
            texture.Image = CreateImage(64, 32);
            texture = world.TextureManager.GetTexture("DBRAIN4", ResourceNamespace.Textures);
            texture.Image = CreateImage(64, 32);

            texture = world.TextureManager.GetTexture("GRAY2", ResourceNamespace.Textures);
            texture.Image = CreateImage(64, 72);

            texture = world.TextureManager.GetTexture("SUPPORT2", ResourceNamespace.Textures);
            texture.Image = CreateImage(64, 128);
        }

        private static Helion.Graphics.Image CreateImage(int width, int height) =>
            new((width, height), Helion.Graphics.ImageType.Argb);

        [Fact(DisplayName = "Doom Action 1 (DR) Door open and Close")]
        public void Action1()
        {
            const int DoorLine = 239;
            GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
            Sector sector = GameActions.GetSector(World, 2);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorSlowSpeed);

            // Player can change door direction during movent
            GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();
            sector.ActiveCeilingMove!.MoveDirection.Should().Be(MoveDirection.Up);
            GameActions.TickWorld(World, 35);
            // Reactivate should move door back down
            GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();
            sector.ActiveCeilingMove!.MoveDirection.Should().Be(MoveDirection.Down);
            GameActions.TickWorld(World, () => { return sector.ActiveCeilingMove != null; }, () => { });

            GameActions.CheckMonsterUseActivation(World, Monster, DoorLine, sector, sector.Ceiling, true);
            GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorSlowSpeed);
        }

        [Fact(DisplayName = "Doom Action 2 (W1) Open door")]
        public void Action2()
        {
            const int DoorLine = 38;
            GameActions.EntityCrossLine(World, Player, DoorLine).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 2);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunDoorOpenStay(World, sector, 128, VanillaConstants.DoorSlowSpeed);

            GameActions.CheckNoReactivateEntityCross(World, Player, DoorLine, sector, sector.Ceiling);
            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 3 (W1) Close door")]
        public void Action3()
        {
            const int DoorLine = 233;
            GameActions.EntityCrossLine(World, Player, DoorLine).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 3);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunDoorClose(World, sector, 0, VanillaConstants.DoorSlowSpeed);

            GameActions.CheckNoReactivateEntityCross(World, Player, DoorLine, sector, sector.Ceiling);
            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 4 (W1) Door open and close (Monster)")]
        public void Action4()
        {
            const int DoorLine = 149;
            GameActions.EntityCrossLine(World, Player, DoorLine).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 4);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorSlowSpeed);

            GameActions.CheckNoReactivateEntityCross(World, Player, DoorLine, sector, sector.Ceiling);

            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, true);
            GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorSlowSpeed);
        }

        [Fact(DisplayName = "Doom Action 5 (W1) Raise floor to lowest adjacent ceiling")]
        public void Action5()
        {
            const int Line = 812;
            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 5);
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.RunFloorRaise(World, sector, 64, 8);

            GameActions.CheckNoReactivateEntityCross(World, Player, Line, sector, sector.Floor);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 6 (W1) Fast crusher ceiling")]
        public void Action6()
        {
            const int Line = 3095;
            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 6);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunCrusherCeiling(World, sector, 16, false);
        }

        [Fact(DisplayName = "Doom Action 7 (S1) Raise stairs 8")]
        public void Action7()
        {
            const int Line = 2957;
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            int[] stairIds = new int[] { 663, 664, 665, 661 };
            GameActions.RunStairs(World, stairIds, 8, 8, 2);
        }

        [Fact(DisplayName = "Doom Action 8 (W1) Raise stairs 8")]
        public void Action8()
        {
            const int Line = 2870;
            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            int[] stairIds = new int[] { 650, 651, 652, 648 };
            GameActions.RunStairs(World, stairIds, 0, 8, 2);
        }

        [Fact(DisplayName = "Doom Action 9 (S1) Donut")]
        public void Action9()
        {
            const int Line = 815;
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            var lowerSector = GameActions.GetSector(World, 220);
            var raiseSector = GameActions.GetSector(World, 219);
            GameActions.CheckPlaneTexture(World, lowerSector.Floor, "SLIME15");
            GameActions.CheckPlaneTexture(World, raiseSector.Floor, "RROCK01");
            raiseSector.SectorDamageSpecial.Should().NotBeNull();
            raiseSector.SectorDamageSpecial!.Damage.Should().Be(10);

            Sector[] sectors = new[] { lowerSector, raiseSector };
            GameActions.RunSectorPlaneSpecials(World, sectors, () =>
            {
                if (raiseSector.Floor.Z != 0)
                {
                    GameActions.CheckPlaneTexture(World, raiseSector.Floor, "RROCK01");
                    raiseSector.SectorDamageSpecial.Should().NotBeNull();
                    raiseSector.SectorDamageSpecial!.Damage.Should().Be(10);
                }
            });

            GameActions.CheckPlaneTexture(World, lowerSector.Floor, "SLIME15");
            GameActions.CheckPlaneTexture(World, raiseSector.Floor, "FLOOR0_1");
            raiseSector.SectorDamageSpecial.Should().BeNull();

            GameActions.CheckNoReactivateEntityUse(World, Player, Line, lowerSector, lowerSector.Floor);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, lowerSector, lowerSector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 10 (W1) Lift")]
        public void Action10()
        {
            const int Line = 2585;
            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 10);
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.RunLift(World, sector, 64, 0, VanillaConstants.LiftFastSpeed, VanillaConstants.LiftDelay);

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, true);
        }

        [Fact(DisplayName = "Doom Action 11 (S) End level")]
        public void Action11()
        {
            bool exited = false;
            World.LevelExit += World_LevelExit;

            const int Line = 4159;
            GameActions.EntityUseLine(World, Player, Line);
            GameActions.TickWorld(World, 35);
            World.LevelExit -= World_LevelExit;

            void World_LevelExit(object? sender, LevelChangeEvent e)
            {
                e.Cancel = true;
                exited = true;
                e.ChangeType.Should().Be(LevelChangeType.Next);
            }

            exited.Should().BeTrue();
        }

        [Fact(DisplayName = "Doom Action 12 (W1) Light level match brightest adjacent")]
        public void Action12()
        {
            const int Line = 3551;
            Sector sector = GameActions.GetSectorByTag(World, 12);
            sector.LightLevel.Should().Be(96);
            sector.Floor.LightLevel.Should().Be(96);
            sector.Ceiling.LightLevel.Should().Be(96);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();

            sector.LightLevel.Should().Be(128);
            sector.Floor.LightLevel.Should().Be(128);
            sector.Ceiling.LightLevel.Should().Be(128);
            sector.SetLightLevel(96, World.Gametick);

            GameActions.GetLine(World, Line).SetActivated(false);
            GameActions.EntityCrossLine(World, Monster, Line).Should().BeTrue();
            sector.LightLevel.Should().Be(96);
            sector.Floor.LightLevel.Should().Be(96);
            sector.Ceiling.LightLevel.Should().Be(96);
        }

        [Fact(DisplayName = "Doom Action 13 (W1) Light level to 255")]
        public void Action13()
        {
            const int Line = 3563;
            Sector sector = GameActions.GetSectorByTag(World, 13);
            sector.LightLevel.Should().Be(96);
            sector.Floor.LightLevel.Should().Be(96);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();

            sector.LightLevel.Should().Be(255);
            sector.Floor.LightLevel.Should().Be(255);
            sector.SetLightLevel(96, World.Gametick);

            GameActions.GetLine(World, Line).SetActivated(false);
            GameActions.EntityCrossLine(World, Monster, Line).Should().BeTrue();
            sector.LightLevel.Should().Be(96);
            sector.Floor.LightLevel.Should().Be(96);
        }

        [Fact(DisplayName = "Doom Action 14 (S1) Raise floor 32 tx")]
        public void Action14()
        {
            const int Line = 834;
            Sector sector = GameActions.GetSectorByTag(World, 14);
            sector.Floor.Z.Should().Be(-16);
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLAT14");

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();

            GameActions.RunFloorRaise(World, sector, 16, 4);
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR1_6");
        }

        [Fact(DisplayName = "Doom Action 15 (S1) Raise floor 24 tx")]
        public void Action15()
        {
            const int Line = 892;
            Sector sector = GameActions.GetSectorByTag(World, 15);
            sector.Floor.Z.Should().Be(-16);
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLAT14");

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();

            GameActions.RunFloorRaise(World, sector, 8, 4);
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR1_6");

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 16 (W1) Door close 30 seconds")]
        public void Action16()
        {
            const int Line = 49;
            Sector sector = GameActions.GetSectorByTag(World, 16);
            sector.ActiveCeilingMove.Should().BeNull();

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            GameActions.RunDoorClose(World, sector, 0, VanillaConstants.DoorSlowSpeed, checkCeilingMove: false);

            GameActions.TickWorld(World, (int)Constants.TicksPerSecond * 30);

            // This returns to where it started, which may not be where a normal door open would stop
            GameActions.RunDoorOpen(World, sector, 120, VanillaConstants.DoorSlowSpeed, includeDoorLip: false);

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 15 (W1) Blink")]
        public void Action17()
        {
            const int Line = 3575;
            Sector sector = GameActions.GetSectorByTag(World, 17);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();

            ISpecial? special = World.SpecialManager.FindSpecialBySector(sector);
            special.Should().NotBeNull();

            var strobe = special as LightStrobeSpecial;
            strobe.Should().NotBeNull();
            strobe!.MinBright.Should().Be(128);
            strobe!.MaxBright.Should().Be(192);
        }

        [Fact(DisplayName = "Doom Action 18 (S1) Raise floor to next higher")]
        public void Action18()
        {
            const int Line = 930;
            Sector sector = GameActions.GetSectorByTag(World, 18);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();

            GameActions.RunFloorRaise(World, sector, 64, 8);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 19 (W1) Lower floor to next higher")]
        public void Action19()
        {
            const int Line = 1101;
            Sector sector = GameActions.GetSectorByTag(World, 19);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();

            GameActions.RunFloorLower(World, sector, 64, 8);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 20 (S1) Raise floor to next higher tx")]
        public void Action20()
        {
            const int Line = 977;
            Sector sector = GameActions.GetSectorByTag(World, 20);
            sector.ActiveFloorMove.Should().BeNull();
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLAT14");

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR1_6");

            GameActions.RunFloorRaise(World, sector, 32, 4);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 21 (S1) Lift")]
        public void Action21()
        {
            const int Line = 2720;
            Sector sector = GameActions.GetSectorByTag(World, 21);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();

            GameActions.RunLift(World, sector, 64, 0, VanillaConstants.LiftFastSpeed, VanillaConstants.LiftDelay);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 22 (S1) Raise floor to next higher tx")]
        public void Action22()
        {
            const int Line = 1041;
            Sector sector = GameActions.GetSectorByTag(World, 22);
            sector.ActiveFloorMove.Should().BeNull();
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLAT14");

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR1_6");

            GameActions.RunFloorRaise(World, sector, 32, 4);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 23 (S1) Lower floor to lowest")]
        public void Action23()
        {
            const int Line = 993;
            Sector sector = GameActions.GetSectorByTag(World, 23);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();

            GameActions.RunFloorLower(World, sector, 0, 8);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 24 (G1) Raise floor to lowest ceiling")]
        public void Action24()
        {
            const int Line = 1108;
            Sector sector = GameActions.GetSectorByTag(World, 24);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.SetEntityToLine(World, Player, Line, Player.Radius * 2).Should().BeTrue();
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();

            GameActions.RunFloorRaise(World, sector, 64, 16);
        }

        [Fact(DisplayName = "Doom Action 25 (W1) Slow crusher ceiling")]
        public void Action25()
        {
            const int Line = 3120;
            GameActions.EntityCrossLine(World, Player, Line, forceActivation: true).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 25);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunCrusherCeiling(World, sector, 8, slowDownOnCrush: true);

            GameActions.ForceStopSectorSpecial(World, sector);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 57 (W1) Stop crusher ceiling")]
        public void Action57()
        {
            const int ActivateLine = 3120;
            const int DeactivateLine = 3144;
            GameActions.EntityCrossLine(World, Player, ActivateLine, forceActivation: true).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 25);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunCrusherCeiling(World, sector, 8, slowDownOnCrush: true);

            GameActions.EntityCrossLine(World, Player, DeactivateLine).Should().BeTrue();

            // The crusher is paused so the ActiveCeilingMove should exist indefinitely.
            sector.ActiveCeilingMove.Should().NotBeNull();
            sector.ActiveCeilingMove!.IsPaused.Should().BeTrue();
            GameActions.ForceStopSectorSpecial(World, sector);
        }

        [Fact(DisplayName = "Doom Action 26 (DR) Door blue key")]
        public void Action26()
        {
            const string KeyCard = "BlueCard";
            const int Line = 222;
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            Sector sector = GameActions.GetSector(World, 16);
            sector.ActiveCeilingMove.Should().BeNull();

            GameActions.GiveItem(Player, KeyCard);
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RemoveItem(Player, KeyCard);
            GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorSlowSpeed);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 27 (DR) Door yellow key")]
        public void Action27()
        {
            const string KeyCard = "YellowCard";
            const int Line = 44;
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            Sector sector = GameActions.GetSector(World, 7);
            sector.ActiveCeilingMove.Should().BeNull();

            GameActions.GiveItem(Player, KeyCard);
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RemoveItem(Player, KeyCard);
            GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorSlowSpeed);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 28 (DR) Door Red key")]
        public void Action28()
        {
            const string KeyCard = "RedCard";
            const int Line = 225;
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            Sector sector = GameActions.GetSector(World, 4);
            sector.ActiveCeilingMove.Should().BeNull();

            GameActions.GiveItem(Player, KeyCard);
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RemoveItem(Player, KeyCard);
            GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorSlowSpeed);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 29 (S1) Door open close")]
        public void Action29()
        {
            const int Line = 163;
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 29);

            GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorSlowSpeed);
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 30 (S1) Raise floor by shortest lower")]
        public void Action30_NoCompatibility()
        {
            World.Config.Compatibility.VanillaShortestTexture.Set(false);
            const int Line = 776;
            GameActions.EntityCrossLine(World, Player, Line, forceActivation: true).Should().BeTrue();
            Sector[] sectors = new[]
            {
                GameActions.GetSector(World, 317),
                GameActions.GetSector(World, 319),
                GameActions.GetSector(World, 318),
                GameActions.GetSector(World, 833),
            };

            sectors.ForEach(x => x.ActiveFloorMove.Should().NotBeNull());

            GameActions.RunSectorPlaneSpecials(World, sectors);

            sectors[0].Floor.Z.Should().Be(24);
            sectors[1].Floor.Z.Should().Be(32);
            sectors[2].Floor.Z.Should().Be(72);
            sectors[3].Floor.Z.Should().Be(128);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            sectors.ForEach(x => x.ActiveFloorMove.Should().BeNull());
            sectors.ForEach(x => x.Floor.SetZ(0));
        }

        [Fact(DisplayName = "Doom Action 30 (S1) Raise floor by shortest lower (vanilla compatbility)")]
        public void Action30_Compatibility()
        {
            World.Config.Compatibility.VanillaShortestTexture.Set(true);
            const int Line = 776;
            GameActions.EntityCrossLine(World, Player, Line, forceActivation: true).Should().BeTrue();
            Sector[] sectors = new[]
            {
                GameActions.GetSector(World, 317),
                GameActions.GetSector(World, 319),
                GameActions.GetSector(World, 318),
                GameActions.GetSector(World, 833),
            };

            sectors.ForEach(x => x.ActiveFloorMove.Should().NotBeNull());

            GameActions.RunSectorPlaneSpecials(World, sectors);

            sectors[0].Floor.Z.Should().Be(24);
            sectors[1].Floor.Z.Should().Be(32);
            sectors[2].Floor.Z.Should().Be(64);
            sectors[3].Floor.Z.Should().Be(64);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            sectors.ForEach(x => x.ActiveFloorMove.Should().BeNull());
            sectors.ForEach(x => x.Floor.SetZ(0));
        }

        [Fact(DisplayName = "Doom Action 31 (D1) Open door stay")]
        public void Action31()
        {
            const int DoorLine = 117;
            GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
            Sector sector = GameActions.GetSector(World, 12);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunDoorOpenStay(World, sector, 128, VanillaConstants.DoorSlowSpeed);

            GameActions.CheckNoReactivateEntityUse(World, Player, DoorLine, sector, sector.Ceiling);
            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 32 (D1) Door blue key stay")]
        public void Action32()
        {
            const string KeyCard = "BlueCard";
            const int Line = 198;
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            Sector sector = GameActions.GetSector(World, 13);
            sector.ActiveCeilingMove.Should().BeNull();

            GameActions.GiveItem(Player, KeyCard);
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RemoveItem(Player, KeyCard);
            GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorSlowSpeed, true);
            GameActions.CheckNoReactivateEntityUse(World, Player, Line, sector, sector.Ceiling);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 33 (D1) Door red key stay")]
        public void Action33()
        {
            const string KeyCard = "RedCard";
            const int Line = 197;
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            Sector sector = GameActions.GetSector(World, 15);
            sector.ActiveCeilingMove.Should().BeNull();

            GameActions.GiveItem(Player, KeyCard);
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RemoveItem(Player, KeyCard);
            GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorSlowSpeed, true);
            GameActions.CheckNoReactivateEntityUse(World, Player, Line, sector, sector.Ceiling);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 34 (D1) Door yellow key stay")]
        public void Action34()
        {
            const string KeyCard = "YellowCard";
            const int Line = 19;
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            Sector sector = GameActions.GetSector(World, 10);
            sector.ActiveCeilingMove.Should().BeNull();

            GameActions.GiveItem(Player, KeyCard);
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RemoveItem(Player, KeyCard);
            GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorSlowSpeed, true);
            GameActions.CheckNoReactivateEntityUse(World, Player, Line, sector, sector.Ceiling);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 35 (W1) Light level to 35")]
        public void Action35()
        {
            const int Line = 3587;
            Sector sector = GameActions.GetSectorByTag(World, 35);
            sector.LightLevel.Should().Be(256);
            sector.Floor.LightLevel.Should().Be(256);
            sector.Ceiling.LightLevel.Should().Be(256);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            World.Tick();

            sector.LightLevel.Should().Be(35);
            sector.Floor.LightLevel.Should().Be(35);
            sector.Ceiling.LightLevel.Should().Be(35);
            sector.SetLightLevel(256, World.Gametick);

            GameActions.GetLine(World, Line).SetActivated(false);
            GameActions.EntityCrossLine(World, Monster, Line).Should().BeTrue();
            sector.LightLevel.Should().Be(256);
            sector.Floor.LightLevel.Should().Be(256);
            sector.Ceiling.LightLevel.Should().Be(256);
        }

        [Fact(DisplayName = "Doom Action 35 (W1) Lower floor 8 above highest")]
        public void Action36()
        {
            const int Line = 1334;
            Sector sector = GameActions.GetSectorByTag(World, 36);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();

            GameActions.RunFloorLower(World, sector, 40, 32);
            GameActions.CheckNoReactivateEntityCross(World, Player, Line, sector, sector.Floor);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 37 (W1) Lower floor to lowest tx ty")]
        public void Action37()
        {
            const int Line = 1222;
            Sector sector = GameActions.GetSectorByTag(World, 37);
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLAT14");
            sector.SectorDamageSpecial.Should().BeNull();

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            GameActions.RunFloorLower(World, sector, -32, 8);
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR0_1");
            sector.SectorDamageSpecial.Should().NotBeNull();
            sector.SectorDamageSpecial!.Damage.Should().Be(5);

            GameActions.CheckNoReactivateEntityCross(World, Player, Line, sector, sector.Floor);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 38 (W1) Lower floor to lowest")]
        public void Action38()
        {
            const int Line = 1230;
            Sector sector = GameActions.GetSectorByTag(World, 38);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            GameActions.RunFloorLower(World, sector, 0, 8);

            GameActions.CheckNoReactivateEntityCross(World, Player, Line, sector, sector.Floor);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 39 (W1) Teleport")]
        public void Action39()
        {
            const int Line = 3405;
            const int TeleportLanding = 34;
            Sector sector = GameActions.GetSectorByTag(World, 97);

            GameActions.EntityCrossLine(World, Player, Line, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            Player.FrozenTics.Should().Be(TeleportSpecial.TeleportFreezeTicks);
            GameActions.RunTeleport(World, Player, sector, TeleportLanding);
            GameActions.TickWorld(World, Player.FrozenTics);
            Player.FrozenTics.Should().Be(0);

            GameActions.EntityCrossLine(World, Player, Line, moveOutofBounds: false).Should().BeTrue();
            GameActions.CheckNoTeleport(World, Player, sector, TeleportLanding);
        }

        [Fact(DisplayName = "Doom Action 40 (W1) Raise ceiling to highest")]
        public void Action40()
        {
            const int Line = 2276;
            Sector sector = GameActions.GetSectorByTag(World, 40);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            GameActions.RunCeilingRaise(World, sector, 128, 8);

            GameActions.CheckNoReactivateEntityCross(World, Player, Line, sector, sector.Floor);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 41 (S1) Ceiling lower to floor")]
        public void Action41()
        {
            const int Line = 2325;
            Sector sector = GameActions.GetSectorByTag(World, 41);

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunCeilingLower(World, sector, 0, 16);

            GameActions.CheckNoReactivateEntityUse(World, Player, Line, sector, sector.Ceiling);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 42 (SR) Close door")]
        public void Action42()
        {
            const int Line = 18;
            Sector sector = GameActions.GetSectorByTag(World, 42);

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunDoorClose(World, sector, 0, VanillaConstants.DoorSlowSpeed);

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            World.Tick();
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 43 (SR) Lower ceiling to floor")]
        public void Action43()
        {
            const int Line = 2336;
            Sector sector = GameActions.GetSectorByTag(World, 43);

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunCeilingLower(World, sector, 0, 8);

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            World.Tick();
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 44 (W1) Lower ceiling to 8 above floor")]
        public void Action44()
        {
            const int Line = 2283;
            Sector sector = GameActions.GetSectorByTag(World, 44);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            GameActions.RunCeilingLower(World, sector, 8, 8);

            GameActions.CheckNoReactivateEntityCross(World, Player, Line, sector, sector.Ceiling);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 45 (SR) Lower floor to highest")]
        public void Action45()
        {
            const int Line = 1289;
            Sector sector = GameActions.GetSectorByTag(World, 45);

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunFloorLower(World, sector, 64, 8);

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunFloorLower(World, sector, 64, 8);

            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 46 (GR) Door open")]
        public void Action46()
        {
            const int Line = 108;
            Sector sector = GameActions.GetSectorByTag(World, 46);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.SetEntityToLine(World, Player, Line, Player.Radius * 2).Should().BeTrue();
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
            GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorSlowSpeed, true);

            sector.Ceiling.SetZ(0);
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
            GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorSlowSpeed, true);
        }

        [Fact(DisplayName = "Doom Action 47 (G1) Raise floor to next tx")]
        public void Action47()
        {
            const int Line = 1136;
            Sector sector = GameActions.GetSectorByTag(World, 47);
            sector.ActiveFloorMove.Should().BeNull();
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLAT14");

            GameActions.SetEntityToLine(World, Player, Line, Player.Radius * 2).Should().BeTrue();
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR1_6");
            GameActions.RunFloorRaise(World, sector, 32, 4);

            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
            sector.ActiveFloorMove.Should().BeNull();
        }

        [Fact(DisplayName = "Doom Action 48 Scroll wall left")]
        public void Action48()
        {
            var line = GameActions.GetLine(World, 3291);
            line.Front.ScrollData.Should().NotBeNull();
            var special = World.SpecialManager.FindLineScrollSpecial(line)!;
            special.Should().NotBeNull();
            special.Speed.Should().Be(new Vec2D(1, 0));
        }

        [Fact(DisplayName = "Doom Action 49 (S1) Slow crusher ceiling")]
        public void Action49()
        {
            const int Line = 2370;
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 49);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunCrusherCeiling(World, sector, 8, true);
        }

        [Fact(DisplayName = "Doom Action 50 (S1) Close door")]
        public void Action50()
        {
            const int DoorLine = 259;
            GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 50);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunDoorClose(World, sector, 0, VanillaConstants.DoorSlowSpeed);

            GameActions.CheckNoReactivateEntityUse(World, Player, DoorLine, sector, sector.Ceiling);
            GameActions.CheckMonsterUseActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 51 (S) End level secret")]
        public void Action51()
        {
            bool exited = false;
            World.LevelExit += World_LevelExit;

            const int Line = 4161;
            GameActions.EntityUseLine(World, Player, Line);
            GameActions.TickWorld(World, 35);
            World.LevelExit -= World_LevelExit;

            void World_LevelExit(object? sender, LevelChangeEvent e)
            {
                e.Cancel = true;
                exited = true;
                e.ChangeType.Should().Be(LevelChangeType.SecretNext);
            }

            exited.Should().BeTrue();
        }

        [Fact(DisplayName = "Doom Action 52 (W) End level")]
        public void Action52()
        {
            bool exited = false;
            World.LevelExit += World_LevelExit;

            const int Line = 4129;
            GameActions.EntityCrossLine(World, Player, Line);
            GameActions.TickWorld(World, 35);
            World.LevelExit -= World_LevelExit;

            void World_LevelExit(object? sender, LevelChangeEvent e)
            {
                e.Cancel = true;
                exited = true;
                e.ChangeType.Should().Be(LevelChangeType.Next);
            }

            exited.Should().BeTrue();
        }

        [Fact(DisplayName = "Doom Action 124 (W) End level secret 2")]
        public void Action124()
        {
            bool exited = false;
            World.LevelExit += World_LevelExit;

            const int Line = 4135;
            GameActions.EntityCrossLine(World, Player, Line);
            GameActions.TickWorld(World, 35);
            World.LevelExit -= World_LevelExit;

            void World_LevelExit(object? sender, LevelChangeEvent e)
            {
                e.Cancel = true;
                exited = true;
                e.ChangeType.Should().Be(LevelChangeType.SecretNext);
            }

            exited.Should().BeTrue();
        }

        [Fact(DisplayName = "Doom Action 53 (W1) Start moving floor")]
        public void Action53()
        {
            const int Line = 1245;
            Sector sector = GameActions.GetSectorByTag(World, 53);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.EntityCrossLine(World, Player, Line, forceActivation: true);
            GameActions.RunPerpetualMovingFloor(World, sector, 0, 96, 8, VanillaConstants.LiftDelay);
            World.SpecialManager.RemoveSpecial(sector.ActiveFloorMove!);
            sector.Floor.SetZ(64);

            GameActions.CheckNoReactivateEntityCross(World, Player, Line, sector, sector.Floor);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 54 (W1) Stop moving floor")]
        public void Action54()
        {
            const int ActivateLine = 1245;
            const int DeactivateLine = 1215;
            Sector sector = GameActions.GetSectorByTag(World, 53);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.EntityCrossLine(World, Player, ActivateLine, forceActivation: true);

            sector.ActiveFloorMove.Should().NotBeNull();
            var special = sector.ActiveFloorMove!;

            // Start direction is randomized
            MoveDirection dir = special.MoveDirection;

            // Only move partially
            if (dir == MoveDirection.Down)
                GameActions.RunFloorLower(World, sector, 32, 8, isSpecDestroyed: false);
            else
                GameActions.RunFloorRaise(World, sector, 80, 8, isSpecDestroyed: false);

            // Monsters can't deactivate
            GameActions.EntityCrossLine(World, Monster, DeactivateLine);
            special.IsPaused.Should().BeFalse();

            GameActions.EntityCrossLine(World, Player, DeactivateLine, forceActivation: true);
            special.Should().NotBeNull();
            special.IsPaused.Should().BeTrue();

            World.SpecialManager.RemoveSpecial(sector.ActiveFloorMove!);
            sector.Floor.SetZ(64);
        }

        [Fact(DisplayName = "Doom Action 55 (W1) Crusher floor raise 8 below lowest ceiling")]
        public void Action55()
        {
            const int Line = 1509;
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 55);
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.RunCrusherFloor(World, sector, 8, false, destZ: 112, repeat: false);

            GameActions.CheckNoReactivateEntityCross(World, Player, Line, sector, sector.Floor);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 58 (W1) Raise floor 24")]
        public void Action58()
        {
            const int Line = 1466;
            Sector sector = GameActions.GetSectorByTag(World, 58);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();

            GameActions.RunFloorRaise(World, sector, 32, 4);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 59 (W1) Raise floor 24 tx ty")]
        public void Action59()
        {
            const int Line = 1462;
            Sector sector = GameActions.GetSectorByTag(World, 59);
            sector.ActiveFloorMove.Should().BeNull();
            sector.SectorDamageSpecial.Should().BeNull();
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLAT14");

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR1_6");
            sector.SectorDamageSpecial.Should().NotBeNull();
            sector.SectorDamageSpecial!.Damage.Should().Be(5);

            GameActions.RunFloorRaise(World, sector, 0, 8);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 60 (SR) Lower floor to lowest")]
        public void Action60()
        {
            const int Line = 1502;
            Sector sector = GameActions.GetSectorByTag(World, 60);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunFloorLower(World, sector, 0, 8);
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
        }

        [Fact(DisplayName = "Doom Action 61 (SR) Door open")]
        public void Action61()
        {
            const int Line = 278;
            Sector sector = GameActions.GetSectorByTag(World, 61);
            sector.ActiveCeilingMove.Should().BeNull();

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorSlowSpeed, true);
            }
        }

        [Fact(DisplayName = "Doom Action 62 (SR) Lift")]
        public void Action62()
        {
            const int Line = 2732;
            Sector sector = GameActions.GetSectorByTag(World, 62);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                GameActions.RunLift(World, sector, 64, 0, VanillaConstants.LiftFastSpeed, VanillaConstants.LiftDelay);
            }
        }

        [Fact(DisplayName = "Doom Action 63 (SR) Door open close")]
        public void Action63()
        {
            const int Line = 281;
            Sector sector = GameActions.GetSectorByTag(World, 63);
            sector.ActiveCeilingMove.Should().BeNull();

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorSlowSpeed);
            }
        }

        [Fact(DisplayName = "Doom Action 64 (SR) Raise floor to lowest ceiling")]
        public void Action64()
        {
            const int Line = 1454;
            Sector sector = GameActions.GetSectorByTag(World, 64);
            sector.ActiveFloorMove.Should().BeNull();

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                GameActions.RunFloorRaise(World, sector, 64, 8);
            }
        }

        [Fact(DisplayName = "Doom Action 65 (SR) Crusher floor raise 8 below lowest ceiling")]
        public void Action65()
        {
            const int Line = 1526;
            Sector sector = GameActions.GetSectorByTag(World, 65);
            sector.ActiveFloorMove.Should().BeNull();

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, Line);
                GameActions.RunCrusherFloor(World, sector, 8, false, destZ: 112, repeat: false);
            }
        }

        [Fact(DisplayName = "Doom Action 66 (SR) Raise floor 24 tx")]
        public void Action66()
        {
            const int Line = 1564;
            Sector sector = GameActions.GetSectorByTag(World, 66);
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLAT14");

            int destZ = -24;
            for (int i = 0; i < 4; i++)
            {
                destZ += 24;
                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR1_6");
                GameActions.RunFloorRaise(World, sector, destZ, 4);
            }
        }

        [Fact(DisplayName = "Doom Action 67 (SR) Raise floor 32 tx")]
        public void Action67()
        {
            const int Line = 1574;
            Sector sector = GameActions.GetSectorByTag(World, 67);
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLAT14");

            int destZ = 8;
            for (int i = 0; i < 4; i++)
            {
                destZ += 32;
                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();

                GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR1_6");
                GameActions.RunFloorRaise(World, sector, destZ, 4);
            }
        }

        [Fact(DisplayName = "Doom Action 68 (SR) Raise floor to next higher tx")]
        public void Action68()
        {
            const int Line = 1600;
            Sector sector = GameActions.GetSectorByTag(World, 68);
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLAT14");

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR1_6");
            GameActions.RunFloorRaise(World, sector, 32, 4);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR1_6");
                GameActions.RunFloorRaise(World, sector, 64, 4);
            }
        }

        [Fact(DisplayName = "Doom Action 69 (SR) Raise floor to next higher")]
        public void Action69()
        {
            const int Line = 1633;
            Sector sector = GameActions.GetSectorByTag(World, 69);

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunFloorRaise(World, sector, 64, 8);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                GameActions.RunFloorRaise(World, sector, 96, 8);
            }
        }

        [Fact(DisplayName = "Doom Action 70 (SR) Lower floor 8 above highest")]
        public void Action70()
        {
            const int Line = 1670;
            Sector sector = GameActions.GetSectorByTag(World, 70);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                GameActions.RunFloorLower(World, sector, 72, 32);
            }
        }

        [Fact(DisplayName = "Doom Action 71 (S1) Lower floor 8 above highest")]
        public void Action71()
        {
            const int Line = 1703;
            Sector sector = GameActions.GetSectorByTag(World, 71);

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunFloorLower(World, sector, 72, 32);

            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        }

        [Fact(DisplayName = "Doom Action 72 (WR) Lower ceiling 8 above floor")]
        public void Action72()
        {
            const int Line = 2295;
            Sector sector = GameActions.GetSectorByTag(World, 72);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.RunCeilingLower(World, sector, 8, 8);
                sector.Ceiling.SetZ(120);
            }
        }

        [Fact(DisplayName = "Doom Action 73 (WR) Slow crusher ceiling")]
        public void Action73()
        {
            const int Line = 3169;
            GameActions.EntityCrossLine(World, Player, Line, forceActivation: true).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 73);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunCrusherCeiling(World, sector, 8, slowDownOnCrush: true);

            GameActions.ForceStopSectorSpecial(World, sector);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Ceiling, false);
            sector.Ceiling.SetZ(120);
        }

        [Fact(DisplayName = "Doom Action 74 (WR) Stop crusher ceiling")]
        public void Action74()
        {
            const int ActivateLine = 3169;
            const int DeactivateLine = 3193;
            GameActions.EntityCrossLine(World, Player, ActivateLine, forceActivation: true).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 73);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunCrusherCeiling(World, sector, 8, slowDownOnCrush: true);
            GameActions.EntityCrossLine(World, Player, DeactivateLine).Should().BeTrue();

            // The crusher is paused so the ActiveCeilingMove should exist indefinitely.
            sector.ActiveCeilingMove.Should().NotBeNull();
            sector.ActiveCeilingMove!.IsPaused.Should().BeTrue();
            GameActions.ForceStopSectorSpecial(World, sector);
            sector.Ceiling.SetZ(120);
        }

        [Fact(DisplayName = "Doom Action 75 (WR) Close door")]
        public void Action75()
        {
            const int DoorLine = 300;
            Sector sector = GameActions.GetSectorByTag(World, 75);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, DoorLine).Should().BeTrue();
                GameActions.RunDoorClose(World, sector, 0, VanillaConstants.DoorSlowSpeed, false);
                sector.Ceiling.SetZ(120);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 76 (WR) Close door open 30 seconds")]
        public void Action76()
        {
            const int DoorLine = 339;
            Sector sector = GameActions.GetSectorByTag(World, 76);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, DoorLine).Should().BeTrue();
                GameActions.RunDoorClose(World, sector, 0, VanillaConstants.DoorSlowSpeed, checkCeilingMove: false);
                GameActions.TickWorld(World, (int)Constants.TicksPerSecond * 30);
                GameActions.RunDoorOpen(World, sector, 120, VanillaConstants.DoorSlowSpeed, false);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 77 (WR) Fast crusher ceiling")]
        public void Action77()
        {
            const int Line = 3218;
            Sector sector = GameActions.GetSectorByTag(World, 77);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.RunCrusherCeiling(World, sector, 16, slowDownOnCrush: false);
                GameActions.ForceStopSectorSpecial(World, sector);
                sector.Ceiling.SetZ(128);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 79 (WR) Light level to 35")]
        public void Action79()
        {
            const int Line = 3599;
            Sector sector = GameActions.GetSectorByTag(World, 79);

            for (int i = 0; i < 2; i++)
            {
                sector.LightLevel.Should().Be(256);
                sector.Floor.LightLevel.Should().Be(256);
                sector.Ceiling.LightLevel.Should().Be(256);

                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();

                sector.LightLevel.Should().Be(35);
                sector.Floor.LightLevel.Should().Be(35);
                sector.Ceiling.LightLevel.Should().Be(35);
                sector.SetLightLevel(256, World.Gametick);
            }

            GameActions.EntityCrossLine(World, Monster, Line).Should().BeTrue();
            sector.LightLevel.Should().Be(256);
            sector.Floor.LightLevel.Should().Be(256);
        }

        [Fact(DisplayName = "Doom Action 80 (WR) Light level to 35")]
        public void Action80()
        {
            const int Line = 3610;
            Sector sector = GameActions.GetSectorByTag(World, 80);

            for (int i = 0; i < 2; i++)
            {
                sector.LightLevel.Should().Be(96);
                sector.Floor.LightLevel.Should().Be(96);
                sector.Ceiling.LightLevel.Should().Be(96);

                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                World.Tick();

                sector.LightLevel.Should().Be(128);
                sector.Floor.LightLevel.Should().Be(128);
                sector.Ceiling.LightLevel.Should().Be(128);
                sector.SetLightLevel(96, World.Gametick);
            }

            GameActions.EntityCrossLine(World, Monster, Line).Should().BeTrue();
            sector.LightLevel.Should().Be(96);
            sector.Floor.LightLevel.Should().Be(96);
            sector.Ceiling.LightLevel.Should().Be(96);
        }

        [Fact(DisplayName = "Doom Action 81 (WR) Light level to 255")]
        public void Action81()
        {
            const int Line = 3621;
            Sector sector = GameActions.GetSectorByTag(World, 81);

            for (int i = 0; i < 2; i++)
            {
                sector.LightLevel.Should().Be(96);
                sector.Floor.LightLevel.Should().Be(96);
                sector.Ceiling.LightLevel.Should().Be(96);

                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                World.Tick();

                sector.LightLevel.Should().Be(255);
                sector.Floor.LightLevel.Should().Be(255);
                sector.Ceiling.LightLevel.Should().Be(255);
                sector.SetLightLevel(96, World.Gametick);
            }

            GameActions.EntityCrossLine(World, Monster, Line).Should().BeTrue();
            sector.LightLevel.Should().Be(96);
            sector.Floor.LightLevel.Should().Be(96);
            sector.Ceiling.LightLevel.Should().Be(96);
        }

        [Fact(DisplayName = "Doom Action 82 (WR) Lower floor to lowest")]
        public void Action82()
        {
            const int Line = 1731;
            Sector sector = GameActions.GetSectorByTag(World, 82);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.RunFloorLower(World, sector, 0, 8);
                sector.Floor.SetZ(64);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 83 (WR) Lower floor to highest")]
        public void Action83()
        {
            const int Line = 1753;
            Sector sector = GameActions.GetSectorByTag(World, 83);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.RunFloorLower(World, sector, 64, 8);
                sector.Floor.SetZ(96);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 84 (WR) Lower floor to lowest tx")]
        public void Action84()
        {
            const int Line = 1775;
            Sector sector = GameActions.GetSectorByTag(World, 84);
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLAT14");

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.RunFloorLower(World, sector, -32, 8);
                GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR0_1");
                sector.Floor.SetZ(64);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 86 (WR) Open door")]
        public void Action86()
        {
            const int DoorLine = 323;
            Sector sector = GameActions.GetSectorByTag(World, 86);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, DoorLine).Should().BeTrue();
                GameActions.RunDoorOpenStay(World, sector, 128, VanillaConstants.DoorSlowSpeed);
                sector.Ceiling.SetZ(0);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 88 (WR) Start moving floor")]
        public void Action87()
        {
            const int Line = 1801;
            Sector sector = GameActions.GetSectorByTag(World, 87);
            sector.ActiveFloorMove.Should().BeNull();

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line);
                GameActions.RunPerpetualMovingFloor(World, sector, 0, 96, 8, VanillaConstants.LiftDelay);
                World.SpecialManager.RemoveSpecial(sector.ActiveFloorMove!);
                sector.Floor.SetZ(64);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 89 (WR) Stop moving floor")]
        public void Action89()
        {
            const int ActivateLine = 1801;
            const int DeactivateLine = 1799;
            Sector sector = GameActions.GetSectorByTag(World, 87);
            sector.ActiveFloorMove.Should().BeNull();

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, ActivateLine);

                sector.ActiveFloorMove.Should().NotBeNull();
                var special = sector.ActiveFloorMove!;

                // Start direction is randomized
                MoveDirection dir = special.MoveDirection;

                // Only move partially
                if (dir == MoveDirection.Down)
                    GameActions.RunFloorLower(World, sector, 32, 8, isSpecDestroyed: false);
                else
                    GameActions.RunFloorRaise(World, sector, 80, 8, isSpecDestroyed: false);

                // Monsters can't deactivate
                GameActions.EntityCrossLine(World, Monster, DeactivateLine);
                special.IsPaused.Should().BeFalse();

                GameActions.EntityCrossLine(World, Player, DeactivateLine);
                special.Should().NotBeNull();
                special.IsPaused.Should().BeTrue();

                World.SpecialManager.RemoveSpecial(sector.ActiveFloorMove!);
                sector.Floor.SetZ(64);
            }
        }

        [Fact(DisplayName = "Doom Action 88 (WR) Lift")]
        public void Action88()
        {
            const int Line = 2628;
            Sector sector = GameActions.GetSectorByTag(World, 88);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.RunLift(World, sector, 64, 0, VanillaConstants.LiftFastSpeed, VanillaConstants.LiftDelay);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, true);
        }

        [Fact(DisplayName = "Doom Action 90 (WR) Door open close")]
        public void Action90()
        {
            const int Line = 301;
            Sector sector = GameActions.GetSectorByTag(World, 90);
            sector.ActiveCeilingMove.Should().BeNull();

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorSlowSpeed);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 91 (WR) Raise floor to lowest ceiling")]
        public void Action91()
        {
            const int Line = 1836;
            Sector sector = GameActions.GetSectorByTag(World, 91);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.RunFloorRaise(World, sector, 64, 8);
                sector.Floor.SetZ(8);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 92 (WR) Raise floor 24")]
        public void Action92()
        {
            const int Line = 1847;
            Sector sector = GameActions.GetSectorByTag(World, 92);

            double destZ = 8;
            for (int i = 0; i < 4; i++)
            {
                destZ += 24;
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.RunFloorRaise(World, sector, destZ, 8);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 93 (WR) Raise floor 24 tx ty")]
        public void Action93()
        {
            const int Line = 1843;
            Sector sector = GameActions.GetSectorByTag(World, 93);
            GameActions.CheckPlaneTexture(World, sector.Floor, "FLAT14");
            sector.SectorDamageSpecial.Should().BeNull();

            double destZ = -24;
            for (int i = 0; i < 4; i++)
            {
                destZ += 24;
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR1_6");
                sector.SectorDamageSpecial.Should().NotBeNull();
                sector.SectorDamageSpecial!.Damage.Should().Be(5);
                GameActions.RunFloorRaise(World, sector, destZ, 8);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 94 (WR) Fast crusher ceiling")]
        public void Action94()
        {
            const int Line = 1874;
            Sector sector = GameActions.GetSectorByTag(World, 94);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.RunCrusherFloor(World, sector, 8, slowDownOnCrush: false, destZ: 112, repeat: false);
                sector.Floor.SetZ(8);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 95 (WR) Raise floor to next higher")]
        public void Action95()
        {
            const int Line = 1900;
            Sector sector = GameActions.GetSectorByTag(World, 95);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            GameActions.RunFloorRaise(World, sector, 32, 4);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            GameActions.RunFloorRaise(World, sector, 64, 4);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 96 (WR) Raise floor by shortest lower")]
        public void Action96()
        {
            World.Config.Compatibility.VanillaShortestTexture.Set(false);
            const int Line = 1921;

            Sector[] sectors = new[]
            {
                GameActions.GetSector(World, 484),
                GameActions.GetSector(World, 486),
                GameActions.GetSector(World, 485),
            };

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line, forceActivation: true).Should().BeTrue();
                sectors.ForEach(x => x.ActiveFloorMove.Should().NotBeNull());

                GameActions.RunSectorPlaneSpecials(World, sectors);

                sectors[0].Floor.Z.Should().Be(24);
                sectors[1].Floor.Z.Should().Be(32);
                sectors[2].Floor.Z.Should().Be(72);
                sectors.ForEach(x => x.Floor.SetZ(0));
            }
        }

        [Fact(DisplayName = "Doom Action 97 (WR) Teleport")]
        public void Action97()
        {
            const int Line = 3499;
            Sector sector = GameActions.GetSectorByTag(World, 97);
            Player.FrozenTics = 0;

            for (int i = 0; i < 2; i++)
            {
                Player.FrozenTics.Should().Be(0);
                GameActions.EntityCrossLine(World, Player, Line, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
                Player.FrozenTics.Should().Be(TeleportSpecial.TeleportFreezeTicks);
                GameActions.RunTeleport(World, Player, sector, 34);
                GameActions.TickWorld(World, Player.FrozenTics);
                Player.FrozenTics.Should().Be(0);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 98 (WR) Lower floor to 8 above highest")]
        public void Action98()
        {
            const int Line = 1957;
            Sector sector = GameActions.GetSectorByTag(World, 98);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.RunFloorLower(World, sector, 72, 32);
                sector.Floor.SetZ(96);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 99 (SR) Door blue key")]
        public void Action99()
        {
            const string KeyCard = "BlueCard";
            const int Line = 366;
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            Sector sector = GameActions.GetSectorByTag(World, 99);
            sector.ActiveCeilingMove.Should().BeNull();

            GameActions.GiveItem(Player, KeyCard);
            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorFastSpeed, true);
                sector.Ceiling.SetZ(0);
            }

            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
            GameActions.RemoveItem(Player, KeyCard);
        }


        [Fact(DisplayName = "Doom Action 100 (W1) Raise stairs fast 16")]
        public void Action100()
        {
            const int Line = 2896;
            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            int[] stairIds = new int[] { 657, 658, 659, 655 };
            GameActions.RunStairs(World, stairIds, 0, 16, 2);

            var sector = GameActions.GetSector(World, 657);
            GameActions.CheckNoReactivateEntityCross(World, Player, Line, sector, sector.Floor);
            GameActions.CheckMonsterCrossActivation(World, Monster, 2896, sector, sector.Floor, false);
        }


        [Fact(DisplayName = "Doom Action 101 (S1) Raise floor to lowest ceiling")]
        public void Action101()
        {
            const int Line = 2002;
            Sector sector = GameActions.GetSectorByTag(World, 101);

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunFloorRaise(World, sector, 64, 8);

            GameActions.CheckNoReactivateEntityUse(World, Player, Line, sector, sector.Floor);
        }

        [Fact(DisplayName = "Doom Action 102 (S1) Raise floor to highest")]
        public void Action102()
        {
            const int Line = 2017;
            Sector sector = GameActions.GetSectorByTag(World, 102);

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunFloorLower(World, sector, 64, 8);

            GameActions.CheckNoReactivateEntityUse(World, Player, Line, sector, sector.Floor);
        }

        [Fact(DisplayName = "Doom Action 2 (S1) Open door")]
        public void Action103()
        {
            const int DoorLine = 383;
            GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 103);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunDoorOpenStay(World, sector, 128, VanillaConstants.DoorSlowSpeed);

            GameActions.CheckNoReactivateEntityCross(World, Player, DoorLine, sector, sector.Ceiling);
            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 104 (W1) Light level match dimmest adjacent")]
        public void Action104()
        {
            const int Line = 3632;
            Sector sector = GameActions.GetSectorByTag(World, 104);
            sector.LightLevel.Should().Be(256);
            sector.Floor.LightLevel.Should().Be(256);
            sector.Ceiling.LightLevel.Should().Be(256);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            World.Tick();

            sector.LightLevel.Should().Be(96);
            sector.Floor.LightLevel.Should().Be(96);
            sector.Ceiling.LightLevel.Should().Be(96);
            sector.SetLightLevel(256, World.Gametick);

            GameActions.GetLine(World, Line).SetActivated(false);
            GameActions.EntityCrossLine(World, Monster, Line).Should().BeTrue();
            sector.LightLevel.Should().Be(256);
            sector.Floor.LightLevel.Should().Be(256);
            sector.Ceiling.LightLevel.Should().Be(256);
        }

        [Fact(DisplayName = "Doom Action 105 (WR) Door open and close fast")]
        public void Action105()
        {
            const int DoorLine = 404;
            Sector sector = GameActions.GetSectorByTag(World, 105);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, DoorLine).Should().BeTrue();
                GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorFastSpeed);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 106 (WR) Door open")]
        public void Action106()
        {
            const int DoorLine = 393;
            Sector sector = GameActions.GetSectorByTag(World, 106);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, DoorLine).Should().BeTrue();
                GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorFastSpeed, true);
                sector.Ceiling.SetZ(0);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 107 (WR) Door close")]
        public void Action107()
        {
            const int DoorLine = 423;
            Sector sector = GameActions.GetSectorByTag(World, 107);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, DoorLine).Should().BeTrue();
                GameActions.RunDoorClose(World, sector, 0, VanillaConstants.DoorFastSpeed, true);
                sector.Ceiling.SetZ(120);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 108 (W1) Open door close")]
        public void Action108()
        {
            const int DoorLine = 462;
            Sector sector = GameActions.GetSectorByTag(World, 108);

            GameActions.EntityCrossLine(World, Player, DoorLine).Should().BeTrue();
            GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorFastSpeed);

            GameActions.CheckNoReactivateEntityCross(World, Player, DoorLine, sector, sector.Ceiling);
            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 109 (W1) Door open")]
        public void Action109()
        {
            const int DoorLine = 446;
            Sector sector = GameActions.GetSectorByTag(World, 109);

            GameActions.EntityCrossLine(World, Player, DoorLine).Should().BeTrue();
            GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorFastSpeed, true);

            GameActions.CheckNoReactivateEntityCross(World, Player, DoorLine, sector, sector.Ceiling);
            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 109 (W1) Door close")]
        public void Action110()
        {
            const int DoorLine = 424;
            Sector sector = GameActions.GetSectorByTag(World, 110);

            GameActions.EntityCrossLine(World, Player, DoorLine).Should().BeTrue();
            GameActions.RunDoorClose(World, sector, 0, VanillaConstants.DoorFastSpeed, true);

            GameActions.CheckNoReactivateEntityCross(World, Player, DoorLine, sector, sector.Ceiling);
            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 111 (S1) Door open and close fast")]
        public void Action111()
        {
            const int DoorLine = 514;
            Sector sector = GameActions.GetSectorByTag(World, 111);

            GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
            GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorFastSpeed);

            GameActions.CheckNoReactivateEntityUse(World, Player, DoorLine, sector, sector.Ceiling);
        }

        [Fact(DisplayName = "Doom Action 112 (S1) Door open")]
        public void Action112()
        {
            const int DoorLine = 493;
            Sector sector = GameActions.GetSectorByTag(World, 112);

            GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
            GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorFastSpeed, true);

            GameActions.CheckNoReactivateEntityUse(World, Player, DoorLine, sector, sector.Ceiling);
        }

        [Fact(DisplayName = "Doom Action 113 (S1) Door close")]
        public void Action113()
        {
            const int DoorLine = 543;
            Sector sector = GameActions.GetSectorByTag(World, 113);

            GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
            GameActions.RunDoorClose(World, sector, 0, VanillaConstants.DoorFastSpeed, true);

            GameActions.CheckNoReactivateEntityUse(World, Player, DoorLine, sector, sector.Ceiling);
        }

        [Fact(DisplayName = "Doom Action 114 (SR) Door open close")]
        public void Action114()
        {
            const int DoorLine = 561;
            Sector sector = GameActions.GetSectorByTag(World, 114);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
                GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorFastSpeed);
                sector.Ceiling.SetZ(0);
            }
        }

        [Fact(DisplayName = "Doom Action 115 (SR) Door open")]
        public void Action115()
        {
            const int DoorLine = 564;
            Sector sector = GameActions.GetSectorByTag(World, 115);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
                GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorFastSpeed, true);
                sector.Ceiling.SetZ(0);
            }
        }

        [Fact(DisplayName = "Doom Action 116 (SR) Door close")]
        public void Action116()
        {
            const int DoorLine = 586;
            Sector sector = GameActions.GetSectorByTag(World, 116);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
                GameActions.RunDoorClose(World, sector, 0, VanillaConstants.DoorFastSpeed);
                sector.Ceiling.SetZ(120);
            }
        }

        [Fact(DisplayName = "Doom Action 117 (DR) Door open close")]
        public void Action117()
        {
            const int DoorLine = 622;
            Sector sector = GameActions.GetSector(World, 155);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
                GameActions.RunDoorOpenClose(World, sector, 0, 128, VanillaConstants.DoorFastSpeed);
            }

            GameActions.CheckMonsterUseActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 118 (D1) Door open")]
        public void Action118()
        {
            const int DoorLine = 630;
            Sector sector = GameActions.GetSector(World, 157);

            GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
            GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorFastSpeed, true);

            GameActions.CheckNoReactivateEntityUse(World, Player, DoorLine, sector, sector.Ceiling);
            GameActions.CheckMonsterUseActivation(World, Monster, DoorLine, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 119 (W1) Raise floor to next")]
        public void Action119()
        {
            const int Line = 2045;
            Sector sector = GameActions.GetSectorByTag(World, 119);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            GameActions.RunFloorRaise(World, sector, 64, 8);

            GameActions.CheckNoReactivateEntityCross(World, Player, Line, sector, sector.Floor);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }


        [Fact(DisplayName = "Doom Action 120 (WR) Lift fast")]
        public void Action120()
        {
            const int Line = 2643;
            Sector sector = GameActions.GetSectorByTag(World, 120);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.RunLift(World, sector, 64, 0, 64, VanillaConstants.LiftDelay);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 121 (W1) Lift fast")]
        public void Action121()
        {
            const int Line = 2657;
            Sector sector = GameActions.GetSectorByTag(World, 121);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            GameActions.RunLift(World, sector, 64, 0, 64, VanillaConstants.LiftDelay);
            GameActions.CheckNoReactivateEntityCross(World, Player, Line, sector, sector.Floor);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 122 (S1) Lift fast")]
        public void Action122()
        {
            const int Line = 2744;
            Sector sector = GameActions.GetSectorByTag(World, 122);

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunLift(World, sector, 64, 0, 64, VanillaConstants.LiftDelay);
            GameActions.CheckNoReactivateEntityUse(World, Player, Line, sector, sector.Floor);
        }

        [Fact(DisplayName = "Doom Action 123 (SR) Lift fast")]
        public void Action123()
        {
            const int Line = 2733;
            Sector sector = GameActions.GetSectorByTag(World, 123);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                GameActions.RunLift(World, sector, 64, 0, 64, VanillaConstants.LiftDelay);
            }
        }

        [Fact(DisplayName = "Doom Action 125 (W1) Teleport monster")]
        public void Action125()
        {
            const int Line = 3508;
            const int TeleportLanding = 34;
            Sector sector = GameActions.GetSectorByTag(World, 97);

            Monster.FrozenTics = 0;
            Monster.FrozenTics.Should().Be(0);
            GameActions.EntityCrossLine(World, Monster, Line, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            GameActions.RunTeleport(World, Monster, sector, TeleportLanding);
            Monster.FrozenTics.Should().Be(0);

            GameActions.EntityCrossLine(World, Monster, Line, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            GameActions.CheckNoTeleport(World, Monster, sector, TeleportLanding);
            Monster.FrozenTics.Should().Be(0);

            GameActions.EntityCrossLine(World, Player, Line, forceActivation: true, moveOutofBounds: false).Should().BeTrue();
            GameActions.CheckNoTeleport(World, Player, sector, TeleportLanding);

            Monster.FrozenTics = int.MaxValue;
        }

        [Fact(DisplayName = "Doom Action 126 (WR) Teleport monster")]
        public void Action126()
        {
            const int Line = 3516;
            const int TeleportLanding = 34;
            Sector sector = GameActions.GetSectorByTag(World, 97);
            Monster.FrozenTics = 0;

            for (int i = 0; i < 2; i++)
            {
                Monster.FrozenTics.Should().Be(0);
                GameActions.EntityCrossLine(World, Monster, Line, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
                GameActions.RunTeleport(World, Monster, sector, TeleportLanding);
                Monster.FrozenTics.Should().Be(0);
            }

            GameActions.EntityCrossLine(World, Player, Line, forceActivation: true, moveOutofBounds: false).Should().BeTrue();
            GameActions.CheckNoTeleport(World, Player, sector, TeleportLanding);

            Monster.FrozenTics = int.MaxValue;
        }

        [Fact(DisplayName = "Doom Action 127 (S1) Raise stairs fast 16")]
        public void Action127()
        {
            const int Line = 2967;
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            int[] stairIds = new int[] { 669, 670, 671, 667 };
            GameActions.RunStairs(World, stairIds, 8, 16, VanillaConstants.StairFastSpeed);

            var sector = GameActions.GetSector(World, 669);
            GameActions.CheckNoReactivateEntityUse(World, Player, Line, sector, sector.Floor);
        }

        [Fact(DisplayName = "Doom Action 128 (WR) Raise floor to next higher")]
        public void Action128()
        {
            const int Line = 2067;
            Sector sector = GameActions.GetSectorByTag(World, 128);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.RunFloorRaise(World, sector, 64, 8);
                sector.Floor.SetZ(8);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 129 (WR) Raise floor to next higher")]
        public void Action129()
        {
            const int Line = 2089;
            Sector sector = GameActions.GetSectorByTag(World, 129);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
                GameActions.RunFloorRaise(World, sector, 64, 32);
                sector.Floor.SetZ(8);
            }

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 130 (W1) Raise floor to next higher")]
        public void Action130()
        {
            const int Line = 2110;
            Sector sector = GameActions.GetSectorByTag(World, 130);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            GameActions.RunFloorRaise(World, sector, 64, 32);

            GameActions.CheckNoReactivateEntityCross(World, Player, Line, sector, sector.Floor);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 131 (S1) Raise floor to next higher")]
        public void Action131()
        {
            const int Line = 2138;
            Sector sector = GameActions.GetSectorByTag(World, 131);

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunFloorRaise(World, sector, 64, 32);

            GameActions.CheckNoReactivateEntityUse(World, Player, Line, sector, sector.Floor);
        }

        [Fact(DisplayName = "Doom Action 132 (SR) Raise floor to next higher")]
        public void Action132()
        {
            const int Line = 2171;
            Sector sector = GameActions.GetSectorByTag(World, 132);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                GameActions.RunFloorRaise(World, sector, 64, 32);
                sector.Floor.SetZ(8);
            }
        }

        [Fact(DisplayName = "Doom Action 133 (S1) Door blue key")]
        public void Action133()
        {
            const string KeyCard = "BlueCard";
            const int Line = 665;
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            Sector sector = GameActions.GetSectorByTag(World, 133);
            sector.ActiveCeilingMove.Should().BeNull();

            GameActions.GiveItem(Player, KeyCard);
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorFastSpeed, true);

            GameActions.CheckNoReactivateEntityUse(World, Player, Line, sector, sector.Ceiling);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
            GameActions.RemoveItem(Player, KeyCard);
        }

        [Fact(DisplayName = "Doom Action 134 (SR) Door red key")]
        public void Action134()
        {
            const string KeyCard = "RedCard";
            const int Line = 688;
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            Sector sector = GameActions.GetSectorByTag(World, 134);
            sector.ActiveCeilingMove.Should().BeNull();

            GameActions.GiveItem(Player, KeyCard);
            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorFastSpeed, true);
                sector.Ceiling.SetZ(0);
            }

            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
            GameActions.RemoveItem(Player, KeyCard);
        }

        [Fact(DisplayName = "Doom Action 135 (S1) Door red key")]
        public void Action135()
        {
            const string KeyCard = "RedCard";
            const int Line = 711;
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            Sector sector = GameActions.GetSectorByTag(World, 135);
            sector.ActiveCeilingMove.Should().BeNull();

            GameActions.GiveItem(Player, KeyCard);
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorFastSpeed, true);

            GameActions.CheckNoReactivateEntityUse(World, Player, Line, sector, sector.Ceiling);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
            GameActions.RemoveItem(Player, KeyCard);
        }

        [Fact(DisplayName = "Doom Action 134 (SR) Door yellow key")]
        public void Action136()
        {
            const string KeyCard = "YellowCard";
            const int Line = 734;
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            Sector sector = GameActions.GetSectorByTag(World, 136);
            sector.ActiveCeilingMove.Should().BeNull();

            GameActions.GiveItem(Player, KeyCard);
            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorFastSpeed, true);
                sector.Ceiling.SetZ(0);
            }

            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
            GameActions.RemoveItem(Player, KeyCard);
        }

        [Fact(DisplayName = "Doom Action 137 (S1) Door yellow key")]
        public void Action137()
        {
            const string KeyCard = "YellowCard";
            const int Line = 757;
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            Sector sector = GameActions.GetSectorByTag(World, 137);
            sector.ActiveCeilingMove.Should().BeNull();

            GameActions.GiveItem(Player, KeyCard);
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunDoorOpen(World, sector, 128, VanillaConstants.DoorFastSpeed, true);

            GameActions.CheckNoReactivateEntityUse(World, Player, Line, sector, sector.Ceiling);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
            GameActions.RemoveItem(Player, KeyCard);
        }

        [Fact(DisplayName = "Doom Action 138 (SR) Light level to 255")]
        public void Action138()
        {
            const int Line = 3671;
            Sector sector = GameActions.GetSectorByTag(World, 138);

            for (int i = 0; i < 2; i++)
            {
                sector.LightLevel.Should().Be(96);
                sector.Floor.LightLevel.Should().Be(96);
                sector.Ceiling.LightLevel.Should().Be(96);

                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                World.Tick();

                sector.LightLevel.Should().Be(255);
                sector.Floor.LightLevel.Should().Be(255);
                sector.Ceiling.LightLevel.Should().Be(255);
                sector.SetLightLevel(96, World.Gametick);
            }

            GameActions.EntityUseLine(World, Monster, Line).Should().BeFalse();
            sector.LightLevel.Should().Be(96);
            sector.Floor.LightLevel.Should().Be(96);
        }

        [Fact(DisplayName = "Doom Action 139 (SR) Light level to 35")]
        public void Action139()
        {
            const int Line = 3664;
            Sector sector = GameActions.GetSectorByTag(World, 139);

            for (int i = 0; i < 2; i++)
            {
                sector.LightLevel.Should().Be(256);
                sector.Floor.LightLevel.Should().Be(256);
                sector.Ceiling.LightLevel.Should().Be(256);

                GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
                World.Tick();

                sector.LightLevel.Should().Be(35);
                sector.Floor.LightLevel.Should().Be(35);
                sector.Ceiling.LightLevel.Should().Be(35);
                sector.SetLightLevel(256, World.Gametick);
            }

            GameActions.EntityUseLine(World, Monster, Line).Should().BeFalse();
            sector.LightLevel.Should().Be(256);
            sector.Floor.LightLevel.Should().Be(256);
        }

        [Fact(DisplayName = "Doom Action 140 (S1) Raise floor 512")]
        public void Action140()
        {
            const int Line = 2199;
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 140);

            GameActions.RunFloorRaise(World, sector, 0, VanillaConstants.SectorSlowSpeed);

            GameActions.CheckNoReactivateEntityUse(World, Player, Line, sector, sector.Floor);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
        }

        [Fact(DisplayName = "Doom Action 141 (W1) Quiet crusher ceiling")]
        public void Action141()
        {
            const int Line = 3240;
            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 141);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunCrusherCeiling(World, sector, 8, true);
        }

        [Fact(DisplayName = "Doom Sector Type 1 - Lights blink random")]
        public void SectorType1()
        {
            var sector = GameActions.GetSector(World, 779);
            ISpecial? special = World.SpecialManager.FindSpecialBySector(sector);
            special.Should().NotBeNull();

            var light = special as LightFlickerDoomSpecial;
            light.Should().NotBeNull();
            light!.MaxBright.Should().Be(256);
            light!.MinBright.Should().Be(128);
        }

        [Fact(DisplayName = "Doom Sector Type 2 - Lights strobe")]
        public void SectorType2()
        {
            var sector = GameActions.GetSector(World, 780);
            ISpecial? special = World.SpecialManager.FindSpecialBySector(sector);
            special.Should().NotBeNull();

            var light = special as LightStrobeSpecial;
            light.Should().NotBeNull();
            light!.MaxBright.Should().Be(256);
            light!.MinBright.Should().Be(128);
            light!.BrightTicks.Should().Be(5);
            light!.DarkTicks.Should().Be(15);
        }

        [Fact(DisplayName = "Doom Sector Type 3 - Lights strobe")]
        public void SectorType3()
        {
            var sector = GameActions.GetSector(World, 781);
            ISpecial? special = World.SpecialManager.FindSpecialBySector(sector);
            special.Should().NotBeNull();

            var light = special as LightStrobeSpecial;
            light.Should().NotBeNull();
            light!.MaxBright.Should().Be(256);
            light!.MinBright.Should().Be(128);
            light!.BrightTicks.Should().Be(5);
            light!.DarkTicks.Should().Be(35);
        }

        [Fact(DisplayName = "Doom Sector Type 4 - Lights blink random")]
        public void SectorType4()
        {
            var sector = GameActions.GetSector(World, 791);
            ISpecial? special = World.SpecialManager.FindSpecialBySector(sector);
            special.Should().NotBeNull();

            var light = special as LightStrobeSpecial;
            light.Should().NotBeNull();
            light!.MaxBright.Should().Be(192);
            light!.MinBright.Should().Be(0);
            light!.BrightTicks.Should().Be(5);
            light!.DarkTicks.Should().Be(35);

            sector.SectorDamageSpecial.Should().NotBeNull();
            sector.SectorDamageSpecial!.Damage.Should().Be(20);
            sector.SectorDamageSpecial!.RadSuitLeakChance.Should().Be(5);
        }

        [Fact(DisplayName = "Doom Sector Type 5 - 5/10% damage")]
        public void SectorType5()
        {
            var sector = GameActions.GetSector(World, 792);
            sector.SectorDamageSpecial.Should().NotBeNull();
            sector.SectorDamageSpecial!.Damage.Should().Be(10);
            sector.SectorDamageSpecial!.RadSuitLeakChance.Should().Be(0);
        }

        [Fact(DisplayName = "Doom Sector Type 7 - 2/5% damage")]
        public void SectorType7()
        {
            var sector = GameActions.GetSector(World, 793);
            sector.SectorDamageSpecial.Should().NotBeNull();
            sector.SectorDamageSpecial!.Damage.Should().Be(5);
            sector.SectorDamageSpecial!.RadSuitLeakChance.Should().Be(0);
        }

        [Fact(DisplayName = "Doom Sector Type 8 - Lights pulsate")]
        public void SectorType8()
        {
            var sector = GameActions.GetSector(World, 782);
            ISpecial? special = World.SpecialManager.FindSpecialBySector(sector);
            special.Should().NotBeNull();

            var light = special as LightPulsateSpecial;
            light.Should().NotBeNull();
            light!.MaxBright.Should().Be(256);
            light!.MinBright.Should().Be(128);
        }

        [Fact(DisplayName = "Doom Sector Type 9 - Secret")]
        public void SectorType9()
        {
            var sector = GameActions.GetSector(World, 804);
            sector.Secret.Should().BeTrue();
            World.LevelStats.SecretCount.Should().Be(0);
            World.LevelStats.TotalSecrets.Should().Be(1);

            GameActions.SetEntityToLine(World, Player, 3972, 64);
            World.Tick();
            World.LevelStats.SecretCount.Should().Be(1);
            World.LevelStats.TotalSecrets.Should().Be(1);

            GameActions.AssertSound(World, DefaultSoundSource.Default, "dssecret");
        }

        [Fact(DisplayName = "Doom Sector Type 10 - Door close 30 seconds")]
        public void SectorType10()
        {
            // This sector is paused on test init so it can be resumed and tested here.
            int delaySeconds = 35 * 30;
            var sector = GameActions.GetSector(World, 726);
            sector.ActiveCeilingMove.Should().NotBeNull();
            var special = sector.ActiveCeilingMove!;
            special.DelayTics.Should().Be(delaySeconds);
            special.Resume();
            GameActions.TickWorld(World, delaySeconds);
            GameActions.RunDoorClose(World, sector, 0, 16);
        }

        [Fact(DisplayName = "Doom Sector Type 11 - 10/20% damage end level")]
        public void SectorType11()
        {
            bool exited = false;
            var sector = GameActions.GetSector(World, 794);
            sector.SectorDamageSpecial.Should().NotBeNull();
            sector.SectorDamageSpecial!.Damage.Should().Be(20);
            sector.SectorDamageSpecial!.RadSuitLeakChance.Should().Be(0);

            World.LevelExit += World_LevelExit;

            GameActions.SetEntityToLine(World, Player, 3866, 64);
            Player.Sector.Id.Should().Be(794);
            Player.IsDead.Should().BeFalse();

            // Player in god mode should still take damage in this special.
            GameActions.TickWorld(World, () => { return !exited; }, () => { });
            Player.Health.Should().Be(1);
            exited.Should().BeTrue();
            World.LevelExit -= World_LevelExit;

            void World_LevelExit(object? sender, LevelChangeEvent e)
            {
                e.Cancel = true;
                exited = true;
                e.ChangeType.Should().Be(LevelChangeType.Next);
            }
        }

        [Fact(DisplayName = "Doom Sector Type 12 - Lights strobe 1 second")]
        public void SectorType12()
        {
            var sector = GameActions.GetSector(World, 783);
            ISpecial? special = World.SpecialManager.FindSpecialBySector(sector);
            special.Should().NotBeNull();

            var light = special as LightStrobeSpecial;
            light.Should().NotBeNull();
            light!.MaxBright.Should().Be(256);
            light!.MinBright.Should().Be(128);
            light!.DarkTicks.Should().Be(35);
            light!.BrightTicks.Should().Be(5);
        }

        [Fact(DisplayName = "Doom Sector Type 13 - Lights strobe 1/4 second")]
        public void SectorType13()
        {
            var sector = GameActions.GetSector(World, 784);
            ISpecial? special = World.SpecialManager.FindSpecialBySector(sector);
            special.Should().NotBeNull();

            var light = special as LightStrobeSpecial;
            light.Should().NotBeNull();
            light!.MaxBright.Should().Be(256);
            light!.MinBright.Should().Be(128);
            light!.DarkTicks.Should().Be(15);
            light!.BrightTicks.Should().Be(5);
        }

        [Fact(DisplayName = "Doom Sector Type 14 - Door open and close 5 minutes")]
        public void SectorType14()
        {
            // This sector is paused on test init so it can be resumed and tested here.
            int delaySeconds = 35 * 60 * 5;
            var sector = GameActions.GetSector(World, 643);
            sector.ActiveCeilingMove.Should().NotBeNull();
            var special = sector.ActiveCeilingMove!;
            special.DelayTics.Should().Be(delaySeconds);
            special.Resume();
            GameActions.TickWorld(World, delaySeconds);
            GameActions.RunDoorOpenClose(World, sector, 0, 128, 16);
        }

        [Fact(DisplayName = "Doom Sector Type 16 - 10/20% damage")]
        public void SectorType16()
        {
            var sector = GameActions.GetSector(World, 795);
            sector.SectorDamageSpecial.Should().NotBeNull();
            sector.SectorDamageSpecial!.Damage.Should().Be(20);
            sector.SectorDamageSpecial!.RadSuitLeakChance.Should().Be(5);
        }

        [Fact(DisplayName = "Doom Sector Type 17 - Lights flicker")]
        public void SectorType17()
        {
            var sector = GameActions.GetSector(World, 785);
            ISpecial? special = World.SpecialManager.FindSpecialBySector(sector);
            special.Should().NotBeNull();

            var light = special as LightFireFlickerDoom;
            light.Should().NotBeNull();
            light!.MaxBright.Should().Be(256);
            light!.MinBright.Should().Be(144);
        }
    }
}
