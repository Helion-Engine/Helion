using Helion.Resources.Archives.Entries;
using Helion.Util;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.Id24;

public class Id24TranslationDefinition
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public List<TranslationDef> Translations = [];

    public TranslationDef? Parse(Entry entry)
    {
        string data = entry.ReadDataAsString();
        try
        {
            var translation = JsonConvert.DeserializeObject<TranslationDef>(data, JsonSerializationSettings.IgnoreNull);
            if (translation == null)
            {
                Log.Error(ParseUtil.GetParseError(entry, "translation"));
                return null;
            }

            Translations.Add(translation);
            return translation;
        }
        catch (Exception ex)
        {
            Log.Error(ex, ParseUtil.GetParseError(entry, "translation"));
        }

        return null;
    }
}
