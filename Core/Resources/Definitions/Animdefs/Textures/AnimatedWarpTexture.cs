namespace Helion.Resources.Definitions.Animdefs.Textures
{
    public class AnimatedWarpTexture
    {
        public readonly string Name;
        public readonly Namespace Namespace;
        public readonly int Speed;
        public readonly bool AllowDecals;
        public readonly bool WaterEffect;
        
        public AnimatedWarpTexture(string name, Namespace resourceNamespace, int? speed, bool allowDecals,
            bool waterEffect)
        {
            Name = name.ToUpper();
            Namespace = resourceNamespace;
            Speed = speed ?? 0;
            AllowDecals = allowDecals;
            WaterEffect = waterEffect;
        }
    }
}