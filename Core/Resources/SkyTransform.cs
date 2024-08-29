using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.Id24;
using Helion.Util;

namespace Helion.Resources;

public class SkyTransform
{
    public static readonly SkyTransform Default = new()
    {
        Scale = Vec2F.One
    };

    public int TextureIndex;
    public float Mid;
    public Vec2F Scroll;
    public Vec2F Scale;
    public Vec2F CurrentScroll;

    public static SkyTransform FromId24SkyDef(int textureIndex, SkyDef skyDef)
    {
        return new()
        {
            TextureIndex = textureIndex,
            Mid = (float)skyDef.Mid,
            Scroll = new((float)(skyDef.ScrollX / Constants.TicksPerSecond), (float)(skyDef.ScrollY / Constants.TicksPerSecond)),
            Scale = new((float)skyDef.ScaleX, (float)skyDef.ScaleY),
        };
    }
}