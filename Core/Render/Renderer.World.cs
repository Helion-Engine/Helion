using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.World.Geometry.Sectors;
using Helion.World;
using OpenTK.Graphics.OpenGL;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.Graphics.Palettes;
using Helion.Geometry.Vectors;
using static Helion.Util.Constants;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using System;
using Helion.World.Geometry.Lines;

namespace Helion.Render;

public partial class Renderer
{
    private readonly SectorUpdates m_updateLightSectors = new();
    private readonly SectorUpdates m_updateColorMapSectors = new();
    private readonly SectorUpdates m_updateLineHeights = new();

    private GLBufferTextureStorage? m_lightBufferStorage;
    private GLBufferTextureStorage? m_sectorColorMapsBuffer;
    private GLBufferTextureStorage? m_colorMapBuffer;
    private GLBufferTextureStorage? m_mapDataBuffer;
    private GLBufferTextureStorage? m_lineHeightsBuffer;

    private float[] m_lightBufferData = [];
    private float[] m_mapBufferData = [];
    private float[] m_lineHeightsBufferData = [];

    public static int GetLightBufferIndex(Side side, Wall wall, Sector sector)
    {
        // The shader will add the light level at this index plus the vertex light level.
        // Return LightBuffer.DarkIndex (lightlevel=0) to not add the sectors light level if absolute.
        if (side.Flags.LightLevelAbsolute || wall.LightLevelAbsolute)
            return LightBuffer.DarkIndex;

        return GetLightBufferIndex(sector, LightBufferType.Wall);
    }

    public static int GetLightBufferIndex(Sector sector, SectorPlaneFace planeType, LightBufferType type)
    {
        var transferLightSector = planeType == SectorPlaneFace.Floor ? sector.TransferFloorLightSector : sector.TransferCeilingLightSector;
        var plane = transferLightSector.GetSectorPlane(planeType);

        if (plane.LightLevelAbsolute)
            return LightBuffer.DarkIndex;

        return GetLightBufferIndex(sector, type);
    }

    public static int GetLightBufferIndex(Sector sector, LightBufferType type)
    {
        return type switch
        {
            LightBufferType.Floor => sector.TransferFloorLightSector.Id * LightBuffer.BufferSize + LightBuffer.SectorIndexStart + LightBuffer.FloorOffset,
            LightBufferType.Ceiling => sector.TransferCeilingLightSector.Id * LightBuffer.BufferSize + LightBuffer.SectorIndexStart + LightBuffer.CeilingOffset,
            LightBufferType.Wall => sector.Id * LightBuffer.BufferSize + LightBuffer.SectorIndexStart + LightBuffer.WallOffset,
            _ => sector.Id * LightBuffer.BufferSize + LightBuffer.SectorIndexStart,
        };
    }

    public static int GetColorMapBufferIndex(Sector sector, LightBufferType type)
    {
        return type switch
        {
            LightBufferType.Floor => (sector.TransferFloorLightSector.Id + 1) * LightBuffer.BufferSize + LightBuffer.FloorOffset,
            LightBufferType.Ceiling => (sector.TransferCeilingLightSector.Id + 1) * LightBuffer.BufferSize + LightBuffer.CeilingOffset,
            LightBufferType.Wall => (sector.Id + 1) * LightBuffer.BufferSize + LightBuffer.WallOffset,
            _ => sector.Id + 1,
        };
    }

