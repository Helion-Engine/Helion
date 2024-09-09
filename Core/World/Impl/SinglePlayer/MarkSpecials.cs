﻿using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Render.OpenGL.Renderers.Legacy.World.Primitives;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Islands;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Walls;
using Helion.World.Special;
using Helion.World.Special.Switches;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helion.World.Impl.SinglePlayer;

public class MarkSpecials
{
    private static readonly Vec3F[] TracerColors =  [new(1f, 0.2f, 0.2f), new(0.2f, 1f, 0.2f), new(0.2f, 0.2f, 1f), new(0.8f, 0.2f, 0.8f), new(0.8f, 0.8f, 0.8f)];
    private static readonly Color[] AutomapColors = [Color.Red, Color.Green, Color.Blue, Color.Purple, Color.Yellow];

    public readonly DynamicArray<Sector> MarkedSectors = new();
    public readonly DynamicArray<Line> MarkedLines = new();
    private readonly DynamicArray<int> m_playerTracers = new();
    private readonly Dictionary<int, List<Line>> m_tagToLines = [];
    private readonly HashSet<int> m_searchedSectors = [];
    private bool m_mappedLineTags;
    private int m_lastLineId = -1;
    private int m_lastGametick = -1;
    private int m_ignoreGametick = -1;
    private int m_lineMarkColor;

    public void Clear(IWorld world, Player player)
    {
        ClearMarkedSectors();
        ClearMarkedLines(world);
        ClearPlayerTracers(player);
        m_lastLineId = -1;
    }

    public void Mark(IWorld world, Entity entity, Line line, int gametick)
    {
        if (!world.Config.Game.MarkSpecials || entity.PlayerObj == null || entity.PlayerObj.IsVooDooDoll)
            return;

        if (line.Id == m_lastLineId || gametick == m_ignoreGametick)
        {
            m_ignoreGametick = gametick;
            return;
        }

        bool newGametick = false;
        if (gametick != m_lastGametick)
        {
            newGametick = true;
            m_lastLineId = line.Id;
            m_lineMarkColor = -1;
            Clear(world, entity.PlayerObj);
        }

        int markedSectors = MarkedSectors.Length;
        int markedLines = MarkedLines.Length;

        MarkSpecialLines(world, line);
        Mark(world, entity, line, true);

        if (MarkedLines.Length != markedLines || MarkedSectors.Length != markedLines)
        {
            if (newGametick)
            {
                m_lastLineId = line.Id;
                m_lastGametick = gametick;
            }
            MarkedLines.Add(line);
            UpdateLineMark(world, line, true);
            return;
        }
    }

    public void Mark(IWorld world, Entity entity, Line line, bool traverseIsland)
    {
        if (!world.Config.Game.MarkSpecials || entity.PlayerObj == null || entity.PlayerObj.IsVooDooDoll)
            return;

        var player = entity.PlayerObj;
        if (!IgnoreLineSpecial(line))
        {
            var sectors = world.SpecialManager.GetSectorsFromSpecialLine(line);
            for (int i = 0; i < sectors.Count; i++)
            {
                var sector = sectors.GetSector(i);
                sector.MarkAutomap = true;
                MarkedSectors.Add(sector);
                if (!SectorHasLine(sector, line))
                {
                    if (traverseIsland && sector.Island != player.Sector.Island && sector.Island.IsVooDooCloset)
                        TraverseIslandSpecialLines(world, entity, sector.Island);
                    ConnectLineToSector(world, player, line, sector);
                }

                if (world.Config.Developer.LogMarkSpecials)
                    world.DisplayMessage($"Line {line.Id} activates sector: {sector.Id} - {GetLineSpecialDescription(line)}");
            }
        }

        if (MarkedLines.Length == 0 || !traverseIsland)
            return;
        
        Sector? markSector = null;
        if (line.Front.Sector.Tag != 0)
            markSector = line.Front.Sector;
        else if (line.Back != null && line.Back.Sector.Tag != 0)
            markSector = line.Back.Sector;

        if (markSector == null)
            return;
        
        for (int i = 0; i < MarkedLines.Length; i++)
        {
            var markLine = MarkedLines[i];
            markSector.MarkAutomap = true;
            MarkedSectors.Add(markSector);
            if (!SectorHasLine(markSector, markLine))
                ConnectLineToSector(world, player, markLine, markSector);

            if (traverseIsland)
            {
                var lineSectorFront = markLine.Front.Sector;
                if (markLine.HasSectorTag && lineSectorFront.Island != player.Sector.Island && lineSectorFront.Island.IsVooDooCloset)
                    TraverseIslandSpecialSectors(world, entity, lineSectorFront.Island);
            }


            if (world.Config.Developer.LogMarkSpecials)
                world.DisplayMessage($"Sector {markSector.Id} activated by line: {markLine.Id} - {GetLineSpecialDescription(markLine)}");
        }
    }

