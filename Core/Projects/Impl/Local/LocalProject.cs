using Helion.Entries;
using Helion.Entries.Tree.Archive;
using Helion.Entries.Tree.Archive.Locator;
using Helion.Projects.Component;
using Helion.Util;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;

namespace Helion.Projects.Impl.Local
{
    /// <summary>
    /// A project that is defined locally. All resources for a local project
    /// are to be on the file system and this has no connection to the outside
    /// world.
    /// </summary>
    /// <remarks>
    /// A local project is designed for an offline user or someone who is not
    /// going to do any online activity. It can also used for engine resource
    /// loading that is played offline.
    /// </remarks>
    public class LocalProject : Project
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private static readonly FilesystemArchiveLocator archiveLocator = new FilesystemArchiveLocator();

        private uint nextComponentId = 0;

        public LocalProject() :
            this(new ProjectId(0), new ProjectInfo("", new Version(0, 0)), new EntryIdAllocator())
        {
        }

        public LocalProject(ProjectId id, ProjectInfo info, EntryIdAllocator idAllocator) :
            base(id, info, idAllocator, new EntryClassifier(idAllocator))
        {
        }

        private ProjectComponentId AllocateComponentId() => new ProjectComponentId(nextComponentId++);

        private ProjectComponentInfo CreateDefaultComponentInfo(string uri)
        {
            string name = Path.GetFileName(uri);
            return new ProjectComponentInfo(name, new Version(0, 0), uri);
        }

        protected override Expected<List<ProjectComponent>> HandleLoad(IList<string> uris)
        {
            List<ProjectComponent> components = new List<ProjectComponent>();

            foreach (string uri in uris)
            {
                log.Info("Loading {0}", uri);
                Expected<Archive> archive = archiveLocator.Locate(uri, Classifier, EntryIdAllocator);

                if (archive.Value != null)
                {
                    ProjectComponentId componentId = AllocateComponentId();
                    ProjectComponentInfo info = CreateDefaultComponentInfo(uri);
                    ProjectComponent component = new ProjectComponent(componentId, info, archive.Value);
                    components.Add(component);
                }
                else
                {
                    log.Error("Failure when loading {0}", uri);
                    return archive.Error;
                }
            }

            return components;
        }
    }
}