    public void UpdateToNewWorld(IWorld world)
    {
        m_updateLightSectors.ClearAndReset();
        m_updateColorMapSectors.ClearAndReset();
        m_updateLineHeights.ClearAndReset();
        m_updateLightSectors.EnsureCapacity(world.Sectors.Count);
        m_updateColorMapSectors.EnsureCapacity(world.Sectors.Count);
        m_updateLineHeights.EnsureCapacity(world.Sectors.Count);

        m_worldRenderer.UpdateToNewWorld(world);
        m_automapRenderer.UpdateTo(world);

        if (m_world != null)
        {
            m_world.SectorLightChanged -= World_SectorLightChanged;
            m_world.SectorColorMapChanged -= World_SectorColorMapChanged;
            m_world.SectorMove -= World_SectorMove;
        }

        m_world = world;
        m_world.SectorLightChanged += World_SectorLightChanged;
        m_world.SectorColorMapChanged += World_SectorColorMapChanged;
        m_world.SectorMove += World_SectorMove;

        var alloc = !m_world.SameAsPreviousMap;
        SetMapDataBuffer(world, alloc);
        SetLightDataBuffer(world, alloc);
        SetSectorColorMapsBuffer(world, alloc);
        SetLineHeights(world, alloc);
    }

    private unsafe void SetLightDataBuffer(IWorld world, bool alloc)
    {
        if (alloc || m_lightBufferStorage == null)
        {
            m_lightBufferStorage?.Dispose();
            m_lightBufferData = new float[world.Sectors.Count * LightBuffer.BufferSize + LightBuffer.SectorIndexStart];
            m_lightBufferStorage = new("Sector lights texture buffer", m_lightBufferData, SizedInternalFormat.R32f, GLInfo.MapPersistentBitSupported);

            m_lightBufferStorage.Map(data =>
            {
                float* lightBuffer = (float*)data.ToPointer();
                SetLightBuffer(world, lightBuffer);
            });
        }
        else
        {
            var lightBuffer = m_lightBufferStorage.GetMappedBufferAndBind();
            SetLightBuffer(world, lightBuffer.MappedMemoryPtr);
        }
    }

    private unsafe void SetLightBuffer(IWorld world, float* lightBuffer)
    {        
        lightBuffer[LightBuffer.DarkIndex] = 0;
        lightBuffer[LightBuffer.FullBrightIndex] = 255;

        for (int i = 0; i < LightBuffer.ColorMapCount; i++)
            lightBuffer[LightBuffer.ColorMapStartIndex + i] =
                256 - ((LightBuffer.ColorMapCount - i) * 256 / LightBuffer.ColorMapCount);

        for (int i = 0; i < world.Sectors.Count; i++)
        {
            Sector sector = world.Sectors[i];
            int index = sector.Id * LightBuffer.BufferSize + LightBuffer.SectorIndexStart;
            lightBuffer[index + LightBuffer.FloorOffset] = sector.LightLevel;
            lightBuffer[index + LightBuffer.CeilingOffset] = sector.LightLevel;
            lightBuffer[index + LightBuffer.WallOffset] = sector.LightLevel;
        }
    }

    private unsafe void SetLineHeights(IWorld world, bool alloc)
    {
        if (!world.Config.Render.VanillaRender)
        {
            m_lineHeightsBufferData = [];
            m_lineHeightsBuffer?.Dispose();
            m_lineHeightsBuffer = null;
            return;
        }

        if (alloc || m_lineHeightsBuffer == null)
        {
            m_lineHeightsBuffer?.Dispose();
            m_lineHeightsBufferData = new float[world.Lines.Count * 2];
            m_lineHeightsBuffer = new("Line heights data buffer", m_lineHeightsBufferData, SizedInternalFormat.Rg32f, GLInfo.MapPersistentBitSupported);

            m_lineHeightsBuffer.Map(data =>
            {
                float* buffer = (float*)data.ToPointer();
                for (int i = 0; i < world.StructLines.Length; i++)
                {
                    ref var line = ref world.StructLines.Data[i];
                    SetLineHeightBuffer(buffer, i, ref line);
                }
            });
        }
        else
        {
            var mappedBuffer = m_lineHeightsBuffer.GetMappedBufferAndBind();
            var lineArray = world.StructLines.Data;
            float* buffer = mappedBuffer.MappedMemoryPtr;
            for (int i = 0; i < world.StructLines.Length; i++)
            {
                ref var line = ref world.StructLines.Data[i];
                SetLineHeightBuffer(buffer, i, ref line);
            }
        }
    }

