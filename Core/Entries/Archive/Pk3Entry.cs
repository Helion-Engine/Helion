using Helion.Resources;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace Helion.Entries.Archive
{
    public class Pk3Entry : Entry
    {
        public readonly Pk3 Parent;
        public readonly ZipArchiveEntry ZipeEntry;

        public Pk3Entry(Pk3 pk3, ZipArchiveEntry zipEntry, EntryPath path, ResourceNamespace resourceNamespace)
            : base(path, resourceNamespace)
        {
            Parent = pk3;
            ZipeEntry = zipEntry;
        }

        public override byte[] ReadData()
        {
            return Parent.ReadData(this);
        }
    }
}