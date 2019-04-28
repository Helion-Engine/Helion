using Helion.Entries;
using Helion.Entries.Tree.Archive;
using Helion.Projects.Component;
using Helion.Projects.Resources;

namespace Helion.Projects
{
    /// <summary>
    /// A component of a project, which wraps around an archive and caches 
    /// relevant resources along with acting as a middleman between project
    /// events and the archive.
    /// </summary>
    public class ProjectComponent
    {
        /// <summary>
        /// The unique project component ID.
        /// </summary>
        public ProjectComponentId Id { get; }

        /// <summary>
        /// The project component metadata.
        /// </summary>
        public ProjectComponentInfo Info { get; }

        /// <summary>
        /// The archive that this is responsible for and wraps around.
        /// </summary>
        public Archive Archive { get; }

        /// <summary>
        /// A cache of the archive entry resources that make data access easier
        /// and cleaner.
        /// </summary>
        public ProjectComponentResourceCache ResourceCache { get; } = new ProjectComponentResourceCache();

        public ProjectComponent(ProjectComponentId id, ProjectComponentInfo info, Archive archive)
        {
            Id = id;
            Info = info;
            Archive = archive;

            foreach (Entry entry in archive)
                ResourceCache.TrackEntry(entry);
        }
    }
}
