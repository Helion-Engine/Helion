using System.Drawing;

namespace Helion.Worlds.Entities.Definition.Properties.Components
{
    public class PowerupColorMap
    {
        public Color Source;
        public Color Destination;

        public PowerupColorMap(Color destination)
        {
            Destination = destination;
        }

        public PowerupColorMap(Color source, Color destination)
        {
            Source = source;
            Destination = destination;
        }
    }
}