using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry.Sectors
{
    public class SectorSpan
    {
        public readonly int Id;
        public readonly SectorPlane Floor;
        public readonly SectorPlane Ceiling;

        public SectorSpan(int id, SectorPlane floor, SectorPlane ceiling)
        {
            Precondition(floor.Facing == SectorPlaneFace.Floor, "Trying to provide a floor to a span that is a ceiling");
            Precondition(ceiling.Facing == SectorPlaneFace.Ceiling, "Trying to provide a ceiling to a span that is a floor");
            
            Id = id;
            Floor = floor;
            Ceiling = ceiling;
        }
    }
}