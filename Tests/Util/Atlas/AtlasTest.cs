using Microsoft.VisualStudio.TestTools.UnitTesting;
using Helion.Util.Atlas;
using Helion.Util.Geometry;

namespace Helion.Test.Util.Atlas
{
    [TestClass]
    public class Atlas2DTest
    {
        [TestMethod]
        public void CreateEmptyAtlas()
        {
            Atlas2D atlas = new Atlas2D(new Dimension(8, 16));
            
            Assert.AreEqual(8, atlas.Dimension.Width);
            Assert.AreEqual(16, atlas.Dimension.Height);
        }
        
        [TestMethod]
        public void CanAddElementsToAtlas()
        {
            Atlas2D atlas = new Atlas2D(new Dimension(8, 8));

            AtlasHandle? first = atlas.Add(new Dimension(4, 4));
            AtlasHandle? second = atlas.Add(new Dimension(2, 2));
            AtlasHandle? third = atlas.Add(new Dimension(1, 1));
            
            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            Assert.IsNotNull(third);
            
            // Expected layout of consumed areas:
            // . . . . . . . .
            // 3 . . . . . . .
            // 2 2 . . . . . .
            // 2 2 . . . . . .
            // 1 1 1 1 . . . .
            // 1 1 1 1 . . . .
            // 1 1 1 1 . . . .
            // 1 1 1 1 . . . .
            
            Assert.AreEqual(0, first.Location.BottomLeft.X);
            Assert.AreEqual(0, first.Location.BottomLeft.Y);
            Assert.AreEqual(4, first.Location.TopRight.X);
            Assert.AreEqual(4, first.Location.TopRight.Y);

            Assert.AreEqual(0, second.Location.BottomLeft.X);
            Assert.AreEqual(4, second.Location.BottomLeft.Y);
            Assert.AreEqual(2, second.Location.TopRight.X);
            Assert.AreEqual(6, second.Location.TopRight.Y);
            
            Assert.AreEqual(0, third.Location.BottomLeft.X);
            Assert.AreEqual(6, third.Location.BottomLeft.Y);
            Assert.AreEqual(1, third.Location.TopRight.X);
            Assert.AreEqual(7, third.Location.TopRight.Y);
        }

        [TestMethod]
        public void CannotAddZeroDimension()
        {
            Atlas2D atlas = new Atlas2D(new Dimension(8, 8));
            
            Assert.IsNull(atlas.Add(new Dimension(0, 1)));
            Assert.IsNull(atlas.Add(new Dimension(1, 0)));
            Assert.IsNull(atlas.Add(new Dimension(0, 0)));
        }

        [TestMethod]
        public void CannotAddSpaceBiggerThanTheAtlasSize()
        {
            Atlas2D atlas = new Atlas2D(new Dimension(8, 8));
            
            Assert.IsNull(atlas.Add(new Dimension(9, 9)));
        }
        
        [TestMethod]
        public void CannotAddToAFullAtlas()
        {
            Atlas2D atlas = new Atlas2D(new Dimension(8, 8));
            
            Assert.IsNotNull(atlas.Add(new Dimension(7, 7)));
            
            Assert.IsNull(atlas.Add(new Dimension(2, 3)));
            Assert.IsNull(atlas.Add(new Dimension(4, 4)));
            Assert.IsNull(atlas.Add(new Dimension(9, 12)));
        }

        [TestMethod]
        public void CanFillUpAtlas()
        {
            Atlas2D atlas = new Atlas2D(new Dimension(3, 3));
            
            // Expected layout of consumed areas:
            // 2 2 3
            // 1 1 3
            // 1 1 3
            
            AtlasHandle? first = atlas.Add(new Dimension(2, 2));
            AtlasHandle? second = atlas.Add(new Dimension(2, 1));
            AtlasHandle? third = atlas.Add(new Dimension(1, 3));

            Assert.IsNotNull(first);
            Assert.AreEqual(0, first.Location.BottomLeft.X);
            Assert.AreEqual(0, first.Location.BottomLeft.Y);
            Assert.AreEqual(2, first.Location.TopRight.X);
            Assert.AreEqual(2, first.Location.TopRight.Y);
            
            Assert.IsNotNull(second);
            Assert.AreEqual(0, second.Location.BottomLeft.X);
            Assert.AreEqual(2, second.Location.BottomLeft.Y);
            Assert.AreEqual(2, second.Location.TopRight.X);
            Assert.AreEqual(3, second.Location.TopRight.Y);
            
            Assert.IsNotNull(third);
            Assert.AreEqual(2, third.Location.BottomLeft.X);
            Assert.AreEqual(0, third.Location.BottomLeft.Y);
            Assert.AreEqual(3, third.Location.TopRight.X);
            Assert.AreEqual(3, third.Location.TopRight.Y);
            
            Assert.IsNull(atlas.Add(new Dimension(1, 1)));
        }
    }
}