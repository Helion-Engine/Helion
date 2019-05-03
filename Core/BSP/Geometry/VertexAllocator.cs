using Helion.Util.Geometry;
using System;
using System.Collections.Generic;

namespace Helion.BSP.Geometry
{
    public struct VertexIndex
    {
        public readonly int Index;

        public VertexIndex(int index) => Index = index;

        public static bool operator ==(VertexIndex first, VertexIndex second) => first.Index == second.Index;
        public static bool operator !=(VertexIndex first, VertexIndex second) => first.Index != second.Index;

        public override bool Equals(object obj) => obj is VertexIndex index && Index == index.Index;
        public override int GetHashCode() => HashCode.Combine(Index);
    }

    public class VertexAllocator
    {
        private readonly List<Vec2D> vertices = new List<Vec2D>();
        private readonly QuantizedGrid<int> grid;

        public int Count => vertices.Count;

        public VertexAllocator(double weldingEpsilon) => grid = new QuantizedGrid<int>(weldingEpsilon);

        public Vec2D this[VertexIndex index] { get { return vertices[index.Index]; } }
        public VertexIndex this[Vec2D vertex] 
        {
            get 
            {
                int index = grid.GetExistingOrAdd(vertex.X, vertex.Y, vertices.Count);
                if (index == vertices.Count)
                    vertices.Add(vertex);

                return new VertexIndex(index);
            }
        }

        public Box2D Bounds()
        {
            if (Count == 0)
                return new Box2D(new Vec2D(0, 0), new Vec2D(0, 0));

            Vec2D min = new Vec2D(double.MaxValue, double.MaxValue);
            Vec2D max = new Vec2D(double.MinValue, double.MinValue);
            vertices.ForEach(v =>
            {
                min.X = Math.Min(min.X, v.X);
                min.Y = Math.Min(min.Y, v.Y);
                max.X = Math.Max(max.X, v.X);
                max.Y = Math.Max(max.Y, v.Y);
            });

            return new Box2D(min, max);
        }
    }
}
