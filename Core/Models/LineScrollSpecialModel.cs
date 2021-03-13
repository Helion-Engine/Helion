using Helion.Util.Geometry.Vectors;
using Helion.World;
using Helion.World.Special;
using Helion.World.Special.Specials;

namespace Helion.Models
{
    public class LineScrollSpecialModel : ISpecialModel
    {
        public int LineId { get; set; }
        public int Scroll { get; set; }
        public double SpeedX { get; set; }
        public double SpeedY { get; set; }
        public bool Front { get; set; }
        public Vec2D[]? OffsetFront { get; set; }
        public Vec2D[]? OffsetBack { get; set; }

        public ISpecial? ToWorldSpecial(IWorld world)
        {
            if (!world.IsLineIdValid(LineId))
                return null;

            return new LineScrollSpecial(world.Lines[LineId], this);
        }
    }
}
