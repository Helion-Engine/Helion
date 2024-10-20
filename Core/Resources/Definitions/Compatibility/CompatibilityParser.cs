using System;
using System.Collections.Generic;
using Helion.Resources.Definitions.Compatibility.Lines;
using Helion.Resources.Definitions.Compatibility.Sides;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using NLog;

namespace Helion.Resources.Definitions.Compatibility;

public class CompatibilityParser : ParserBase
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public readonly Dictionary<string, CompatibilityDefinition> Files = new(StringComparer.OrdinalIgnoreCase);
    public readonly Dictionary<string, CompatibilityDefinition> Hashes = new(StringComparer.OrdinalIgnoreCase);
    private CompatibilityDefinition m_definition = new CompatibilityDefinition();
    private CompatibilityMapDefinition m_mapDefinition = new CompatibilityMapDefinition();
    private string m_mapName = string.Empty;

    protected override void PerformParsing()
    {
        while (!Done)
            ConsumeDefinitions();
    }

    private void AddDefinitionToIdentifier(IEnumerable<string> identifiers)
    {
        foreach (string identifier in identifiers)
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
        else if (ConsumeIf("midtexturehacksector"))
        {
            m_mapDefinition.MidTextureHackSectors.Add(ConsumeInteger());
            Consume(';');
        }
        else if (ConsumeIf("norenderfloorsector"))
        {
            m_mapDefinition.NoRenderFloorSectors.Add(ConsumeInteger());
            Consume(';');
        }
        else if (ConsumeIf("norenderceiling"))
        {
            m_mapDefinition.NoRenderCeilingSectors.Add(ConsumeInteger());
            Consume(';');
        }
        else if (ConsumeIf("maxdistanceoverride"))
        {
            m_mapDefinition.MaxDistanceOverride = ConsumeInteger();
            Consume(';');
        }
        else if (ConsumeIf("maxdistanceovveridetag"))
        {
            m_mapDefinition.MaxDistanceOverrideTags.Add(ConsumeInteger());
            Consume(';');
        }
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
                setDefinition.BackSideId = ConsumeInteger();
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
        if (ConsumeIf("OFFSET"))
        {
            int x = ConsumeSignedInteger();
            Consume(',');
            int y = ConsumeSignedInteger();
            sideSetDefinition.Offset = (x, y);
        }

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
        HashSet<string> identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ConsumeString() };

        InvokeUntilAndConsume('{', () => identifiers.Add(ConsumeString()));
        ConsumeDefinition();

        AddDefinitionToIdentifier(identifiers);
    }
}
