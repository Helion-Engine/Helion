using Helion.Util;
using Helion.Util.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Util
{
    [TestClass]
    public class MathHelperTest
    {
        [TestMethod]
        public void CheckIfValuesAreZero()
        {
            Assert.IsTrue(MathHelper.IsZero(new Fixed(0)));
            Assert.IsFalse(MathHelper.IsZero(new Fixed(1)));
            Assert.IsFalse(MathHelper.IsZero(new Fixed(-1589)));
            Assert.IsFalse(MathHelper.IsZero(new Fixed(21489284)));

            Assert.IsFalse(MathHelper.IsZero(new Fixed(0), new Fixed(0)));
            Assert.IsTrue(MathHelper.IsZero(new Fixed(0), new Fixed(1)));
            Assert.IsTrue(MathHelper.IsZero(new Fixed(1), new Fixed(2)));
            Assert.IsFalse(MathHelper.IsZero(new Fixed(2), new Fixed(2)));
            Assert.IsTrue(MathHelper.IsZero(new Fixed(-1), new Fixed(2)));
            Assert.IsFalse(MathHelper.IsZero(new Fixed(-2), new Fixed(2)));

            Assert.IsTrue(MathHelper.IsZero(0.0f));
            Assert.IsTrue(MathHelper.IsZero(0.000001f));
            Assert.IsFalse(MathHelper.IsZero(0.01f));
            Assert.IsTrue(MathHelper.IsZero(-0.000001f));
            Assert.IsFalse(MathHelper.IsZero(-0.01f));

            Assert.IsTrue(MathHelper.IsZero(0.1f, 0.15f));
            Assert.IsFalse(MathHelper.IsZero(0.15f, 0.15f));
            Assert.IsTrue(MathHelper.IsZero(-0.1f, 0.15f));
            Assert.IsFalse(MathHelper.IsZero(-0.15f, 0.15f));

            Assert.IsTrue(MathHelper.IsZero(0.0));
            Assert.IsTrue(MathHelper.IsZero(0.000001));
            Assert.IsFalse(MathHelper.IsZero(0.01));
            Assert.IsTrue(MathHelper.IsZero(-0.000001));
            Assert.IsFalse(MathHelper.IsZero(-0.01));

            Assert.IsTrue(MathHelper.IsZero(0.1, 0.15));
            Assert.IsFalse(MathHelper.IsZero(0.15, 0.15));
            Assert.IsTrue(MathHelper.IsZero(-0.1, 0.15));
            Assert.IsFalse(MathHelper.IsZero(-0.15, 0.15));
        }

        [TestMethod]
        public void CheckIfValuesAreEqual()
        {
            Assert.IsTrue(MathHelper.AreEqual(new Fixed(5), new Fixed(5)));
            Assert.IsFalse(MathHelper.AreEqual(new Fixed(5), new Fixed(6)));
            Assert.IsTrue(MathHelper.AreEqual(new Fixed(5.0f), new Fixed(5.1f), new Fixed(0.15f)));
            Assert.IsTrue(MathHelper.AreEqual(new Fixed(5.0f), new Fixed(4.9f), new Fixed(0.15f)));
            Assert.IsFalse(MathHelper.AreEqual(new Fixed(5.0f), new Fixed(5.1f), new Fixed(0.05f)));
            Assert.IsFalse(MathHelper.AreEqual(new Fixed(5.0f), new Fixed(4.8f), new Fixed(0.15f)));
            
            Assert.IsTrue(MathHelper.AreEqual(5.0f, 5.0f));
            Assert.IsTrue(MathHelper.AreEqual(5.0f, 5.1f, 0.15f));
            Assert.IsTrue(MathHelper.AreEqual(5.0f, 4.9f, 0.15f));
            Assert.IsFalse(MathHelper.AreEqual(5.0f, 5.1f, 0.05f));
            Assert.IsFalse(MathHelper.AreEqual(5.0f, 4.8f, 0.15f));
            
            Assert.IsTrue(MathHelper.AreEqual(5.0, 5.0));
            Assert.IsTrue(MathHelper.AreEqual(5.0, 5.1, 0.15));
            Assert.IsTrue(MathHelper.AreEqual(5.0, 4.9, 0.15));
            Assert.IsFalse(MathHelper.AreEqual(5.0, 5.1, 0.05));
            Assert.IsFalse(MathHelper.AreEqual(5.0, 4.8, 0.15));
        }
        
        [TestMethod]
        public void CheckIfValuesAreDifferentSigns()
        {
            Assert.IsFalse(MathHelper.DifferentSign(1, 5));
            Assert.IsFalse(MathHelper.DifferentSign(-1, -5));
            Assert.IsTrue(MathHelper.DifferentSign(1, -5));
            Assert.IsTrue(MathHelper.DifferentSign(-1, 5));
            
            Assert.IsFalse(MathHelper.DifferentSign(new Fixed(1), new Fixed(5)));
            Assert.IsFalse(MathHelper.DifferentSign(new Fixed(-1), new Fixed(-5)));
            Assert.IsTrue(MathHelper.DifferentSign(new Fixed(1), new Fixed(-5)));
            Assert.IsTrue(MathHelper.DifferentSign(new Fixed(-1), new Fixed(5)));
            
            Assert.IsFalse(MathHelper.DifferentSign(1.0f, 5.0f));
            Assert.IsFalse(MathHelper.DifferentSign(-1.0f, -5.0f));
            Assert.IsTrue(MathHelper.DifferentSign(1.0f, -5.0f));
            Assert.IsTrue(MathHelper.DifferentSign(-1.0f, 5.0f));
            
            Assert.IsFalse(MathHelper.DifferentSign(1.0, 5.0));
            Assert.IsFalse(MathHelper.DifferentSign(-1.0, -5.0));
            Assert.IsTrue(MathHelper.DifferentSign(1.0, -5.0));
            Assert.IsTrue(MathHelper.DifferentSign(-1.0, 5.0));
        }
        
        [TestMethod]
        public void CheckIfValuesInNormalRange()
        {
            Assert.IsTrue(MathHelper.InNormalRange(0.0f));
            Assert.IsTrue(MathHelper.InNormalRange(0.01f));
            Assert.IsTrue(MathHelper.InNormalRange(0.5f));
            Assert.IsTrue(MathHelper.InNormalRange(0.99f));
            Assert.IsTrue(MathHelper.InNormalRange(1.0f));
            Assert.IsFalse(MathHelper.InNormalRange(-0.0001f));
            Assert.IsFalse(MathHelper.InNormalRange(1.0001f));
            
            Assert.IsTrue(MathHelper.InNormalRange(0.0));
            Assert.IsTrue(MathHelper.InNormalRange(0.01));
            Assert.IsTrue(MathHelper.InNormalRange(0.5));
            Assert.IsTrue(MathHelper.InNormalRange(0.99));
            Assert.IsTrue(MathHelper.InNormalRange(1.0));
            Assert.IsFalse(MathHelper.InNormalRange(-0.0001));
            Assert.IsFalse(MathHelper.InNormalRange(1.0001));
        }
        
        [TestMethod]
        public void CheckMinMax()
        {
            Assert.AreEqual(new Fixed(1), MathHelper.Min(new Fixed(1), new Fixed(3)));
            Assert.AreEqual(new Fixed(3), MathHelper.Max(new Fixed(1), new Fixed(3)));
        }
    }
}