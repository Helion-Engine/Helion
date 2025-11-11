using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Util.RandomGenerators;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Mbf21;

[Collection("GameActions")]
public class PlayerAttack
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public PlayerAttack()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, dehackedPatch: Dehacked, cacheWorld: false);
        var random = new NoRandom
        {
            RandomValue = 2
        };
        World.SetRandom(random);
        GameActions.SetEntityPosition(World, World.Player, (-320, -320));
    }


    [Fact(DisplayName = "WeaponProjectile")]
    public void WeaponProjectile()
    {
        SetState(176);
        var entity = GameActions.GetEntity(World, "PlasmaBall");

        entity.Position.X.Should().BeApproximately(-306.0106, 4);
        entity.Position.Y.Should().BeApproximately(-322.7558, 4);
        entity.Position.Z.Should().BeApproximately(35.5114, 4);

        entity.Velocity.X.Should().BeApproximately(24.9809, 4);
        entity.Velocity.Y.Should().BeApproximately(0.4360, 4);
        entity.Velocity.Z.Should().BeApproximately(-0.8724, 4);
    }

    [Fact(DisplayName = "WeaponBulletAttack Default")]
    public void WeaponBulletAttackDefault()
    {
        var monster = CreateMonster();
        SetState(177);
        monster.Health.Should().Be(985);
    }

    [Fact(DisplayName = "WeaponBulletAttack")]
    public void WeaponBulletAttack()
    {
        var monster = CreateMonster();
        SetState(178);
        monster.Health.Should().Be(928);
    }

    [Fact(DisplayName = "WeaponMeleeAttack Default")]
    public void WeaponMeleeAttackDefault()
    {
        var monster = CreateMonster();
        // Not in default range
        SetState(179);
        monster.Health.Should().Be(1000);

        SetState(180);
        monster.Health.Should().Be(994);
    }

    [Fact(DisplayName = "WeaponMeleeAttack")]
    public void WeaponMeleeAttack()
    {
        var monster = CreateMonster();
        SetState(181);
        monster.Health.Should().Be(988);
    }

    private void SetState(int frame)
    {
        Player.Weapon!.FrameState.SetState(Player, World.ArchiveCollection.Definitions.EntityFrameTable.VanillaFrameMap[frame]);
    }

    private Entity CreateMonster()
    {
        return GameActions.CreateEntity(World, "BaronOfHell", (-192, -320, 0), false);
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
Args4 = 8
Args5 = 6

FRAME 180
Args5 = 16777216

FRAME 181
Args1 = 4
Args2 = 20
Args5 = 16777216

[CODEPTR]
FRAME 176 = WeaponProjectile
FRAME 177 = WeaponBulletAttack
FRAME 178 = WeaponBulletAttack
FRAME 179 = WeaponMeleeAttack
FRAME 180 = WeaponMeleeAttack
FRAME 181 = WeaponMeleeAttack
";
}
