using System;
using System.Diagnostics;
using System.Linq;
using Helion.Geometry.Vectors;
using Helion.World;
using Helion.World.Special;
using Helion.World.Special.Specials;
using static Helion.Util.Assertion.Assert;

namespace Helion.Models
{
    public class LineScrollSpecialModel : ISpecialModel
    {
        public int LineId { get; set; }
        public int Scroll { get; set; }
        public double SpeedX { get; set; }
        public double SpeedY { get; set; }
        public bool Front { get; set; }
        public double[]? OffsetFrontX { get; set; }
        public double[]? OffsetFrontY { get; set; }
        public double[]? OffsetBackX { get; set; }
        public double[]? OffsetBackY { get; set; }

        public ISpecial? ToWorldSpecial(IWorld world)
        {
            if (!world.IsLineIdValid(LineId))
                return null;

            return new LineScrollSpecial(world.Lines[LineId], this);
        }

        public Vec2D[] GenerateFrontOffsets() => GenerateOffsets(OffsetFrontX, OffsetFrontY);
        
        public Vec2D[] GenerateBackOffsets() => GenerateOffsets(OffsetBackX, OffsetBackY);
        
        private static Vec2D[] GenerateOffsets(double[]? offsetX, double[]? offsetY)
        {
            if (offsetX == null || offsetY == null || offsetX.Length != offsetY.Length)
                return Array.Empty<Vec2D>();
            return offsetX.Select((x, i) => new Vec2D(x, offsetY[i])).ToArray();
        }
    }
}
