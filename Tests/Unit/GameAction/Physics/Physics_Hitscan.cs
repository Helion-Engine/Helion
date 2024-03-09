using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Util;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

public partial class Physics
{
    private Vec3D HitscanEnemyPos = new(1920, 896, 0);
    private Vec3D HitscanPlayerPos = new(1920, 832, 0);

    [Fact(DisplayName = "Hitscan")]
    public void Hitscan()
    {
        var entity = GameActions.CreateEntity(World, "Zombieman", HitscanEnemyPos);
        GameActions.SetEntityPosition(World, Player, HitscanPlayerPos);
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        var hitEntity = World.FireHitscan(Player, Player.AngleRadians, 0, 8192, Constants.HitscanTestDamage);
        hitEntity.Should().Be(entity, "Shootable and solid");

        entity.Flags.Solid = false;
        hitEntity = World.FireHitscan(Player, Player.AngleRadians, 0, 8192, Constants.HitscanTestDamage);
        hitEntity.Should().Be(entity, "Shootable and non-solid");

        entity.Flags.Shootable = false;
        hitEntity = World.FireHitscan(Player, Player.AngleRadians, 0, 8192, Constants.HitscanTestDamage);
        hitEntity.Should().BeNull("Non-shootable and non-solid");

        entity.Flags.Solid = false;
        hitEntity = World.FireHitscan(Player, Player.AngleRadians, 0, 8192, Constants.HitscanTestDamage);
        hitEntity.Should().BeNull("Non-shootable and solid");
    }
}
