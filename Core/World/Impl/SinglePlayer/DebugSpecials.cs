using Helion.Maps.Specials;
using Helion.Util.Configs.Impl;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Special;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helion.World.Impl.SinglePlayer;

public class DebugSpecials
{
    private readonly DynamicArray<Sector> m_developerMarkedSectors = new();
    private readonly DynamicArray<Line> m_developerMarkedLines = new();
    private int m_developerMarkedLineId = -1;

    public void MarkSpecials(IWorld world, Entity entity, Line line)
    {
        if (!world.Config.Developer.DebugSpecials || entity.PlayerObj == null || entity.PlayerObj.IsVooDooDoll)
            return;

        if (line.Id == m_developerMarkedLineId)
            return;

        ClearDeveloperMarkedSectors();
        ClearDeveloperMarkedLines();
        MarkSpecialLines(world, line);

        if (line.HasSpecial)
        {
            var sectors = world.SpecialManager.GetSectorsFromSpecialLine(line);
            foreach (Sector sector in sectors)
            {
                sector.MarkAutomap = true;
                m_developerMarkedSectors.Add(sector);
                world.DisplayMessage($"Line {line.Id} activates sector: {sector.Id} - {GetActivations(line)}");
            }
        }

        if (m_developerMarkedLines.Length > 0)
        {
            Sector? markSector = null;
            if (line.Front.Sector.Tag != 0)
                markSector = line.Front.Sector;
            else if (line.Back != null & line.Back.Sector.Tag != 0)
                markSector = line.Back.Sector;

            if (markSector != null)
            {
                for (int i = 0; i < m_developerMarkedLines.Length; i++)
                {
                    var markLine = m_developerMarkedLines[i];
                    markSector.MarkAutomap = true;
                    m_developerMarkedSectors.Add(markSector);
                    world.DisplayMessage($"Sector {markSector.Id} activated by line: {markLine.Id} - {GetActivations(markLine)}");
                }
            }
        }

        if (m_developerMarkedLines.Length > 0 || m_developerMarkedSectors.Length > 0)
        {
            m_developerMarkedLineId = line.Id;
            m_developerMarkedLines.Add(line);
            line.MarkAutomap = true;
        }
    }

    private static string GetActivations(Line line)
    {
        StringBuilder sb = new();
        for (int i = 0; i < 32; i++)
        {
            int flag = 1 << i;
            if (((int)line.Flags.Activations & flag) != 0)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append((LineActivations)flag);
            }
        }
        return sb.ToString();
    }

    private void MarkSpecialLines(IWorld world, Line sourceLine)
    {
        int frontTag = sourceLine.Front.Sector.Tag;
        int backTag = sourceLine.Back == null ? 0 : sourceLine.Back.Sector.Tag;

        for (int i = 0; i < world.Lines.Count; i++)
        {
            Line line = world.Lines[i];
            if (!line.HasSpecial)
                continue;

            if (line.SectorTag == 0)
                continue;

            if (line.SectorTag != frontTag && line.SectorTag != backTag)
                continue;

            line.MarkAutomap = true;
            m_developerMarkedLines.Add(line);
        }
    }

    private void ClearDeveloperMarkedLines()
    {
        for (int i = 0; i < m_developerMarkedLines.Length; i++)
            m_developerMarkedLines[i].MarkAutomap = false;
        m_developerMarkedLines.Clear();
    }
    private void ClearDeveloperMarkedSectors()
    {
        for (int i = 0; i < m_developerMarkedSectors.Length; i++)
            m_developerMarkedSectors[i].MarkAutomap = false;
        m_developerMarkedSectors.Clear();
    }
}
