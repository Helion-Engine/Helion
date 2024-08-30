using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Archives.Directories;
using Helion.Resources.Archives.Entries;

namespace Helion.Resources.Archives.Collection;

/// <summary>
/// Tracks all the entries in a collection by both name and namespace for
/// O(1) lookup.
/// </summary>
public class ArchiveCollectionEntries
{
    /// <summary>
    /// A mapping of an upper case string to the most recently loaded
    /// entry.
    /// </summary>
    private readonly Dictionary<string, Entry> m_pathToEntry = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A mapping of an upper case string to the most recently loaded
    /// entry.
    /// </summary>
    private readonly Dictionary<string, Entry> m_nameToEntries = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A mapping of upper case name and namespace to the most recent entry
    /// for that pair of keys.
    /// </summary>
    private readonly ResourceTracker<Entry> m_namespaceEntries = new(ResourceTrackerOptions.None);

    /// <summary>
    /// Tracks a new entry, meaning the entry provided will be accessible
    /// to any callers assuming it is not overridden by another entry with
    /// the same name.
    /// </summary>
    /// <param name="entry">The entry to track.</param>
    public void Track(Entry entry)
    {
        ResourceNamespace ns = entry.Namespace;
        // If this entry has no namespace and was previously defined with one, use that
        // e.g. RSKY1 in a PWAD
        if (ns == ResourceNamespace.Global && m_nameToEntries.TryGetValue(entry.Path.Name, out var existingEntry))
            ns = existingEntry.Namespace;

        string fullPath = entry.Path.FullPath;
        // Lookups for directory paths need to be relative to the directory
        if (entry.Parent is DirectoryArchive)
            fullPath = entry.Path.FullPath[(entry.Parent.Path.FullPath.Length + 1)..];

        m_pathToEntry[fullPath] = entry;
        m_nameToEntries[entry.Path.Name] = entry;
        m_namespaceEntries.Insert(entry.Path.Name, ns, entry);
    }

    /// <summary>
    /// Finds the entry if it exists. This is case insensitive.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <returns>The most recently loaded entry with the name provided, or
    /// null if it does not exist.</returns>
    public Entry? FindByName(string name)
    {
        return m_nameToEntries.TryGetValue(name, out Entry? entry) ? entry : null;
    }

    /// <summary>
    /// Finds the entry by path. This is case insensitive.
    /// </summary>
    /// <param name="path">The path to the entry, such as "FILE.txt" or
    /// "my/folder/path.txt".</param>
    /// <returns>The most recently loaded entry with the name provided, or
    /// null if it does not exist.</returns>
    public Entry? FindByPath(string path)
    {
        return m_pathToEntry.TryGetValue(path, out Entry? entry) ? entry : null;
    }

    /// <summary>
    /// Finds the entry by looking it up relative to some namespace, and
    /// then the case insensitive name.
    /// </summary>
    /// <param name="name">The name of the entry.</param>
    /// <param name="priorityNamespace">The namespace to look in first
    /// before any other namespaces.</param>
    /// <returns>The entry if it exists, null if not.</returns>
    public Entry? FindByNamespace(string name, ResourceNamespace priorityNamespace)
    {
        var entry = m_namespaceEntries.Get(name, priorityNamespace);
        if (entry != null)
            return entry;

        m_nameToEntries.TryGetValue(name, out entry);
        return entry;
    }

    public IEnumerable<string> GetNames(ResourceNamespace specificNamespace)
    {
        return m_namespaceEntries.GetNames(specificNamespace);
    }

    // WARNING: Should only be used sparingly on startup. Never at runtime. This list is allocated each time.
    public List<Entry> GetAllByNamespace(ResourceNamespace resourceNamespace)
    {
        return m_namespaceEntries.GetValues(resourceNamespace).OrderBy(x => x.Index).ToList();
    }
}
