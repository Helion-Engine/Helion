using Helion.Resources.Archives.Entries;
using Helion.Util;
using NLog;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Helion.Resources.Definitions.Id24;

public class Id24TranslationDefinition
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public Dictionary<string, TranslationDef> Translations = [];

    public TranslationDef? Parse(Entry entry)
    {
        if (Translations.TryGetValue(entry.Path.Name, out var existing))
            return existing;

        string data = entry.ReadDataAsString();
        try
        {
            var translation = JsonSerializer.Deserialize<TranslationDef>(data, JsonSerializationSettings.IgnoreNull);
            if (translation == null)
            {
                Log.Error(ParseUtil.GetParseError(entry, "translation"));
                return null;
            }

            Translations[entry.Path.Name] = translation;
            return translation;
        }
        catch (Exception ex)
        {
            Log.Error(ex, ParseUtil.GetParseError(entry, "translation"));
        }

        return null;
    }
}
