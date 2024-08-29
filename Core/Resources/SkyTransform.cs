using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.Id24;
using Helion.Util;
using System;

namespace Helion.Resources;

public class SkyTransformTexture
{
    public int TextureIndex;
    public float Mid;
    public Vec2F Scroll;
    public Vec2F Scale;
    public Vec2F CurrentScroll;
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

    public SkyTransformTexture Sky;
    public SkyTransformTexture? Foreground;

    public static SkyTransform FromId24SkyDef(int textureIndex, int? foregroundTextureIndex, SkyDef skyDef)
    {
        return new()
        {
            Sky = new()
            {
                TextureIndex = textureIndex,
                Mid = (float)skyDef.Mid,
                Scroll = new((float)(skyDef.ScrollX / Constants.TicksPerSecond), (float)(skyDef.ScrollY / Constants.TicksPerSecond)),
                Scale = new((float)skyDef.ScaleX, (float)skyDef.ScaleY),
            },
            Foreground = CreateSkyTextureFromForegroundTexture(skyDef, foregroundTextureIndex)
        };
    }

    private static SkyTransformTexture? CreateSkyTextureFromForegroundTexture(SkyDef skyDef, int? foregroundTextureIndex)
    {
        var foreground = skyDef.ForegroundTex;
        if (foregroundTextureIndex == null || foreground == null)
            return null;

        return new()
        {
            TextureIndex = foregroundTextureIndex.Value,
            Mid = (float)foreground.Mid,
            Scroll = new((float)(foreground.ScrollX / Constants.TicksPerSecond), (float)(foreground.ScrollY / Constants.TicksPerSecond)),
            Scale = new((float)foreground.ScaleX, (float)foreground.ScaleY),
        };
    }
}