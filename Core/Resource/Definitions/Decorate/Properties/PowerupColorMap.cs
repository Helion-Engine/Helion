using System.Drawing;

namespace Helion.Resource.Definitions.Decorate.Properties
{
    public class PowerupColorMap
    {
        public Color? Source;
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