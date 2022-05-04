using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;
using Helion.World.Entities;
using Helion.World.Physics;
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
            Player.OnEntity.Entity.Should().NotBeNull();
            Player.OnGround.Should().BeTrue();
            Player.Position.Z.Should().Be(Player.OnEntity.Entity!.Height);
            Player.Jump();
            GameActions.RunPlayerJump(World, Player);
            Player.OnGround.Should().BeTrue();
            Player.OnEntity.Entity.Should().NotBeNull();
            Player.Position.Z.Should().Be(Player.OnEntity.Entity!.Height);
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
                Player.OnEntity.Entity.Should().NotBeNull();
                Player.OnEntity.Entity.Should().Be(bridge);
                Player.Sector.Id.Should().Be(9);
                Player.OnGround.Should().BeTrue();
            }
        }

        [Fact(DisplayName = "Player can run on ledges")]
        public void LedgeRun()
        {
            // This one is designed to fail if we are missing doom's gravity skip
            // See comment in PhysicsManager.MoveZ near noVelocity variable
            GameActions.SetEntityPosition(World, Player, new Vec3D(1104, 1312, 0));
            GameActions.PlayerRunForward(World, GameActions.GetAngle(Bearing.East), () => { return Player.Position.X < 1760; });

            Player.Position.X.Should().BeGreaterOrEqualTo(1760);
        }

        [Fact(DisplayName = "Player fall doesn't hard hit")]
        public void PlayerFallNoHardHit()
        {
            GameActions.SetEntityPosition(World, Player, new Vec3D(320, -64, 35));
            Player.ViewZ.Should().Be(41);
            Player.DeltaViewHeight.Should().Be(0);

            GameActions.TickWorld(World, () => { return !Player.OnGround; }, () => { });

            Player.DeltaViewHeight.Should().Be(0);
            Player.ViewZ.Should().Be(41);

            // No oof sound
            World.SoundManager.FindBySource(Player).Should().BeNull();
        }

        [Fact(DisplayName = "Player fall hard hit")]
        public void PlayerFallHardHit()
        {
            GameActions.SetEntityPosition(World, Player, new Vec3D(320, -64, 64));
            Player.ViewZ.Should().Be(41);
            Player.DeltaViewHeight.Should().Be(0);

            GameActions.TickWorld(World, () => { return !Player.OnGround; }, () => { });
            Player.DeltaViewHeight.Should().Be(-1.375);
            Player.ViewZ.Should().Be(41);

            var audio = World.SoundManager.FindBySource(Player);
            audio.Should().NotBeNull();
            audio!.AudioData.SoundInfo.EntryName.EqualsIgnoreCase("dsoof").Should().BeTrue();

            double[] values = new[] { 39.625, 38.5, 37.625, 37, 36.625, 36.5, 36.625, 37, 37.625, 38.5, 39.625 };
            int index = 0;

            GameActions.TickWorld(World, () => { return Player.ViewZ != 41; }, () =>
            {
                Player.ViewZ.Should().Be(values[index]);
                index++;
            });

            index.Should().Be(values.Length);

            World.Tick();
            Player.DeltaViewHeight.Should().Be(0);
            GameActions.TickWorld(World, 35);
            World.SoundManager.FindBySource(Player).Should().BeNull();
        }

        [Fact(DisplayName = "Player fall hard hit (maximum delta)")]
        public void PlayerFallHardHitMax()
        {
            GameActions.SetEntityPosition(World, Player, new Vec3D(320, -64, 2048));
            Player.ViewZ.Should().Be(41);
            Player.DeltaViewHeight.Should().Be(0);

            GameActions.TickWorld(World, () => { return !Player.OnGround; }, () => { });
            Player.DeltaViewHeight.Should().Be(-8);
            Player.ViewZ.Should().Be(41);

            var audio = World.SoundManager.FindBySource(Player);
            audio.Should().NotBeNull();
            audio!.AudioData.SoundInfo.EntryName.EqualsIgnoreCase("dsoof").Should().BeTrue();

            double[] values = new[] { 33, 25.25, 20.5, 20.75, 21.25, 22, 23, 24.25, 25.75, 27.5, 29.5, 31.75, 34.25, 37, 40 };
            int index = 0;

            GameActions.TickWorld(World, () => { return Player.ViewZ != 41; }, () =>
            {
                Player.ViewZ.Should().Be(values[index]);
                index++;
            });

            index.Should().Be(values.Length);

            World.Tick();
            Player.DeltaViewHeight.Should().Be(0);
            GameActions.TickWorld(World, 35);
            World.SoundManager.FindBySource(Player).Should().BeNull();
        }

        [Fact(DisplayName = "Player step up and smooths out view height change")]
        public void PlayerStepUpSmoothViewHeight()
        {
            GameActions.SetEntityPosition(World, Player, PlayerStepPos);
            Player.ViewZ.Should().Be(41);
            Player.DeltaViewHeight.Should().Be(0);

            Player.Position.Z.Should().Be(0);
            GameActions.MoveEntity(World, Player, Player.Position.XY + new Vec2D(0, 16));
            Player.Position.Z.Should().Be(24);
            Player.Sector.Id.Should().Be(17);
            Player.OnGround.Should().BeTrue();

            Player.DeltaViewHeight.Should().Be(3.25);
                        
            double[] values = new[] { 23.75, 27.25, 31, 35, 39.25 };
            int index = 0;

            GameActions.TickWorld(World, () => { return Player.ViewZ != 41; }, () =>
            {
                Player.ViewZ.Should().Be(values[index]);
                index++;
            });

            index.Should().Be(values.Length);

            World.Tick();
            Player.DeltaViewHeight.Should().Be(0);
        }

        [Fact(DisplayName = "Player runs up stairs and smooths out view height change")]
        public void StepRun()
        {
            GameActions.SetEntityPosition(World, Player, new Vec3D(-192, -1248, 0));

            Player.ViewZ.Should().Be(41);
            Player.DeltaViewHeight.Should().Be(0);

            double[] delta = new[] { 0, 0, 0, 0, 0, 2.25, 2.5, 2.75, 3, 2.9375, 3.1875, 3.4375, 3.6171875, 3.8671875, 3.876953125, 4.126953125, 4.376953125, 3.264892578125, 3.514892578125, 3.96502685546875, 4.21502685546875, 3.7899932861328125, 4.0399932861328125, 4.2899932861328125, 3.2975025177001953, 3.5475025177001953, 3.7975025177001953, 4.047502517700195, 4.297502517700195, 4.547502517700195, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            double[] view = new[] { 41, 41, 41, 41, 41, 25, 27.25, 29.75, 32.5, 20.5, 23.4375, 26.625, 20.5, 24.1171875, 20.5, 24.376953125, 28.50390625, 20.5, 23.764892578125, 20.5, 24.46502685546875, 20.5, 24.289993286132812, 28.329986572265625, 20.5, 23.797502517700195, 27.34500503540039, 31.142507553100586, 35.19001007080078, 39.48751258850098, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41, 41 };
            int index = 0;

            GameActions.PlayerRunForward(World, GameActions.GetAngle(Bearing.North), () => { return Player.Position.Y < -448; }, 
                onTick: () =>
            {
                Player.DeltaViewHeight.Should().Be(delta[index]);
                Player.ViewZ.Should().Be(view[index]);
                index++;
            });

            index.Should().Be(delta.Length);
            Player.Position.Y.Should().BeGreaterOrEqualTo(-448);
        }

        [Fact(DisplayName = "Player uses one sided line with no activation")]
        public void PlayerUseFailOneSidedLine()
        {
            GameActions.SetEntityPosition(World, Player, new Vec3D(-224, -224, 0));
            Player.AngleRadians = GameActions.GetAngle(Bearing.West);
            World.EntityUse(Player).Should().BeFalse();

            var audio = World.SoundManager.FindBySource(Player);
            audio.Should().NotBeNull();
            audio!.AudioData.SoundInfo.EntryName.EqualsIgnoreCase("dsnoway").Should().BeTrue();

            GameActions.TickWorld(World, 70);
            World.SoundManager.FindBySource(Player).Should().BeNull();
        }

        [Fact(DisplayName = "Player uses two sided line with no activation")]
        public void PlayerUseFailTwoSidedLine()
        {
            GameActions.SetEntityPosition(World, Player, new Vec3D(-128, 480, 0));
            Player.AngleRadians = GameActions.GetAngle(Bearing.South);
            World.EntityUse(Player).Should().BeFalse();

            var audio = World.SoundManager.FindBySource(Player);
            audio.Should().NotBeNull();
            audio!.AudioData.SoundInfo.EntryName.EqualsIgnoreCase("dsnoway").Should().BeTrue();

            GameActions.TickWorld(World, 70);
            World.SoundManager.FindBySource(Player).Should().BeNull();
        }

        [Fact(DisplayName = "Player can activate switch above head")]
        public void PlayerUseHeightIgnored()
        {
            GameActions.SetEntityPosition(World, Player, new Vec3D(864, 320, 0));
            Player.AngleRadians = GameActions.GetAngle(Bearing.East);
            World.EntityUse(Player).Should().BeTrue();

            var sector = GameActions.GetSectorByTag(World, 3);
            sector.ActiveFloorMove.Should().NotBeNull();
            GameActions.RunSectorPlaneSpecial(World, sector);
        }

        [Fact(DisplayName = "Player can't activate switch through a closed door")]
        public void PlayerUseDoorBlock()
        {
            GameActions.SetEntityPosition(World, Player, new Vec3D(864, 224, 0));
            Player.AngleRadians = GameActions.GetAngle(Bearing.East);
            World.EntityUse(Player).Should().BeFalse();

            var audio = World.SoundManager.FindBySource(Player);
            audio.Should().NotBeNull();
            audio!.AudioData.SoundInfo.EntryName.EqualsIgnoreCase("dsnoway").Should().BeTrue();

            var sector = GameActions.GetSectorByTag(World, 3);
            sector.ActiveFloorMove.Should().BeNull();

            GameActions.TickWorld(World, 70);
            World.SoundManager.FindBySource(Player).Should().BeNull();

            var doorSector = GameActions.GetSector(World, 53);
            doorSector.Ceiling.SetZ(129);
            World.EntityUse(Player).Should().BeTrue();

            sector.ActiveFloorMove.Should().NotBeNull();
            GameActions.RunSectorPlaneSpecial(World, sector);
        }

        [Fact(DisplayName = "Player pickup item XY movement")]
        public void PlayerPickupItem()
        {
            GameActions.SetEntityPosition(World, Player, new Vec2D(320, 0));
            var bonus = GameActions.CreateEntity(World, "HealthBonus", new Vec3D(320, 32, 0));
            Player.Health = 100;
            GameActions.MoveEntity(World, Player, new Vec2D(320, 32));
            bonus.IsDisposed.Should().BeTrue();
            Player.Health.Should().Be(101);
        }

        [Fact(DisplayName = "Player pickup item sector movement")]
        public void PlayerPickupItemSectorMovement()
        {
            GameActions.SetEntityPosition(World, Player, new Vec2D(216, 428));
            var bonus = GameActions.CreateEntity(World, "HealthBonus", new Vec3D(216, 428, 0));
            Player.Health = 100;
            GameActions.ActivateLine(World, Player, 16, ActivationContext.UseLine);
            GameActions.RunSectorPlaneSpecial(World, GameActions.GetSectorByTag(World, 2));
            bonus.IsDisposed.Should().BeTrue();
            Player.Health.Should().Be(101);
        }

        private static bool ApproxEquals(Vec3D v1, Vec3D v2)
        {
            return v1.X.ApproxEquals(v2.X) && v1.Y.ApproxEquals(v2.Y) && v1.Z.ApproxEquals(v2.Z);
        }
    }
}
