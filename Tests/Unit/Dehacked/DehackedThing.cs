using FluentAssertions;
using Helion.Dehacked;
using Xunit;

namespace Helion.Tests.Unit.Dehacked;

public class DehackedThing
{
    [Fact(DisplayName="Dehacked things")]
    public void DehackedThings()
    {
        string data = @"Thing 27 (some thing) 
ID # = 420
Hit points = 18000
initial frame = 726
First moving frame = 729
Reaction time = 1
Attack sound = 2
Bits = SOLID+SHOOTABLE+COUNTKILL+JUSTHIT
Speed = 19
Width = 2621440
Height = 10485760
Alert sound = 96
Action sound = 101
Death sound = 98
Pain sound = 97
Far attack frame = 736
Close attack frame = 773
Injury frame = 743
Pain chance = 5
Mass = 400
Death frame = 744
Exploding frame = 744
Missile damage = 80
Action sound = 44
Respawn frame = 45
Splash group = 1
Infighting group = 2
Projectile group = 3";

        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);

        dehacked.Things.Count.Should().Be(1);
        var thing = dehacked.Things[0];
        thing.Number.Should().Be(27);
        thing.ID.Should().Be(420);
        thing.Hitpoints.Should().Be(18000);
        thing.InitFrame.Should().Be(726);
        thing.FirstMovingFrame.Should().Be(729);
        thing.AlertSound.Should().Be(96);
        thing.AlertSound.Should().Be(96);
        thing.ReactionTime.Should().Be(1);
        thing.AttackSound.Should().Be(2);
        thing.InjuryFrame.Should().Be(743);
        thing.PainChance.Should().Be(5);
        thing.PainSound.Should().Be(97);
        thing.CloseAttackFrame.Should().Be(773);
        thing.FarAttackFrame.Should().Be(736);
        thing.DeathFrame.Should().Be(744);
        thing.ExplodingFrame.Should().Be(744);
        thing.DeathSound.Should().Be(98);
        thing.Speed.Should().Be(19);
        thing.Width.Should().Be(2621440);
        thing.Height.Should().Be(10485760);
        thing.Mass.Should().Be(400);
        thing.MisileDamage.Should().Be(80);
        thing.ActionSound.Should().Be(44);
        thing.RespawnFrame.Should().Be(45);
        thing.SplashGroup.Should().Be(1);
        thing.InfightingGroup.Should().Be(2);
        thing.ProjectileGroup.Should().Be(3);
        thing.Bits.Should().Be(4194374);
    }
}
