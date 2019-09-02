using Helion.Resources.Archives.Collection;

namespace Helion.Audio
{
    public abstract class AudioSoundSystem
    {
        private readonly ArchiveCollection m_archiveCollection;
        
        protected AudioSoundSystem(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
        }
    }
}