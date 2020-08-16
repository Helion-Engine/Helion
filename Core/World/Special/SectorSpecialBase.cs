using Helion.World.Entities;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special
{
    public abstract class SectorSpecialBase : ISectorSpecial
    {
        public Sector Sector { get; protected set; }

        public abstract SpecialTickStatus Tick();

        public virtual void FinalizeDestroy()
        {

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
