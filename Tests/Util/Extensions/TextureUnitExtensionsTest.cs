using Helion.Util.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTK.Graphics.OpenGL;

namespace Helion.Test.Util.Extensions
{
    [TestClass]
    public class TextureUnitExtensionsTest
    {
        [TestMethod]
        public void MapsTextureUnitToIndex()
        {
            Assert.AreEqual(0, TextureUnit.Texture0.ToIndex());
            Assert.AreEqual(1, TextureUnit.Texture1.ToIndex());
            Assert.AreEqual(2, TextureUnit.Texture2.ToIndex());
            Assert.AreEqual(3, TextureUnit.Texture3.ToIndex());
            Assert.AreEqual(4, TextureUnit.Texture4.ToIndex());
            Assert.AreEqual(5, TextureUnit.Texture5.ToIndex());
            Assert.AreEqual(6, TextureUnit.Texture6.ToIndex());
            Assert.AreEqual(7, TextureUnit.Texture7.ToIndex());
            Assert.AreEqual(8, TextureUnit.Texture8.ToIndex());
            Assert.AreEqual(9, TextureUnit.Texture9.ToIndex());
            Assert.AreEqual(10, TextureUnit.Texture10.ToIndex());
            Assert.AreEqual(11, TextureUnit.Texture11.ToIndex());
            Assert.AreEqual(12, TextureUnit.Texture12.ToIndex());
            Assert.AreEqual(13, TextureUnit.Texture13.ToIndex());
            Assert.AreEqual(14, TextureUnit.Texture14.ToIndex());
            Assert.AreEqual(15, TextureUnit.Texture15.ToIndex());
        }
    }
}