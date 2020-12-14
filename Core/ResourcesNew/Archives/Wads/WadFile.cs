using System;
using System.IO;
using System.Text;
using Helion.Util.Bytes;

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
            try
            {
                FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                string md5 = CalculateMD5(fileStream);
                return new WadFile(md5, fileStream);
            }
            catch
            {
                return null;
            }
        }

        private static string CalculateMD5(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] data = md5.ComputeHash(stream);

            StringBuilder hex = new(data.Length * 2);
            foreach (byte b in data)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
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
