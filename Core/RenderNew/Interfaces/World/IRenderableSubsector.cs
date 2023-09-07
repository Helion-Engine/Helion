using System;
using Helion.Geometry.Vectors;

namespace Helion.RenderNew.Interfaces.World;

public interface IRenderableSubsector
{
    ReadOnlySpan<Vec2F> GetClockwiseVertices();
}