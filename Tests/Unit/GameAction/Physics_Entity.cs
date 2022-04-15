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

        [Fact(DisplayName = "OnEntity/OverEntity simple stack change")]
        public void StackEntityChange()
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
    }
}
