using System;
using Helion.ResourcesNew.Archives;
using Helion.ResourcesNew.Tracker;
using Helion.Util;

namespace Helion.ResourcesNew.Definitions.Textures
{
    public class TextureDefinitionManager : INamespaceResourceProvider<TextureDefinition>
    {
        private readonly NamespaceTracker<TextureDefinition> m_definitions = new();
        private Pnames? m_lastPnames;
        private TextureX? m_lastTexture1;
        private TextureX? m_lastTexture2;

        public TextureDefinition? Get(CIString name, Namespace priorityNamespace)
        {
            return m_definitions.Get(name, priorityNamespace);
        }

        public TextureDefinition? GetOnly(CIString name, Namespace resourceNamespace)
        {
            return m_definitions.GetOnly(name, resourceNamespace);
        }

        public void AddPnames(Entry entry)
        {
            m_lastPnames = Pnames.From(entry.ReadData());
        }

        public void AddTextureX(Entry entry)
        {
            TextureX? textureX = TextureX.From(entry.ReadData());
            if (entry.Path.Name.Equals("TEXTURE1", StringComparison.OrdinalIgnoreCase))
                m_lastTexture1 = textureX;
            else
                m_lastTexture2 = textureX;
        }

        public void NotifyArchiveFinished()
        {
            if (m_lastPnames != null)
            {
                if (m_lastTexture1 != null)
                    AddDefinitions(m_lastPnames, m_lastTexture1);
                if (m_lastTexture2 != null)
                    AddDefinitions(m_lastPnames, m_lastTexture2);
            }

            m_lastTexture1 = null;
            m_lastTexture2 = null;
        }

        private void AddDefinitions(Pnames pnames, TextureX textureX)
        {
            // For compatibility reasons, earlier ones get priority.
            foreach (TextureDefinition definition in textureX.ToTextureDefinitions(pnames))
                if (!m_definitions.Contains(definition.Name, definition.Namespace))
                    m_definitions.Insert(definition.Name, definition.Namespace, definition);
        }
    }
}
