using Helion.Worlds.Geometry.Sectors;

namespace Helion.Worlds.Special
{
    public interface ISectorSpecial : ISpecial
    {
        Sector Sector { get; }
        void FinalizeDestroy();
        void Pause();
        void Resume();
        bool IsPaused { get; }
    }
}