using System;
using System.Collections;
using System.Collections.Generic;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.Geometry
{
    /// <summary>
    /// An allocator of vertices. Also responsible for welding vertices to each
    /// other if one tries to be made close to another.
    /// </summary>
    public class VertexAllocator : IEnumerable<Vec2D>
    {
        private readonly List<Vec2D> vertices = new List<Vec2D>();
        private readonly QuantizedGrid<int> grid;

        /// <summary>
        /// How many vertices are in this allocator.
        /// </summary>
        public int Count => vertices.Count;

        /// <summary>
        /// Creates a new allocator with a vertex welding distance in the map
        /// unit range provided.
        /// </summary>
        /// <param name="weldingEpsilon">The welding map unit range. This value
        /// should be greater than zero.</param>
        public VertexAllocator(double weldingEpsilon)
        {
            Precondition(weldingEpsilon > 0, "Cannot have a zero or negative welding epsilon");

            grid = new QuantizedGrid<int>(weldingEpsilon);
        }

        /// <summary>
        /// Gets the actual vertex data from the index provided.
        /// </summary>
        /// <param name="index">The vertex index.</param>
        /// <returns>The vertex for the index provided.</returns>
        /// 
        public Vec2D this[int index] => vertices[index];

        /// <summary>
        /// Either gets the existing index for this vertex, or allocates a new
        /// index and tracks the vertex provided.
        /// </summary>
        /// <remarks>
        /// This is similar to how std::[unordered_]map work in C++ with the []
        /// operator.
        /// </remarks>
        /// <param name="vertex">The vertex to get the index for or allocate
        /// with.</param>
        /// <returns>The index for the existing vertex, or the newly allocated
        /// index.</returns>
        public int this[Vec2D vertex] => Insert(vertex);
        
        /// <summary>
        /// Either gets the existing index for this vertex, or allocates a new
        /// index and tracks the vertex provided.
        /// </summary>
        /// <remarks>
        /// This is similar to how std::[unordered_]map work in C++ with the []
        /// operator.
        /// </remarks>
        /// <param name="vertex">The vertex to get the index for or allocate
        /// with.</param>
        /// <returns>The index for the existing vertex, or the newly allocated
        /// index.</returns>
        public int Insert(Vec2D vertex)
        {
            int index = grid.GetExistingOrAdd(vertex.X, vertex.Y, vertices.Count);
            if (index == vertices.Count)
                vertices.Add(vertex);

            return index;
        }

        /// <summary>
        /// Tries to get the index for the vertex provided.
        /// </summary>
        /// <param name="vertex">The vertex to try to get the index from.
        /// </param>
        /// <param name="index">The index that is set if it exists.</param>
        /// <returns>True if it exists and it is safe to use the index out
        /// value, otherwise false.</returns>
        public bool TryGetValue(Vec2D vertex, out int index)
        {
            int indexValue = 0;
            if (grid.TryGetValue(vertex.X, vertex.Y, ref indexValue))
            {
                index = indexValue;
                return true;
            }

            index = default;
            return false;
        }

        /// <summary>
        /// Gets the bounds around all the vertices. This performs an O(n)
        /// calculation to discover the bounds, where `n` is the number of
        /// vertices.
        /// </summary>
        /// <returns>A new box that bounds all the vertices.</returns>
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

        /// <inheritdoc/>
        public IEnumerator<Vec2D> GetEnumerator() => vertices.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}