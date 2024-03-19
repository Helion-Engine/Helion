using FluentAssertions;
using Helion.Dehacked;
using Xunit;

namespace Helion.Tests.Unit.Dehacked;

public class DehackedMisc
{
    [Fact(DisplayName="Dehacked misc")]
    public void DehackedMiscellaneous()
    {
        string data = @"Misc whocares
Initial Health = 1
Initial Bullets = 2
Max Health = 3
Max Armor = 4
Green Armor Class = 5
Blue Armor Class = 6
Max Soulsphere = 7
Soulsphere Health = 8
Megasphere Health = 9
God Mode Health = 10
IDFA Armor = 11
IDFA Armor Class = 12
IDKFA Armor = 13
IDKFA Armor Class = 14
BFG Cells/Shot = 15
Monsters Infight = 221
Monsters Ignore Each Other = 1";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);

        dehacked.Misc.Should().NotBeNull();
        var misc = dehacked.Misc!;
        misc.InitialHealth.Should().Be(1);
        misc.InitialBullets.Should().Be(2);
        misc.MaxHealth.Should().Be(3);
        misc.MaxArmor.Should().Be(4);
        misc.GreenArmorClass.Should().Be(5);
        misc.BlueArmorClass.Should().Be(6);
        misc.MaxSoulsphere.Should().Be(7);
        misc.SoulsphereHealth.Should().Be(8);
        misc.MegasphereHealth.Should().Be(9);
        misc.GodModeHealth.Should().Be(10);
        misc.IdfaArmor.Should().Be(11);
        misc.IdfaArmorClass.Should().Be(12);
        misc.IdkfaArmor.Should().Be(13);
        misc.IdkfaArmorClass.Should().Be(14);
        misc.BfgCellsPerShot.Should().Be(15);
        misc.MonstersInfight.Should().Be(MonsterInfightType.Enable);
        misc.MonstersIgnoreEachOther.Should().Be(true);


    }
}
