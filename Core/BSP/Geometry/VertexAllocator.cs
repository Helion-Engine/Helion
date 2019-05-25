using Helion.Util.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Helion.BSP.Geometry
{
    public class VertexAllocator : IEnumerable<Vec2D>
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

        public bool TryGetValue(Vec2D vertex, out VertexIndex index)
        {
            int indexValue = 0;
            if (grid.TryGetValue(vertex.X, vertex.Y, ref indexValue))
            {
                index = new VertexIndex(indexValue);
                return true;
            }

            index = default;
            return false;
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

        public IEnumerator<Vec2D> GetEnumerator() => vertices.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
