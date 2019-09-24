using Helion.Bsp.Geometry;
using Helion.Util.Geometry.Vectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Bsp.Geometry
{
    [TestClass]
    public class VertexAllocatorTest
    {
        private const double DEFAULT_EPSILON = 0.1;
        
        [TestMethod]
        public void CanAddVerticesAndGetWelding()
        {
            VertexAllocator vertexAllocator = new VertexAllocator(DEFAULT_EPSILON);
            Vec2D firstPos = Vec2D.Zero;
            Vec2D secondPos = new Vec2D(0.05, 0.05);
            Vec2D thirdPos = new Vec2D(0.2, 0.05);

            BspVertex first = vertexAllocator[firstPos];
            BspVertex second = vertexAllocator[secondPos];
            BspVertex third = vertexAllocator[thirdPos];

            BspVertex firstIndexVertex = vertexAllocator[first.Index];
            BspVertex thirdIndexVertex = vertexAllocator[third.Index];
            
            Assert.AreEqual(first, second);
            Assert.IsTrue(first == firstIndexVertex);
            Assert.IsTrue(first != third);
            Assert.IsTrue(third == thirdIndexVertex);
            
            Assert.AreEqual(2, vertexAllocator.Count);
        }

        [TestMethod]
        public void AbleToTryGetValue()
        {
            VertexAllocator vertexAllocator = new VertexAllocator(DEFAULT_EPSILON);
            Vec2D vec = new Vec2D(12, 34);
            vertexAllocator.Insert(new Vec2D(1, 2));
            
            Assert.IsFalse(vertexAllocator.TryGetValue(vec, out _));

            BspVertex vertex = vertexAllocator[vec];
            bool found = vertexAllocator.TryGetValue(vec, out int index);
            Assert.IsTrue(found);
            Assert.AreEqual(vertex.Index, index);
        }
    }
}