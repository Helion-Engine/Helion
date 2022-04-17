using FluentAssertions;
using Helion.Geometry.Vectors;
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

            top.OnEntity.Should().Be(bottom);
            bottom.OverEntity.Should().Be(top);
        }

        [Fact(DisplayName = "OnEntity/OverEntity two on bottom")]
        public void StackEntityMutliple()
        {
            var bottom1 = GameActions.CreateEntity(World, "BaronOfHell", StackPos1.To3D(0));
            var bottom2 = GameActions.CreateEntity(World, "BaronOfHell", StackPos3.To3D(0));
            var top = GameActions.CreateEntity(World, "BaronOfHell", StackPos2.To3D(64));

            top.OnEntity.Should().NotBe(null);
            (top.OnEntity!.Equals(bottom1) || top.OnEntity!.Equals(bottom2)).Should().BeTrue();
            bottom1.OverEntity.Should().Be(top);
            bottom2.OverEntity.Should().Be(top);
        }

        [Fact(DisplayName = "OnEntity/OverEntity simple stack change when entity dies")]
        public void StackEntityChangeKill()
        {
            var bottom = GameActions.CreateEntity(World, "BaronOfHell", StackPos1.To3D(0));
            var top = GameActions.CreateEntity(World, "BaronOfHell", StackPos1.To3D(64));

            top.OnEntity.Should().Be(bottom);
            bottom.OverEntity.Should().Be(top);

            bottom.Kill(null);

            top.OnEntity.Should().BeNull();
            bottom.OverEntity.Should().BeNull();

            World.Tick();

            top.Velocity.Z.Should().NotBe(0);
        }

        [Fact(DisplayName = "OnEntity/OverEntity simple stack change when entity pves")]
        public void StackEntityChangeMove()
        {
            var bottom = GameActions.CreateEntity(World, "BaronOfHell", StackPos1.To3D(0));
            var top = GameActions.CreateEntity(World, "BaronOfHell", StackPos1.To3D(64));

            top.OnEntity.Should().Be(bottom);
            bottom.OverEntity.Should().Be(top);

            GameActions.MoveEntity(World, bottom, new Vec2D(-928, 768));

            top.OnEntity.Should().BeNull();
            bottom.OverEntity.Should().BeNull();

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
            (bottom.OverEntity!.Equals(top1) || bottom.OverEntity!.Equals(top2)).Should().BeTrue();
            top1.OnEntity.Should().Be(bottom);
            top2.OnEntity.Should().Be(bottom);

            bottom.Kill(null);

            top1.OnEntity.Should().BeNull();
            top2.OnEntity.Should().BeNull();
            bottom.OverEntity.Should().BeNull();
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
            GameActions.SetEntityPosition(World, Player, new Vec3D(-1632, -224, 48));
            GameActions.SetEntityTarget(moveEntity, Player);

            double previousX = moveEntity.Position.X;
            GameActions.TickWorld(World, () => { return moveEntity.Position.X > -1348; }, () =>
            {
                moveEntity.Position.X.Should().BeLessOrEqualTo(previousX);
            });

            moveEntity.Position.X.Should().Be(-1348);
        }

        [Fact(DisplayName = "Entity can float through obstacles")]
        public void CacodemonFloatMovement()
        {
            Vec3D pos1 = new(-1536, 256, 0);
            var moveEntity = GameActions.CreateEntity(World, "Cacodemon", pos1, frozen: false);
            GameActions.SetEntityPosition(World, Player, new Vec3D(-512, 256, 0));
            GameActions.SetEntityTarget(moveEntity, Player);

            GameActions.TickWorld(World, () => { return moveEntity.Position.X < -1056; }, () => { });

            moveEntity.Position.X.Should().Be(-1056);
            moveEntity.Position.Y.Should().Be(256);
            moveEntity.Position.Z.Should().Be(192);
        }

        [Fact(DisplayName = "Monster can walk on bridge")]
        public void MonsterBridgeWalk()
        {
            Vec3D pos1 = new(1168, 1064, 24);
            Vec3D moveTo = pos1 + new Vec3D(16, 0, 0);
            var moveEntity = GameActions.CreateEntity(World, "DoomImp", pos1, frozen: false);
            moveEntity.OnEntity.Should().NotBeNull();
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
    }
}
