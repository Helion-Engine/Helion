using Helion.BspOld;
using Helion.BspOld.States.Split;
using Helion.BspOld;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Bsp
{
    [TestClass]
    public class BspConfigTest
    {
        [TestMethod]
        public void CreateDefaultConfig()
        {
            BspConfig config = new BspConfig();

            Assert.AreEqual(0.005, config.VertexWeldingEpsilon);
            Assert.AreEqual(0.1, config.PunishableEndpointDistance);
            Assert.AreEqual(1, config.SplitWeights.LeftRightSplitImbalanceScore);
            Assert.AreEqual(1000, config.SplitWeights.NearEndpointSplitScore);
            Assert.AreEqual(5, config.SplitWeights.NotAxisAlignedScore);
            Assert.AreEqual(1, config.SplitWeights.SplitScoreFactor);
        }
        
        [TestMethod]
        public void CreateCustomConfig()
        {
            SplitWeights splitWeights = new SplitWeights
            {
                SplitScoreFactor = 3,
                NotAxisAlignedScore = 4,
                LeftRightSplitImbalanceScore = 5,
                NearEndpointSplitScore = 6
            };
            BspConfig config = new BspConfig(1, 2, splitWeights);

            Assert.AreEqual(1, config.VertexWeldingEpsilon);
            Assert.AreEqual(2, config.PunishableEndpointDistance);
            Assert.AreEqual(5, config.SplitWeights.LeftRightSplitImbalanceScore);
            Assert.AreEqual(6, config.SplitWeights.NearEndpointSplitScore);
            Assert.AreEqual(4, config.SplitWeights.NotAxisAlignedScore);
            Assert.AreEqual(3, config.SplitWeights.SplitScoreFactor);
        }
    }
}