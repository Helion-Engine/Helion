using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Shared.World.ViewClipper
{
    public struct ClipSpan
    {
        public readonly uint StartAngle;
        public readonly uint EndAngle;

        public ClipSpan(uint startAngle, uint endAngle)
        {
            Precondition(startAngle < endAngle, "Cannot have the end angle be less than the start angle");
            
            StartAngle = startAngle;
            EndAngle = endAngle;
        }

        public bool Contains(uint angle) => angle >= StartAngle && angle <= EndAngle;
    }
}