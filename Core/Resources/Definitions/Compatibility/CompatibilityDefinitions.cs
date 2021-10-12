using System;
using System.Collections.Generic;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using NLog;

namespace Helion.Resources.Definitions.Compatibility;

public class CompatibilityDefinitions
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<string, CompatibilityDefinition> FileDefinitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CompatibilityDefinition> HashDefinitions = new(StringComparer.OrdinalIgnoreCase);

    public CompatibilityMapDefinition? Find(Archive archive, string mapName)
    {
        string name = archive.Path.NameWithExtension;
        if (FileDefinitions.TryGetValue(name, out CompatibilityDefinition? fileCompatDef))
        {
            if (fileCompatDef.MapDefinitions.TryGetValue(mapName, out CompatibilityMapDefinition? mapDef))
                return mapDef;
        }

        if (HashDefinitions.TryGetValue(archive.MD5, out CompatibilityDefinition? hashCompatDef))
        {
            if (hashCompatDef.MapDefinitions.TryGetValue(mapName, out CompatibilityMapDefinition? mapDef))
                return mapDef;
        }

        return null;
    }

    public void AddDefinitions(Entry entry)
    {
        CompatibilityParser parser = new CompatibilityParser();
        if (!parser.Parse(entry))
        {
            Log.Error("Unable to parse compatibility file, certain maps will be broken in some wads");
            return;
        }

        foreach (var (fileName, definition) in parser.Files)
        {
            if (FileDefinitions.ContainsKey(fileName))
                Log.Warn("Overwriting existing compatibility definition for file {0}", fileName);
            FileDefinitions[fileName] = definition;
        }

        foreach (var (hash, definition) in parser.Hashes)
        {
            if (HashDefinitions.ContainsKey(hash))
                Log.Warn("Overwriting existing compatibility definition for hash {0}", hash);
            HashDefinitions[hash] = definition;
        }
    }
}

