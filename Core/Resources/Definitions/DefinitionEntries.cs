using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Animdefs;
using Helion.Resources.Definitions.Compatibility;
using Helion.Resources.Definitions.Decorate;
using Helion.Resources.Definitions.Decorate.Locks;
using Helion.Resources.Definitions.Fonts;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Resources.Definitions.Texture;
using Helion.Util;
using Helion.Util.Extensions;
using MoreLinq;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions
{
    /// <summary>
    /// All the text-based entries that have been parsed into usable data
    /// structures.
    /// </summary>
    public class DefinitionEntries
    {
        public readonly AnimatedDefinitions Animdefs = new AnimatedDefinitions();
        public readonly CompatibilityDefinitions Compatibility = new CompatibilityDefinitions();
        public readonly DecorateDefinitions Decorate;
        public readonly FontDefinitionCollection Fonts = new FontDefinitionCollection();
        public readonly ResourceTracker<TextureDefinition> Textures = new ResourceTracker<TextureDefinition>();
        public readonly SoundInfoDefinition SoundInfo = new SoundInfoDefinition();
        public readonly LockDefinitions LockDefininitions = new LockDefinitions();
        private readonly Dictionary<CIString, Action<Entry>> m_entryNameToAction = new Dictionary<CIString, Action<Entry>>();
        private PnamesTextureXCollection m_pnamesTextureXCollection = new PnamesTextureXCollection();

        /// <summary>
        /// Creates a definition entries data structure which has no tracked
        /// data.
        /// </summary>
        public DefinitionEntries(ArchiveCollection archiveCollection)
        {
            Decorate = new DecorateDefinitions(archiveCollection);

            m_entryNameToAction["ANIMDEFS"] = entry => Animdefs.AddDefinitions(entry);
            m_entryNameToAction["COMPATIBILITY"] = entry => Compatibility.AddDefinitions(entry);
            m_entryNameToAction["DECORATE"] = entry => Decorate.AddDecorateDefinitions(entry);
            m_entryNameToAction["FONTS"] = entry => Fonts.AddFontDefinitions(entry);
            m_entryNameToAction["PNAMES"] = entry => m_pnamesTextureXCollection.AddPnames(entry);
            m_entryNameToAction["TEXTURE1"] = entry => m_pnamesTextureXCollection.AddTextureX(entry);
            m_entryNameToAction["TEXTURE2"] = entry => m_pnamesTextureXCollection.AddTextureX(entry);
            m_entryNameToAction["TEXTURE3"] = entry => m_pnamesTextureXCollection.AddTextureX(entry);
            m_entryNameToAction["SNDINFO"] = entry => ParseSoundInfo(entry);
        }

        private void ParseSoundInfo(Entry entry)
        {
            SoundInfo.Parse(entry);
        }
        
        /// <summary>
        /// Tracks all the resources from an archive.
        /// </summary>
        /// <param name="archive">The archive to examine for any texture
        /// definitions.</param>
        public void Track(Archive archive)
        {
            m_pnamesTextureXCollection = new PnamesTextureXCollection();

            foreach (Entry entry in archive.Entries)
                if (m_entryNameToAction.TryGetValue(entry.Path.Name, out var action))
                    action(entry);

            if (m_pnamesTextureXCollection.Valid)
                CreateImageDefinitionsFrom(m_pnamesTextureXCollection);
        }

        private void CreateImageDefinitionsFrom(PnamesTextureXCollection collection)
        {
            Precondition(!collection.Pnames.Empty(), "Expecting pnames to exist when reading TextureX definitions");

            // Note: We don't handle multiple pnames. I am not sure how they're
            // handled, it might be 'one pnames to textureX' when more than one
            // pnames exist. If so, the logic will need to change here a bit.
            Pnames pnames = collection.Pnames.First();
            collection.TextureX.SelectMany(textureX => textureX.ToTextureDefinitions(pnames))
                               .ForEach(def => Textures.Insert(def.Name, def.Namespace, def));
        }
    }
}