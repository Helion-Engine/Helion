using Helion.Geometry.Vectors;
using Helion.World.Entities;

namespace Helion.World;

internal struct SightTraverseData
{
    public double TopSlope;
    public double BottomSlope;
    public double SegLength;
    public Vec3D SightPos;
    public Entity From;
    public bool NormalSolid;
    public bool Result;
    public bool InitSet;
    public bool OnLine;
    public bool CrossLined;

    public void Init(in Vec3D sightPos, in Vec3D endSightPos, double segLength, Entity from, Entity to, bool normalSolid)
    {
        TopSlope = (endSightPos.Z + to.Height - sightPos.Z) / segLength;
        BottomSlope = (endSightPos.Z - sightPos.Z) / segLength;
        SegLength = segLength;
        SightPos = sightPos;
        From = from;
        NormalSolid = normalSolid;
        Result = true;
        InitSet = true;
        OnLine = false;
        CrossLined = false;
    }
}