    public void FindKeys(IWorld world, List<object> keys)
    {
        var node = world.EntityManager.Head;
        while (node != null)
        {
            if (node.Definition.IsType(Inventory.KeyClassName))
                keys.Add(node);
            node = node.Next;
        }
    }

    public void FindKeyLines(IWorld world, List<object> lines)
    {
        for (int i = 0; i < world.Lines.Count; i++)
        {
            var line = world.Lines[i];
            if (line.Special == null)
                continue;

            if (LockSpecialUtil.IsLockSpecial(line, out _))
                lines.Add(line);
        }
    }

    public void FindExits(IWorld world, List<object> items)
    {
        for (int i = 0; i < world.Lines.Count; i++)
        {
            var line = world.Lines[i];
            if (line.Special.IsExitSpecial())
                items.Add(line);
        }

        for (int i = 0; i < world.Sectors.Count; i++)
        {
            var sec = world.Sectors[i];
            if (sec.SectorSpecialType == ZDoomSectorSpecialType.DamageEnd || (sec.KillEffect & InstantKillEffect.KillAllPlayersExit) != 0 || (sec.KillEffect & InstantKillEffect.KillAllPlayersSecretExit) != 0)
                items.Add(sec);
        }
    }

    private static bool IgnoreLineSpecial(Line line) =>
        !line.HasSpecial || (line.Flags.Activations & LineActivations.LevelStart) != 0;

    private void TraverseIslandSpecialSectors(IWorld world, Entity entity, Island island)
    {
        var sectors = world.Sectors;
        m_searchedSectors.Clear();
        for (int i = 0; i < island.Subsectors.Count; i++)
        {
            var subsector = island.Subsectors[i];
            if (!subsector.SectorId.HasValue || m_searchedSectors.Contains(subsector.SectorId.Value))
                continue;
            m_searchedSectors.Add(subsector.SectorId.Value);

            var sector = sectors[subsector.SectorId.Value];
            if (sector.Tag == 0 || !m_tagToLines.TryGetValue(sector.Tag, out var lines))
                continue;

            for (int j = 0; j < lines.Count; j++)
                Mark(world, entity, lines[j], false);
        }
    }

    private void TraverseIslandSpecialLines(IWorld world, Entity entity, Island island)
    {
        for (int i = 0; i < island.LineIds.Count; i++)
        {
            var line = world.Lines[island.LineIds[i]];
            if (IgnoreLineSpecial(line))
                continue;

            Mark(world, entity, line, false);
        }
    }

    private void ConnectLineToSector(IWorld world, Player player, Line line, Sector sector)
    {
        if (sector.Id < 0 || sector.Id >= world.Geometry.IslandGeometry.SectorIslands.Length)
            return;

        var islands = world.Geometry.IslandGeometry.SectorIslands[sector.Id];
        for (int i = 0; i < islands.Count; i++)
        {
            var island = islands[i];
            m_lineMarkColor = ++m_lineMarkColor % TracerColors.Length;
            Vec3D start = GetActivatedLinePoint(world, line);
            var box = island.Box;
            Vec3D end = new((box.Min.X + box.Max.X) / 2, (box.Min.Y + box.Max.Y) / 2, Math.Min(sector.Floor.Z + 8, sector.Ceiling.Z));

            // Check if the point is in the sector. The center point of the bounding box may not be in the center if it's a complex concave polygon.
            if (world.BspTree.ToSector(end).Id != sector.Id)
            {
                var endLine = GetClosestLineCenterFrom(world, island, player.Position.XY);
                if (endLine != null)
                {
                    end = endLine.Segment.FromTime(0.5).To3D(Math.Min(sector.Floor.Z + 8, sector.Ceiling.Z));
                    end += (Vec2D.UnitCircle(start.Angle(end)) * 16).To3D(0);
                }
            }

            m_playerTracers.Add(player.Tracers.AddTracer(PrimitiveRenderType.Line, (start, end), world.Gametick, TracerColors[m_lineMarkColor],
                ticks: 0, automapColor: AutomapColors[m_lineMarkColor]));
        }
    }

    public static Line? GetClosestLineCenterFrom(IWorld world, Island island, in Vec2D point)
    {
        double minDist = double.MaxValue;
        Line? minLine = null;

        for (int i = 0; i < island.LineIds.Count; i++)
        {
            var lineId = island.LineIds[i];
            if (!world.IsLineIdValid(lineId))
                continue;

            var line = world.Lines[lineId];
            double dist = line.Segment.FromTime(0.5).Distance(point);
            if (dist < minDist)
            {
                minDist = dist;
                minLine = line;
            }
        }

        if (minLine != null)
            return minLine;

        return null;
    }

