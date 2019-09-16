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
            Vec2D first = Vec2D.Zero;
            Vec2D second = new Vec2D(0.05, 0.05);
            Vec2D third = new Vec2D(0.2, 0.05);

            int firstIndex = vertexAllocator[first];
            int secondIndex = vertexAllocator[second];
            int thirdIndex = vertexAllocator[third];

            Vec2D firstIndexVertex = vertexAllocator[firstIndex];
            Vec2D thirdIndexVertex = vertexAllocator[thirdIndex];
            
            Assert.AreEqual(firstIndex, secondIndex);
            Assert.IsTrue(first == firstIndexVertex);
            Assert.IsTrue(firstIndex != thirdIndex);
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

            int allocatedIndex = vertexAllocator[vec];
            bool found = vertexAllocator.TryGetValue(vec, out int index);
            Assert.IsTrue(found);
            Assert.AreEqual(allocatedIndex, index);
        }
    }
}