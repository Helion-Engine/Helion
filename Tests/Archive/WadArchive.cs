/*using Helion.Resources;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Entries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Util;
using System.Linq;

namespace Tests.Archive
{
    [TestClass]
    public class WadArchive
    {
        [TestInitialize]
        public void LoadResources()
        {
            TestUtil.CopyResourceZip("duel2020zc.zip");
        }

        [TestMethod]
        public void Entries()
        {
            using (Wad wad = new Wad(new EntryPath("duel2020zc.wad")))
            {
                Assert.AreEqual(519, wad.Entries.Count);
                Assert.IsFalse(wad.IsIwad);

                int map00 = 0;
                int things = 1;
                int linedefs = 2;
                int sidedefs = 3;
                int vertexes = 4;
                int segs = 5;
                int ssectors = 6;
                int nodes = 7;
                int sectors = 8;
                int reject = 9;
                int blockmap = 10;
                int behavior = 11;
                int maptitle = 12;
                int map_01 = 15;
                int texture1 = 13;
                int pnames = 14;
                int mapinfo = 514;
                int loadacs = 518;

                Assert.AreEqual("MAP00", wad.Entries[map00].Path.Name);
                Assert.AreEqual("THINGS", wad.Entries[things].Path.Name);
                Assert.AreEqual("LINEDEFS", wad.Entries[linedefs].Path.Name);
                Assert.AreEqual("SIDEDEFS", wad.Entries[sidedefs].Path.Name);
                Assert.AreEqual("VERTEXES", wad.Entries[vertexes].Path.Name);
                Assert.AreEqual("SEGS", wad.Entries[segs].Path.Name);
                Assert.AreEqual("SSECTORS", wad.Entries[ssectors].Path.Name);
                Assert.AreEqual("NODES", wad.Entries[nodes].Path.Name);
                Assert.AreEqual("SECTORS", wad.Entries[sectors].Path.Name);
                Assert.AreEqual("REJECT", wad.Entries[reject].Path.Name);
                Assert.AreEqual("BLOCKMAP", wad.Entries[blockmap].Path.Name);
                Assert.AreEqual("BEHAVIOR", wad.Entries[behavior].Path.Name);
                Assert.AreEqual("MAPTITLE", wad.Entries[maptitle].Path.Name);
                Assert.AreEqual("TEXTURE1", wad.Entries[texture1].Path.Name);
                Assert.AreEqual("PNAMES", wad.Entries[pnames].Path.Name);
                Assert.AreEqual("MAP_01", wad.Entries[map_01].Path.Name);

                Assert.AreEqual("D_D5M2", wad.Entries[424].Path.Name);
                Assert.AreEqual("D_D5M7", wad.Entries[425].Path.Name);
                Assert.AreEqual("_BRICK11", wad.Entries[452].Path.Name);
                Assert.AreEqual("S8_WOLF1", wad.Entries[453].Path.Name);

                Assert.AreEqual("MAPINFO", wad.Entries[mapinfo].Path.Name);

                Assert.AreEqual("LOADACS", wad.Entries[loadacs].Path.Name);

                Assert.AreEqual(0, wad.Entries[map00].ReadData().Length);
                Assert.AreEqual(180, wad.Entries[things].ReadData().Length);
                Assert.AreEqual(11424, wad.Entries[linedefs].ReadData().Length);
                Assert.AreEqual(42480, wad.Entries[sidedefs].ReadData().Length);
                Assert.AreEqual(3680, wad.Entries[vertexes].ReadData().Length);
                Assert.AreEqual(20700, wad.Entries[segs].ReadData().Length);
                Assert.AreEqual(2440, wad.Entries[ssectors].ReadData().Length);
                Assert.AreEqual(17052, wad.Entries[nodes].ReadData().Length);
                Assert.AreEqual(1924, wad.Entries[sectors].ReadData().Length);
                Assert.AreEqual(0, wad.Entries[reject].ReadData().Length);
                Assert.AreEqual(0, wad.Entries[blockmap].ReadData().Length);
                Assert.AreEqual(5144, wad.Entries[behavior].ReadData().Length);
                Assert.AreEqual(68232, wad.Entries[maptitle].ReadData().Length);

                Assert.AreEqual(23664, wad.Entries[texture1].ReadData().Length);
                Assert.AreEqual(4292, wad.Entries[pnames].ReadData().Length);
                Assert.AreEqual(6, wad.Entries[loadacs].ReadData().Length);
            }
        }

        [TestMethod]
        public void Namespace()
        {
            using (Wad wad = new Wad(new EntryPath("duel2020zc.wad")))
            {
                Assert.AreEqual(519, wad.Entries.Count);

                int F_START = 489;
                int F_END = 513;
                int A_START = 515;
                int A_END = 517;

                Assert.AreEqual("F_START", wad.Entries[F_START].Path.Name);
                Assert.AreEqual("F_END", wad.Entries[F_END].Path.Name);

                Assert.AreEqual("A_START", wad.Entries[A_START].Path.Name);
                Assert.AreEqual("A_END", wad.Entries[A_END].Path.Name);

                for (int i = 0; i < F_START; i++)
                    Assert.AreEqual(ResourceNamespace.Global, wad.Entries[i].Namespace);

                for (int i = F_START; i < F_END; i++)
                    Assert.AreEqual(ResourceNamespace.Flats, wad.Entries[i].Namespace);

                for (int i = F_END; i < A_START; i++)
                    Assert.AreEqual(ResourceNamespace.Global, wad.Entries[i].Namespace);

                for (int i = A_START; i < A_END; i++)
                    Assert.AreEqual(ResourceNamespace.ACS, wad.Entries[i].Namespace);

                for (int i = A_END; i < 519; i++)
                    Assert.AreEqual(ResourceNamespace.Global, wad.Entries[i].Namespace);
            }
        }
    }
}
*/