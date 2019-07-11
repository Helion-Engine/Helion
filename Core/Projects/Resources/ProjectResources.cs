using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Entries;
using Helion.Graphics;
using Helion.Graphics.Palette;
using Helion.Resources;
using Helion.Resources.Definitions;
using Helion.Resources.Definitions.Texture;
using Helion.Resources.Sprites;
using Helion.Util;
using NLog;

namespace Helion.Projects.Resources
{
    /// <summary>
    /// A collection of project-wide resources.
    /// </summary>
    public class ProjectResources
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// A manager of all the sprite frames that have been found in this 
        /// project.
        /// </summary>
        public SpriteFrameManager SpriteFrameManager { get; } = new SpriteFrameManager();
        public Palette Palette { get; private set; }
        public DefinitionEntries? DefinitionEntries { get; private set; }

        private readonly Project m_project;
        private readonly Dictionary<CiString, Entry> m_masterEntries = new Dictionary<CiString, Entry>();

        public ProjectResources(Project project)
        {
            m_project = project;
            Palette = Palettes.GetDefaultPalette();
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
        public Entry? FindEntry(CiString name)
        {
            if (m_masterEntries.ContainsKey(name))            
                return m_masterEntries[name];

            return null;
        }

        /// <summary>
        /// Tracks a list of new components with respect to all of their
        /// entries. All the entries that are not corrupt will be tracked by
        /// this object, making lookup of resources easier.
        /// </summary>
        /// <param name="components">The components to track.</param>
        public void TrackNewComponents(List<ProjectComponent> components)
        {
            foreach(ProjectComponent component in components)
                component.Archive.Entries.ForEach(x => m_masterEntries[x.Path.Name] = x);

            Palette = ReadPalette();
            DefinitionEntries = ReadDefinitionEntries();
        }

        public Image? GetImage(CiString name)
        {
            if (DefinitionEntries?.Pnames != null)
            {
                var textureX = DefinitionEntries.GetTextureXImage(name);
                if (textureX != null)
                    return ImageFromTextureX(DefinitionEntries.Pnames, textureX);
            }

            //If it's not a texture try to load the data as an image
            return LoadImageFromEntryName(name);
        }

        private Image ImageFromTextureX(Pnames pnames, TextureXImage imageDefinition)
        {
            ImageMetadata metadata = new ImageMetadata(ResourceNamespace.Textures);
            Image image = new Image(imageDefinition.Width, imageDefinition.Height, Image.Transparent, metadata);

            foreach (TextureXPatch patch in imageDefinition.Patches)
            {
                if (patch.PnamesIndex < 0 || patch.PnamesIndex >= pnames.Names.Count)
                {
                    log.Warn("Unable to find patch index {0} for texture X definition '{1}'", patch.PnamesIndex, imageDefinition.Name);
                    continue;
                }

                CiString patchName = pnames.Names[patch.PnamesIndex];
                Image? patchImage = LoadImageFromEntryName(patchName);
                if (patchImage == null)
                {
                    log.Warn("Unable to find patch '{0}' for texture X definition '{1}'", patchName, imageDefinition.Name);
                    continue;
                }

                patchImage.DrawOnTopOf(image, patch.Offset);
            }

            return image;
        }

        private Image? LoadImageFromEntryName(CiString name)
        {
            var entry = m_project.Resources.FindEntry(name);
            if (entry != null)
                return ImageFromEntry(entry);

            return null;
        }

        private Image? ImageFromEntry(Entry entry)
        {
            byte[] data = entry.ReadData();

            if (ImageReader.CanRead(data))
            {
                return ImageReader.Read(data);
            }
            else if (PaletteReaders.LikelyColumn(data))
            {
                var paletteImage = PaletteReaders.ReadColumn(data, entry.Namespace);
                if (paletteImage != null)
                    return paletteImage.ToImage(Palette);
            }
            else if (PaletteReaders.LikelyFlat(data))
            {
                var paletteImage = PaletteReaders.ReadFlat(data, entry.Namespace);
                if (paletteImage != null)
                    return paletteImage.ToImage(Palette);
            }

            return null;
        }

        private Palette ReadPalette()
        {
            Palette palette = Palettes.GetDefaultPalette();

            Entry? paletteEntry = FindEntry(Defines.Playpal);
            if (paletteEntry != null)
            {
                var readPalette = Palette.From(paletteEntry.ReadData());
                if (readPalette != null)
                    palette = readPalette;
                else
                    log.Warn($"Corrupt palette at: {paletteEntry.Path}");
            }

            return palette;
        }

        private DefinitionEntries? ReadDefinitionEntries()
        {
            DefinitionEntries definitionEntries = new DefinitionEntries();     
            
            Entry? pnamesEntry = FindEntry(Defines.Pnames);
            if (pnamesEntry != null)
                AddPnames(definitionEntries, pnamesEntry);

            GetTextureEntries().ForEach(x => AddTextureX(definitionEntries, x));

            return definitionEntries;
        }

        private List<Entry> GetTextureEntries()
        {
            List<Entry?> entries = new List<Entry?>();
            Array.ForEach(Defines.TextureDefinitions, x => entries.Add(FindEntry(x)));
            return entries.Where(x => x != null).Cast<Entry>().ToList();
        }

        private static void AddPnames(DefinitionEntries definitionEntries, Entry pnamesEntry)
        {
            Pnames? pnames = Pnames.From(pnamesEntry.ReadData());
            if (pnames != null)
                definitionEntries.Pnames = pnames;
            else
                log.Warn($"Corrupt Pnames at: {pnamesEntry.Path}");
        }

        private static void AddTextureX(DefinitionEntries definitionEntries, Entry textureEntry)
        {
            TextureX? textureX = TextureX.From(textureEntry.ReadData());
            if (textureX != null)
                definitionEntries.AddTextureX(textureX);
            else
                log.Warn($"Corrupt TextureX {textureEntry.Path}");
        }
    }
}
