using Helion.Resource.Textures;
using Helion.Util;

namespace Helion.Worlds.Textures.Types
{
    /// <summary>
    /// A texture that never changes. It has only one image, and can be shared
    /// among many different lines without needing new instances.
    /// </summary>
    public class StaticWorldTexture : IWorldTexture
    {
        public CIString Name { get; }
        public Texture Texture { get; }
        public bool IsMissing { get; }
        public bool IsSky { get; }

        public StaticWorldTexture(CIString name, Texture texture, bool isMissing = false, bool isSky = false)
        {
            Name = name;
            Texture = texture;
            IsMissing = isMissing;
            IsSky = isSky;
        }

        public override string ToString() => Name.ToString();
    }
}
