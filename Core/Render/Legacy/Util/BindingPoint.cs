namespace Helion.Render.Legacy.Util;

/// <summary>
/// An index for a binding point, which the index is either used for a
/// buffer texture or a UBO/SSBO in modern OpenGL.
/// </summary>
public enum BindingPoint
{
    TextureData = 0,
    TextureIndirection = 1,
    Sector = 2,
    SectorPlane = 3,
}
