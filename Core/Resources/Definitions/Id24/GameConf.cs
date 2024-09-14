using System;
using System.Collections.Generic;
using Helion.Util.Parser;
using Newtonsoft.Json;

namespace Helion.Resources.Definitions.Id24;

public class GameConf
{
    public string Type { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    // TODO: id1.wad's GAMECONF has an empty "metadata": {} block here, but it doesn't seem to be in the spec
    public GameConfData Data { get; set; } = new();
}

public class GameConfData
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? Iwad { get; set; }
    public string[]? Pwads { get; set; }
    public string[]? PlayerTranslations { get; set; }
    public string? WadTranslation { get; set; }
    public string? Executable { get; set; }
    public string? Mode { get; set; }

    [JsonConverter(typeof(OptionsConverter))]
    public Dictionary<string, string> Options { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class OptionsConverter : JsonConverter<Dictionary<string, string>>
{
    public override void WriteJson(JsonWriter writer, Dictionary<string, string>? value, JsonSerializer serializer) => throw new NotImplementedException();

    public override Dictionary<string, string>? ReadJson(JsonReader reader, Type objectType, Dictionary<string, string>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        Dictionary<string, string> options = [];
        if (reader.TokenType == JsonToken.String)
        {
            SimpleParser parser = new();
            parser.Parse(reader.Value?.ToString() ?? "");
            while (!parser.IsDone())
            {
                int lineNumber = parser.GetCurrentLine();
                try
                {
                    string key = parser.ConsumeString();
                    string value = parser.ConsumeString();
                    options[key] = value;
                }
                catch { }
                if (parser.GetCurrentLine() == lineNumber)
                    parser.ConsumeLine();
            }
        }
        return options;
    }
}

public static class GameConfConstants
{
    public static class Header
    {
        public const string Type = "gameconf";
        public const string Version = "1.0.0";
    }

    public static class Executable
    {
        public const string Doom1_9 = "doom1.9";
        public const string LimitRemoving = "limitremoving";
        public const string BugFixed = "bugfixed";
        public const string Boom2_02 = "boom2.02";
        public const string Complevel9 = "complevel9";
        public const string Mbf = "mbf";
        public const string Mbf21 = "mbf21";
        public const string Mbf21Ex = "mbf21ex";
        public const string Id24 = "id24";
    }

    /// <remarks>
    /// from least to highest priority
    /// </remarks>
    public static readonly string[] ValidExecutables = [
        Executable.Doom1_9,
        Executable.LimitRemoving,
        Executable.BugFixed,
        Executable.Boom2_02,
        Executable.Complevel9,
        Executable.Mbf,
        Executable.Mbf21,
        Executable.Mbf21Ex,
        Executable.Id24,
    ];

    public static class Mode
    {
        public const string Registered = "registered";
        public const string Retail = "retail";
        public const string Commercial = "commercial";
    }

    /// <remarks>
    /// from least to highest priority
    /// </remarks>
    public static readonly string[] ValidModes = [
        Mode.Registered,
        Mode.Retail,
        Mode.Commercial,
    ];
}