using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Maps;
using Helion.World.Bsp;
using Helion.World.Geometry.Builder;
using Helion.World.Geometry.Islands;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;

namespace Helion.World.Geometry;

public class MapGeometry
{
    public readonly List<Line> Lines;
    public readonly List<Side> Sides;
    public readonly List<Wall> Walls;
    public readonly List<Sector> Sectors;
    public readonly List<SectorPlane> SectorPlanes;
    public readonly CompactBspTree CompactBspTree;
    public readonly List<Island> Islands;
    private readonly Dictionary<int, IList<Sector>> m_tagToSector = new Dictionary<int, IList<Sector>>();
    private readonly Dictionary<int, IList<Line>> m_idToLine = new Dictionary<int, IList<Line>>();

    internal MapGeometry(GeometryBuilder builder, CompactBspTree bspTree)
    {
        Lines = builder.Lines;
        Sides = builder.Sides;
        Walls = builder.Walls;
        Sectors = builder.Sectors;
        SectorPlanes = builder.SectorPlanes;
        CompactBspTree = bspTree;

        TrackSectorsByTag();
        TrackLinesByLineId();

        // Requires geometry to be attached to each other before classifying.
        Islands = IslandClassifier.Classify(Lines);
    }

    public IEnumerable<Sector> FindBySectorTag(int tag)
    {
        return m_tagToSector.TryGetValue(tag, out IList<Sector>? sectors) ? sectors : Enumerable.Empty<Sector>();
    }

    public IEnumerable<Line> FindByLineId(int lineId)
    {
        return m_idToLine.TryGetValue(lineId, out IList<Line>? lines) ? lines : Enumerable.Empty<Line>();
    }

    public void SetLineId(Line line, int lineId)
    {
        line.LineId = lineId;
        TrackLineId(line);
    }

    private void TrackSectorsByTag()
    {
        foreach (Sector sector in Sectors)
        {
            if (m_tagToSector.TryGetValue(sector.Tag, out IList<Sector>? sectors))
                sectors.Add(sector);
            else
                m_tagToSector[sector.Tag] = new List<Sector> { sector };
        }
    }

    private void TrackLinesByLineId()
    {
        foreach (Line line in Lines)
        {
            if (line.LineId == Line.NoLineId)
                continue;

            TrackLineId(line);
        }
    }

    private void TrackLineId(Line line)
    {
        if (m_idToLine.TryGetValue(line.LineId, out IList<Line>? lines))
            lines.Add(line);
        else
            m_idToLine[line.LineId] = new List<Line> { line };
    }

    private static void AttachBspToGeometry(BspTreeNew bspTree)
    {
        foreach (BspSubsector subsector in bspTree.Subsectors)
            subsector.Sector.Subsectors.Add(subsector);

        foreach (BspSubsectorSeg seg in bspTree.Segments)
            if (seg.Line != null)
                seg.Line.SubsectorSegs.Add(seg);
    }
}
