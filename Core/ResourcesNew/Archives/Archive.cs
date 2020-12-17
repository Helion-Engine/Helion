using System;
using System.Collections;
using System.Collections.Generic;
using Helion.ResourcesNew.Archives.PK3s;
using Helion.ResourcesNew.Archives.Wads;
using Helion.Util;

namespace Helion.ResourcesNew.Archives
{
    public abstract class Archive : IDisposable, IEnumerable<Entry>
    {
        public readonly string MD5;
        private readonly List<Entry> m_entries = new();
        private readonly Dictionary<CIString, Entry> m_nameToEntry = new();
        private readonly Dictionary<CIString, Entry> m_pathToEntry = new();

        protected Archive(string md5)
        {
            MD5 = md5;
        }

        public static Archive? Open(string path)
        {
            if (path.EndsWith(".wad", StringComparison.OrdinalIgnoreCase))
                return WadFile.From(path);
            return PK3.FromFile(path);
        }

        public Entry? FindByName(CIString name)
        {
            return m_nameToEntry.TryGetValue(name, out Entry? entry) ? entry : null;
        }

        public Entry? FindByPath(CIString path)
        {
            return m_pathToEntry.TryGetValue(path, out Entry? entry) ? entry : null;
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);

            m_entries.Clear();
        }

        protected void AddEntry(Entry entry)
        {
            m_entries.Add(entry);
            m_nameToEntry[entry.Path.Name] = entry;
            m_pathToEntry[entry.Path.ToString()] = entry;
        }

        public IEnumerator<Entry> GetEnumerator() => m_entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
