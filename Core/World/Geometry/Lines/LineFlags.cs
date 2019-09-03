using Helion.World.Special;

namespace Helion.World.Geometry.Lines
{
    public struct LineFlags
    {
        public LineAutomapFlags Automap;
        public LineBlockFlags Blocking;
        public UnpeggedFlags Unpegged;
        public ActivationType ActivationType;
        public bool BlockSound;
        public bool Repeat;
    }
}