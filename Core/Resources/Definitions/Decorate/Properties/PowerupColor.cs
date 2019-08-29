namespace Helion.Resources.Definitions.Decorate.Properties
{
    public class PowerupColor
    {
        public string Color;
        public double? Alpha;

        public PowerupColor(string color, double? alpha = null)
        {
            Color = color;
            Alpha = alpha;
        }
    }
}