namespace Helion.World.Geometry.Sectors;

public enum PlaneFace3D { Bottom, Top }

public struct SectorPlane3D(SectorPlane controlPlane, SectorPlane plane, Sector3D? sector3D, PlaneFace3D face, Sector lightSector)
{
    public SectorPlane ControlPlane = controlPlane;
    public SectorPlane Plane = plane;
    public Sector3D? Sector3D = sector3D;
    public PlaneFace3D Face = face;
    public Sector LightSector = lightSector;
    public bool Ignore;
}
