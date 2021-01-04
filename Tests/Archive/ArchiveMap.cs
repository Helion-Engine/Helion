using Helion.Maps;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Archives.Iterator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Tests.Util;

namespace Tests.Archive
{
    [TestClass]
    public class ArchiveMap
    {
        [TestInitialize]
        public void LoadResources()
        {
            TestUtil.CopyResourceZip("duel2020zc.zip");
        }

        [TestMethod]
        public void MapData()
        {          
            using (Wad wad = new Wad(new EntryPath("duel2020zc.wad")))
            {
                ArchiveMapIterator map = new ArchiveMapIterator(wad);

                var mapEntries = map.ToList();
                Assert.AreEqual(35, mapEntries.Count);

                Assert.AreEqual("MAP00", mapEntries[0].Name);
                Assert.AreEqual("MAP01", mapEntries[1].Name);
                Assert.AreEqual("MAP02", mapEntries[2].Name);
                Assert.AreEqual("MAP03", mapEntries[3].Name);
                Assert.AreEqual("MAP04", mapEntries[4].Name);
                Assert.AreEqual("MAP05", mapEntries[5].Name);
                Assert.AreEqual("MAP06", mapEntries[6].Name);
                Assert.AreEqual("MAP07", mapEntries[7].Name);
                Assert.AreEqual("MAP08", mapEntries[8].Name);
                Assert.AreEqual("MAP09", mapEntries[9].Name);
                Assert.AreEqual("MAP10", mapEntries[10].Name);
                Assert.AreEqual("MAP11", mapEntries[11].Name);
                Assert.AreEqual("MAP12", mapEntries[12].Name);
                Assert.AreEqual("MAP13", mapEntries[13].Name);
                Assert.AreEqual("MAP14", mapEntries[14].Name);
                Assert.AreEqual("MAP15", mapEntries[15].Name);
                Assert.AreEqual("MAP16", mapEntries[16].Name);
                Assert.AreEqual("MAP17", mapEntries[17].Name);
                Assert.AreEqual("MAP18", mapEntries[18].Name);
                Assert.AreEqual("MAP19", mapEntries[19].Name);
                Assert.AreEqual("MAP20", mapEntries[20].Name);
                Assert.AreEqual("MAP21", mapEntries[21].Name);
                Assert.AreEqual("MAP22", mapEntries[22].Name);
                Assert.AreEqual("MAP23", mapEntries[23].Name);
                Assert.AreEqual("MAP24", mapEntries[24].Name);
                Assert.AreEqual("MAP25", mapEntries[25].Name);
                Assert.AreEqual("MAP26", mapEntries[26].Name);
                Assert.AreEqual("MAP27", mapEntries[27].Name);
                Assert.AreEqual("MAP28", mapEntries[28].Name);
                Assert.AreEqual("MAP29", mapEntries[29].Name);
                Assert.AreEqual("MAP30", mapEntries[30].Name);
                Assert.AreEqual("MAP31", mapEntries[31].Name);
                Assert.AreEqual("MAP32", mapEntries[32].Name);
                Assert.AreEqual("MAP33", mapEntries[33].Name);
                Assert.AreEqual("MAP34", mapEntries[34].Name);

                Assert.IsTrue(mapEntries[0].IsValid());
                Assert.AreEqual("MAP00", mapEntries[0].Name);
                //Assert.AreEqual(MapType.Hexen, mapEntries[0].MapType);
                Assert.IsNotNull(mapEntries[0].Vertices);
                Assert.IsNotNull(mapEntries[0].Sectors);
                Assert.IsNotNull(mapEntries[0].Sidedefs);
                Assert.IsNotNull(mapEntries[0].Linedefs);
                Assert.IsNotNull(mapEntries[0].Segments);
                Assert.IsNotNull(mapEntries[0].Subsectors);

                Assert.IsNotNull(mapEntries[0].Nodes);
                Assert.IsNotNull(mapEntries[0].Things);
                Assert.IsNotNull(mapEntries[0].Blockmap);
                Assert.IsNotNull(mapEntries[0].Reject);

                Assert.IsNull(mapEntries[0].Scripts);
                Assert.IsNotNull(mapEntries[0].Behavior);
                Assert.IsNull(mapEntries[0].Dialogue);
                Assert.IsNull(mapEntries[0].Textmap);
                Assert.IsNull(mapEntries[0].Znodes);
                Assert.IsNull(mapEntries[0].Endmap);
                Assert.IsNull(mapEntries[0].GLMap);
                Assert.IsNull(mapEntries[0].GLVertices);
                Assert.IsNull(mapEntries[0].GLSegments);
                Assert.IsNull(mapEntries[0].GLSubsectors);
                Assert.IsNull(mapEntries[0].GLSubsectors);
                Assert.IsNull(mapEntries[0].GLNodes);
                Assert.IsNull(mapEntries[0].GLPVS);

                Assert.IsTrue(mapEntries[1].IsValid());
                Assert.AreEqual("MAP01", mapEntries[1].Name);
                //Assert.AreEqual(MapType.Doom, mapEntries[1].MapType);
                Assert.IsNotNull(mapEntries[1].Vertices);
                Assert.IsNotNull(mapEntries[1].Sectors);
                Assert.IsNotNull(mapEntries[1].Sidedefs);
                Assert.IsNotNull(mapEntries[1].Linedefs);
                Assert.IsNotNull(mapEntries[1].Segments);
                Assert.IsNotNull(mapEntries[1].Subsectors);

                Assert.IsNotNull(mapEntries[1].Nodes);
                Assert.IsNotNull(mapEntries[1].Things);
                Assert.IsNotNull(mapEntries[1].Blockmap);
                Assert.IsNotNull(mapEntries[1].Reject);

                Assert.IsNull(mapEntries[1].Scripts);
                Assert.IsNull(mapEntries[1].Behavior);
                Assert.IsNull(mapEntries[1].Dialogue);
                Assert.IsNull(mapEntries[1].Textmap);
                Assert.IsNull(mapEntries[1].Znodes);
                Assert.IsNull(mapEntries[1].Endmap);
                Assert.IsNull(mapEntries[1].GLMap);
                Assert.IsNull(mapEntries[1].GLVertices);
                Assert.IsNull(mapEntries[1].GLSegments);
                Assert.IsNull(mapEntries[1].GLSubsectors);
                Assert.IsNull(mapEntries[1].GLSubsectors);
                Assert.IsNull(mapEntries[1].GLNodes);
                Assert.IsNull(mapEntries[1].GLPVS);
            }
        }
    }
}
