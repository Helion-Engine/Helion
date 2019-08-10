namespace Helion.Render.OpenGL.Util
{
    /// <summary>
    /// An index for a binding point, which the index is either used for a
    /// buffer texture or a UBO/SSBO in modern OpenGL.
    /// </summary>
    public enum BindingPoint
    {
        TextureInfo = 1,
        TextureTable = 2,
        Sector = 3,
        SectorPlane = 4,
    }
}