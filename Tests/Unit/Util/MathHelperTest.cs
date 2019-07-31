using Helion.Util;
using Helion.Util.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util
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

        [TestMethod]
        public void CheckClampValues()
        {
            Assert.AreEqual((byte)3, MathHelper.Clamp((byte)0, (byte)3, (byte)5));
            Assert.AreEqual((byte)4, MathHelper.Clamp((byte)4, (byte)3, (byte)5));
            Assert.AreEqual((byte)5, MathHelper.Clamp((byte)6, (byte)3, (byte)5));
            
            Assert.AreEqual((short)3, MathHelper.Clamp((short)0, (short)3, (short)5));
            Assert.AreEqual((short)4, MathHelper.Clamp((short)4, (short)3, (short)5));
            Assert.AreEqual((short)5, MathHelper.Clamp((short)6, (short)3, (short)5));
            
            Assert.AreEqual((ushort)3, MathHelper.Clamp((ushort)0, (ushort)3, (ushort)5));
            Assert.AreEqual((ushort)4, MathHelper.Clamp((ushort)4, (ushort)3, (ushort)5));
            Assert.AreEqual((ushort)5, MathHelper.Clamp((ushort)6, (ushort)3, (ushort)5));
            
            Assert.AreEqual(3, MathHelper.Clamp(0, 3, 5));
            Assert.AreEqual(4, MathHelper.Clamp(4, 3, 5));
            Assert.AreEqual(5, MathHelper.Clamp(6, 3, 5));
            
            Assert.AreEqual(3U, MathHelper.Clamp(0U, 3U, 5U));
            Assert.AreEqual(4U, MathHelper.Clamp(4U, 3U, 5U));
            Assert.AreEqual(5U, MathHelper.Clamp(6U, 3U, 5U));
            
            Assert.AreEqual(3L, MathHelper.Clamp(0L, 3L, 5L));
            Assert.AreEqual(4L, MathHelper.Clamp(4L, 3L, 5L));
            Assert.AreEqual(5L, MathHelper.Clamp(6L, 3L, 5L));
            
            Assert.AreEqual(3LU, MathHelper.Clamp(0LU, 3LU, 5LU));
            Assert.AreEqual(4LU, MathHelper.Clamp(4LU, 3LU, 5LU));
            Assert.AreEqual(5LU, MathHelper.Clamp(6LU, 3LU, 5LU));
            
            Assert.AreEqual(3.0f, MathHelper.Clamp(0.0f, 3.0f, 5.0f));
            Assert.AreEqual(4.0f, MathHelper.Clamp(4.0f, 3.0f, 5.0f));
            Assert.AreEqual(5.0f, MathHelper.Clamp(6.0f, 3.0f, 5.0f));
            
            Assert.AreEqual(3.0, MathHelper.Clamp(0.0, 3.0, 5.0));  
            Assert.AreEqual(4.0, MathHelper.Clamp(4.0, 3.0, 5.0));  
            Assert.AreEqual(5.0, MathHelper.Clamp(6.0, 3.0, 5.0));  
        }

        [TestMethod]
        public void MinMaxCalculatedCorrectly()
        {
            int first = 6;
            int second = 8;
            (int lower, int higher) = MathHelper.MinMax(first, second);
            Assert.AreEqual(first, lower);
            Assert.AreEqual(second, higher);
            
            (lower, higher) = MathHelper.MinMax(second, first);
            Assert.AreEqual(first, lower);
            Assert.AreEqual(second, higher);
        }
    }
}