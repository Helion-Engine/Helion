using Helion.Worlds.Geometry.Lines;
using Helion.Worlds.Textures;

namespace Helion.Worlds.Geometry.Walls
{
    public class Wall
    {
        public readonly int Index;
        public readonly WallLocation Location;
        public Side Side { get; internal set; }
        public IWorldTexture Texture { get; private set; }

        public Wall(int index, IWorldTexture texture, WallLocation location)
        {
            Index = index;
            Texture = texture;
            Location = location;

            // We are okay with things blowing up violently if someone forgets
            // to assign it, because that is such a critical error on the part
            // of the developer if this ever happens that it's deserved. Fixing
            // this would lead to some very messy logic, and when this is added
            // to a parent object, it will add itself for us. If this can be
            // fixed in the future with non-messy code, go for it.
            Side = null !;
        }

        public void SetTexture(IWorldTexture texture, WorldTextureManager textureManager)
        {
            // Since we could be setting a switch, we can't apply the texture
            // directly since it will need its own state. Instead, we have to
            // ask the texture manager to create a managed instance so it will
            // tick on its own.
            Texture = textureManager.Get(texture.Name, texture.Texture.Namespace);
        }
    }
}