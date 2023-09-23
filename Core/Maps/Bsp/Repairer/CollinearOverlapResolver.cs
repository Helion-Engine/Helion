using System.Collections.Generic;
using Helion.Maps.Bsp.Geometry;
using Helion.Maps.Bsp.States.Miniseg;

namespace Helion.Maps.Bsp.Repairer;

public class CollinearOverlapResolver
{
    public readonly BspSegment FirstSeg;
    public readonly BspSegment SecondSeg;
    public readonly VertexSplitterTime FirstStart;
    public readonly VertexSplitterTime FirstEnd;
    public readonly VertexSplitterTime SecondStart;
    public readonly VertexSplitterTime SecondEnd;

    public CollinearOverlapResolver(BspSegment firstSeg, BspSegment secondSeg, double startTime, double endTime)
    {
        FirstSeg = firstSeg;
        SecondSeg = secondSeg;
        FirstStart = new VertexSplitterTime(firstSeg.StartVertex, 0.0);
        FirstEnd = new VertexSplitterTime(firstSeg.EndVertex, 1.0);
        SecondStart = new VertexSplitterTime(secondSeg.StartVertex, startTime);
        SecondEnd = new VertexSplitterTime(secondSeg.EndVertex, endTime);

        List<VertexSplitterTime> vertices = new List<VertexSplitterTime>
        {
            FirstStart, FirstEnd, SecondStart, SecondEnd,
        };
        vertices.Sort();
    }

    public void Resolve()
    {
        // 1) Every vertex that has 2+ lines coming out of it will be
        // preserved and merged.
        // TODO

        // 2) Endpoints are preserved always.
        // TODO

        // 3) Anything in the middle that is not (2) will be thrown out.
        // TODO

        // 4) Now with the merging, select which lines will persist and be
        // used for the ranges provided.
        // TODO
    }
}
