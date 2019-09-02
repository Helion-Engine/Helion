using Helion.Resources.Archives.Collection;

namespace Helion.Audio
{
    public class AudioMusicSystem
    {
        private readonly ArchiveCollection m_archiveCollection;
        
        protected AudioMusicSystem(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
        }
    }
}