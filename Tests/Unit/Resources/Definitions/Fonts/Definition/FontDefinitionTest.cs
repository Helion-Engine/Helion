using Helion.Resource.Definitions.Fonts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Resources.Definitions.Fonts.Definition
{
    [TestClass]
    public class FontDefinitionTest
    {
        [TestMethod]
        public void CreateFontDefinition()
        {
            const string name = "name";
            FontDefinition fontDefinition = new FontDefinition(name);

            Assert.AreEqual(name, fontDefinition.Name);
        }
    }
}