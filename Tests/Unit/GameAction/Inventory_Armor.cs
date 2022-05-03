using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Cheats;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    public partial class Inventory
    {
        [Fact(DisplayName = "Give armor bonus")]
        public void GiveArmorBonus()
        {
            var armorBonus = GameActions.CreateEntity(World, "ArmorBonus", Vec3D.Zero);
            Player.GiveItem(armorBonus.Definition, armorBonus.Flags).Should().BeTrue();
            Player.Armor.Should().Be(1);

            for (int i = 0; i < 199; i++)
            {
                Player.GiveItem(armorBonus.Definition, armorBonus.Flags).Should().BeTrue();
                Player.Armor.Should().Be(1 + i + 1);
            }

            Player.Armor.Should().Be(200);
            // Has always pickup flag, should return true
            Player.GiveItem(armorBonus.Definition, armorBonus.Flags).Should().BeTrue();
            Player.Armor.Should().Be(200);
            Player.Inventory.HasItem("ArmorBonus").Should().BeFalse();
        }

        [Fact(DisplayName = "Give green armor")]
        public void GiveGreenArmor()
        {
            var armor = GameActions.CreateEntity(World, "GreenArmor", Vec3D.Zero);
            var armorBonus = GameActions.CreateEntity(World, "ArmorBonus", Vec3D.Zero);
            Player.Armor.Should().Be(0);
            Player.ArmorDefinition.Should().BeNull();
            Player.GiveItem(armor.Definition, armor.Flags).Should().BeTrue();

            Player.Armor.Should().Be(100);
            Player.ArmorDefinition.Should().Be(armor.Definition);

            Player.Inventory.HasItem("GreenArmor").Should().BeFalse();

            // At max armor for green
            Player.GiveItem(armor.Definition, armor.Flags).Should().BeFalse();

            Player.GiveItem(armorBonus.Definition, armorBonus.Flags).Should().BeTrue();
            Player.Armor.Should().Be(101);
            Player.ArmorDefinition.Should().Be(armor.Definition);
        }

        [Fact(DisplayName = "Give blue armor")]
        public void GiveBlueArmor()
        {
            var armor = GameActions.CreateEntity(World, "BlueArmor", Vec3D.Zero);
            var armorBonus = GameActions.CreateEntity(World, "ArmorBonus", Vec3D.Zero);
            Player.Armor.Should().Be(0);
            Player.ArmorDefinition.Should().BeNull();
            Player.GiveItem(armor.Definition, armor.Flags).Should().BeTrue();

            Player.Armor.Should().Be(200);
            Player.ArmorDefinition.Should().Be(armor.Definition);

            Player.Inventory.HasItem("BlueArmor").Should().BeFalse();

            // At max armor for green
            Player.GiveItem(armor.Definition, armor.Flags).Should().BeFalse();

            // Giving armor bonus should keep blue armor
            Player.GiveItem(armorBonus.Definition, armorBonus.Flags).Should().BeTrue();
            Player.Armor.Should().Be(200);
            Player.ArmorDefinition.Should().Be(armor.Definition);

            Player.Armor = 99;
            Player.GiveItem(armorBonus.Definition, armorBonus.Flags).Should().BeTrue();
            Player.Armor.Should().Be(100);
            Player.ArmorDefinition.Should().Be(armor.Definition);

            var greenArmor = GameActions.CreateEntity(World, "GreenArmor", Vec3D.Zero);
            Player.GiveItem(greenArmor.Definition, greenArmor.Flags).Should().BeFalse();
        }

        [Fact(DisplayName = "Armor type changes between blue and green")]
        public void ArmorTypeChange()
        {
            var greenArmor = GameActions.CreateEntity(World, "GreenArmor", Vec3D.Zero);
            var blueArmor = GameActions.CreateEntity(World, "BlueArmor", Vec3D.Zero);
            var armorBonus = GameActions.CreateEntity(World, "ArmorBonus", Vec3D.Zero);

            Player.Armor.Should().Be(0);
            Player.ArmorDefinition.Should().BeNull();
            Player.GiveItem(greenArmor.Definition, greenArmor.Flags).Should().BeTrue();
            Player.ArmorDefinition.Should().Be(greenArmor.Definition);

            Player.GiveItem(blueArmor.Definition, blueArmor.Flags).Should().BeTrue();
            Player.ArmorDefinition.Should().Be(blueArmor.Definition);

            Player.GiveItem(greenArmor.Definition, greenArmor.Flags).Should().BeFalse();
            Player.ArmorDefinition.Should().Be(blueArmor.Definition);

            // Picking up armor bonus keeps green armor
            Player.Armor = 99;
            Player.GiveItem(armorBonus.Definition, armorBonus.Flags).Should().BeTrue();
            Player.ArmorDefinition.Should().Be(blueArmor.Definition);

            // Picking up green armor with blue type changes back to green
            Player.Armor = 99;
            Player.GiveItem(greenArmor.Definition, greenArmor.Flags).Should().BeTrue();
            Player.ArmorDefinition.Should().Be(greenArmor.Definition);

            Player.GiveItem(blueArmor.Definition, blueArmor.Flags).Should().BeTrue();
            Player.ArmorDefinition.Should().Be(blueArmor.Definition);
        }

        [Fact(DisplayName = "Green armor damage")]
        public void GreenArmorDamage()
        {
            if (Player.Cheats.IsCheatActive(CheatType.God))
                World.CheatManager.ActivateCheat(Player, CheatType.God);

            Player.Health.Should().Be(100);
            Player.Armor.Should().Be(0);
            Player.Damage(null, 10, false, DamageType.Normal).Should().BeTrue();
            Player.Health.Should().Be(90);

            var armor = GameActions.CreateEntity(World, "GreenArmor", Vec3D.Zero);
            Player.GiveItem(armor.Definition, armor.Flags).Should().BeTrue();
            Player.Armor.Should().Be(100);

            Player.Damage(null, 10, false, DamageType.Normal).Should().BeTrue();
            Player.Health.Should().Be(83);
            Player.Armor.Should().Be(97);

            Player.Health = 100;
            Player.Armor = 1;

            Player.Damage(null, 10, false, DamageType.Normal).Should().BeTrue();
            Player.Health.Should().Be(91);
            Player.Armor.Should().Be(0);
        }

        [Fact(DisplayName = "Blue armor damage")]
        public void BlueArmorDamage()
        {
            if (Player.Cheats.IsCheatActive(CheatType.God))
                World.CheatManager.ActivateCheat(Player, CheatType.God);

            Player.Health.Should().Be(100);
            Player.Armor.Should().Be(0);
            Player.Damage(null, 10, false, DamageType.Normal).Should().BeTrue();
            Player.Health.Should().Be(90);

            var armor = GameActions.CreateEntity(World, "BlueArmor", Vec3D.Zero);
            Player.GiveItem(armor.Definition, armor.Flags).Should().BeTrue();
            Player.Armor.Should().Be(200);

            Player.Damage(null, 10, false, DamageType.Normal).Should().BeTrue();
            Player.Health.Should().Be(85);
            Player.Armor.Should().Be(195);

            Player.Health = 100;
            Player.Armor = 3;

            Player.Damage(null, 10, false, DamageType.Normal).Should().BeTrue();
            Player.Health.Should().Be(93);
            Player.Armor.Should().Be(0);
        }

        [Fact(DisplayName = "Armor clears when used")]
        public void ArmorClear()
        {
            if (Player.Cheats.IsCheatActive(CheatType.God))
                World.CheatManager.ActivateCheat(Player, CheatType.God);

            Player.Health.Should().Be(100);
            Player.Armor.Should().Be(0);

            var armor = GameActions.CreateEntity(World, "BlueArmor", Vec3D.Zero);
            Player.GiveItem(armor.Definition, armor.Flags).Should().BeTrue();
            Player.Armor.Should().Be(200);
            Player.ArmorDefinition.Should().NotBeNull();
            Player.ArmorDefinition!.Name.EqualsIgnoreCase("BlueArmor").Should().BeTrue();

            Player.Armor = 1;
            Player.Damage(null, 10, false, DamageType.Normal).Should().BeTrue();
            Player.ArmorDefinition.Should().BeNull();

            // Picking up armor bonus will set armor def to green
            var armorBonus = GameActions.CreateEntity(World, "ArmorBonus", Vec3D.Zero);
            Player.GiveItem(armorBonus.Definition, armorBonus.Flags).Should().BeTrue();
            Player.ArmorDefinition.Should().NotBeNull();
            Player.ArmorDefinition!.Name.EqualsIgnoreCase("GreenArmor").Should().BeTrue();
        }
    }
}
