using System;
using System.IO;
using System.IO.Compression;
using Helion.Resources;
using NLog;

namespace Helion.ResourcesNew.Archives.PK3s
{
    public class PK3Entry : Entry
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ZipArchiveEntry m_zipEntry;
        private byte[]? m_data;

        public PK3Entry(ZipArchiveEntry zipEntry, EntryPath path, Namespace resourceNamespace) :
            base(path, resourceNamespace)
        {
            m_zipEntry = zipEntry;
        }

        public override byte[] ReadData()
        {
            if (m_data != null)
                return m_data;

            try
            {
                using Stream stream = m_zipEntry.Open();

                byte[] data = new byte[m_zipEntry.Length];
                stream.Read(data, 0, (int) m_zipEntry.Length);
                m_data = data;
            }
            catch
            {
                Log.Warn("Unexpected error reading PK3 entry: {0}", m_zipEntry.FullName);
                m_data = Array.Empty<byte>();
            }

            return m_data;
        }
    }
}
