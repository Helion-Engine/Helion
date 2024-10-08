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

namespace Helion.Render;

public partial class Renderer
{
    private readonly SectorUpdates m_updateLightSectors = new();
    private readonly SectorUpdates m_updateColorMapSectors = new();

    private GLBufferTextureStorage? m_lightBufferStorage;
    private GLBufferTextureStorage? m_sectorColorMapsBuffer;

    private float[] m_lightBufferData = [];

    public static int GetLightBufferIndex(Sector sector, LightBufferType type)
    {
        int index = sector.Id * Constants.LightBuffer.BufferSize + Constants.LightBuffer.SectorIndexStart;
        return type switch
        {
            LightBufferType.Floor => sector.TransferFloorLightSector.Id * Constants.LightBuffer.BufferSize + Constants.LightBuffer.SectorIndexStart + Constants.LightBuffer.FloorOffset,
            LightBufferType.Ceiling => sector.TransferCeilingLightSector.Id * Constants.LightBuffer.BufferSize + Constants.LightBuffer.SectorIndexStart + Constants.LightBuffer.CeilingOffset,
            LightBufferType.Wall => sector.Id * Constants.LightBuffer.BufferSize + Constants.LightBuffer.SectorIndexStart + Constants.LightBuffer.WallOffset,
            _ => index,
        };
    }

    public static int GetColorMapBufferIndex(Sector sector, LightBufferType type)
    {
        return type switch
        {
            LightBufferType.Floor => (sector.TransferFloorLightSector.Id + 1) * Constants.LightBuffer.BufferSize + Constants.LightBuffer.FloorOffset,
            LightBufferType.Ceiling => (sector.TransferCeilingLightSector.Id + 1) * Constants.LightBuffer.BufferSize + Constants.LightBuffer.CeilingOffset,
            LightBufferType.Wall => (sector.Id + 1) * Constants.LightBuffer.BufferSize + Constants.LightBuffer.WallOffset,
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
            m_lightBufferData = new float[world.Sectors.Count * Constants.LightBuffer.BufferSize * FloatSize + (Constants.LightBuffer.SectorIndexStart * FloatSize)];
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
        int index = (sector.Id + 1) * Constants.LightBuffer.BufferSize;
        if (ShaderVars.PaletteColorMode)
        {
            int colorMapIndex = colormap == null ? 0 : colormap.Index;
            colorMapBuffer[index + Constants.LightBuffer.FloorOffset] = colorMapIndex;
            colorMapBuffer[index + Constants.LightBuffer.CeilingOffset] = colorMapIndex;
            colorMapBuffer[index + Constants.LightBuffer.WallOffset] = colorMapIndex;
            return;
        }

        const int VectorSize = 3;
        Vec3F setColor = colormap == null ? Vec3F.One : colormap.ColorMix;
        *(Vec3F*)&colorMapBuffer[(index + Constants.LightBuffer.FloorOffset) * VectorSize] = setColor;
        *(Vec3F*)&colorMapBuffer[(index + Constants.LightBuffer.CeilingOffset) * VectorSize] = setColor;
        *(Vec3F*)&colorMapBuffer[(index + Constants.LightBuffer.WallOffset) * VectorSize] = setColor;
    }

    private unsafe void SetSectorLightBuffer(IWorld world)
    {
        m_lightBufferStorage?.Dispose();
        m_lightBufferStorage = new("Sector lights texture buffer", m_lightBufferData, SizedInternalFormat.R32f, GLInfo.MapPersistentBitSupported);

        m_lightBufferStorage.Map(data =>
        {
            float* lightBuffer = (float*)data.ToPointer();
            lightBuffer[Constants.LightBuffer.DarkIndex] = 0;
            lightBuffer[Constants.LightBuffer.FullBrightIndex] = 255;

            for (int i = 0; i < Constants.LightBuffer.ColorMapCount; i++)
                lightBuffer[Constants.LightBuffer.ColorMapStartIndex + i] =
                    256 - ((Constants.LightBuffer.ColorMapCount - i) * 256 / Constants.LightBuffer.ColorMapCount);

            for (int i = 0; i < world.Sectors.Count; i++)
            {
                Sector sector = world.Sectors[i];
                int index = sector.Id * Constants.LightBuffer.BufferSize + Constants.LightBuffer.SectorIndexStart;
                lightBuffer[index + Constants.LightBuffer.FloorOffset] = sector.Floor.LightLevel;
                lightBuffer[index + Constants.LightBuffer.CeilingOffset] = sector.Ceiling.LightLevel;
                lightBuffer[index + Constants.LightBuffer.WallOffset] = sector.LightLevel;
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
            int index = sector.Id * Constants.LightBuffer.BufferSize + Constants.LightBuffer.SectorIndexStart;
            lightData[index + Constants.LightBuffer.FloorOffset] = level;
            lightData[index + Constants.LightBuffer.CeilingOffset] = level;
            lightData[index + Constants.LightBuffer.WallOffset] = level;
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
