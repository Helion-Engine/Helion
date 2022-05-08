using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.MapInfo;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using MoreLinq;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    public partial class Physics
    {
        private const int TeleportLine = 281;
        private const int TeleportLandingId = 55;
        private Vec3D TeleportDestination = new(2080, 416, 16);
        private Sector TeleportDestSector => GameActions.GetSector(World, 65);

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

            monsters.ForEach(x => x.IsDead.Should().BeFalse());
            GameActions.EntityCrossLine(World, Player, TeleportLine, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
            GameActions.RunTeleport(World, Player, TeleportDestSector, TeleportLandingId);
            monsters.ForEach(x => x.IsDead.Should().BeTrue());
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
    }
}
