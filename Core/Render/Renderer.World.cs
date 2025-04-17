using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.World.Geometry.Sectors;
using Helion.World;
using OpenTK.Graphics.OpenGL;
using Helion.Render.OpenGL.Textures;
using Helion.Util;
using Helion.Render.OpenGL.Util;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.Graphics.Palettes;
using Helion.Geometry.Vectors;
using static Helion.Util.Constants;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;

namespace Helion.Render;

public partial class Renderer
{
    private readonly SectorUpdates m_updateLightSectors = new();
    private readonly SectorUpdates m_updateColorMapSectors = new();

    private GLBufferTextureStorage? m_lightBufferStorage;
    private GLBufferTextureStorage? m_sectorColorMapsBuffer;

    private float[] m_lightBufferData = [];
    private float[] m_mapBufferData = [];

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
        m_updateLightSectors.EnsureCapacity(world.Sectors.Count);
        m_updateColorMapSectors.EnsureCapacity(world.Sectors.Count);

        m_worldRenderer.UpdateToNewWorld(world);
        m_automapRenderer.UpdateTo(world);

        if (m_world != null)
        {
            m_world.SectorLightChanged -= World_SectorLightChanged;
            m_world.SectorColorMapChanged -= World_SectorColorMapChanged;
        }

        m_world = world;
        m_world.SectorLightChanged += World_SectorLightChanged;
        m_world.SectorColorMapChanged += World_SectorColorMapChanged;

        if (!m_world.SameAsPreviousMap)
        {
            const int FloatSize = 4;
            m_lightBufferData = new float[world.Sectors.Count * LightBuffer.BufferSize * FloatSize + (LightBuffer.SectorIndexStart * FloatSize)];
            m_mapBufferData = new float[world.Lines.Count * FloatSize];
            SetMapDataBuffer(world);
        }

        SetSectorLightBuffer(world);
        SetSectorColorMapsBuffer(world);
    }

    private unsafe void SetSectorColorMapsBuffer(IWorld world)
    {
        bool usePalette = ShaderVars.PaletteColorMode;
        // First index will always map to default colormap
        const int FloatSize = 4;
        int sectorBufferCount = world.Sectors.Count + 1 * Constants.LightBuffer.BufferSize;
        // PaletteColorMode is index to colormap, true color will be RGB mix
        int size = usePalette ? 1 : 3;
        var sectorBuffer = new float[sectorBufferCount * FloatSize * size];

        m_sectorColorMapsBuffer?.Dispose();
        m_sectorColorMapsBuffer = new("Sector colormaps", sectorBuffer, usePalette ? SizedInternalFormat.R32f : SizedInternalFormat.Rgb32f, GLInfo.MapPersistentBitSupported);

        if (usePalette)
        {
            m_sectorColorMapsBuffer.Map(data =>
            {
                float* colorMapBuffer = (float*)data.ToPointer();
                for (int i = 0; i < world.Sectors.Count; i++)
                {
                    var sector = world.Sectors[i];
                    SetSectorColorMap(colorMapBuffer, sector, sector.Colormap);
                }
            });
        }
        else
        {
            m_sectorColorMapsBuffer.Map(data =>
            {
                float* colorMapBuffer = (float*)data.ToPointer();
                Vec3F* color = (Vec3F*)&colorMapBuffer[0];
                *color = Vec3F.One;
                for (int i = 0; i < world.Sectors.Count; i++)
                {
                    var sector = world.Sectors[i];
                    SetSectorColorMap(colorMapBuffer, sector, sector.Colormap);
                }
            });
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

    private unsafe void SetSectorLightBuffer(IWorld world)
    {
        m_lightBufferStorage?.Dispose();
        m_lightBufferStorage = new("Sector lights texture buffer", m_lightBufferData, SizedInternalFormat.R32f, GLInfo.MapPersistentBitSupported);

        m_lightBufferStorage.Map(data =>
        {
            float* lightBuffer = (float*)data.ToPointer();
            lightBuffer[LightBuffer.DarkIndex] = 0;
            lightBuffer[LightBuffer.FullBrightIndex] = 255;

            for (int i = 0; i < Constants.LightBuffer.ColorMapCount; i++)
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
        });
    }

    public unsafe void SetMapDataBuffer(IWorld world)
    {
        m_mapDataBuffer?.Dispose();
        m_mapDataBuffer = new("Map data buffer", m_mapBufferData, SizedInternalFormat.Rgba32f, GLInfo.MapPersistentBitSupported);

        m_mapDataBuffer.Map(data =>
        {
            float* buffer = (float*)data.ToPointer();
            for (int i = 0; i < world.Lines.Count; i++)
            {
                var line = world.Lines[i];
                *(Vec2F*)&buffer[i * 4] = line.Segment.Start.Float;
                *(Vec2F*)&buffer[i * 4 + 2] = line.Segment.Delta.Float;
            }
        });
    }

    private void World_SectorLightChanged(object? sender, Sector sector)
    {
        m_updateLightSectors.Add(sector);
    }

    private void World_SectorColorMapChanged(object? sender, Sector sector)
    {
        m_updateColorMapSectors.Add(sector);
    }

    private void UpdateBuffers()
    {
        UpdateLights();
        UpdateColorMaps();
        m_updateLightSectors.Clear();
        m_updateColorMapSectors.Clear();
    }

    private unsafe void UpdateLights()
    {
        if (m_updateLightSectors.UpdateSectors.Length == 0 || m_lightBufferStorage == null)
            return;

        GLMappedBuffer<float> lightBuffer = m_lightBufferStorage.GetMappedBufferAndBind();
        float* lightData = lightBuffer.MappedMemoryPtr;

        for (int i = 0; i < m_updateLightSectors.UpdateSectors.Length; i++)
        {
            Sector sector = m_updateLightSectors.UpdateSectors[i];
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

        GLMappedBuffer<float> mappedBuffer = m_sectorColorMapsBuffer.GetMappedBufferAndBind();
        float* colorMapBuffer = mappedBuffer.MappedMemoryPtr;

        for (int i = 0; i < m_updateColorMapSectors.UpdateSectors.Length; i++)
        {
            Sector sector = m_updateColorMapSectors.UpdateSectors[i];
            SetSectorColorMap(colorMapBuffer, sector, sector.Colormap);
        }

        m_sectorColorMapsBuffer.Unbind();
    }
}
