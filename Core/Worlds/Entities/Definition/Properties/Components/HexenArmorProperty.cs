namespace Helion.Worlds.Entities.Definition.Properties.Components
{
    public class HexenArmorProperty
    {
        public int Value;
        public int Armor;
        public int Shield;
        public int Helm;
        public int Amulet;

        public HexenArmorProperty(int value, int armor, int shield, int helm, int amulet)
        {
            Value = value;
            Armor = armor;
            Shield = shield;
            Helm = helm;
            Amulet = amulet;
        }
    }
}