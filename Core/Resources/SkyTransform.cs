using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.Id24;
using Helion.Util;

namespace Helion.Resources;

public enum SkyTransformType
{
    Normal,
    Fire
}

public class SkyTransformTexture
{
    public string TextureName = string.Empty;
    public int TextureIndex;
    public Vec2F Offset;
    public Vec2F Scroll;
    public Vec2F Scale;
    public Vec2F CurrentScroll;
    public float? MidTexel;
    public SkyTransformType Type;
}

public class SkyTransform
{
    public static readonly SkyTransform Default = new()
    {
        Sky = new()
        {
            Scale = Vec2F.One
        }
    };

    public SkyTransformTexture Sky = null!;
    public SkyTransformTexture? Foreground;

    public static SkyTransform FromId24SkyDef(int textureIndex, int? foregroundTextureIndex, Id24SkyDef skyDef)
    {
        return new()
        {
            Sky = new()
            {
                TextureName = skyDef.Name,
                TextureIndex = textureIndex,
                Offset = default,
                Scroll = new((float)(skyDef.ScrollX / Constants.TicksPerSecond), (float)(skyDef.ScrollY / Constants.TicksPerSecond)),
                Scale = new((float)skyDef.ScaleX, (float)skyDef.ScaleY),
                Type = FromId24SkyType(skyDef.Type),
                MidTexel = (float)skyDef.Mid
            },
            Foreground = CreateSkyTextureFromForegroundTexture(skyDef, foregroundTextureIndex)
        };
    }

    public static SkyTransform FromMapInfoSky(int textureIndex, Definitions.MapInfo.MapInfoDef mapInfo)
    {
        return new()
        {
            Sky = new()
            {
                TextureName = mapInfo.Sky1.Name,
                TextureIndex = textureIndex,
                Offset = default,
                Scroll = new ((float)mapInfo.Sky1.ScrollSpeed, 0),
                Scale = Vec2F.One,
                Type = SkyTransformType.Normal
            }
        };
    }

    private static SkyTransformType FromId24SkyType(SkyType type)
    {
        return type switch
        {
           SkyType.Fire => SkyTransformType.Fire,
            _ => SkyTransformType.Normal,
        };
    }

    private static SkyTransformTexture? CreateSkyTextureFromForegroundTexture(Id24SkyDef skyDef, int? foregroundTextureIndex)
    {
        var foreground = skyDef.ForegroundTex;
        if (foregroundTextureIndex == null || foreground == null)
            return null;

        return new()
        {
            TextureName = foreground.Name,
            TextureIndex = foregroundTextureIndex.Value,
            Offset = default,
            Scroll = new((float)(foreground.ScrollX / Constants.TicksPerSecond), (float)(foreground.ScrollY / Constants.TicksPerSecond)),
            Scale = new((float)foreground.ScaleX, (float)foreground.ScaleY),
            Type = SkyTransformType.Normal,
            MidTexel = (float)foreground.Mid
        };
    }
}