using Helion.Util;

namespace Helion.Resource.Definitions.Animations.Textures
{
    public class AnimatedWarpTexture : IAnimatedTexture
    {
        public CIString Name { get; }
        public Namespace Namespace { get; }
        public readonly int Speed;
        public readonly bool AllowDecals;
        public readonly bool WaterEffect;

        public AnimatedWarpTexture(CIString name, Namespace resourceNamespace, int? speed, bool allowDecals,
            bool waterEffect)
        {
            Name = name;
            Namespace = resourceNamespace;
            Speed = speed ?? 0;
            AllowDecals = allowDecals;
            WaterEffect = waterEffect;
        }
    }
}