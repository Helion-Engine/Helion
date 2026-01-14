using System;

namespace Helion.World.Geometry.Sectors;

public enum PlaneFace3D { Top, Bottom }

public struct SectorPlane3D(SectorPlane controlPlane, SectorPlane plane, Sector3D? sector3D, PlaneFace3D face, Sector lightSector)
{
    public int ControlSectorId = controlPlane.Sector.Id;
    public SectorPlane ControlPlane = controlPlane;
    public SectorPlane Plane = plane;
    public Sector3D? Sector3D = sector3D;
    public PlaneFace3D Face = face;
    public Sector LightSector = lightSector;
    public bool Ignore;
    public PlaneSortKey SortKey;

    public void UpdateSortKey()
    {
        SortKey = new(this);
    }

    public override readonly string ToString() => $"ControlPlane={{{ControlPlane}}} Sector3D={{{Sector3D}}}";
}

public readonly struct PlaneSortKey : IComparable<PlaneSortKey>
{
    public readonly double Z;
    public readonly double ControlTopZ;
    public readonly bool IsSolid;
    public readonly bool IsOpaque;
    public readonly int ControlSectorId;
    public readonly PlaneFace3D Face;

    public PlaneSortKey(SectorPlane3D p)
    {
        Z = (p.Sector3D != null && p.Face == PlaneFace3D.Bottom)
            ? p.Sector3D.ClipBottomZ
            : p.Plane.Z;

        ControlTopZ = p.Sector3D?.ControlTop.Z ?? p.Plane.Z;

        IsSolid = p.Sector3D?.IsSolid ?? true;
        IsOpaque = p.Sector3D?.IsOpaque ?? true;

        ControlSectorId = p.ControlSectorId;
        Face = p.Face;
    }

    public int CompareTo(PlaneSortKey other)
    {
        var cmp = other.Z.CompareTo(Z);
        if (cmp != 0)
            return cmp;

        cmp = other.ControlTopZ.CompareTo(ControlTopZ);
        if (cmp != 0)
            return cmp;

        cmp = IsOpaque.CompareTo(other.IsOpaque);
        if (cmp != 0)
            return cmp;

        cmp = IsSolid.CompareTo(other.IsSolid);
        if (cmp != 0)
            return cmp;

        cmp = ControlSectorId.CompareTo(other.ControlSectorId);
        if (cmp != 0)
            return cmp;

        return Face.CompareTo(other.Face);
    }
}
