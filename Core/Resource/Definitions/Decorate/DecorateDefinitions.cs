using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helion.Resource.Archives;
using Helion.Resource.Definitions.Decorate.Parser;
using Helion.Util;
using MoreLinq;

namespace Helion.Resource.Definitions.Decorate
{
    public class DecorateDefinitions
    {
        private readonly Dictionary<CIString, ActorDefinition> m_definitions = new();
        private readonly Dictionary<int, ActorDefinition> m_definitionsByEditorNumber = new();
        private readonly Resources m_resources;

        public ActorDefinition? this[CIString name] => m_definitions.TryGetValue(name, out ActorDefinition? def) ? def : null;
        public ActorDefinition? this[int editorNum] => m_definitionsByEditorNumber.TryGetValue(editorNum, out ActorDefinition? def) ? def : null;
        public List<ActorDefinition> GetActorDefinitions() => m_definitions.Values.ToList();

        public DecorateDefinitions(Resources resources)
        {
            m_resources = resources;
        }

        public void AddDecorateDefinitions(Entry entry, Archive archive)
        {
            DecorateParser parser = new(entry.Path.FullPath, MakeIncludeLocator(archive));
            if (parser.Parse(entry))
                parser.ActorDefinitions.ForEach(AddDefinition);
        }

        private Func<string, string?> MakeIncludeLocator(Archive archive)
        {
            return path =>
            {
                Entry? entry = archive.FindByPath(path);
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