using FluentAssertions;
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
using System;
using System.Drawing;
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

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();

            sector.LightLevel.Should().Be(128);
            sector.Floor.LightLevel.Should().Be(128);

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
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

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
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
            Sector sector1 = GameActions.GetSector(World, 317);
            Sector sector2 = GameActions.GetSector(World, 319);
            Sector sector3 = GameActions.GetSector(World, 318);
            Sector sector4 = GameActions.GetSector(World, 833);

            GameActions.RunFloorRaise(World, sector1, 24, 8);
            GameActions.RunFloorRaise(World, sector2, 32, 8);
            GameActions.RunFloorRaise(World, sector3, 72, 8);
            GameActions.RunFloorRaise(World, sector4, 128, 8);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            sector1.ActiveFloorMove.Should().BeNull();
            sector2.ActiveFloorMove.Should().BeNull();
            sector3.ActiveFloorMove.Should().BeNull();
            sector4.ActiveFloorMove.Should().BeNull();

            sector1.Floor.Z = 0;
            sector2.Floor.Z = 0;
            sector3.Floor.Z = 0;
            sector4.Floor.Z = 0;
        }

        [Fact(DisplayName = "Doom Action 30 (S1) Raise floor by shortest lower (vanilla compatbility)")]
        public void Action30_Compatibility()
        {
            World.Config.Compatibility.VanillaShortestTexture.Set(true);
            const int Line = 776;
            GameActions.EntityCrossLine(World, Player, Line, forceActivation: true).Should().BeTrue();
            Sector sector1 = GameActions.GetSector(World, 317);
            Sector sector2 = GameActions.GetSector(World, 319);
            Sector sector3 = GameActions.GetSector(World, 318);
            Sector sector4 = GameActions.GetSector(World, 833);

            GameActions.RunFloorRaise(World, sector1, 24, 8);
            GameActions.RunFloorRaise(World, sector2, 32, 8);
            GameActions.RunFloorRaise(World, sector3, 64, 8);
            GameActions.RunFloorRaise(World, sector4, 64, 8);

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            sector1.ActiveFloorMove.Should().BeNull();
            sector2.ActiveFloorMove.Should().BeNull();
            sector3.ActiveFloorMove.Should().BeNull();
            sector4.ActiveFloorMove.Should().BeNull();

            sector1.Floor.Z = 0;
            sector2.Floor.Z = 0;
            sector3.Floor.Z = 0;
            sector4.Floor.Z = 0;
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

            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();

            sector.LightLevel.Should().Be(35);
            sector.Floor.LightLevel.Should().Be(35);

            GameActions.CheckMonsterCrossActivation(World, Monster, Line, sector, sector.Floor, false);
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
    }
}           
