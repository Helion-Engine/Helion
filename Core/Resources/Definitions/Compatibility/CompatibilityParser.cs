using System.Collections.Generic;
using Helion.Resources.Definitions.Compatibility.Lines;
using Helion.Resources.Definitions.Compatibility.Sides;
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
            if (ConsumeIf("LINE"))
                ConsumeLineMapElement();
            else if (ConsumeIf("SIDE"))
                ConsumeSideMapElement();
        }

        private void ConsumeLineMapElement()
        {
            int id = ConsumeInteger();

            switch (ConsumeIdentifier().ToUpper())
            {
            case "DELETE":
                m_mapDefinition.Lines.Add(new LineDeleteDefinition(id));
                break;
            case "SET":
                ConsumeSetDefinition(id);
                break;
            case "SPLIT":
                ConsumeSplitDefinition(id);
                break;
            default:
                throw MakeException("Unknown line map element type");
            }

            Consume(';');
        }

        private void ConsumeSetDefinition(int id)
        {
            LineSetDefinition setDefinition = new LineSetDefinition(id);

            if (ConsumeIf("FLIP"))
                setDefinition.Flip = true;
            if (ConsumeIf("START"))
                setDefinition.StartVertexId = ConsumeInteger();
            if (ConsumeIf("END"))
                setDefinition.EndVertexId = ConsumeInteger();
            if (ConsumeIf("FRONT"))
                setDefinition.FrontSideId = ConsumeInteger();
            if (ConsumeIf("BACK"))
            {
                if (ConsumeIf("NONE"))
                    setDefinition.RemoveBack = true;
                else
                    setDefinition.StartVertexId = ConsumeInteger();
            }
            
            m_mapDefinition.Lines.Add(setDefinition);
        }

        private void ConsumeSplitDefinition(int id)
        {
            int startId = ConsumeInteger();
            int endId = ConsumeInteger();
            Consume("VERTEX");
            int vertexId = ConsumeInteger();
            
            m_mapDefinition.Lines.Add(new LineSplitDefinition(id, startId, endId, vertexId));
        }
        
        private void ConsumeSideMapElement()
        {
            int id = ConsumeInteger();

            switch (ConsumeIdentifier().ToUpper())
            {
            case "SET":
                ConsumeSideSetDefinition(id);
                break;
            default:
                throw MakeException("Unknown side map element type");
            }
            
            Consume(';');
        }

        private void ConsumeSideSetDefinition(int sideId)
        {
            SideSetDefinition sideSetDefinition = new SideSetDefinition(sideId);
            
            if (ConsumeIf("LOWER"))
                sideSetDefinition.Lower = ConsumeString();
            if (ConsumeIf("MIDDLE"))
                sideSetDefinition.Middle = ConsumeString();
            if (ConsumeIf("UPPER"))
                sideSetDefinition.Upper = ConsumeString();
            
            m_mapDefinition.Sides.Add(sideSetDefinition);
        }
        
        private void ConsumeDefinition()
        {
            m_definition = new CompatibilityDefinition();

            InvokeUntilAndConsume('}', () =>
            {
                m_mapDefinition = new CompatibilityMapDefinition();
                
                Consume("MAP");
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