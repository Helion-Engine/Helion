using System;
using System.IO;
using Helion.Util;
using ByteReader = Helion.Util.Bytes.ByteReader;

namespace Helion.ResourcesNew.Archives.Wads
{
    public class WadFile : Wad
    {
        private readonly FileStream m_fileStream;
        private readonly BinaryReader m_binaryReader;
        private readonly ByteReader m_byteReader;

        private WadFile(string md5, FileStream fileStream) : base(md5)
        {
            m_fileStream = fileStream;
            m_binaryReader = new BinaryReader(m_fileStream);
            m_byteReader = new ByteReader(m_binaryReader);

            ReadEntriesOrThrow(m_byteReader);
        }

        public static WadFile? From(string path)
        {
            string? md5 = Files.CalculateMD5(path);
            if (md5 == null)
                return null;

            try
            {
                FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return new WadFile(md5, fileStream);
            }
            catch
            {
                return null;
            }
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            base.Dispose();

            m_fileStream.Dispose();
            m_binaryReader.Dispose();
        }

        protected internal override byte[] ReadEntryData(DirectoryEntry dirEntry)
        {
            return m_byteReader.Bytes(dirEntry.Size, dirEntry.Offset);
        }
    }
}