    private unsafe void SetSectorColorMapsBuffer(IWorld world, bool alloc)
    {
        bool usePalette = ShaderVars.PaletteColorMode;
        if (alloc || m_sectorColorMapsBuffer == null)
        {
            // First index will always map to default colormap
            int sectorBufferCount = (world.Sectors.Count + 1) * LightBuffer.BufferSize;
            // PaletteColorMode is index to colormap, true color will be RGB mix
            int size = usePalette ? 1 : 3;
            var sectorBuffer = new float[sectorBufferCount * size];

            m_sectorColorMapsBuffer?.Dispose();
            m_sectorColorMapsBuffer = new("Sector colormaps", sectorBuffer, usePalette ? SizedInternalFormat.R32f : SizedInternalFormat.Rgb32f, GLInfo.MapPersistentBitSupported);
        }

        if (alloc)
        {
            m_sectorColorMapsBuffer.Map(data =>
            {
                float* colorMapBuffer = (float*)data.ToPointer();
                InitSectorColorMap(world, colorMapBuffer, usePalette);
            });
        }
        else
        {
            var mappedBuffer = m_sectorColorMapsBuffer.GetMappedBufferAndBind();
            float* colorMapBuffer = mappedBuffer.MappedMemoryPtr;
            InitSectorColorMap(world, colorMapBuffer, usePalette);
        }
    }

    private static unsafe void InitSectorColorMap(IWorld world, float* colorMapBuffer, bool usePalette)
    {
        if (!usePalette)
        {
            Vec3F* color = (Vec3F*)&colorMapBuffer[0];
            *color = Vec3F.One;
        }

        for (int i = 0; i < world.Sectors.Count; i++)
        {
            var sector = world.Sectors[i];
            SetSectorColorMap(colorMapBuffer, sector, sector.Colormap);
        }
    }

    private static unsafe void SetSectorColorMap(float* colorMapBuffer, Sector sector, Colormap? colormap)
    {
        int index = (sector.Id + 1) * LightBuffer.BufferSize;
        if (ShaderVars.PaletteColorMode)
        {
            int colorMapIndex = colormap == null ? 0 : colormap.Index;
            colorMapBuffer[index + LightBuffer.FloorOffset] = colorMapIndex;
            colorMapBuffer[index + LightBuffer.CeilingOffset] = colorMapIndex;
            colorMapBuffer[index + LightBuffer.WallOffset] = colorMapIndex;
            return;
        }

        const int VectorSize = 3;
        Vec3F setColor = colormap == null ? Vec3F.One : colormap.ColorMix;
        *(Vec3F*)&colorMapBuffer[(index + LightBuffer.FloorOffset) * VectorSize] = setColor;
        *(Vec3F*)&colorMapBuffer[(index + LightBuffer.CeilingOffset) * VectorSize] = setColor;
        *(Vec3F*)&colorMapBuffer[(index + LightBuffer.WallOffset) * VectorSize] = setColor;
    }

    public unsafe void SetMapDataBuffer(IWorld world, bool alloc)
    {
        if (!alloc)
            return;

        m_mapDataBuffer?.Dispose();
        m_mapBufferData = new float[world.Lines.Count * 6];
        m_mapDataBuffer = new("Map data buffer", m_mapBufferData, SizedInternalFormat.Rgba32f, false);

        m_mapDataBuffer.Map(data =>
        {
            float* buffer = (float*)data.ToPointer();
            for (int i = 0; i < world.StructLines.Length; i++)
            {
                ref var line = ref world.StructLines.Data[i];
                int index = i * 4;
                buffer[index] = (float)line.Segment.Start.X;
                buffer[index + 1] = (float)line.Segment.Start.Y;
                buffer[index + 2] = (float)line.Segment.End.X;
                buffer[index + 3] = (float)line.Segment.End.Y;
            }
        });
    }

