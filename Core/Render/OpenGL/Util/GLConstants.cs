using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Util
{
    public static class GLConstants
    {
        public static TextureUnit TextureAtlasUnit = TextureUnit.Texture0;
        public static TextureUnit TextureDataUnit = TextureUnit.Texture1;
        public static TextureUnit TextureAnimationUnit = TextureUnit.Texture2;
        public static TextureUnit WallDataUnit = TextureUnit.Texture3;
        public static TextureUnit SectorDataUnit = TextureUnit.Texture4;
        public static TextureUnit SectorPlaneDataUnit = TextureUnit.Texture5;
    }
}