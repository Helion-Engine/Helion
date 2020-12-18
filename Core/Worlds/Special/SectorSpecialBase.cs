using Helion.Worlds.Entities;
using Helion.Worlds.Geometry.Sectors;

namespace Helion.Worlds.Special
{
    public abstract class SectorSpecialBase : ISectorSpecial
    {
        public Sector Sector { get; protected set; }

        public abstract SpecialTickStatus Tick();

        public virtual void FinalizeDestroy()
        {
            // Unused
        }

        public virtual void Pause()
        {
            // Unused
        }

        public virtual void Resume()
        {
            // Unused
        }

        public virtual void Use(Entity entity)
        {
            // Unused
        }

        public virtual bool IsPaused { get; }
    }
}