    private static bool SectorHasLine(Sector sector, Line line)
    {
        for (int i = 0; i < sector.Lines.Count; i++)
        {
            var sectorLine = sector.Lines[i];
            if (sectorLine.Id == line.Id)
                return true;
        }

        return false;
    }

    private void ClearPlayerTracers(Player player)
    {
        for (int i = 0; i < m_playerTracers.Length; i++)
            player.Tracers.RemoveTracer(m_playerTracers[i]);
        m_playerTracers.Clear();
    }

    private static Vec3D GetActivatedLinePoint(IWorld world, Line line)
    {
        var lineCenter = line.Segment.FromTime(0.5);
        var lineAngle = line.Segment.Start.Angle(line.Segment.End);
        lineCenter = lineCenter + Vec2D.UnitCircle(lineAngle - MathHelper.HalfPi) * 8;

        if (line.Back == null)
            return lineCenter.To3D((line.Front.Sector.Floor.Z + line.Front.Sector.Ceiling.Z) / 2);

        if (SwitchManager.IsLineSwitch(world.ArchiveCollection, line))
        {
            var location = SwitchManager.GetLineLineSwitchTexture(world.ArchiveCollection, line, false);
            switch (location.Item2)
            {
                case WallLocation.Upper:
                    return lineCenter.To3D((line.Back.Sector.Ceiling.Z + line.Front.Sector.Ceiling.Z) / 2);
                case WallLocation.Middle:
                    return lineCenter.To3D((line.Back.Sector.Floor.Z + line.Back.Sector.Ceiling.Z) / 2);
                case WallLocation.Lower:
                    return lineCenter.To3D((line.Front.Sector.Floor.Z + line.Back.Sector.Floor.Z) / 2);
            }
        }

        return lineCenter.To3D(Math.Min(line.Front.Sector.Floor.Z + 8, line.Front.Sector.Ceiling.Z));
    }

    private static string GetLineSpecialDescription(Line line) =>
        $"[{(int)line.Special.LineSpecialType}]{line.Special.LineSpecialType} - {GetArgs(line)} - {GetActivations(line)} - Activated[{GetIntBool(line.Activated)}] Repeat[{GetIntBool(line.Flags.Repeat)}]";

    private static object GetArgs(Line line) =>
        $"{line.Args.Arg0},{line.Args.Arg1},{line.Args.Arg2},{line.Args.Arg3},{line.Args.Arg4}";

    private static int GetIntBool(bool b) => b ? 1 : 0;

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

        if (!m_mappedLineTags)
        {
            MarkSpecialLines(world, world.Lines, frontTag, backTag);
            m_mappedLineTags = true;
            return;
        }

        if (frontTag != 0 && m_tagToLines.TryGetValue(frontTag, out var lines))
            MarkSpecialLines(world, lines, frontTag, backTag);
        if (backTag != 0 && m_tagToLines.TryGetValue(backTag, out lines))
            MarkSpecialLines(world, lines, frontTag, backTag);
    }

    private void MarkSpecialLines(IWorld world, IList<Line> lines, int frontTag, int backTag)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            Line line = lines[i];
            if (IgnoreLineSpecial(line))
                continue;

            if (line.SectorTag == 0)
                continue;

            MapLineTag(line);

            if (line.SectorTag != frontTag && line.SectorTag != backTag)
                continue;

            UpdateLineMark(world, line, true);
            MarkedLines.Add(line);
        }
    }

    private void MapLineTag(Line line)
    {
        if (m_mappedLineTags || !line.HasSectorTag)
            return;

        if (!m_tagToLines.TryGetValue(line.SectorTag, out var lines))
        {
            lines = new();
            m_tagToLines[line.SectorTag] = lines;
        }

        lines.Add(line);
    }

    private void ClearMarkedLines(IWorld world)
    {
        for (int i = 0; i < MarkedLines.Length; i++)
            UpdateLineMark(world, MarkedLines[i], false);
        MarkedLines.Clear();
    }

    private void UpdateLineMark(IWorld world, Line line, bool set)
    {
        ref var structLine = ref world.StructLines.Data[line.Id];
        if (set)
            structLine.Flags |= StructLineFlags.MarkAutomap;
        else
            structLine.Flags &= ~StructLineFlags.MarkAutomap;
    }

    private void ClearMarkedSectors()
    {
        for (int i = 0; i < MarkedSectors.Length; i++)
            MarkedSectors[i].MarkAutomap = false;
        MarkedSectors.Clear();
    }
}