    private static unsafe void SetLineHeightBuffer(float* buffer, int index, ref StructLine line)
    {
        var prevFloorZ = (float)line.FrontFloorPlane.PrevZ;
        var floorZ = (float)line.FrontFloorPlane.Z;
        if (line.BackFloorPlane != null)
        {
            prevFloorZ = Math.Max(prevFloorZ, (float)line.BackFloorPlane.PrevZ);
            floorZ = Math.Max(floorZ, (float)line.BackFloorPlane.Z);
        }

        buffer[index] = prevFloorZ;
        buffer[index] = floorZ;
    }

    private void World_SectorLightChanged(object? sender, Sector sector)
    {
        m_updateLightSectors.Add(sector);
    }

    private void World_SectorColorMapChanged(object? sender, Sector sector)
    {
        m_updateColorMapSectors.Add(sector);
    }

    private void World_SectorMove(object? sender, SectorPlane e)
    {
        m_updateLineHeights.Add(e.Sector);
    }

    private void UpdateBuffers()
    {
        UpdateLights();
        UpdateColorMaps();
        UpdateLineHeights();
        m_updateLightSectors.Clear();
        m_updateColorMapSectors.Clear();
        m_updateLineHeights.Clear();
    }

    private unsafe void UpdateLights()
    {
        if (m_updateLightSectors.UpdateSectors.Length == 0 || m_lightBufferStorage == null)
            return;

        var lightBuffer = m_lightBufferStorage.GetMappedBufferAndBind();
        float* lightData = lightBuffer.MappedMemoryPtr;

        for (int i = 0; i < m_updateLightSectors.UpdateSectors.Length; i++)
        {
            var sector = m_updateLightSectors.UpdateSectors[i];
            float level = sector.LightLevel;
            int index = sector.Id * LightBuffer.BufferSize + LightBuffer.SectorIndexStart;
            lightData[index + LightBuffer.FloorOffset] = level;
            lightData[index + LightBuffer.CeilingOffset] = level;
            lightData[index + LightBuffer.WallOffset] = level;
        }

        m_lightBufferStorage.Unbind();
    }

    private unsafe void UpdateColorMaps()
    {
        if (m_updateColorMapSectors.UpdateSectors.Length == 0 || m_sectorColorMapsBuffer == null)
            return;

        var mappedBuffer = m_sectorColorMapsBuffer.GetMappedBufferAndBind();
        float* colorMapBuffer = mappedBuffer.MappedMemoryPtr;

        for (int i = 0; i < m_updateColorMapSectors.UpdateSectors.Length; i++)
        {
            var sector = m_updateColorMapSectors.UpdateSectors[i];
            SetSectorColorMap(colorMapBuffer, sector, sector.Colormap);
        }

        m_sectorColorMapsBuffer.Unbind();
    }

    private unsafe void UpdateLineHeights()
    {
        if (m_updateLineHeights.UpdateSectors.Length == 0 || m_lineHeightsBuffer == null || m_world == null)
            return;

        var checkCounter = ++WorldStatic.CheckCounter;
        var mappedBuffer = m_lineHeightsBuffer.GetMappedBufferAndBind();
        var lineArray = m_world.StructLines.Data;
        float* buffer = mappedBuffer.MappedMemoryPtr;

        for (int i = 0; i < m_updateLineHeights.UpdateSectors.Length; i++)
        {
            var sector = m_updateLineHeights.UpdateSectors[i];
            for (int j = 0; j < sector.LineIds.Length; j++)
            {
                var lineId = sector.LineIds[j];
                if (WorldStatic.CheckedLines[lineId] == checkCounter)
                    continue;

                ref var line = ref lineArray[lineId];
                WorldStatic.CheckedLines[lineId] = checkCounter;
                SetLineHeightBuffer(buffer, lineId, ref line);
            }
        }

        m_lineHeightsBuffer.Unbind();
    }
}
