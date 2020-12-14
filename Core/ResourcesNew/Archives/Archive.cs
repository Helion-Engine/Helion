using System;
using System.Collections;
using System.Collections.Generic;
using Helion.Util;

namespace Helion.ResourcesNew.Archives
{
    public abstract class Archive : IDisposable, IEnumerable<Entry>
    {
        public readonly string MD5;
        private readonly List<Entry> m_entries = new();
        private readonly Dictionary<CIString, Entry> m_nameToEntry = new();

        public Archive(string md5)
        {
            MD5 = md5;
        }

        public static Archive? Open(string path)
        {
            // TODO: Open wads, pk3s, etc...
            throw new NotImplementedException();
        }

        public Entry? FindByName(CIString name)
        {
            return m_nameToEntry.TryGetValue(name, out Entry? entry) ? entry : null;
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
        }

        public IEnumerator<Entry> GetEnumerator() => m_entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
