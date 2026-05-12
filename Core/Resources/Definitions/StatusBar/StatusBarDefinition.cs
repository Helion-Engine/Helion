using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Helion.Layer.Worlds.StatusBar;
using Helion.Util.Extensions;
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
    private List<StatusBarNumberFontDef> m_numberFonts = [];
    private List<StatusBarHudFontDef> m_hudFonts = [];
    private List<StatusBarLayoutDef> m_statusBars = [];
    private int m_statusBarId;

    public const string HiddenLayoutName = "Hidden";
    public const string MinimalLayoutName = "Minimal";
    public const string DetailedLayoutName = "Detailed";

    [JsonPropertyName("numberfonts")]
    public List<StatusBarNumberFontDef> NumberFonts
    {
        get => m_numberFonts;
        set => m_numberFonts = value ?? [];
    }

    // v1.1 Extension: HUD Fonts
    [JsonPropertyName("hudfonts")]
    public List<StatusBarHudFontDef> HudFonts
    {
        get => m_hudFonts;
        set => m_hudFonts = value ?? [];
    }

    [JsonPropertyName("statusbars")]
    public List<StatusBarLayoutDef> StatusBars
    {
        get => m_statusBars;
        set => m_statusBars = value ?? [];
    }

    public void Parse(string data, List<StatusBarLayoutDef> protectedLayouts)
    {
        m_statusBarId++;

        try
        {
            StatusBarDeserializeContext.Prefix = $"{m_statusBarId}";
            var file = JsonSerializer.Deserialize(data, StatusBarJsonContext.Default.StatusBarFileDef);

            if (!string.IsNullOrEmpty(file?.Type))
            {
                LoadDefinition(file.Data, protectedLayouts);
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
                    LoadDefinition(loaded, protectedLayouts);
            }
            catch (JsonException ex)
            {
                throw new ParserException(0, 0, 0, $"SBARDEF JSON Error: {ex.Message}");
            }
        }
    }

    private void LoadDefinition(StatusBarDefinition def, List<StatusBarLayoutDef> protectedLayouts)
    {
        StatusBars.Clear();

        EnsureValidNames(def.StatusBars);
        NumberFonts.AddRange(def.NumberFonts);
        HudFonts.AddRange(def.HudFonts);
        StatusBars.AddRange(def.StatusBars.Where(x => x.Children.Length > 0));
        StatusBars.AddRange(protectedLayouts);
        
        AddHiddenLayout();
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
    
    private static void EnsureValidNames(List<StatusBarLayoutDef> layouts)
    {
        for (int i = 0; i < layouts.Count; i++)
        {
            var layout = layouts[i];
            if (string.IsNullOrWhiteSpace(layout.Name))
            {
                layout.Name = $"Layout {i}";
            }
            else if (layout.Name.EqualsIgnoreCase(HiddenLayoutName) || layout.Name.EqualsIgnoreCase(DetailedLayoutName))
            {
                layout.Name += "*";
            }
        }
    }

    public bool TryGetLayoutByProtectedName(string name, [NotNullWhen(true)] out StatusBarLayoutDef? layout)
    {
        layout = StatusBars.FirstOrDefault(x => x.Name.StartsWithIgnoreCase(name));
        if (layout == null)
            return false;

        return layout.Name.EqualsIgnoreCase(name) || (layout.Name.Length == name.Length + 1 && layout.Name[^1] == '*');
    }
}

public class StatusBarNumberFontDef
{
    private string m_name = string.Empty;

    [JsonPropertyName("name")]
    public string Name
    {
        get => m_name;
        set => m_name = StatusBarDeserializeContext.GetName(value);
    }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("stem")]
    public string Stem { get; set; } = string.Empty;
}

public class StatusBarHudFontDef
{
    private string m_name = string.Empty;

    [JsonPropertyName("name")]
    public string Name
    {
        get => m_name;
        set => m_name = StatusBarDeserializeContext.GetName(value);
    }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("stem")]
    public string Stem { get; set; } = string.Empty;
}

public class StatusBarLayoutDef
{
    private StatusBarElementWrapper[] m_children = [];

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("fullscreenrender")]
    public bool FullscreenRender { get; set; }

    [JsonPropertyName("fillflat")]
    public string? FillFlat { get; set; }

    [JsonPropertyName("children")]
    public StatusBarElementWrapper[] Children
    {
        get => m_children;
        set => m_children = value ?? [];
    }

    [JsonIgnore]
    public bool CoverageSet { get; set; }

    [JsonIgnore]
    public StatusBarCoverage Coverage { get; set; }

    public override string ToString() => Name;
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