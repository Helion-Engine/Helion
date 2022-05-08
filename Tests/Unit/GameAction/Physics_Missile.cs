using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.World.Entities;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    public partial class Physics
    {
        private Vec3D MissileCenterPlayerPos = new(1920, 896, 0);
        private Vec3D MissileCenterPos = new(1920, 896, 32);
        private Vec3D MissileCenterPosSky = new(2304, 896, 32);

        [Fact(DisplayName = "Missile explodes on wall")]
        public void MissileHitsWall()
        {
            var line = GameActions.GetLine(World, 266);
            var rocket = GameActions.CreateEntity(World, "Rocket", MissileCenterPos);
            rocket.AngleRadians = GameActions.GetAngle(Bearing.North);
            rocket.Velocity = Vec3D.UnitSphere(rocket.AngleRadians, 0) * 16;

            RunMissileExplode(rocket);
            rocket.Position.Should().Be(new Vec3D(1920, 1008, 32));

            RunEntityDisposed(rocket);
        }

        [Fact(DisplayName = "Missile explodes on ceiling plane")]
        public void MissileHitsCeilingPlane()
        {
            var rocket = GameActions.CreateEntity(World, "Rocket", MissileCenterPos);
            rocket.AngleRadians = GameActions.GetAngle(Bearing.North);
            rocket.Velocity = Vec3D.UnitSphere(rocket.AngleRadians, 64) * 16;

            RunMissileExplode(rocket);
            rocket.Position.Z.Should().Be(248);

            RunEntityDisposed(rocket);
        }

        [Fact(DisplayName = "Missile explodes on floor plane")]
        public void MissileHitsFloorPlane()
        {
            var rocket = GameActions.CreateEntity(World, "Rocket", MissileCenterPos);
            rocket.AngleRadians = GameActions.GetAngle(Bearing.North);
            rocket.Velocity = Vec3D.UnitSphere(rocket.AngleRadians, -64) * 16;

            RunMissileExplode(rocket);
            rocket.Position.Z.Should().Be(0);

            RunEntityDisposed(rocket);
        }

        [Fact(DisplayName = "Missile removed when hitting sky plane")]
        public void MissileHitsSkyPlane()
        {
            var rocket = GameActions.CreateEntity(World, "Rocket", MissileCenterPosSky);
            rocket.AngleRadians = GameActions.GetAngle(Bearing.North);
            rocket.Velocity = Vec3D.UnitSphere(rocket.AngleRadians, 64) * 16;

            RunMissileDispose(rocket);
            rocket.Position.Z.Should().Be(248);
        }

        [Fact(DisplayName = "Missile removed when hitting sky line")]
        public void MissileHitsSkyLine()
        {
            var rocket = GameActions.CreateEntity(World, "Rocket", MissileCenterPosSky);
            rocket.AngleRadians = GameActions.GetAngle(Bearing.East);
            rocket.Velocity = Vec3D.UnitSphere(rocket.AngleRadians, 0) * 16;

            RunMissileDispose(rocket);
            rocket.Position.Should().Be(new Vec3D(2416, 896, 32));
        }

        [Fact(DisplayName = "Missile hits entity")]
        public void MissileHitsEntity()
        {
            var entity = GameActions.CreateEntity(World, "Zombieman", MissileCenterPlayerPos + new Vec3D(0, 96, 0));
            var rocket = GameActions.CreateEntity(World, "Rocket", MissileCenterPos);
            rocket.AngleRadians = GameActions.GetAngle(Bearing.North);
            rocket.Velocity = Vec3D.UnitSphere(rocket.AngleRadians, 0) * 16;

            RunMissileExplode(rocket);
            rocket.Position.Should().Be(new Vec3D(1920, 960, 32));
            entity.Velocity.ApproxEquals(new(0, 4.53125, 0));

            RunEntityDisposed(rocket);
        }

        [Fact(DisplayName = "Missile hits entity feet")]
        public void MissileHitsEntityFeet()
        {
            var entity = GameActions.CreateEntity(World, "Zombieman", MissileCenterPlayerPos + new Vec3D(0, 96, 0));
            var rocket = GameActions.CreateEntity(World, "Rocket", MissileCenterPos);
            rocket.AngleRadians = GameActions.GetAngle(Bearing.North);
            rocket.Velocity = Vec3D.UnitSphere(rocket.AngleRadians, rocket.PitchTo(entity)) * 16;

            RunMissileExplode(rocket);
            rocket.Position.ApproxEquals(new(1920, 956.71573107523272, 11.761422974922368));
            entity.Velocity.ApproxEquals(new(0, 4.53125, 0));

            RunEntityDisposed(rocket);
        }

        [Fact(DisplayName = "Player fires missile against wall")]
        public void PlayerFiresMissileAgainstWall()
        {
            GameActions.SetEntityPosition(World, Player, new Vec3D(1920, 1004, 0));
            Player.Velocity = Vec3D.Zero;
            Player.AngleRadians = GameActions.GetAngle(Bearing.North);
            World.FireProjectile(Player, Player.AngleRadians, 0, 0, false, "Rocket", out _).Should().BeNull();
            Player.Velocity.ApproxEquals(new(0, -13.875, 0));
        }

        [Fact(DisplayName = "Player fires missile directly below feet")]
        public void PlayerRocketJumpStraight()
        {
            GameActions.SetEntityPosition(World, Player, MissileCenterPlayerPos);
            Player.Velocity = Vec3D.Zero;
            Player.AngleRadians = GameActions.GetAngle(Bearing.North);
            var rocket = World.FireProjectile(Player, Player.AngleRadians, -1.5697963267948967, 0, false, "Rocket", out _);
            rocket.Should().NotBeNull();
            RunMissileExplode(rocket!);
            Player.Velocity.ApproxEquals(new(0, 0, 16));
        }

        [Fact(DisplayName = "Player fires missile offset")]
        public void PlayerRocketJumpOffseet()
        {
            GameActions.SetEntityPosition(World, Player, MissileCenterPlayerPos.XY.To3D(64));
            Player.Velocity = Vec3D.Zero;
            Player.AngleRadians = GameActions.GetAngle(Bearing.North);
            var rocket = World.FireProjectile(Player, Player.AngleRadians, -1.4555555968545377, 0, false, "Rocket", out _);
            rocket.Should().NotBeNull();
            RunMissileExplode(rocket!);
            Player.Velocity.ApproxEquals(new(0, -10.875, 5.532654554741212));
        }

        private void RunMissileExplode(Entity rocket)
        {
            GameActions.TickWorld(World, () =>
            {
                return rocket.Frame.ActionFunction == null || !rocket.Frame.ActionFunction.Method.Name.Equals("A_Explode");
            }, () => { });
        }

        private void RunMissileDispose(Entity rocket)
        {
            GameActions.TickWorld(World, () => {  return !rocket.IsDisposed; }, () =>
            {
                if (rocket.Frame.ActionFunction != null)
                    rocket.Frame.ActionFunction.Method.Name.Equals("A_Explode").Should().BeFalse();
            });
        }

        private void RunEntityDisposed(Entity entity) =>
            GameActions.TickWorld(World, () => { return !entity.IsDisposed; }, () => { });
    }
}
