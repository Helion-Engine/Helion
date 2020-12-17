using Helion.Resource.Definitions.Fonts.Definition;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Resources.Definitions.Fonts.Definition
{
    [TestClass]
    public class CharDefinitionTest
    {
        [TestMethod]
        public void CanConstructCharDefinition()
        {
            CharDefinition definition = new CharDefinition('c', "img", true, FontAlignment.Top);

            Assert.AreEqual('c', definition.Character);
            Assert.AreEqual("img", definition.ImageName);
            Assert.IsTrue(definition.Default);
            Assert.AreEqual(FontAlignment.Top, definition.Alignment);
        }

        [TestMethod]
        public void ConstructorWithNullWorks()
        {
            CharDefinition definition = new CharDefinition('c', "img", true, null);

            Assert.IsNull(definition.Alignment);
        }
    }
}