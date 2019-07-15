using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Texture;
using Helion.Util;
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
        public readonly ResourceTracker<TextureDefinition> Textures = new ResourceTracker<TextureDefinition>();
        private readonly Dictionary<CIString, Action<Entry>> m_entryNameToAction = new Dictionary<CIString, Action<Entry>>();
        private PnamesTextureXCollection m_pnamesTextureXCollection = new PnamesTextureXCollection();

        public DefinitionEntries()
        {
            m_entryNameToAction["PNAMES"] = entry => m_pnamesTextureXCollection.Add(Pnames.From(entry.ReadData()));
            m_entryNameToAction["TEXTURE1"] = entry => m_pnamesTextureXCollection.Add(TextureX.From(entry.ReadData()));
            m_entryNameToAction["TEXTURE2"] = entry => m_pnamesTextureXCollection.Add(TextureX.From(entry.ReadData()));
            m_entryNameToAction["TEXTURE3"] = entry => m_pnamesTextureXCollection.Add(TextureX.From(entry.ReadData()));
        }
        
        public void Track(Archive archive)
        {
            m_pnamesTextureXCollection = new PnamesTextureXCollection();
            
            foreach (Entry entry in archive.Entries)
                if (m_entryNameToAction.TryGetValue(entry.Path.Name, out var action))
                    action.Invoke(entry);

            if (m_pnamesTextureXCollection.Valid)
                CreateImageDefinitionsFrom(m_pnamesTextureXCollection);
        }

        private void CreateImageDefinitionsFrom(PnamesTextureXCollection collection)
        {
            Precondition(collection.Pnames.Count > 0, "Expecting pnames to exist when reading TextureX definitions");

            // Note: We don't handle multiple pnames. I am not sure how they're
            // handled, it might be 'one pnames to textureX' when more than one
            // pnames exist. If so, the logic will need to change here a bit.
            Pnames pnames = collection.Pnames.First();
            collection.TextureX.SelectMany(textureX => textureX.ToTextureDefinitions(pnames))
                               .ForEach(def => Textures.Insert(def.Name, def.Namespace, def));
        }
    }
}