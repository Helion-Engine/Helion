using Helion.Entries;
using Helion.Entries.Tree.Archive.Iterator;
using Helion.Map;
using Helion.Projects.Resources;
using Helion.Util;
using NLog;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Projects
{
    /// <summary>
    /// A top level manager for a series of zero or more project components. It
    /// also coordinates all the loaded resources under it.
    /// </summary>
    /// <remarks>
    /// <para>See the wiki for significant more information.</para>
    /// <para>In short, a project manages all the resources for the components
    /// that are loaded into it.</para>
    /// </remarks>
    public abstract class Project
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The unique identifier of a project.
        /// </summary>
        public ProjectId Id { get; }

        /// <summary>
        /// The information about a project.
        /// </summary>
        public ProjectInfo Info { get; }

        /// <summary>
        /// All the cached resources that make up a project.
        /// </summary>
        public ProjectResources Resources { get; } = new ProjectResources();

        protected EntryIdAllocator EntryIdAllocator;
        protected EntryClassifier Classifier;
        protected List<ProjectComponent> Components = new List<ProjectComponent>();

        protected Project(ProjectId id, ProjectInfo info, EntryIdAllocator entryIdAllocator,
            EntryClassifier entryClassifier)
        {
            Id = id;
            Info = info;
            EntryIdAllocator = entryIdAllocator;
            Classifier = entryClassifier;
        }

        private bool AlreadyLoadedOrDuplicate(IList<string> uris)
        {
            HashSet<string> uriSet = new HashSet<string>();
            Components.ForEach(component => uriSet.Add(component.Info.Uri));

            foreach (string uri in uris)
            {
                if (uriSet.Contains(uri))
                {
                    log.Error("Duplicate URI detected with: {0}", uri);
                    return true;
                }

                uriSet.Add(uri);
            }

            return false;
        }

        /// <summary>
        /// Loads a list of URIs. All are loaded or none at all, so the state
        /// will not be changed if loading of a component at a URI fails. This
        /// is a guarantee that the internal state will not be updated if any
        /// one of the URIs are invalid/corrupt/broken/etc.
        /// </summary>
        /// <param name="uris">A list of URIs to load.</param>
        /// <returns>True if all the URIs successfully loaded, false if one
        /// or more failed.</returns>
        public bool Load(IList<string> uris)
        {
            if (AlreadyLoadedOrDuplicate(uris))
            {
                log.Error("Trying to load the same resource twice");
                return false;
            }

            Expected<List<ProjectComponent>, string> loadedComponents = HandleLoad(uris);
            if (!loadedComponents)
            {
                log.Error($"Unable to load one or more project components: {loadedComponents.Error}");
                return false;
            }

            Components.AddRange(loadedComponents.Value);
            Resources.TrackNewComponents(loadedComponents.Value);

            return true;
        }

        public Optional<ValidMapEntryCollection> GetMap(UpperString mapName)
        {
            // TODO: Can we make some cleaner 'reverse iterator' extension?
            // Like an IList<>.ForEachReverse(...)?
            for (int i = Components.Count - 1; i >= 0; i--)
            {
                ProjectComponent component = Components[i];

                ArchiveMapIterator mapIterator = new ArchiveMapIterator(component.Archive);
                foreach (MapEntryCollection mapEntryCollection in mapIterator)
                {
                    if (mapEntryCollection.Name == mapName)
                    {
                        Optional<ValidMapEntryCollection> map = ValidMapEntryCollection.From(mapEntryCollection);
                        if (map)
                            return map;
                    }
                }
            }

            return Optional.Empty;
        }

        /// <summary>
        /// Will perform the actual loading of the URIs. This will either
        /// return a list of all the loaded components, or an error message
        /// if any of them fail. Like <see cref="Load(IList{string})"/>, this
        /// will either succeed completely or it will fail and not change the
        /// internal state.
        /// </summary>
        /// <param name="uris">The list of URIs to load. There will not be
        /// duplicates in this list.</param>
        /// <returns>A list of loaded components, or an error result if loading
        /// failed (and why).</returns>
        protected abstract Expected<List<ProjectComponent>, string> HandleLoad(IList<string> uris);
    }
}
