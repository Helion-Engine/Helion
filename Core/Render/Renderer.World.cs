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
    enum BlockSide
    {
        Front = 1,
        Back = 2,
        Both = 3
    }

    private readonly SectorUpdates m_updateLightSectors = new();
    private readonly SectorUpdates m_updateColorMapSectors = new();
    private readonly SectorUpdates m_updateLineHeights = new();

    private GLBufferTextureStorage<byte>? m_lightBufferStorage;
    private GLBufferTextureStorage<float>? m_sectorColorMapsBuffer;
    private GLBufferTextureStorage<float>? m_colorMapBuffer;
    private GLBufferTextureStorage<float>? m_mapDataBuffer;
    private GLBufferTextureStorage<float>? m_lineHeightsBuffer;

    private byte[] m_lightBufferData = [];
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
        m_lastDrawWorldCmd = default;
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
            m_world.SectorMoveComplete -= World_SectorMoveComplete;
        }

        m_world = world;
        m_world.SectorLightChanged += World_SectorLightChanged;
        m_world.SectorColorMapChanged += World_SectorColorMapChanged;
        m_world.SectorMove += World_SectorMove;
        m_world.SectorMoveComplete += World_SectorMoveComplete;

        m_vanillaRender = m_config.Render.VanillaRender;

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
            m_lightBufferData = new byte[world.Sectors.Count * LightBuffer.BufferSize + LightBuffer.SectorIndexStart];
            m_lightBufferStorage = new("Sector lights texture buffer", m_lightBufferData, SizedInternalFormat.R8ui, GLInfo.MapPersistentBitSupported);

            m_lightBufferStorage.Map(data =>
            {
                var lightBuffer = (byte*)data.ToPointer();
                SetLightBuffer(world, lightBuffer);
            });
        }
        else
        {
            var lightBuffer = m_lightBufferStorage.GetMappedBufferAndBind();
            SetLightBuffer(world, lightBuffer.MappedMemoryPtr);
            m_lightBufferStorage.Unbind();
        }
    }

    private unsafe void SetLightBuffer(IWorld world, byte* lightBuffer)
    {        
        lightBuffer[LightBuffer.DarkIndex] = 0;
        lightBuffer[LightBuffer.FullBrightIndex] = 255;

        for (int i = 0; i < LightBuffer.ColorMapCount; i++)
            lightBuffer[LightBuffer.ColorMapStartIndex + i] =
                (byte)(256 - ((LightBuffer.ColorMapCount - i) * 256 / LightBuffer.ColorMapCount));

        for (int i = 0; i < world.Sectors.Count; i++)
        {
            Sector sector = world.Sectors[i];
            var lightLevel = (byte)Math.Clamp(sector.LightLevel, (short)0, (short)255);
            int index = sector.Id * LightBuffer.BufferSize + LightBuffer.SectorIndexStart;
            lightBuffer[index + LightBuffer.FloorOffset] = lightLevel;
            lightBuffer[index + LightBuffer.CeilingOffset] = lightLevel;
            lightBuffer[index + LightBuffer.WallOffset] = lightLevel;
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
            m_lineHeightsBufferData = new float[world.Lines.Count * GLBufferTextureStorage<float>.FourComponentLength];
            m_lineHeightsBuffer = new("Line heights data buffer", m_lineHeightsBufferData, SizedInternalFormat.Rgba32f, GLInfo.MapPersistentBitSupported);

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
            m_lineHeightsBuffer.Unbind();
        }
    }

    private unsafe void SetSectorColorMapsBuffer(IWorld world, bool alloc)
    {
        if (alloc || m_sectorColorMapsBuffer == null)
        {
            // First index will always map to default colormap
            int sectorBufferCount = (world.Sectors.Count + 1) * LightBuffer.BufferSize;
            // PaletteColorMode is index to colormap, true color will be RGB mix
            int size = 4;
            var sectorBuffer = new float[sectorBufferCount * size];

            m_sectorColorMapsBuffer?.Dispose();
            m_sectorColorMapsBuffer = new("Sector colormaps", sectorBuffer, SizedInternalFormat.Rgba32f, GLInfo.MapPersistentBitSupported);
        }

        if (alloc)
        {
            m_sectorColorMapsBuffer.Map(data =>
            {
                float* colorMapBuffer = (float*)data.ToPointer();
                InitSectorColorMap(world, colorMapBuffer);
            });
        }
        else
        {
            var mappedBuffer = m_sectorColorMapsBuffer.GetMappedBufferAndBind();
            float* colorMapBuffer = mappedBuffer.MappedMemoryPtr;
            InitSectorColorMap(world, colorMapBuffer);
            m_sectorColorMapsBuffer.Unbind();
        }
    }

    private static unsafe void InitSectorColorMap(IWorld world, float* colorMapBuffer)
    {
        *(Vec3F*)&colorMapBuffer[0] = Vec3F.One;
        
        for (int i = 0; i < world.Sectors.Count; i++)
        {
            var sector = world.Sectors[i];
            SetSectorColorMap(colorMapBuffer, sector, sector.Colormap);
        }
    }

    private static unsafe void SetSectorColorMap(float* colorMapBuffer, Sector sector, Colormap? colormap)
    {
        int index = (sector.Id + 1) * LightBuffer.BufferSize;

        const int VectorSize = 4;
        var setColor = GetSectorSetColor(colormap);

        *(Vec3F*)&colorMapBuffer[(index + LightBuffer.FloorOffset) * VectorSize] = setColor;
        *(Vec3F*)&colorMapBuffer[(index + LightBuffer.CeilingOffset) * VectorSize] = setColor;
        *(Vec3F*)&colorMapBuffer[(index + LightBuffer.WallOffset) * VectorSize] = setColor;
    }

    private static Vec3F GetSectorSetColor(Colormap? colormap)
    {
        // True color always uses rgb color mix.
        // Palette color uses r channel as colormap index when b channel is -1.
        // When b channel is not -1 then palette color mode will treat as true color mix to support rgb mix values.

        if (!ShaderVars.PaletteColorMode)
            return colormap == null ? Vec3F.One : colormap.ColorMix;

        if (colormap != null && colormap.Type == ColorMapType.SectorRgb)
            return colormap.ColorMix;

        return colormap == null ? new Vec3F(0, -1, -1) : new Vec3F(colormap.Index, -1, -1);
    }

    public unsafe void SetMapDataBuffer(IWorld world, bool alloc)
    {
        if (!world.Config.Render.VanillaRender)
        {
            m_mapBufferData = [];
            m_mapDataBuffer?.Dispose();
            m_mapDataBuffer = null;
            return;
        }

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
                int index = i * GLBufferTextureStorage<float>.FourComponentLength;
                buffer[index] = (float)line.Segment.Start.X;
                buffer[index + 1] = (float)line.Segment.Start.Y;
                buffer[index + 2] = (float)line.Segment.End.X;
                buffer[index + 3] = (float)line.Segment.End.Y;
            }
        });
    }

    private static unsafe void SetLineHeightBuffer(float* buffer, int lineId, ref StructLine line)
    {
        var prevFloorZ = (float)line.FrontFloorPlane.PrevZ;
        var floorZ = (float)line.FrontFloorPlane.Z;
        if (line.BackFloorPlane != null)
        {
            prevFloorZ = Math.Max(prevFloorZ, (float)line.BackFloorPlane.PrevZ);
            floorZ = Math.Max(floorZ, (float)line.BackFloorPlane.Z);
        }

        var index = lineId * GLBufferTextureStorage<float>.FourComponentLength;
        buffer[index] = prevFloorZ;
        buffer[index + 1] = floorZ;

        // CoverWallUtil.GetProjectHeights forces top and bottom projection to cover. Ensure it's set to blocked for the shader.
        if (RenderBlock.IsBlocked(line))
        {
            buffer[index + 2] = (int)BlockSide.Both;
            return;
        }

        int blockSide = 0;
        if (line.Line.Front.Middle.TextureHandle != NoTextureIndex)
            blockSide |= (int)BlockSide.Front;
        if (line.Line.Back != null && line.Line.Back.Middle.TextureHandle != NoTextureIndex)
            blockSide |= (int)BlockSide.Back;

        if (line.BackFloorPlane != null)
        {
            if (line.BackFloorPlane.Z < line.FrontFloorPlane.Z)
                blockSide |= (int)BlockSide.Front;
            else if (line.FrontFloorPlane.Z < line.BackFloorPlane.Z)
                blockSide |= (int)BlockSide.Back;
        }

        buffer[index + 2] = blockSide;
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

    private void World_SectorMoveComplete(object? sender, SectorPlane e)
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
        var lightData = lightBuffer.MappedMemoryPtr;

        for (int i = 0; i < m_updateLightSectors.UpdateSectors.Length; i++)
        {
            var sector = m_updateLightSectors.UpdateSectors[i];
            var level = (byte)Math.Clamp(sector.LightLevel, (short)0, (short)255);
            var index = sector.Id * LightBuffer.BufferSize + LightBuffer.SectorIndexStart;
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
