using Helion.World.Geometry.Sides;

namespace Helion.World.Geometry.Walls
{
    public class Wall
    {
        public readonly int Id;
        public readonly WallLocation Location;
        public Side Side { get; internal set; }
        public int TextureHandle;

        public Wall(int id, int textureHandle, WallLocation location)
        {
            Id = id;
            TextureHandle = textureHandle;
            Location = location;
            
            // We are okay with things blowing up violently if someone forgets
            // to assign it, because that is such a critical error on the part
            // of the developer if this ever happens that it's deserved. Fixing
            // this would lead to some very messy logic, and when this is added
            // to a parent object, it will add itself for us. If this can be
            // fixed in the future with non-messy code, go for it.
            Side = null !;
        }
    }
}