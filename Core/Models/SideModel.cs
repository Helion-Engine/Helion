using Helion.Geometry.Vectors;

namespace Helion.Models
{
    public class SideModel
    {
        public int DataChanges { get; set; }
        public int? UpperTexture { get; set; }
        public int? MiddleTexture { get; set; }
        public int? LowerTexture { get; set; }
        public Vec2D[]? FrontOffset { get; set; }
        public Vec2D[]? BackOffset { get; set; }
    }
}
