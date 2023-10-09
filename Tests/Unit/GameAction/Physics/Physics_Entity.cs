using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    public partial class Physics
    {
        private static readonly Vec2D StackPos1 = new(-928, 672);
        private static readonly Vec2D StackPos2 = new(-928, 640);
        private static readonly Vec2D StackPos3 = new(-928, 656);

        [Fact(DisplayName = "OnEntity/OverEntity simple stack")]
        public void StackEntity()
        {
            var bottom = GameActions.CreateEntity(World, "BaronOfHell", StackPos1.To3D(0));
            var top = GameActions.CreateEntity(World, "BaronOfHell", StackPos1.To3D(64));

            top.OnEntity.Entity.Should().Be(bottom);
            bottom.OverEntity.Entity!.Should().Be(top);
        }

        [Fact(DisplayName = "OnEntity/OverEntity two on bottom")]
        public void StackEntityMutliple()
        {
            var bottom1 = GameActions.CreateEntity(World, "BaronOfHell", StackPos1.To3D(0));
            var bottom2 = GameActions.CreateEntity(World, "BaronOfHell", StackPos3.To3D(0));
            var top = GameActions.CreateEntity(World, "BaronOfHell", StackPos2.To3D(64));

            top.OnEntity.Entity.Should().NotBe(null);
            (top.OnEntity.Entity!.Equals(bottom1) || top.OnEntity.Entity!.Equals(bottom2)).Should().BeTrue();
            bottom1.OverEntity.Entity!.Should().Be(top);
            bottom2.OverEntity.Entity!.Should().Be(top);
        }

        [Fact(DisplayName = "OnEntity/OverEntity simple stack change when entity dies")]
        public void StackEntityChangeKill()
        {
            var bottom = GameActions.CreateEntity(World, "BaronOfHell", StackPos1.To3D(0));
            var top = GameActions.CreateEntity(World, "BaronOfHell", StackPos1.To3D(64));

            top.OnEntity.Entity.Should().Be(bottom);
            bottom.OverEntity.Entity!.Should().Be(top);

            bottom.Kill(null);

            top.OnEntity.Entity.Should().BeNull();
            bottom.OverEntity.Entity!.Should().BeNull();

            World.Tick();

            top.Velocity.Z.Should().NotBe(0);
        }

        [Fact(DisplayName = "OnEntity/OverEntity simple stack change when entity pves")]
        public void StackEntityChangeMove()
        {
            var bottom = GameActions.CreateEntity(World, "BaronOfHell", StackPos1.To3D(0));
            var top = GameActions.CreateEntity(World, "BaronOfHell", StackPos1.To3D(64));

            top.OnEntity.Entity.Should().Be(bottom);
            bottom.OverEntity.Entity!.Should().Be(top);

            GameActions.MoveEntity(World, bottom, new Vec2D(-928, 768));

            top.OnEntity.Entity.Should().BeNull();
            bottom.OverEntity.Entity!.Should().BeNull();

            World.Tick();

            top.Velocity.Z.Should().NotBe(0);
        }

        [Fact(DisplayName = "OnEntity/OverEntity two on top with change")]
        public void StackEntityMutlipleChange()
        {
            var bottom = GameActions.CreateEntity(World, "BaronOfHell", StackPos2.To3D(0));
            var top1 = GameActions.CreateEntity(World, "BaronOfHell", StackPos1.To3D(64));
            var top2 = GameActions.CreateEntity(World, "BaronOfHell", StackPos2.To3D(64));

            bottom.OverEntity.Should().NotBe(null);
            (bottom.OverEntity.Entity!.Equals(top1) || bottom.OverEntity.Entity!.Equals(top2)).Should().BeTrue();
            top1.OnEntity.Entity.Should().Be(bottom);
            top2.OnEntity.Entity.Should().Be(bottom);

            bottom.Kill(null);

            top1.OnEntity.Entity.Should().BeNull();
            top2.OnEntity.Entity.Should().BeNull();
            bottom.OverEntity.Entity!.Should().BeNull();
        }

        [Fact(DisplayName = "Entity can move partially clipped")]
        public void PartiallyClippedEntityMovement()
        {
            Vec3D pos1 = new(-928, 256, 0);
            Vec3D pos2 = pos1 + new Vec3D(32, 0, 0);
            GameActions.CreateEntity(World, "Zombieman", pos1);
            var moveEntity = GameActions.CreateEntity(World, "Zombieman", pos2);
            GameActions.MoveEntity(World, moveEntity, moveEntity.Position.XY + new Vec2D(32, 0));
            moveEntity.Position.X.Should().Be(-864);
            moveEntity.Position.Y.Should().Be(256);
            moveEntity.Position.Z.Should().Be(0);
        }

        [Fact(DisplayName = "Entity can't move clipped")]
        public void ClippedEntityMovement()
        {
            Vec3D pos1 = new(-928, 256, 0);
            Vec3D pos2 = pos1 + new Vec3D(16, 0, 0);
            GameActions.CreateEntity(World, "Zombieman", pos1);
            var moveEntity = GameActions.CreateEntity(World, "Zombieman", pos2);
            GameActions.MoveEntity(World, moveEntity, moveEntity.Position.XY + new Vec2D(32, 0));
            moveEntity.Position.Should().Be(pos2);
        }

        [Fact(DisplayName = "Entity can move partially clipped from wall")]
        public void PartiallyClippedWallMovement()
        {
            Vec3D pos1 = new(-928, 1076, 0);
            Vec3D pos2 = new(-928, 1068, 0);
            var moveEntity = GameActions.CreateEntity(World, "Zombieman", pos1);
            GameActions.MoveEntity(World, moveEntity, pos2.XY);
            moveEntity.Position.Should().Be(pos2);
        }

        [Fact(DisplayName = "Entity can walk down stairs")]
        public void EntityWalkStairs()
        {
            Vec3D pos1 = new(-1172, -224, 96);
            var moveEntity = GameActions.CreateEntity(World, "DoomImp", pos1, frozen: false);
            var player = GameActions.CreateEntity(World, "DoomPlayer", new Vec3D(-1632, -224, 48));
            GameActions.SetEntityTarget(moveEntity, player);

            double previousX = moveEntity.Position.X;
            GameActions.TickWorld(World, () => { return moveEntity.Position.X > -1348; }, () =>
            {
                moveEntity.Position.Z.Should().Be(moveEntity.HighestFloorZ);
            });

            moveEntity.Position.X.Should().Be(-1348);
        }

        [Fact(DisplayName = "Entity can float through obstacles")]
        public void CacodemonFloatMovement()
        {
            Vec3D pos1 = new(-1536, 256, 0);
            var moveEntity = GameActions.CreateEntity(World, "Cacodemon", pos1, frozen: false);
            var player = GameActions.CreateEntity(World, "DoomPlayer", new Vec3D(-512, 256, 0));
            GameActions.SetEntityTarget(moveEntity, player);

            GameActions.TickWorld(World, () => { return moveEntity.Position.X < -1056; }, () => { });

            moveEntity.Position.X.Should().Be(-1056);
            moveEntity.Position.Y.Should().Be(256);
            moveEntity.Position.Z.Should().Be(188);
        }

        [Fact(DisplayName = "Monster can walk on bridge")]
        public void MonsterBridgeWalk()
        {
            Vec3D pos1 = new(1168, 1064, 24);
            Vec3D moveTo = pos1 + new Vec3D(16, 0, 0);
            var moveEntity = GameActions.CreateEntity(World, "DoomImp", pos1, frozen: false);
            moveEntity.OnEntity.Entity.Should().NotBeNull();
            moveEntity.OnGround.Should().BeTrue();

            GameActions.MoveEntity(World, moveEntity, moveTo.XY);
            moveEntity.Position.Should().Be(moveTo);
        }

        [Fact(DisplayName = "Monster can't drop off high ledges")]
        public void MonsterDropOff()
        {
            Vec3D pos1 = new(1120, 1072, 25);
            Vec3D moveTo = pos1 + new Vec3D(0, 32, 0);
            var moveEntity = GameActions.CreateEntity(World, "DoomImp", pos1, frozen: false);

            GameActions.MoveEntity(World, moveEntity, moveTo.XY);
            moveEntity.Position.Should().Be(pos1);
        }

        [Fact(DisplayName = "Spawn ceiling entity in floor move sector")]
        public void SpawnCeilingFloorMove()
        {
            var sector = GameActions.GetSectorByTag(World, 3);
            sector.ActiveFloorMove.Should().BeNull();
            var entity = GameActions.CreateEntity(World, "NonSolidMeat2", new Vec3D(-128, 216, 0));
            entity.OnGround.Should().BeFalse();
            entity.Position.Z.Should().Be(4012);
            GameActions.ActivateLine(World, Player, 23, ActivationContext.UseLine);

            GameActions.RunSectorPlaneSpecial(World, sector, () =>
            {
                entity.Position.Z.Should().Be(4012);
            });
        }

        [Fact(DisplayName = "Stepping on not move linked entity does not cause it to clamp to highest floor")]
        public void SteppingOnEntityClamping()
        {
            var sector = GameActions.GetSectorByTag(World, 12);
            sector.Floor.SetZ(-32);

            // Stepping on things triggers stacked entity checks. If an entity hasn't set MoveLinked then it should still be on the lower floor.
            var entity = GameActions.CreateEntity(World, "Column", new Vec3D(1344, 640, -32));
            entity.MoveLinked.Should().BeFalse();
            entity.Position.Z.Should().Be(-32);
            GameActions.SetEntityPosition(World, Player, new Vec2D(1344, 608));

            Player.Position.Z.Should().Be(0);
            GameActions.MoveEntity(World, Player, Player.Position.XY + new Vec2D(0, 16));
            Player.Position.XY.Should().Be(new Vec2D(1344, 624));

            entity.Position.Z.Should().Be(-32);
            Player.Position.Z.Should().Be(16);
            Player.OnEntity.Entity.Should().Be(entity);
            entity.OverEntity.Entity.Should().Be(Player);
        }

        [Fact(DisplayName = "Not moved linked entity will not pop up if there is a blocking entity")]
        public void EntityClampingPopClipFloorRaise()
        {
            var sector = GameActions.GetSectorByTag(World, 12);
            sector.Floor.SetZ(-32);
            var entity = GameActions.CreateEntity(World, "Column", new Vec3D(1344, 640, -32));
            entity.MoveLinked.Should().BeFalse();
            entity.Position.Z.Should().Be(-32);
            GameActions.SetEntityPosition(World, Player, new Vec2D(1344, 660));
            Player.Position.Z.Should().Be(32);
            GameActions.ActivateLine(World, Player, 255, ActivationContext.HitscanImpactsWall).Should().BeTrue();

            World.Tick();
            entity.Position.Z.Should().NotBe(0);
            GameActions.RunSectorPlaneSpecial(World, sector);

            entity.Position.Z.Should().Be(0);
            Player.Position.Z.Should().Be(entity.Height);
            Player.OnEntity.Entity.Should().Be(entity);
            entity.OverEntity.Entity.Should().Be(Player);
        }

        [Fact(DisplayName = "Not moved linked entity will not pop up if there is a blocking entity")]
        public void EntityClampingPopClipFloorLower()
        {
            var sector = GameActions.GetSectorByTag(World, 12);
            sector.Floor.SetZ(-32);
            var entity = GameActions.CreateEntity(World, "Column", new Vec3D(1344, 640, -32));
            entity.MoveLinked.Should().BeFalse();
            entity.Position.Z.Should().Be(-32);
            GameActions.SetEntityPosition(World, Player, new Vec2D(1344, 660));
            GameActions.ActivateLine(World, Player, 256, ActivationContext.HitscanImpactsWall).Should().BeTrue();

            World.Tick();
            entity.Position.Z.Should().NotBe(0);
            GameActions.RunSectorPlaneSpecial(World, sector);

            Player.Position.Z.Should().Be(32);
            entity.Position.Z.Should().Be(-64);
        }

        [Fact(DisplayName = "Entity with NoSector = true and NoBlockmap = false should move with the floor")]
        public void EntityMovesWithNoSector()
        {
            // Helion isn't using the blockmap to determine entity movement with sectors.
            // Physics still needs to link it to the sector but then check if NoBlockmap = false for movement.
            var sector = GameActions.GetSectorByTag(World, 1);
            var def = World.EntityManager.DefinitionComposer.GetByName(Zombieman)!;
            def.Flags.NoSector = true;
            def.Flags.NoBlockmap = false;

            var monster = GameActions.CreateEntity(World, Zombieman, LiftCenter1.To3D(0));
            monster.SubsectorNode.Should().BeNull();
            monster.BlockmapNodes.Length.Should().Be(1);
            monster.SectorNodes.Length.Should().Be(1);
            monster.Sector.Entities.Contains(monster).Should().BeTrue();

            GameActions.ActivateLine(World, Player, LiftLine1, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.TickWorld(World, () => { return sector.ActiveFloorMove != null; }, () =>
            {
                monster.Position.Z.Should().Be(sector.Floor.Z);
            });

            def.Flags.NoSector = false;
            def.Flags.NoBlockmap = false;
        }

        [Fact(DisplayName = "Entity with NoSector = true and NoBlockmap = true should NOT move with the floor")]
        public void EntityDoesNotMoveWithNoBlockMap()
        {
            var sector = GameActions.GetSectorByTag(World, 1);
            var def = World.EntityManager.DefinitionComposer.GetByName(Zombieman)!;
            def.Flags.NoSector = true;
            def.Flags.NoBlockmap = true;

            var monster = GameActions.CreateEntity(World, Zombieman, LiftCenter1.To3D(0));
            monster.SubsectorNode.Should().BeNull();

            var intersections = World.BlockmapTraverser.GetEntityIntersections(monster.GetBox2D());
            intersections.Length.Should().Be(0);

            monster.SectorNodes.Length.Should().Be(1);
            monster.Sector.Entities.Contains(monster).Should().BeTrue();

            GameActions.ActivateLine(World, Player, LiftLine1, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.TickWorld(World, () => { return sector.ActiveFloorMove != null; }, () =>
            {
                monster.Position.Z.Should().Be(0);
            });

            def.Flags.NoSector = false;
            def.Flags.NoBlockmap = false;
        }
    }
}
