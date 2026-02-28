using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Helion.Layer.Worlds.StatusBar;
using Helion.Util.Parser;

namespace Helion.Resources.Definitions.StatusBar;

public class StatusBarFileDef
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public StatusBarDefinition Data { get; set; } = new();
}

public class StatusBarDefinition
{
    public const string HiddenLayoutName = "Hidden"; 
    
    [JsonPropertyName("numberfonts")]
    public List<StatusBarNumberFontDef> NumberFonts { get; set; } = [];

    // v1.1 Extension: HUD Fonts
    [JsonPropertyName("hudfonts")]
    public List<StatusBarHudFontDef> HudFonts { get; set; } = [];

    [JsonPropertyName("statusbars")]
    public List<StatusBarLayoutDef> StatusBars { get; set; } = [];

    public void Parse(string data)
    {
        try 
        {
            var file = JsonSerializer.Deserialize(data, StatusBarJsonContext.Default.StatusBarFileDef);

            if (!string.IsNullOrEmpty(file?.Type))
            {
                NumberFonts.Clear();
                HudFonts.Clear();
                StatusBars.Clear();

                NumberFonts.AddRange(file.Data.NumberFonts);
                HudFonts.AddRange(file.Data.HudFonts);
                StatusBars.AddRange(file.Data.StatusBars);
                
                EnsureValidNames();
                AddHiddenLayout();
            }
            else
            {
                throw new JsonException();
            }
        }
        catch (JsonException) 
        { 
            // Fallback to legacy (unwrapped) format
            try
            {
                var loaded = JsonSerializer.Deserialize(data, StatusBarJsonContext.Default.StatusBarDefinition);
                if (loaded != null)
                {
                    NumberFonts.Clear();
                    HudFonts.Clear();
                    StatusBars.Clear();

                    NumberFonts.AddRange(loaded.NumberFonts);
                    HudFonts.AddRange(loaded.HudFonts);
                    StatusBars.AddRange(loaded.StatusBars);
                    
                    EnsureValidNames();
                    AddHiddenLayout();
                }
            }
            catch (JsonException ex)
            {
                throw new ParserException(0, 0, 0, $"SBARDEF JSON Error: {ex.Message}");
            }
        }
    }
    
    private void AddHiddenLayout()
    {
        StatusBars.Add(new StatusBarLayoutDef
        {
            Name = HiddenLayoutName,
            Height = 0,
            FullscreenRender = true,
            Children = []
        });
    }
    
    private void EnsureValidNames()
    {
        for (int i = 0; i < StatusBars.Count; i++)
        {
            var layout = StatusBars[i];
            if (string.IsNullOrWhiteSpace(layout.Name))
            {
                layout.Name = $"Layout {i}";
            }
            else if (string.Equals(layout.Name, HiddenLayoutName, StringComparison.OrdinalIgnoreCase))
            {
                layout.Name += "*";
            }
        }
    }
}

public class StatusBarNumberFontDef
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("stem")]
    public string Stem { get; set; } = string.Empty;
}

public class StatusBarHudFontDef
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("stem")]
    public string Stem { get; set; } = string.Empty;
}

public class StatusBarLayoutDef
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("fullscreenrender")]
    public bool FullscreenRender { get; set; }

    [JsonPropertyName("fillflat")]
    public string? FillFlat { get; set; }

    [JsonPropertyName("children")]
    public StatusBarElementWrapper[] Children { get; set; } = [];

    [JsonIgnore]
    public bool CoverageSet { get; set; }

    [JsonIgnore]
    public StatusBarCoverage Coverage { get; set; }
}

[JsonSourceGenerationOptions(
    ReadCommentHandling = JsonCommentHandling.Skip, 
    AllowTrailingCommas = true, 
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(StatusBarFileDef))]
[JsonSerializable(typeof(StatusBarDefinition))]
[JsonSerializable(typeof(StatusBarListDef))]
[JsonSerializable(typeof(StatusBarStringDef))]
internal sealed partial class StatusBarJsonContext : JsonSerializerContext
{
}