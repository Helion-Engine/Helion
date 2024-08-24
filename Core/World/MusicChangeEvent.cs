using Helion.Resources.Archives.Entries;

namespace Helion.World
{
    public readonly record struct MusicChangeEvent(Entry Entry, MusicFlags MusicFlags);
}
