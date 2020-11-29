using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Decorate.Parser;
using Helion.Util;
using MoreLinq;

namespace Helion.Resources.Definitions.Decorate
{
    public class DecorateDefinitions
    {
        private readonly Dictionary<CIString, ActorDefinition> m_definitions = new Dictionary<CIString, ActorDefinition>();
        private readonly Dictionary<int, ActorDefinition> m_definitionsByEditorNumber = new Dictionary<int, ActorDefinition>();
        private readonly ArchiveCollection m_archiveCollection;
        
        public ActorDefinition? this[CIString name] => m_definitions.TryGetValue(name, out ActorDefinition? def) ? def : null;
        public ActorDefinition? this[int editorNum] => m_definitionsByEditorNumber.TryGetValue(editorNum, out ActorDefinition? def) ? def : null;

        public List<ActorDefinition> GetActorDefinitions() => m_definitions.Values.ToList();

        public DecorateDefinitions(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
        }

        public void AddDecorateDefinitions(Entry entry)
        {
            DecorateParser parser = new DecorateParser(entry.Path.FullPath, MakeIncludeLocator());
            if (parser.Parse(entry))
                parser.ActorDefinitions.ForEach(AddDefinition);
        }

        private Func<string, string?> MakeIncludeLocator()
        {
            return path =>
            {
                Entry? entry = m_archiveCollection.Entries.FindByPath(path);
                return entry != null ? Encoding.UTF8.GetString(entry.ReadData()) : null;
            };
        }

        private void AddDefinition(ActorDefinition definition)
        {
            m_definitions[definition.Name] = definition;
            
            if (definition.EditorNumber != null)
                m_definitionsByEditorNumber[definition.EditorNumber.Value] = definition;
        }
    }
}