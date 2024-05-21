using System;
using Helion.Render.OpenGL.Shared.World;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util;
using Helion.World;
using Helion.World.Geometry.Islands;
using Helion.World.Geometry.Sectors;
using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;

public class MidTextureHack
{
    private readonly HashSet<int> m_midTextureHackLines = new(128);
    private readonly HashSet<int> m_floodLineSet = new(128);
    private readonly HashSet<int> m_midTextureHackSectors = new(128);
    private readonly List<Sector> m_containingSectors = new(128);

    public void Apply(IWorld world, LegacyGLTextureManager textureManager, GeometryRenderer geometryRenderer)
    {
        for (int i = 0; i < world.Sectors.Count; i++)
            ApplyToSector(world, textureManager, geometryRenderer, world.Sectors[i]);

        m_floodLineSet.Clear();
        m_midTextureHackLines.Clear();
        m_midTextureHackSectors.Clear();
    }

    public void Apply(IWorld world, IList<int> sectorIds, LegacyGLTextureManager textureManager, GeometryRenderer geometryRenderer)
    {
        foreach (var sectorId in sectorIds)
        {
            if (world.IsSectorIdValid(sectorId))
                ApplyToSector(world, textureManager, geometryRenderer, world.Sectors[sectorId]);
        }

        m_floodLineSet.Clear();
        m_midTextureHackLines.Clear();
        m_midTextureHackSectors.Clear();
    }

    private void ApplyToSector(IWorld world, LegacyGLTextureManager textureManager, GeometryRenderer geometryRenderer, Sector sector)
    {
        if (sector.TransferHeights != null)
            return;

        bool clippedFloor = false;
        bool clippedCeiling = false;

        for (int i = 0; i < sector.Lines.Count; i++)
        {
            var line = sector.Lines[i];
            if (m_midTextureHackLines.Contains(line.Id))
                continue;

            if (line.Back == null)
                continue;

            if (line.Front.Middle.TextureHandle == Constants.NoTextureIndex && line.Front.Middle.TextureHandle == Constants.NoTextureIndex)
                continue;

            var side = line.Front.Middle.TextureHandle == Constants.NoTextureIndex ? line.Back : line.Front;
            var texture = textureManager.GetTexture(side.Middle.TextureHandle);
            // Do not emulate unless there are transparent pixels or alpha.
            if (texture.TransparentPixelCount == 0 && line.Alpha >= 1)
                continue;

            m_midTextureHackLines.Add(line.Id);
            (double bottomZ, double topZ) = GeometryRenderer.FindOpeningFlats(side.Sector, side.PartnerSide!.Sector);

            WallVertices wall = default;
            WorldTriangulator.HandleTwoSidedMiddle(side,
                texture.Dimension, texture.UVInverse, bottomZ, topZ, bottomZ, topZ, side.Id == line.Front.Id, ref wall, out _, 0, 0);

            if (wall.BottomRight.Z < sector.Floor.Z && line.Front.Sector.Floor.Z == line.Back.Sector.Floor.Z)
                clippedFloor = true;

            if (wall.TopLeft.Z > sector.Ceiling.Z && line.Front.Sector.Ceiling.Z == line.Back.Sector.Ceiling.Z)
                clippedCeiling = true;
        }

        if (clippedFloor)
            SetSectorLinesForMidTextureHack(world, geometryRenderer, sector, SectorPlaneFace.Floor);

        if (clippedCeiling)
            SetSectorLinesForMidTextureHack(world, geometryRenderer, sector, SectorPlaneFace.Ceiling);

        if (clippedFloor || clippedCeiling)
            m_midTextureHackSectors.Add(sector.Id);

        SetSectorForMidTextureHack(sector, clippedFloor, clippedCeiling);
        FindContainingFloodSectors(world, sector, m_containingSectors, clippedFloor, clippedCeiling);

        foreach (var containingsector in m_containingSectors)
            SetSectorForMidTextureHack(containingsector, clippedFloor, clippedCeiling);

        m_containingSectors.Clear();
        m_midTextureHackLines.Clear();
    }

    private static void SetSectorForMidTextureHack(Sector sector, bool clippedFloor, bool clippedCeiling)
    {
        sector.Floor.MidTextureHack = sector.Floor.MidTextureHack || clippedFloor;
        sector.Ceiling.MidTextureHack = sector.Ceiling.MidTextureHack || clippedCeiling;
        sector.Floor.NoRender = sector.Floor.NoRender || clippedFloor;
        sector.Ceiling.NoRender = sector.Ceiling.NoRender || clippedCeiling;
    }

    private void FindContainingFloodSectors(IWorld world, Sector sector, List<Sector> sectors, bool floor, bool ceiling)
    {
        if (sector.Id >= world.Geometry.IslandGeometry.SectorIslands.Length)
            return;

        var searchIslands = world.Geometry.IslandGeometry.SectorIslands[sector.Id];
        for (int sectorId = 0; sectorId < world.Geometry.IslandGeometry.SectorIslands.Length; sectorId++)
        {
            if (sectorId == sector.Id)
                continue;

            if (!world.IsSectorIdValid(sectorId))
                continue;

            // Only flood matching planes
            var checkSector = world.Sectors[sectorId];
            if (floor && (checkSector.Floor.Z != sector.Floor.Z || checkSector.Floor.TextureHandle != sector.Floor.TextureHandle))
                continue;

            if (ceiling && (checkSector.Ceiling.Z != sector.Ceiling.Z || checkSector.Ceiling.TextureHandle != sector.Ceiling.TextureHandle))
                continue;

            var checkIslands = world.Geometry.IslandGeometry.SectorIslands[sectorId];
            if (AnyBoxContains(searchIslands, checkIslands))
                sectors.Add(checkSector);
        }
    }

    private static bool AnyBoxContains(IList<Island> search, IList<Island> check)
    {
        foreach (var searchIsland in search)
        {
            foreach (var checkIsland in check)
            {
                if (searchIsland.Contains(checkIsland.Box))
                    return true;
            }
        }

        return false;
    }

    private void SetSectorLinesForMidTextureHack(IWorld world, GeometryRenderer geometryRenderer, Sector sector, SectorPlaneFace face)
    {
        for (int i = 0; i < sector.Lines.Count; i++)
        {
            var line = sector.Lines[i];
            if (line.Back != null && ReferenceEquals(line.Front.Sector, line.Back.Sector))
                continue;

            var plane = line.Front.Sector.GetSectorPlane(face);
            if (plane.NoRender || world.ArchiveCollection.TextureManager.IsSkyTexture(sector.GetSectorPlane(face).TextureHandle))
                continue;

            if (m_floodLineSet.Contains(line.Id))
                continue;

            if (m_midTextureHackSectors.Contains(line.Front.Sector.Id) || line.Back != null && m_midTextureHackSectors.Contains(line.Back.Sector.Id))
                continue;

            var facingSide = line.Back == null || ReferenceEquals(line.Front.Sector, sector) ? line.Front : line.Back;
            if (line.Back != null && face == SectorPlaneFace.Floor && facingSide.Lower.TextureHandle == Constants.NoTextureIndex)
                continue;

            m_floodLineSet.Add(line.Id);
            facingSide.MidTextureFlood |= face switch
            {
                SectorPlaneFace.Floor => SectorPlanes.Floor,
                SectorPlaneFace.Ceiling => SectorPlanes.Ceiling,
            };
        }
    }
}
