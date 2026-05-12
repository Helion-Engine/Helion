using Helion.Geometry.Vectors;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Textures;
using Helion.Resources.Definitions.StatusBar.Enums;
using System;
using System.Text.Json.Serialization;

namespace Helion.Resources.Definitions.StatusBar;

public record struct ElementBounds(int X1, int Y1, int X2, int Y2)
{
    public readonly int Width => X2 - X1;
    public readonly int Height => Y2 - Y1;
    public static readonly ElementBounds Empty = new(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);

    public static void Union(ref ElementBounds a, in ElementBounds b)
    {
        a.X1 = Math.Min(a.X1, b.X1);
        a.Y1 = Math.Min(a.Y1, b.Y1);
        a.X2 = Math.Max(a.X2, b.X2);
        a.Y2 = Math.Max(a.Y2, b.Y2);
    }
}

public class StatusBarElementWrapper
{
    [JsonPropertyName("canvas")]
    public StatusBarCanvasDef? Canvas { get; set; }
    
    [JsonPropertyName("list")]
    public StatusBarListDef? List { get; set; }
    
    [JsonPropertyName("native")]
    public StatusBarNativeDef? Native { get; set; }

    [JsonPropertyName("graphic")]
    public StatusBarGraphicDef? Graphic { get; set; }

    [JsonPropertyName("animation")]
    public StatusBarAnimationDef? Animation { get; set; }

    [JsonPropertyName("face")]
    public StatusBarFaceDef? Face { get; set; }

    [JsonPropertyName("facebackground")]
    public StatusBarFaceDef? FaceBackground { get; set; }

    [JsonPropertyName("number")]
    public StatusBarNumberDef? Number { get; set; }

    [JsonPropertyName("percent")]
    public StatusBarNumberDef? Percent { get; set; }
    
    [JsonPropertyName("string")]
    public StatusBarStringDef? String { get; set; }

    [JsonPropertyName("component")]
    public StatusBarComponentDef? Component { get; set; }

    [JsonPropertyName("carousel")]
    public StatusBarCarouselDef? Carousel { get; set; }

    [JsonIgnore]
    public bool HasConditions { get; set; }

    [JsonIgnore]
    public bool BoundsSet { get; set; }

    [JsonIgnore]
    public ElementBounds Bounds { get; set; }

    [JsonIgnore]
    public Vec2I Size { get; set; }

    public bool CheckHasConditions()
    {
        return
            (Canvas?.Conditions != null && Canvas.Conditions.Length > 0) ||
            (List?.Conditions != null && List.Conditions.Length > 0) ||
            (Native?.Conditions != null && Native.Conditions.Length > 0) ||
            (Graphic?.Conditions != null && Graphic.Conditions.Length > 0) ||
            (Animation?.Conditions != null && Animation.Conditions.Length > 0) ||
            (FaceBackground?.Conditions != null && FaceBackground.Conditions.Length > 0) ||
            (Number?.Conditions != null && Number.Conditions.Length > 0) ||
            (Percent?.Conditions != null && Percent.Conditions.Length > 0) ||
            (String?.Conditions != null && String.Conditions.Length > 0) ||
            (Component?.Conditions != null && Component.Conditions.Length > 0) ||
            (Carousel?.Conditions != null && Carousel.Conditions.Length > 0);
    }
}

public class StatusBarCropDef
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("left")]
    public int Left { get; set; }

    [JsonPropertyName("top")]
    public int Top { get; set; }
    
    [JsonPropertyName("center")]
    public bool Center { get; set; }
}

public abstract class StatusBarBaseDef
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("alignment")]
    public StatusBarAlignment Alignment { get; set; }

    [JsonPropertyName("tranmap")]
    public string? Tranmap { get; set; }

    [JsonPropertyName("translation")]
    public string? Translation { get; set; }

    [JsonPropertyName("conditions")]
    public StatusBarConditionDef[]? Conditions { get; set; }

    [JsonPropertyName("children")]
    public StatusBarElementWrapper[]? Children { get; set; }
    
    [JsonIgnore]
    public int ResolvedHeight { get; set; }

    [JsonIgnore]
    public bool LastEvaluatedConditionValue { get; set; }

    [JsonIgnore]
    public ElementBounds LastBounds { get; set; }
}

public class StatusBarCanvasDef : StatusBarBaseDef { }

public class StatusBarNativeDef : StatusBarBaseDef { }

public class StatusBarListDef : StatusBarBaseDef 
{
    [JsonPropertyName("horizontal")]
    public bool Horizontal { get; set; }
    
    [JsonPropertyName("spacing")]
    public int Spacing { get; set; }
}

public class StatusBarStringDef : StatusBarBaseDef
{
    private string m_font = "";

    // Update this
    [JsonPropertyName("font")]
    public string Font
    {
        get => m_font;
        set => m_font = StatusBarDeserializeContext.GetName(value);
    }

    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    [JsonPropertyName("data")]
    public string? Data { get; set; }
    
