using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Util.RandomGenerators;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Mbf21;

[Collection("GameActions")]
public class MonsterAttack
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public MonsterAttack()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, dehackedPatch: Dehacked, cacheWorld: false);
        var random = new NoRandom
        {
            RandomValue = 1
        };
        World.SetRandom(random);
        World.Player.Health = 1000;
        GameActions.SetEntityPosition(World, World.Player, (-256, -320));
    }

    [Fact(DisplayName = "MonsterProjectile")]
    public void MonsterProjectile()
    {
        CreateEntityAndSetState(176);
        var entity = GameActions.GetEntity(World, "PlasmaBall");
        entity.AngleRadians.Should().BeApproximately(-2.6604, 4);

        entity.Position.X.Should().BeApproximately(-302.0137, 4);
        entity.Position.Y.Should().BeApproximately(-322.6860, 4);
        entity.Position.Z.Should().BeApproximately(35.3718, 4);

        entity.Velocity.X.Should().BeApproximately(24.9809, 4);
        entity.Velocity.Y.Should().BeApproximately(0.4360, 4);
        entity.Velocity.Z.Should().BeApproximately(-0.8724, 4);
    }

    [Fact(DisplayName = "MonsterBulletAttack Defaults")]
    public void MonsterBulletAttackDefaults()
    {
        CreateEntityAndSetState(177);
        Player.Health.Should().Be(1000);
    }

    [Fact(DisplayName = "MonsterBulletAttack")]
    public void MonsterBulletAttack()
    {
        CreateEntityAndSetState(178);
        Player.Health.Should().Be(970);
    }

    [Fact(DisplayName = "MonsterMeleeAttack Defaults")]
    public void MonsterMeleeAttackDefaults()
    {
        // Out of default range
        var entity = CreateEntityAndSetState(179);
        Player.Health.Should().Be(1000);

        World.EntityManager.Destroy(entity);
        
        CreateEntityAndSetState(180);
        Player.Health.Should().Be(994);
    }

    [Fact(DisplayName = "MonsterMeleeAttack")]
    public void MonsterMeleeAttack()
    {
        CreateEntityAndSetState(181);
        Player.Health.Should().Be(980);
    }

    private Entity CreateEntityAndSetState(int frame)
    {
        var entity = GameActions.CreateEntity(World, "ZombieMan", (-320, -320, 0), false);
        entity.SetTarget(Player);
        entity.FrameState.SetState(entity, World.ArchiveCollection.Definitions.EntityFrameTable.VanillaFrameMap[frame]);
        return entity;
    }

    private static readonly string Dehacked =
@"
FRAME 176
Args1 = 35
Args2 = 65536
Args3 = 131072
Args4 = 196608
Args5 = 262144

FRAME 178
Args1 = 65536
Args2 = 65536
Args3 = 3
Args4 = 5
Args5 = 10

FRAME 180
Args4 = 16777216

FRAME 181
Args1 = 10
Args2 = 15
Args3 = 3
Args4 = 16777216

[CODEPTR]
FRAME 176 = MonsterProjectile
FRAME 177 = MonsterBulletAttack
FRAME 178 = MonsterBulletAttack
FRAME 179 = MonsterMeleeAttack
FRAME 180 = MonsterMeleeAttack
FRAME 181 = MonsterMeleeAttack
";
}
