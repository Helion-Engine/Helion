using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials.Vanilla;
using Helion.Resources;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Special;
using Helion.World.Special.SectorMovement;
using Helion.World.Special.Specials;
using MoreLinq;
using System;
using System.Drawing;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    public class VanillaActions
    {
        private static readonly string ResourceZip = "Resources/vandacts.zip";

        private static readonly string MapName = "MAP01";
        private readonly SinglePlayerWorld World;
        private Player Player => World.Player;
        private Entity Monster => GameActions.GetEntity(World, 1);

        private static SinglePlayerWorld? StaticWorld = null;

        public VanillaActions()
        {
            // This is to save time so we aren't loading the data off disk each time.
            if (StaticWorld != null)
            {
                World = StaticWorld;
                return;
            }

            World = GameActions.LoadMap(ResourceZip, MapName, IWadType.Doom2);
            SetUnitTestTextures();
            CheatManager.Instance.ActivateCheat(Player, CheatType.God);
            StaticWorld = World;
        }

        private static void SetUnitTestTextures()
        {
            // We're not loading doom2.wad so the texture images are empty. Load fake images to test action 30 (raise by shortest lower)

            // For raise by shortest lower compatibility testing
            Texture texture = TextureManager.Instance.GetTexture("AASHITTY", ResourceNamespace.Textures);
            texture.Image = CreateImage(64, 64);

            TextureManager.Instance.NullCompatibilityTextureIndex = texture.Index;

            texture = TextureManager.Instance.GetTexture("NUKE24", ResourceNamespace.Textures);
            texture.Image = CreateImage(64, 24);

            texture = TextureManager.Instance.GetTexture("DBRAIN1", ResourceNamespace.Textures);
            texture.Image = CreateImage(64, 32);

            texture = TextureManager.Instance.GetTexture("GRAY2", ResourceNamespace.Textures);
            texture.Image = CreateImage(64, 72);

            texture = TextureManager.Instance.GetTexture("SUPPORT2", ResourceNamespace.Textures);
            texture.Image = CreateImage(64, 128);
        }

        private static Helion.Graphics.Image CreateImage(int width, int height) =>
            new(new Bitmap(width, height), Helion.Graphics.ImageType.Argb);

        [Fact(DisplayName = "Doom Action 1 (DR) Door open and Close")]
        public void Action1()
        {
            const int DoorLine = 239;
            GameActions.EntityUseLine(World, Player, DoorLine).Should().BeTrue();
            Sector sector = GameActions.GetSector(World, 2);
            sector.ActiveCeilingMove.Should().NotBeNull();

            GameActions.RunDoorOpenClose(World, sector, 128, VanillaConstants.DoorSlowSpeed);

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
            GameActions.RunDoorOpenClose(World, sector, 128, VanillaConstants.DoorSlowSpeed);
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

            GameActions.RunDoorOpenClose(World, sector, 128, VanillaConstants.DoorSlowSpeed);

            GameActions.CheckNoReactivateEntityCross(World, Player, DoorLine, sector, sector.Ceiling);

            GameActions.CheckMonsterCrossActivation(World, Monster, DoorLine, sector, sector.Ceiling, true);
            GameActions.RunDoorOpenClose(World, sector, 128, VanillaConstants.DoorSlowSpeed);
        }

        [Fact(DisplayName = "Doom Action 5 (W1) Raise floor to lowest adjacent ceiling")]
        public void Action5()
        {
            const int Line = 812;
            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 5);
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.RunFloorRaise(World, sector, 64, VanillaConstants.SectorSlowSpeed);

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

            GameActions.RunCrusherCeiling(World, sector, VanillaConstants.SectorFastSpeed, false);
        }

        [Fact(DisplayName = "Doom Action 7 (S1) Raise stairs 8")]
        public void Action7()
        {
            const int Line = 2957;
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            int[] stairIds = new int[] { 663, 664, 665, 661 };
            GameActions.RunStairs(World, stairIds, 8, 8, VanillaConstants.StairSlowSpeed);
        }

        [Fact(DisplayName = "Doom Action 8 (W1) Raise stairs 8")]
        public void Action8()
        {
            const int Line = 2870;
            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            int[] stairIds = new int[] { 650, 651, 652, 648 };
            GameActions.RunStairs(World, stairIds, 0, 8, VanillaConstants.StairSlowSpeed);
        }

        [Fact(DisplayName = "Doom Action 9 (S1) Donut")]
        public void Action9()
        {
            const int Line = 815;
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            // TODO
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

        // Action11 is EndLevel

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

            GameActions.RunFloorRaise(World, sector, 16, VanillaConstants.FloorSlowSpeed);
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

            GameActions.RunFloorRaise(World, sector, 8, VanillaConstants.FloorSlowSpeed);
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

            GameActions.RunFloorRaise(World, sector, 64, VanillaConstants.SectorSlowSpeed);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 19 (W1) Lower floor to next higher")]
        public void Action19()
        {
            const int Line = 1101;
            Sector sector = GameActions.GetSectorByTag(World, 19);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();

            GameActions.RunFloorLower(World, sector, 64, VanillaConstants.SectorSlowSpeed);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Ceiling, false);
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

            GameActions.RunFloorRaise(World, sector, 32, VanillaConstants.FloorSlowSpeed);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 21 (S1) Lift")]
        public void Action21()
        {
            const int Line = 2720;
            Sector sector = GameActions.GetSectorByTag(World, 21);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();

            GameActions.RunLift(World, sector, 64, 0, VanillaConstants.LiftFastSpeed, VanillaConstants.LiftDelay);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
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
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 23 (S1) Lower floor to lowest")]
        public void Action23()
        {
            const int Line = 993;
            Sector sector = GameActions.GetSectorByTag(World, 23);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();

            GameActions.RunFloorLower(World, sector, 0, 8);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
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
            GameActions.RunDoorOpenClose(World, sector, 128, VanillaConstants.DoorSlowSpeed);
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
            GameActions.RunDoorOpenClose(World, sector, 128, VanillaConstants.DoorSlowSpeed);
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
            GameActions.RunDoorOpenClose(World, sector, 128, VanillaConstants.DoorSlowSpeed);
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
        }

        [Fact(DisplayName = "Doom Action 29 (S1) Door open close")]
        public void Action29()
        {
            const int Line = 163;
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            Sector sector = GameActions.GetSectorByTag(World, 29);

            GameActions.RunDoorOpenClose(World, sector, 128, VanillaConstants.DoorSlowSpeed);
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
            sectors.ForEach(x => x.Floor.Z = 0);
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
            sectors.ForEach(x => x.Floor.Z = 0);
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
            Sector sector = GameActions.GetSectorByTag(World, 97);

            GameActions.EntityCrossLine(World, Player, Line, moveOutofBounds: false).Should().BeTrue();
            GameActions.RunTeleport(World, Player, sector, 34);

            GameActions.CheckNoReactivateEntityCross(World, Player, Line, sector, sector.Floor);
            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
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

            sector.Ceiling.Z = 0;
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

        // Action51 is EndLevel
        // Action52 is EndLevel

        [Fact(DisplayName = "Doom Action 53 (W1) Start moving floor")]
        public void Action53()
        {
            const int Line = 1245;
            Sector sector = GameActions.GetSectorByTag(World, 53);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.EntityCrossLine(World, Player, Line, forceActivation: true);
            GameActions.RunPerpetualMovingFloor(World, sector, 0, 96, 8, VanillaConstants.LiftDelay);
            World.SpecialManager.RemoveSpecial(sector.ActiveFloorMove!);
            sector.Floor.Z = 64;

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

            GameActions.EntityCrossLine(World, Player, DeactivateLine, forceActivation: true);
            special.Should().NotBeNull();
            special.IsPaused.Should().BeTrue();

            World.SpecialManager.RemoveSpecial(sector.ActiveFloorMove!);
            sector.Floor.Z = 64;
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
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
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
            GameActions.CheckMonsterUseActivation(World, Monster, Line, sector, sector.Ceiling, false);
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
                GameActions.RunDoorOpenClose(World, sector, 128, VanillaConstants.DoorSlowSpeed);
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
                sector.Ceiling.Z = 120;
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
            sector.Ceiling.Z = 120;
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
            sector.Ceiling.Z = 120;
        }

        [Fact(DisplayName = "Doom Action 75 (WR) Close door")]
        public void Action75()
        {
            const int DoorLine = 300;
            Sector sector = GameActions.GetSectorByTag(World, 75);

            for (int i = 0; i < 2; i++)
            {
                GameActions.EntityCrossLine(World, Player, DoorLine).Should().BeTrue();
                GameActions.RunDoorClose(World, sector, 0, VanillaConstants.DoorSlowSpeed);
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
                sector.Ceiling.Z = 128;
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
    }
}           
