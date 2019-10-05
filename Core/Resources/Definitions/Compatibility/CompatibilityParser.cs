using System.Collections.Generic;
using Helion.Resources.Definitions.Compatibility.Lines;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using NLog;

namespace Helion.Resources.Definitions.Compatibility
{
    public class CompatibilityParser : ParserBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly Dictionary<CIString, CompatibilityDefinition> Files = new Dictionary<CIString, CompatibilityDefinition>();
        public readonly Dictionary<CIString, CompatibilityDefinition> Hashes = new Dictionary<CIString, CompatibilityDefinition>();
        private CompatibilityDefinition m_definition = new CompatibilityDefinition();
        private CompatibilityMapDefinition m_mapDefinition = new CompatibilityMapDefinition();
        private CIString m_mapName = "";

        protected override void PerformParsing()
        {
            while (!Done)
                ConsumeDefinitions();
        }
        
        private void AddDefinitionToIdentifier(IEnumerable<CIString> identifiers)
        {
            foreach (CIString identifier in identifiers)
            {
                if (identifier.ToString().IsMD5())
                {
                    if (Hashes.ContainsKey(identifier))
                        Log.Warn("Duplicate compatibility definition for hash {0}, overwriting old definition", identifier);
                    Hashes[identifier] = m_definition;
                }
                else
                {
                    if (Files.ContainsKey(identifier))
                        Log.Warn("Duplicate compatibility definition for file {0}, overwriting old definition", identifier);
                    Files[identifier] = m_definition;
                }
            }
        }

        private void ConsumeMapElementDefinition()
        {
            Consume("line");
            int id = ConsumeInteger();

            switch (ConsumeIdentifier().ToUpper())
            {
            case "ADD":
                // Right now we only support adding a side to a line.
                Consume("side");
                int sideId = ConsumeInteger();
                m_mapDefinition.Lines.Add(new LineAddDefinition(id, sideId));
                break;
            
            case "DELETE":
                m_mapDefinition.Lines.Add(new LineDeleteDefinition(id));
                break;
            
            case "REMOVE":
                // Right now we only support removing the back side of a line.
                Consume("back");
                m_mapDefinition.Lines.Add(new LineRemoveSideDefinition(id));
                break;
            
            default:
                throw MakeException("Unknown map element type");
            }

            Consume(';');
        }

        private void ConsumeDefinition()
        {
            m_definition = new CompatibilityDefinition();

            InvokeUntilAndConsume('}', () =>
            {
                m_mapDefinition = new CompatibilityMapDefinition();
                
                Consume("map");
                m_mapName = ConsumeString();
                Consume('{');
                InvokeUntilAndConsume('}', ConsumeMapElementDefinition);

                if (m_definition.MapDefinitions.ContainsKey(m_mapName))
                    Log.Warn("Multiple compatibility map definitions for {0}", m_mapName);
                m_definition.MapDefinitions[m_mapName] = m_mapDefinition;
            });
        }

        private void ConsumeDefinitions()
        {
            HashSet<CIString> identifiers = new HashSet<CIString> { ConsumeString().ToUpper() };

            InvokeUntilAndConsume('{', () => identifiers.Add(ConsumeString().ToUpper()));
            ConsumeDefinition();

            AddDefinitionToIdentifier(identifiers);
        }
    }
}