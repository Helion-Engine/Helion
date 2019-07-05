using NLog;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.Shared
{
    public enum FilterType
    {
        Nearest,
        Bilinear,
        Trilinear
    }

    public static class FilterTypeExtensions 
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public static TextureMinFilter ToOpenTKTextureMinFilter(this FilterType minFilter)
        {
            switch (minFilter)
            {
            case FilterType.Nearest:
                return TextureMinFilter.Nearest;
            case FilterType.Bilinear:
                return TextureMinFilter.LinearMipmapNearest;
            case FilterType.Trilinear:
                return TextureMinFilter.LinearMipmapLinear;
            default:
                log.Error("Unsupported texture filtering type, defaulting to Nearest filter");
                goto case FilterType.Nearest;
            }
        }
    }
}
