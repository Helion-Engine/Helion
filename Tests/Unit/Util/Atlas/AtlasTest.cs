using Helion.Util.Atlas;
using Helion.Util.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Atlas
{
    [TestClass]
    public class AtlasTest
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

        [TestMethod]
        public void AtlasResizesWhenFullAndAddingSmallArea()
        {
            Atlas2D atlas = new Atlas2D(new Dimension(2, 2), 4);
            
            // t t
            // b b
            AtlasHandle? firstBottom = atlas.Add(new Dimension(2, 1));
            AtlasHandle? firstTop = atlas.Add(new Dimension(2, 1));
            Assert.IsNotNull(firstBottom);
            Assert.IsNotNull(firstTop);
            Assert.AreEqual(0, firstBottom.Location.BottomLeft.X);
            Assert.AreEqual(0, firstBottom.Location.BottomLeft.Y);
            Assert.AreEqual(2, firstBottom.Location.TopRight.X);
            Assert.AreEqual(1, firstBottom.Location.TopRight.Y);
            Assert.AreEqual(0, firstTop.Location.BottomLeft.X);
            Assert.AreEqual(1, firstTop.Location.BottomLeft.Y);
            Assert.AreEqual(2, firstTop.Location.TopRight.X);
            Assert.AreEqual(2, firstTop.Location.TopRight.Y);
            
            // 3 3 4 4
            // 3 3 4 4
            // t t 2 2
            // b b 2 2
            AtlasHandle? second = atlas.Add(new Dimension(2, 2));
            Assert.IsNotNull(second);
            Assert.AreEqual(0, second.Location.BottomLeft.X);
            Assert.AreEqual(2, second.Location.BottomLeft.Y);
            Assert.AreEqual(2, second.Location.TopRight.X);
            Assert.AreEqual(4, second.Location.TopRight.Y);
            
            AtlasHandle? third = atlas.Add(new Dimension(2, 2));
            Assert.IsNotNull(third);
            Assert.AreEqual(2, third.Location.BottomLeft.X);
            Assert.AreEqual(0, third.Location.BottomLeft.Y);
            Assert.AreEqual(4, third.Location.TopRight.X);
            Assert.AreEqual(2, third.Location.TopRight.Y);
            
            AtlasHandle? fourth = atlas.Add(new Dimension(2, 2));
            Assert.IsNotNull(fourth);
            Assert.AreEqual(2, fourth.Location.BottomLeft.X);
            Assert.AreEqual(2, fourth.Location.BottomLeft.Y);
            Assert.AreEqual(4, fourth.Location.TopRight.X);
            Assert.AreEqual(4, fourth.Location.TopRight.Y);
            
            // Due to the resizing that happens, all the new locations should
            // be valid and not overwrite any other one. Likewise the old one
            // should stay in tact.
            Assert.AreEqual(0, firstBottom.Location.BottomLeft.X);
            Assert.AreEqual(0, firstBottom.Location.BottomLeft.Y);
            Assert.AreEqual(2, firstBottom.Location.TopRight.X);
            Assert.AreEqual(1, firstBottom.Location.TopRight.Y);
            Assert.AreEqual(0, firstTop.Location.BottomLeft.X);
            Assert.AreEqual(1, firstTop.Location.BottomLeft.Y);
            Assert.AreEqual(2, firstTop.Location.TopRight.X);
            Assert.AreEqual(2, firstTop.Location.TopRight.Y);

            AtlasHandle? overflow = atlas.Add(new Dimension(1, 1));
            Assert.IsNull(overflow);
        }
        
        [TestMethod]
        public void AtlasResizesWhenFullAndAddingLargeArea()
        {
            Atlas2D atlas = new Atlas2D(new Dimension(2, 2), 16);
            
            AtlasHandle? first = atlas.Add(new Dimension(2, 2));
            Assert.IsNotNull(first);
            
            AtlasHandle? second = atlas.Add(new Dimension(12, 12));
            Assert.IsNotNull(second);
            Assert.AreEqual(2, second.Location.BottomLeft.X);
            Assert.AreEqual(0, second.Location.BottomLeft.Y);
            Assert.AreEqual(14, second.Location.TopRight.X);
            Assert.AreEqual(12, second.Location.TopRight.Y);
            
            AtlasHandle? tooLarge = atlas.Add(new Dimension(21, 17));
            Assert.IsNull(tooLarge);
        }
    }
}