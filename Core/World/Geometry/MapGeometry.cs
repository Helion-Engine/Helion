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
using NLog;

namespace Helion.World.Geometry;

public class MapGeometry
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public readonly List<Line> Lines;
    public readonly List<Side> Sides;
    public readonly List<Wall> Walls;
    public readonly List<Sector> Sectors;
    public readonly List<SectorPlane> SectorPlanes;
    public readonly BspTreeNew BspTree;
    public readonly CompactBspTree CompactBspTree;
    public readonly List<Island> Islands;
    public readonly List<Island>[] SectorIslands;
    private readonly Dictionary<int, IList<Sector>> m_tagToSector = new Dictionary<int, IList<Sector>>();
    private readonly Dictionary<int, IList<Line>> m_idToLine = new Dictionary<int, IList<Line>>();
    public readonly HashSet<int> BadSubsectors = new();

    internal MapGeometry(IMap map, GeometryBuilder builder, CompactBspTree bspTree, BspTreeNew bspTreeNew)
    {
        Lines = builder.Lines;
        Sides = builder.Sides;
        Walls = builder.Walls;
        Sectors = builder.Sectors;
        SectorPlanes = builder.SectorPlanes;
        CompactBspTree = bspTree;
        BspTree = bspTreeNew;

        TrackSectorsByTag();
        TrackLinesByLineId();
        AttachBspToGeometry(BspTree);

        // Requires geometry to be attached to each other before classifying.
        Islands = IslandClassifier.Classify(bspTreeNew.Subsectors, Sectors, Lines);

        SectorIslands = new List<Island>[Sectors.Count];
        foreach (var sector in Sectors)
            SectorIslands[sector.Id] = IslandClassifier.Classify(bspTreeNew.Subsectors, Sectors, Lines, sector.Id);

        foreach (var subsector in bspTreeNew.Subsectors)
            SetSegs(subsector);

        for (int sectorId = 0; sectorId < SectorIslands.Length; sectorId++)
        {
            foreach (var island in SectorIslands[sectorId])
            {
                foreach (var subsector in island.Subsectors)
                {
                    if (subsector.SelfReferenceSegs >= subsector.LineSegs && subsector.LineSegs > 0)
                    {
                        if (subsector.LineSegs == 1 && !IsPartnerSubsectorBad(subsector))
                            continue;

                        BadSubsectors.Add(subsector.Id);
                    }

                    if (subsector.Segments.Count >= 3)
                        continue;

                    BadSubsectors.Add(subsector.Id);
                }
            }
        }
    }

    // Bsp will split on one sided lines and can generate subsectors with a single self-referencing seg that isn't actually 'bad'.
    // Check if the partner subsectors are valid before condemning this subsector.
    private bool IsPartnerSubsectorBad(BspSubsector subsector)
    {
        for (int i = 0; i < subsector.Segments.Count;i++)
        {
            var seg = subsector.Segments[i];
            if (!seg.LineId.HasValue)
                continue;

            if (seg.Partner.Subsector.SelfReferenceSegs >= seg.Partner.Subsector.LineSegs)
                return true;
        }

        return false;
    }

    private void SetSegs(BspSubsector subsector)
    {
        for (int i = 0; i < subsector.Segments.Count; i++)
        {
            var seg = subsector.Segments[i];
            if (!seg.LineId.HasValue)
                continue;

            subsector.LineSegs++;

            var line = Lines[seg.LineId.Value];
            if (line.Back == null)
                continue;

            if (ReferenceEquals(line.Front.Sector, line.Back.Sector))
                subsector.SelfReferenceSegs++;
        }
    }

    public IList<Sector> FindBySectorTag(int tag)
    {
        return m_tagToSector.TryGetValue(tag, out IList<Sector>? sectors) ? sectors : Array.Empty<Sector>();
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

    private void AttachBspToGeometry(BspTreeNew bspTree)
    {
        foreach (BspSubsector subsector in bspTree.Subsectors)
            if (subsector.SectorId.HasValue)
                Sectors[subsector.SectorId.Value].Subsectors.Add(subsector);

        foreach (BspSubsectorSeg seg in bspTree.Segments)
            if (seg.LineId.HasValue)
                Lines[seg.LineId.Value].SubsectorSegs.Add(seg);
    }
}
