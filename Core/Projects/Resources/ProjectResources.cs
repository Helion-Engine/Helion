using Helion.Entries;
using Helion.Entries.Types;
using Helion.Graphics.Palette;
using Helion.Maps;
using Helion.Resources;
using Helion.Resources.Definitions;
using Helion.Resources.Definitions.Texture;
using Helion.Resources.Images;
using Helion.Resources.Sprites;
using Helion.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Projects.Resources
{
    /// <summary>
    /// A collection of project-wide resources.
    /// </summary>
    public class ProjectResources
    {
        private readonly ResourceTracker<Entry> m_masterEntries = new ResourceTracker<Entry>();

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

            Entry? entryOpt = m_masterEntries.GetWithAny(Defines.Playpal, ResourceNamespace.Global);
            if (entryOpt != null && entryOpt is PaletteEntry paletteEntry)
                palette = paletteEntry.Palette;

            foreach (ProjectComponent component in components)
            {
                ProjectComponentResourceCache cache = component.ResourceCache;
                PaletteEntry? entry = cache.FindEntryAs<PaletteEntry>(Defines.Playpal);
                if (entry != null)
                    palette = entry.Palette;
            }

            return palette;
        }

        private DefinitionEntries? GetLatestDefinitionEntries(List<ProjectComponent> components)
        {
            DefinitionEntries? definitionEntries = null;

            foreach(ProjectComponent component in components)
            {
                if (definitionEntries == null)
                {
                    definitionEntries = component.ResourceCache.DefinitionEntries;
                }
                else
                {
                    if (component.ResourceCache.DefinitionEntries.Pnames != null)
                        definitionEntries.Pnames = component.ResourceCache.DefinitionEntries.Pnames;
                    if (component.ResourceCache.DefinitionEntries.TextureXList.Count > 0)
                        definitionEntries.TextureXList = component.ResourceCache.DefinitionEntries.TextureXList;
                }
            }

            return definitionEntries;
        }

        //Caches all image resources needed for the map and clears any previously cached image resources
        public void LoadMapResources(Project project, Map map)
        {
            ImageManager.ClearImages();

            DefinitionEntries? definitionEntries = GetLatestDefinitionEntries(project.Components);

            if (definitionEntries != null && definitionEntries.Pnames != null && definitionEntries.TextureXList.Count > 0)
            {
                var latestPalette = FindLatestPalette(project.Components);
                var textureNames = map.GetUniqueTextureNames();
                textureNames.Add("SKY1"); //temporary hax

                var flatNames = map.GetUniqueFlatNames();
                var textureX = definitionEntries.TextureXList.SelectMany(textureX => textureX.Definitions).Where(x => textureNames.Contains(x.Name)).ToList();

                //TODO sprites
                CacheImageEntriesByNames(latestPalette, flatNames);
                CacheImageEntriesByNames(latestPalette, GetPatchesForTextures(textureX, definitionEntries.Pnames));

                ImageManager.AddTextureDefinitions(definitionEntries.Pnames, textureX);
            }
        }

        private void CacheImageEntriesByNames(Palette palette, IEnumerable<UpperString> names)
        {
            foreach (var name in names)
            {
                var entry = FindEntry(name);
                if (entry != null && !entry.Corrupt)
                {
                    switch(entry)
                    {
                        case ImageEntry imageEntry:
                            ImageManager.Add(imageEntry);
                            break;
                        case PaletteImageEntry paletteImageEntry:
                            ImageManager.Add(paletteImageEntry, palette);
                            break;
                        default:
                            break;
                    }
                }           
            }
        }

        private HashSet<UpperString> GetPatchesForTextures(List<TextureXImage> textureX, Pnames pnames)
        {
            HashSet<UpperString> mapPatches = new HashSet<UpperString>();

            foreach (var tex in textureX)
            {
                foreach (var patch in tex.Patches)
                {
                    if (patch.PnamesIndex > 0 && patch.PnamesIndex < pnames.Names.Count)
                        mapPatches.Add(pnames.Names[patch.PnamesIndex]);
                }
            }

            return mapPatches;
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
        public Entry? FindEntry(UpperString name, ResourceNamespace resourceNamespace = ResourceNamespace.Global)
        {
            return m_masterEntries.GetWithAny(name, resourceNamespace);
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
        public T? FindEntryAs<T>(UpperString name, ResourceNamespace resourceNamespace = ResourceNamespace.Global) where T : Entry
        {
            return m_masterEntries.GetWithAny(name, resourceNamespace) as T;
        }

        /// <summary>
        /// Tracks a list of new components with respect to all of their
        /// entries. All the entries that are not corrupt will be tracked by
        /// this object, making lookup of resources easier.
        /// </summary>
        /// <param name="components">The components to track.</param>
        public void TrackNewComponents(List<ProjectComponent> components)
        {
            foreach (ProjectComponent component in components)
            {
                foreach (Entry entry in component.Archive)
                    if (!entry.Corrupt)
                        m_masterEntries.AddOrOverwrite(entry.Path.Name, entry.Namespace, entry);
            }
        }
    }
}
