namespace Helion.Resources.Definitions.Decorate.Properties
{
    public class PlayerColorSetFileProperty
    {
        public int Number;
        public string Name;
        public int Table;
        public string Color;

        public PlayerColorSetFileProperty(int number, string name, int table, string color)
        {
            Number = number;
            Name = name;
            Table = table;
            Color = color;
        }
    }
}