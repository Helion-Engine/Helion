using Helion.Util;

namespace Helion.Resource.Definitions.Animations.Textures
{
    public class AnimatedCameraTexture : IAnimatedTexture
    {
        public CIString Name { get; }
        public Namespace Namespace => Namespace.Textures;
        public readonly int Width;
        public readonly int Height;
        public readonly int? FitWidth;
        public readonly int? FitHeight;
        public readonly bool WorldPanning;

        public AnimatedCameraTexture(CIString name, int width, int height, int? fitWidth, int? fitHeight, bool worldPanning)
        {
            Name = name;
            Width = width;
            Height = height;
            FitWidth = fitWidth;
            FitHeight = fitHeight;
            WorldPanning = worldPanning;
        }
    }
}