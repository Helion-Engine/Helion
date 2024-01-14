using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.MapInfo;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    public partial class Physics
    {
        private const int TeleportLine = 280;
        private const int TeleportLandingId = 55;
        private Vec3D TeleportDestination = new(2080, 416, 16);
        private Sector TeleportDestSector => GameActions.GetSector(World, 65);

        private const int TeleportLineLow = 330;
        private const int TeleportLandingIdLow = 56;
        private Sector TeleportDestSectorLow => GameActions.GetSector(World, 75);

        private const int TeleportLineMove = 338;
        private const int TeleportLandingIdMove = 57;
        private Sector TeleportDestSectorMove => GameActions.GetSectorByTag(World, 16);

        [Fact(DisplayName = "Player teleport")]
        public void PlayerTeleport()
        {
            GameActions.EntityCrossLine(World, Player, TeleportLine, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            GameActions.RunTeleport(World, Player, TeleportDestSector, TeleportLandingId);
        }

        [Fact(DisplayName = "Player teleport and telefrag monster")]
        public void PlayerTelefrag()
        {
            var monster = GameActions.CreateEntity(World, "Zombieman", TeleportDestination);
            monster.IsDead.Should().BeFalse();
            GameActions.EntityCrossLine(World, Player, TeleportLine, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            GameActions.RunTeleport(World, Player, TeleportDestSector, TeleportLandingId);
            monster.IsDead.Should().BeTrue();
        }

        [Fact(DisplayName = "Player teleport and telefrag multiple monsters")]
        public void PlayerTelefragMultiple()
        {
            var monsters = new Entity[]
            {
                GameActions.CreateEntity(World, "Zombieman", TeleportDestination),
                GameActions.CreateEntity(World, "Zombieman", TeleportDestination),
                GameActions.CreateEntity(World, "Zombieman", TeleportDestination),
                GameActions.CreateEntity(World, "Zombieman", TeleportDestination),
            };

            foreach (Entity monster in monsters)
                monster.IsDead.Should().BeFalse();
                    
            GameActions.EntityCrossLine(World, Player, TeleportLine, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            GameActions.RunTeleport(World, Player, TeleportDestSector, TeleportLandingId);
            
            foreach (Entity monster in monsters)
                monster.IsDead.Should().BeTrue();
        }

        [Fact(DisplayName = "Monster teleport fail")]
        public void MonsterNoTelefrag()
        {
            World.MapInfo.HasOption(MapOptions.AllowMonsterTelefrags).Should().BeFalse();
            var monster = GameActions.CreateEntity(World, "Zombieman", TeleportDestination);
            monster.IsDead.Should().BeFalse();
            var teleportMonster = GameActions.CreateEntity(World, "Zombieman", Vec3D.Zero);
            GameActions.EntityCrossLine(World, teleportMonster, TeleportLine, moveOutofBounds: false).Should().BeTrue();
            GameActions.CheckNoTeleport(World, teleportMonster, TeleportDestSector, TeleportLandingId);
            monster.IsDead.Should().BeFalse();
        }

        [Fact(DisplayName = "Teleport failure doesn't activate single activation line")]
        public void SingleActivateTeleportFailure()
        {
            const int SingleTeleportLine = 341;
            World.MapInfo.HasOption(MapOptions.AllowMonsterTelefrags).Should().BeFalse();
            var monster = GameActions.CreateEntity(World, "Zombieman", TeleportDestination);
            monster.IsDead.Should().BeFalse();
            var teleportMonster = GameActions.CreateEntity(World, "Zombieman", Vec3D.Zero);
            GameActions.EntityCrossLine(World, teleportMonster, SingleTeleportLine, moveOutofBounds: false).Should().BeTrue();
            GameActions.CheckNoTeleport(World, teleportMonster, TeleportDestSector, TeleportLandingId);
            monster.IsDead.Should().BeFalse();

            var teleportLine = GameActions.GetLine(World, SingleTeleportLine);
            teleportLine.Activated.Should().BeFalse();

            monster.Kill(null);
            GameActions.TickWorld(World, 35);
            GameActions.EntityCrossLine(World, teleportMonster, SingleTeleportLine, moveOutofBounds: false).Should().BeTrue();
            GameActions.RunTeleport(World, teleportMonster, TeleportDestSector, TeleportLandingId);
            teleportLine.Activated.Should().BeTrue();
        }

        [Fact(DisplayName = "Monster teleport not blocked by non-solid")]
        public void MonsterTeleportNonSolid()
        {
            var monster = GameActions.CreateEntity(World, "Zombieman", TeleportDestination);
            monster.Kill(null);
            GameActions.TickWorld(World, 200);

            var teleportMonster = GameActions.CreateEntity(World, "Zombieman", Vec3D.Zero);
            GameActions.EntityCrossLine(World, teleportMonster, TeleportLine, moveOutofBounds: false).Should().BeTrue();
            GameActions.RunTeleport(World, teleportMonster, TeleportDestSector, TeleportLandingId);
        }

        [Fact(DisplayName = "Monster telefrag")]
        public void MonsterTelefrag()
        {
            World.MapInfo.SetOption(MapOptions.AllowMonsterTelefrags, true);
            var monster = GameActions.CreateEntity(World, "Zombieman", TeleportDestination);
            monster.IsDead.Should().BeFalse();
            var teleportMonster = GameActions.CreateEntity(World, "Zombieman", Vec3D.Zero);
            GameActions.EntityCrossLine(World, teleportMonster, TeleportLine, moveOutofBounds: false).Should().BeTrue();
            GameActions.RunTeleport(World, teleportMonster, TeleportDestSector, TeleportLandingId);
            monster.IsDead.Should().BeTrue();
            World.MapInfo.SetOption(MapOptions.AllowMonsterTelefrags, false);
        }

        [Fact(DisplayName = "Player telefrag z bounds checks")]
        public void TelefragZ()
        {
            var monster = GameActions.CreateEntity(World, "Cacodemon", TeleportDestination.XY.To3D(128));
            monster.IsDead.Should().BeFalse();
            GameActions.EntityCrossLine(World, Player, TeleportLine, moveOutofBounds: false).Should().BeTrue();
            GameActions.RunTeleport(World, Player, TeleportDestSector, TeleportLandingId);
            monster.IsDead.Should().Be(false);

            GameActions.SetEntityPosition(World, Player, Vec3D.Zero);
            GameActions.SetEntityPosition(World, monster, TeleportDestination.XY.To3D(64));
            GameActions.EntityCrossLine(World, Player, TeleportLine, moveOutofBounds: false).Should().BeTrue();
            GameActions.RunTeleport(World, Player, TeleportDestSector, TeleportLandingId);
            monster.IsDead.Should().Be(true);
        }

        [Fact(DisplayName = "Teleport player to lower z")]
        public void TeleportPlayerLowerZ()
        {
            GameActions.EntityCrossLine(World, Player, TeleportLineLow, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            GameActions.RunTeleport(World, Player, TeleportDestSectorLow, TeleportLandingIdLow);
            Player.Position.Z.Should().Be(-128);
        }

        [Fact(DisplayName = "Teleport player to lower z after floor moved")]
        public void TeleportPlayerMoveLowerZ()
        {
            if (TeleportDestSectorMove.Floor.Z == 0)
            {
                GameActions.ActivateLine(World, Player, 323, ActivationContext.UseLine).Should().BeTrue();
                GameActions.RunSectorPlaneSpecial(World, TeleportDestSectorMove);
                TeleportDestSectorMove.Floor.Z.Should().Be(-128);
            }

            // Teleport landings have no blockmap and do not move with the sector.
            GameActions.GetEntity(World, TeleportLandingIdMove).Position.Z.Should().Be(0);

            // Teleport special should set the player to the floor z even though the teleport landing is higher.
            GameActions.EntityCrossLine(World, Player, TeleportLineMove, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            GameActions.RunTeleport(World, Player, TeleportDestSectorMove, TeleportLandingIdMove);
            Player.Position.Z.Should().Be(-128);
        }

        [Fact(DisplayName = "Teleport monster to lower z")]
        public void TeleporMonsterLowerZ()
        {
            var monster = GameActions.CreateEntity(World, "Zombieman", Vec3D.Zero);
            GameActions.EntityCrossLine(World, monster, TeleportLineLow, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            GameActions.RunTeleport(World, monster, TeleportDestSectorLow, TeleportLandingIdLow);
            monster.Position.Z.Should().Be(-128);
        }

        [Fact(DisplayName = "Teleport monster to lower z after floor moved")]
        public void TeleportMonsterMoveLowerZ()
        {
            if (TeleportDestSectorMove.Floor.Z == 0)
            {
                GameActions.ActivateLine(World, Player, 323, ActivationContext.UseLine).Should().BeTrue();
                GameActions.RunSectorPlaneSpecial(World, TeleportDestSectorMove);
                TeleportDestSectorMove.Floor.Z.Should().Be(-128);
            }

            // Teleport landings have no blockmap and do not move with the sector.
            GameActions.GetEntity(World, TeleportLandingIdMove).Position.Z.Should().Be(0);

            var monster = GameActions.CreateEntity(World, "DoomImp", (1790, 416, 0));
            // Teleport special should set the player to the floor z even though the teleport landing is higher.
            monster.FrozenTics = 0;
            monster.SetEnemyDirection(Entity.MoveDir.East);
            monster.MoveEnemy(out _).Should().BeTrue();
            GameActions.RunTeleport(World, monster, TeleportDestSectorMove, TeleportLandingIdMove);
            monster.Position.Z.Should().Be(-128);
        }

        [Fact(DisplayName = "Teleport to teleport dest with sector tag 0")]
        public void TeleportTagZero()
        {
            int telportLandingId = 64;
            int telportLine = 398;
            Sector sector = GameActions.GetSector(World, 86);

            GameActions.EntityCrossLine(World, Player, telportLine, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            GameActions.RunTeleport(World, Player, sector, telportLandingId);
        }

        [Fact(DisplayName = "Teleport fog with z")]
        public void TeleportWithZ()
        {
            int telportLine = 405;
            Sector sector = GameActions.GetSector(World, 89);
            Entity caco = GameActions.GetEntity(World, 68);
            Entity teleportDest = GameActions.GetEntity(World, 67);
            Vec3D pos = caco.Position;

            GameActions.EntityCrossLine(World, caco, telportLine, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            var fogEntities = GameActions.GetEntities(World, "TeleportFog");
            var fog = fogEntities.FirstOrDefault(x => x.Position.Z == pos.Z);
            fog.Should().NotBeNull();
            bool check = caco.Position == teleportDest.Position;
            check.Should().BeTrue();
        }

        [Fact(DisplayName = "Teleport fog with teleport flag should fail")]
        public void TeleportWithTeleportFlagFail()
        {
            int telportLine = 405;
            Entity caco = GameActions.GetEntity(World, 68);
            Entity teleportDest = GameActions.GetEntity(World, 67);

            caco.Flags.Teleport = true;
            GameActions.EntityCrossLine(World, caco, telportLine, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            bool check = caco.Position == teleportDest.Position;
            check.Should().BeFalse();
            caco.Flags.Teleport = false;
        }

        [Fact(DisplayName = "Teleport fog with no clip flag should fail")]
        public void TeleportWithNoClipFlagFail()
        {
            int telportLine = 405;
            Entity caco = GameActions.GetEntity(World, 68);
            Entity teleportDest = GameActions.GetEntity(World, 67);

            caco.Flags.NoClip = true;
            GameActions.EntityCrossLine(World, caco, telportLine, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            bool check = caco.Position == teleportDest.Position;
            check.Should().BeFalse();
            caco.Flags.NoClip = false;
        }

        [Fact(DisplayName = "Teleport fog with no teleport flag should fail")]
        public void TeleportWithNoTeleportFlagFail()
        {
            int telportLine = 405;
            Entity caco = GameActions.GetEntity(World, 68);
            Entity teleportDest = GameActions.GetEntity(World, 67);

            caco.Flags.NoTeleport = true;
            GameActions.EntityCrossLine(World, caco, telportLine, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            bool check = caco.Position == teleportDest.Position;
            check.Should().BeFalse();
            caco.Flags.NoTeleport = false;
        }
    }
}
