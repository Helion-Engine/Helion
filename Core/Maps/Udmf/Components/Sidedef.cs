using Helion.Util;

namespace Helion.Maps.Udmf.Components;

internal class Sidedef
{
    public int Sector;
    public string TextureTop = Constants.NoTexture;
    public string TextureMiddle = Constants.NoTexture;
    public string TextureBottom = Constants.NoTexture;
    public float TopOffsetX;
    public float TopOffsetY;
    public float MiddleOffsetX;
    public float MiddleOffsetY;
    public float BottomOffsetX;
    public float BottomOffsetY;

    public float TopScaleX = 1f;
    public float TopScaleY = 1f;
    public float MiddleScaleX = 1f;
    public float MiddleScaleY = 1f;
    public float BottomScaleX = 1f;
    public float BottomScaleY = 1f;
}
