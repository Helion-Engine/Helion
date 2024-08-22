using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.World.Geometry.Sectors;
using Helion.World;
using OpenTK.Graphics.OpenGL;
using Helion.Render.OpenGL.Textures;
using Helion.Util;
using Helion.Util.Container;
using Helion.Render.OpenGL.Util;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.Graphics.Palettes;
using Helion.Geometry.Vectors;

namespace Helion.Render;

public partial class Renderer
{
    private readonly DynamicArray<Sector> m_updateLightSectors = new();
    private readonly DynamicArray<int> m_updateLightSectorsLookup = new();
    private readonly DynamicArray<Sector> m_updateColorMapSectors = new();
    private readonly DynamicArray<int> m_updateColorMapSectorsLookup = new();

    private GLBufferTextureStorage? m_lightBufferStorage;
    private GLBufferTextureStorage? m_sectorColorMapsBuffer;

    private float[] m_lightBufferData = [];
    private int m_counter;

    public static int GetLightBufferIndex(Sector sector, LightBufferType type)
    {
        int index = sector.Id * Constants.LightBuffer.BufferSize + Constants.LightBuffer.SectorIndexStart;
        switch (type)
        {
            case LightBufferType.Floor:
                return sector.TransferFloorLightSector.Id * Constants.LightBuffer.BufferSize + Constants.LightBuffer.SectorIndexStart + Constants.LightBuffer.FloorOffset;
            case LightBufferType.Ceiling:
                return sector.TransferCeilingLightSector.Id * Constants.LightBuffer.BufferSize + Constants.LightBuffer.SectorIndexStart + Constants.LightBuffer.CeilingOffset;
            case LightBufferType.Wall:
                return sector.Id * Constants.LightBuffer.BufferSize + Constants.LightBuffer.SectorIndexStart + Constants.LightBuffer.WallOffset;
        }

        return index;
    }

    public void UpdateToNewWorld(IWorld world)
    {
        m_updateLightSectors.Clear();

        m_updateLightSectors.FlushReferences();
        m_updateLightSectors.FlushReferences();

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

        m_lightBufferStorage?.Dispose();
        m_lightBufferStorage = new("Sector lights texture buffer", m_lightBufferData, SizedInternalFormat.R32f, GLInfo.MapPersistentBitSupported);

        SetLightBufferData(world, m_lightBufferStorage);
        SetSectorColorMapsBuffer(world);

        UpdateLookup(m_updateLightSectorsLookup, world.Sectors.Count);
        UpdateLookup(m_updateColorMapSectorsLookup, world.Sectors.Count);
    }

    private unsafe void SetSectorColorMapsBuffer(IWorld world)
    {
        bool usePalette = ShaderVars.PaletteColorMode;
        m_sectorColorMapsBuffer?.Dispose();
        // First index will always map to default colormap
        int sectorBufferCount = world.Sectors.Count + 1;
        // PaletteColorMode is index to colormap, true color will be RGB mix
        int size = usePalette ? 1 : 3;
        var sectorBuffer = new float[sectorBufferCount * 4];

        m_sectorColorMapsBuffer = new("Sector colormaps", sectorBuffer, usePalette ? SizedInternalFormat.R32f : SizedInternalFormat.Rgb32f, GLInfo.MapPersistentBitSupported);

        if (usePalette)
        {
            m_sectorColorMapsBuffer.Map(data =>
            {
                float* colorMapBuffer = (float*)data.ToPointer();
                for (int i = 0; i < world.Sectors.Count; i++)
                {
                    var sector = world.Sectors[i];
                    SetSectorColorMap(colorMapBuffer, sector.Id, sector.Colormap);
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
                    SetSectorColorMap(colorMapBuffer, sector.Id, sector.Colormap);
                }
            });
        }
    }

    private static unsafe void SetSectorColorMap(float* colorMapBuffer, int sectorId, Colormap? colormap)
    {
        if (ShaderVars.PaletteColorMode)
        {
            colorMapBuffer[sectorId + 1] = colormap == null ? 0 : colormap.Index;
            return;
        }

        Vec3F* color = (Vec3F*)&colorMapBuffer[(sectorId + 1) * 3];
        if (colormap == null)
            *color = Vec3F.One;
        else
            *color = colormap.ColorMix;
    }

    private unsafe void SetLightBufferData(IWorld world, GLBufferTextureStorage lightBuffer)
    {
        lightBuffer.Map(data =>
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
        if (m_updateLightSectorsLookup.Data[sector.Id] == m_counter)
            return;

        m_updateLightSectorsLookup.Data[sector.Id] = m_counter;
        m_updateLightSectors.Add(sector);
    }

    private void World_SectorColorMapChanged(object? sender, Sector sector)
    {
        if (m_updateColorMapSectorsLookup.Data[sector.Id] == m_counter)
            return;

        m_updateColorMapSectorsLookup.Data[sector.Id] = m_counter;
        m_updateColorMapSectors.Add(sector);
    }

    private void UpdateBuffers()
    {
        UpdateLights();
        UpdateColorMaps();
        m_counter++;
    }

    private unsafe void UpdateLights()
    {
        if (m_updateLightSectors.Length == 0 || m_lightBufferStorage == null)
            return;

        GLMappedBuffer<float> lightBuffer = m_lightBufferStorage.GetMappedBufferAndBind();
        float* lightData = lightBuffer.MappedMemoryPtr;

        for (int i = 0; i < m_updateLightSectors.Length; i++)
        {
            Sector sector = m_updateLightSectors[i];
            float level = sector.LightLevel;
            int index = sector.Id * Constants.LightBuffer.BufferSize + Constants.LightBuffer.SectorIndexStart;
            lightData[index + Constants.LightBuffer.FloorOffset] = level;
            lightData[index + Constants.LightBuffer.CeilingOffset] = level;
            lightData[index + Constants.LightBuffer.WallOffset] = level;
        }

        m_lightBufferStorage.Unbind();
        m_updateLightSectors.Clear();
    }

    private unsafe void UpdateColorMaps()
    {
        if (m_updateColorMapSectors.Length == 0 || m_sectorColorMapsBuffer == null)
            return;

        GLMappedBuffer<float> mappedBuffer = m_sectorColorMapsBuffer.GetMappedBufferAndBind();
        float* colorMapBuffer = mappedBuffer.MappedMemoryPtr;

        for (int i = 0; i < m_updateColorMapSectors.Length; i++)
        {
            Sector sector = m_updateColorMapSectors[i];
            SetSectorColorMap(colorMapBuffer, sector.Id, sector.Colormap);
        }

        m_sectorColorMapsBuffer.Unbind();
        m_updateLightSectors.Clear();
    }

    private static void UpdateLookup(DynamicArray<int> array, int count)
    {
        if (array.Capacity < count)
            array.Resize(count);

        for (int i = 0; i < array.Capacity; i++)
            array.Data[i] = -1;
    }
}