    [JsonPropertyName("translucency")]
    public bool Translucency { get; set; }
}

public class StatusBarFaceDef : StatusBarBaseDef 
{
    [JsonIgnore]
    public IRenderableTextureHandle? Handle { get; set; }
    
    // v1.1 Extensions: Image Cropping & Translucency
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("topoffset")]
    public int TopOffset { get; set; }

    [JsonPropertyName("leftoffset")]
    public int LeftOffset { get; set; }

    [JsonPropertyName("midoffset")]
    public int MidOffset { get; set; }

    [JsonPropertyName("translucency")]
    public bool Translucency { get; set; }
    
    // v1.2 Crop Object
    [JsonPropertyName("crop")]
    public StatusBarCropDef? Crop { get; set; }
}

public class StatusBarComponentDef : StatusBarBaseDef 
{
    private string m_type = string.Empty;
    private string m_font = string.Empty;

    [JsonPropertyName("type")]
    public string Type 
    { 
        get => m_type; 
        set 
        {
            m_type = value;
            ComponentType = ParseType(value);
        }
    }

    public StatusBarComponentType ComponentType { get; private set; }

    [JsonPropertyName("font")]
    public string Font
    {
        get => m_font;
        set => m_font = StatusBarDeserializeContext.GetName(value);
    }

    // v1.1
    [JsonPropertyName("vertical")]
    public bool Vertical { get; set; }
    
    [JsonPropertyName("translucency")]
    public bool Translucency { get; set; }
    
    // Additional undocumented property in woof's SBARDEF!
    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    private static StatusBarComponentType ParseType(string type)
    {
        if (string.IsNullOrEmpty(type)) return StatusBarComponentType.Unknown;
        
        return type.ToLowerInvariant() switch
        {
            "stat_totals" => StatusBarComponentType.StatTotals,
            "time" => StatusBarComponentType.Time,
            "coordinates" => StatusBarComponentType.Coordinates,
            "speedometer" => StatusBarComponentType.Speedometer,
            "level_title" => StatusBarComponentType.LevelTitle,
            "fps_counter" => StatusBarComponentType.FpsCounter,
            "message" => StatusBarComponentType.Message,
            "announce_level_title" => StatusBarComponentType.AnnounceLevelTitle,
            "render_stats" => StatusBarComponentType.RenderStats,
            "command_history" => StatusBarComponentType.CommandHistory,
            "chat" => StatusBarComponentType.Chat,
            _ => StatusBarComponentType.Unknown
        };
    }
}

    
public class StatusBarCarouselDef : StatusBarBaseDef 
{
    // v1.1 Extensions
    [JsonPropertyName("translucency")]
    public bool Translucency { get; set; }
}

public class StatusBarGraphicDef : StatusBarBaseDef
{
    [JsonPropertyName("patch")]
    public string Patch { get; set; } = string.Empty;

    [JsonIgnore]
    public string? ResolvedPatchName { get; set; }

    [JsonIgnore]
    public IRenderableTextureHandle? Handle { get; set; }

    // v1.1 Extensions: Image Cropping
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("topoffset")]
    public int TopOffset { get; set; }

    [JsonPropertyName("leftoffset")]
    public int LeftOffset { get; set; }

    [JsonPropertyName("midoffset")]
    public int MidOffset { get; set; }

    [JsonPropertyName("translucency")]
    public bool Translucency { get; set; }
    
    // v1.2 Crop Object
    [JsonPropertyName("crop")]
    public StatusBarCropDef? Crop { get; set; }
}

public class StatusBarAnimationDef : StatusBarBaseDef
{
    [JsonPropertyName("frames")]
    public StatusBarFrameDef[] Frames { get; set; } = [];
}

public class StatusBarNumberDef : StatusBarBaseDef
{
    private string m_font = string.Empty;

    [JsonPropertyName("font")]
    public string Font
    {
        get => m_font;
        set => m_font = StatusBarDeserializeContext.GetName(value);
    }

    [JsonPropertyName("type")]
    public StatusBarNumberType Type { get; set; }

    [JsonPropertyName("param")]
    public int Param { get; set; }

    [JsonPropertyName("maxlength")]
    public int MaxLength { get; set; }
    
    [JsonPropertyName("translucency")]
    public bool Translucency { get; set; }
}

public struct StatusBarConditionDef
{
    [JsonPropertyName("condition")]
    public StatusBarConditionType Condition { get; set; }

    [JsonPropertyName("param")]
    public int Param { get; set; }
    
    [JsonPropertyName("param2")]
    public int Param2 { get; set; }
    
    [JsonPropertyName("param_string")]
    public string? ParamString { get; set; }
}

public struct StatusBarFrameDef
{
    [JsonPropertyName("lump")]
    public string Lump { get; set; }

    [JsonIgnore]
    public string? ResolvedPatchName { get; set; }

    [JsonIgnore]
    public IRenderableTextureHandle? Handle { get; set; }

    [JsonPropertyName("duration")]
    public double Duration { get; set; }
}
