using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Bsp.States.Convex;
using Helion.Util.Geometry.Segments;
using Helion.Util.Geometry.Segments.Enums;
using Helion.Util.Geometry.Vectors;

namespace Helion.Bsp.Impl.Optimized
{
    public class OptimizedConvexChecker : ConvexChecker
    {
        public override void Execute()
        {
            BspSegment currentSeg = States.StartSegment!;

            do
            {
                Vec2D firstVertex = currentSeg[States.CurrentEndpoint];
                Vec2D secondVertex = currentSeg.Opposite(States.CurrentEndpoint);
                int pivotIndex = currentSeg.OppositeIndex(States.CurrentEndpoint);

                List<ConvexTraversalPoint> linesAtPivot = VertexMap[pivotIndex];
                BspSegment nextSeg = linesAtPivot[0].Segment;
                if (ReferenceEquals(currentSeg, nextSeg))
                    nextSeg = linesAtPivot[1].Segment;

                Endpoint nextSegPivotEndpoint = nextSeg.EndpointFrom(pivotIndex);
                Vec2D thirdVertex = nextSeg.Opposite(nextSegPivotEndpoint);

                Rotation rotation = Seg2D.Rotation(firstVertex, secondVertex, thirdVertex);
                if (rotation != Rotation.On)
                {
                    if (States.Rotation == Rotation.On)
                        States.Rotation = rotation;
                    else if (States.Rotation != rotation)
                    {
                        States.State = ConvexState.FinishedIsSplittable;
                        return;
                    }
                }

                States.ConvexTraversal.AddTraversal(currentSeg, States.CurrentEndpoint);
                States.CurrentEndpoint = nextSegPivotEndpoint;
                States.SegsVisited++;
                
                currentSeg = nextSeg;
            } 
            while (!ReferenceEquals(currentSeg, States.StartSegment));
            
            if (States.Rotation == Rotation.On)
            {
                States.State = ConvexState.FinishedIsDegenerate;
                return;
            }

            bool isConvex = (States.SegsVisited == States.TotalSegs);
            States.State = (isConvex ? ConvexState.FinishedIsConvex : ConvexState.FinishedIsSplittable);
        }
    }
}