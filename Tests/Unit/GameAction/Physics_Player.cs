using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;
using Helion.World.Entities;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    public partial class Physics
    {
        private static readonly Vec3D PlayerTestPos = new(1088, 512, 0);
        private static readonly Vec3D PlayerJumpBlockPos = new(1216, 512, 0);
        private static readonly Vec3D PlayerJumpBlockPartialPos = new(1344, 512, 0);
        private static readonly Vec3D PlayerJumpEntityPos = new(1216, 640, 48);

        private static readonly Vec3D PlayerStepPos = new(1056, 1040, 0);
        private static readonly Vec3D PlayerStepBlockPos = new(1120, 1040, 0);

        private static readonly Vec3D PlayerStepEntityPos = new(1184, 1024, 0);
        private static readonly Vec3D PlayerStepEntityBlockPos = new(1248, 1024, 0);
        private static readonly Vec3D PlayerBridgeBlockPos = new(1328, 1024, 0);
        private static readonly Vec3D PlayerBridgeUnderPos = new(1392, 1024, 0);
        private static readonly Vec3D PlayerWalkBridgePos = new(1120, 1072, 0);

        [Fact(DisplayName = "Player jump from normal sector floors")]
        public void PlayerJumpSector()
        {
            GameActions.SetEntityPosition(World, Player, PlayerTestPos);
            Player.Jump();
            GameActions.RunPlayerJump(World, Player);
        }

        [Fact(DisplayName = "Player jump completely blocked")]
        public void PlayerJumpBlock()
        {
            GameActions.SetEntityPosition(World, Player, PlayerJumpBlockPos);
            Player.Jump();
            Player.BlockingSectorPlane.Should().BeNull();
            Player.OnGround.Should().BeTrue();
            Player.Velocity.Z.Should().Be(Player.Properties.Player.JumpZ);
            World.Tick();

            // Completely blocked by the ceiling
            Player.OnGround.Should().BeTrue();
            Player.BlockingSectorPlane.Should().NotBeNull();
            Player.BlockingSectorPlane!.Sector.Id.Should().Be(15);
        }

        [Fact(DisplayName = "Player jump partially blocked")]
        public void PlayerJumpPartialBlock()
        {
            GameActions.SetEntityPosition(World, Player, PlayerJumpBlockPartialPos);
            Player.Sector.Ceiling.Z.Should().Be(72);
            Player.Jump();
            Player.BlockingSectorPlane.Should().BeNull();
            Player.OnGround.Should().BeTrue();
            Player.Velocity.Z.Should().Be(Player.Properties.Player.JumpZ);
            World.Tick();

            Player.OnGround.Should().BeFalse();

            GameActions.TickWorld(World, () => { return Player.Position.Z < 16; }, () => { });
            Player.BlockingSectorPlane.Should().NotBeNull();
            Player.BlockingSectorPlane!.Sector.Id.Should().Be(16);
            World.Tick();
            Player.Velocity.Z.Should().Be(-1);
        }

        [Fact(DisplayName = "Player jump from entity")]
        public void PlayerJumpEntity()
        {
            GameActions.SetEntityPosition(World, Player, PlayerJumpEntityPos);
            Player.OnEntity.Should().NotBeNull();
            Player.OnGround.Should().BeTrue();
            Player.Position.Z.Should().Be(Player.OnEntity!.Height);
            Player.Jump();
            GameActions.RunPlayerJump(World, Player);
            Player.OnGround.Should().BeTrue();
            Player.OnEntity.Should().NotBeNull();
            Player.Position.Z.Should().Be(Player.OnEntity!.Height);
        }

        [Fact(DisplayName = "Player step up (max stair height)")]
        public void PlayerStepUp()
        {
            GameActions.SetEntityPosition(World, Player, PlayerStepPos);
            Player.Position.Z.Should().Be(0);
            GameActions.MoveEntity(World, Player, Player.Position.XY + new Vec2D(0, 16));
            Player.Position.Z.Should().Be(24);
            Player.Sector.Id.Should().Be(17);
            Player.OnGround.Should().BeTrue();
        }

        [Fact(DisplayName = "Player step block (max stair height + 1)")]
        public void PlayerStepBlock()
        {
            GameActions.SetEntityPosition(World, Player, PlayerStepBlockPos);
            Player.Position.Z.Should().Be(0);
            GameActions.MoveEntity(World, Player, Player.Position.XY + new Vec2D(0, 16));
            Player.Position.Z.Should().Be(0);
            Player.Sector.Id.Should().Be(9);
            Player.OnGround.Should().BeTrue();
        }

        [Fact(DisplayName = "Player step up entity (max stair height)")]
        public void PlayerStepUpEntity()
        {
            GameActions.SetEntityPosition(World, Player, PlayerStepEntityPos);
            Player.Position.Z.Should().Be(0);
            GameActions.MoveEntity(World, Player, Player.Position.XY + new Vec2D(0, 32));
            Player.Position.Z.Should().Be(24);
            Player.Sector.Id.Should().Be(9);
            Player.OnGround.Should().BeTrue();
        }

        [Fact(DisplayName = "Player step entity (max stair height + 1)")]
        public void PlayerStepEntityBlock()
        {
            GameActions.SetEntityPosition(World, Player, PlayerStepEntityBlockPos);
            Player.Position.Z.Should().Be(0);
            GameActions.MoveEntity(World, Player, Player.Position.XY + new Vec2D(0, 32));
            Player.Position.Z.Should().Be(0);
            Player.Sector.Id.Should().Be(9);
            Player.OnGround.Should().BeTrue();
        }

        [Fact(DisplayName = "Player blocked by 3d bridge")]
        public void BridgeBlock()
        {
            GameActions.SetEntityPosition(World, Player, PlayerBridgeBlockPos);
            Player.Position.Z.Should().Be(0);
            GameActions.MoveEntity(World, Player, Player.Position.XY + new Vec2D(0, 32));
            ApproxEquals(Player.Position, new Vec3D(1328, 1024, 0)).Should().BeTrue();
            Player.Position.Z.Should().Be(0);
            Player.Sector.Id.Should().Be(9);
            Player.OnGround.Should().BeTrue();
        }

        [Fact(DisplayName = "Player move under 3d bridge")]
        public void BridgeUnder()
        {
            GameActions.SetEntityPosition(World, Player, PlayerBridgeUnderPos);
            Player.Position.Z.Should().Be(0);
            GameActions.MoveEntity(World, Player, Player.Position.XY + new Vec2D(0, 32));
            ApproxEquals(Player.Position, PlayerBridgeUnderPos + new Vec3D(0, 32, 0)).Should().BeTrue();
            Player.Sector.Id.Should().Be(9);
            Player.OnGround.Should().BeTrue();
        }

        [Fact(DisplayName = "Player walk 3d bridge")]
        public void BridgeWalk()
        {
            GameActions.SetEntityPosition(World, Player, PlayerWalkBridgePos);
            Player.Position.Z.Should().Be(25);
            Vec2D step = new(64, 0);
            Vec2D position = Player.Position.XY;

            var bridges = new Entity[]
            {
                GameActions.GetEntity(World, 2),
                GameActions.GetEntity(World, 3),
                GameActions.GetEntity(World, 4),
                GameActions.GetEntity(World, 5)
            };

            foreach (var bridge in bridges)
            {
                GameActions.MoveEntity(World, Player, Player.Position.XY + step);
                World.Tick();
                World.Tick();
                position += step;
                ApproxEquals(Player.Position, position.To3D(bridge.Position.Z + 8)).Should().BeTrue();
                Player.OnEntity.Should().NotBeNull();
                Player.OnEntity.Should().Be(bridge);
                Player.Sector.Id.Should().Be(9);
                Player.OnGround.Should().BeTrue();
            }
        }

        private static bool ApproxEquals(Vec3D v1, Vec3D v2)
        {
            return v1.X.ApproxEquals(v2.X) && v1.Y.ApproxEquals(v2.Y) && v1.Z.ApproxEquals(v2.Z);
        }
    }
}
