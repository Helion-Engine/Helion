using Helion.Entries;
using Helion.Entries.Types;
using Helion.Graphics.Palette;
using Helion.Resources;
using Helion.Resources.Images;
using Helion.Resources.Sprites;
using Helion.Util;
using System.Collections.Generic;

namespace Helion.Project.Resources
{
    /// <summary>
    /// A collection of project-wide resources.
    /// </summary>
    public class ProjectResources
    {
        private readonly ResourceTracker<Entry> mostRecentEntries = new ResourceTracker<Entry>();

        /// <summary>
        /// A manager of all the images loaded for this project.
        /// </summary>
        public ImageManager ImageManager { get; } = new ImageManager();

        /// <summary>
        /// A manager of all the sprite frames that have been found in this 
        /// project.
        /// </summary>
        public SpriteFrameManager SpriteFrameManager { get; } = new SpriteFrameManager();

        private Palette FindLatestPalette(List<ProjectComponent> components)
        {
            Palette palette = Palettes.GetDefaultPalette();

            Optional<Entry> entryOpt = mostRecentEntries.GetWithAny(Defines.Playpal, ResourceNamespace.Global);
            if (entryOpt.Value is PaletteEntry paletteEntry)
                palette = paletteEntry.Palette;

            foreach (ProjectComponent component in components)
            {
                ProjectComponentResourceCache cache = component.ResourceCache;
                Optional<PaletteEntry> entry = cache.FindEntryAs<PaletteEntry>(Defines.Playpal);
                if (entry)
                    palette = entry.Value.Palette;
            }

            return palette;
        }

        private void TrackEntry(Entry entry, Palette latestPalette)
        {
            mostRecentEntries.AddOrOverwrite(entry.Path.Name, entry.Namespace, entry);

            switch (entry)
            {
            case ImageEntry imageEntry:
                ImageManager.Add(imageEntry);
                SpriteFrameManager.Track(entry.Path.Name, entry.Namespace);
                break;
            case PaletteImageEntry paletteImageEntry:
                ImageManager.Add(paletteImageEntry, latestPalette);
                SpriteFrameManager.Track(entry.Path.Name, entry.Namespace);
                break;
            }
        }

        /// <summary>
        /// Looks up an entry by name. The most recent entry in the entire
        /// archive with the name is loaded.
        /// </summary>
        /// <param name="name">The name to look up.</param>
        /// <param name="resourceNamespace">The namespace to look at first. It
        /// will default to the global namespace if not provided.</param>
        /// <returns>The entry if it exists or an empty value if no entry had
        /// that name.</returns>
        public Optional<Entry> FindEntry(UpperString name, ResourceNamespace resourceNamespace = ResourceNamespace.Global)
        {
            return mostRecentEntries.GetWithAny(name, resourceNamespace);
        }

        /// <summary>
        /// Similar to <see cref="FindEntry(UpperString, ResourceNamespace)"/>, 
        /// this funds the entry but also will attempt to return the type. If 
        /// the name matches but the type is wrong, an empty value is returned.
        /// </summary>
        /// <typeparam name="T">The type to get.</typeparam>
        /// <param name="name">The name to get.</param>
        /// <param name="resourceNamespace">The namespace to look at first. It
        /// will default to the global namespace if not provided.</param>
        /// <returns>The entry of the type provided with the name, or an empty
        /// value if both conditions are not met.</returns>
        public Optional<T> FindEntryAs<T>(UpperString name, ResourceNamespace resourceNamespace = ResourceNamespace.Global)
        {
            Optional<Entry> entry = mostRecentEntries.GetWithAny(name, resourceNamespace);
            return (entry && entry.Value is T entryType) ? entryType : Optional<T>.Empty();
        }

        /// <summary>
        /// Tracks a list of new components with respect to all of their
        /// entries. All the entries that are not corrupt will be tracked by
        /// this object, making lookup of resources easier.
        /// </summary>
        /// <param name="components">The components to track.</param>
        public void TrackNewComponents(List<ProjectComponent> components)
        {
            Palette latestPalette = FindLatestPalette(components);

            foreach (ProjectComponent component in components)
                foreach (Entry entry in component.Archive)
                    if (!entry.Corrupt)
                        TrackEntry(entry, latestPalette);
        }
    }
}
