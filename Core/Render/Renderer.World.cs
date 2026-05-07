using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Graphics.Palettes;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Textures;
using Helion.Util.Assertion;
using Helion.World;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using OpenTK.Graphics.OpenGL;
using System;
using static Helion.Util.Constants;

namespace Helion.Render;

public partial class Renderer
{
    enum BlockSide
    {
        Front = 1,
        Back = 2,
        Both = 3
    }

    private unsafe delegate void SetLightBufferPtrAction<T>(IWorld world, T* data, int length);

    private readonly SectorUpdates m_updateLightSectors = new();
    private readonly SectorUpdates m_updateColorMapSectors = new();
    private readonly SectorUpdates m_updateFogColorSectors = new();
    private readonly SectorUpdates m_updateLineHeights = new();

    private GLBufferTextureStorage<byte>? m_sectorLightsBuffer;
    private GLBufferTextureStorage<float>? m_sectorColorMapsBuffer;
    private GLBufferTextureStorage<float>? m_sectorFogBuffer;
    private GLBufferTextureStorage<float>? m_colorMapBuffer;
    private GLBufferTextureStorage<float>? m_mapDataBuffer;
    private GLBufferTextureStorage<float>? m_lineHeightsBuffer;

    private bool m_sectorFog;
    private bool m_sectorColor;

    public static int GetLightBufferIndex(Side side, Wall wall, Sector sector, out int overrideLightIndex)
    {
        // The shader will add the light level at this index plus the vertex light level.
        // Return LightBuffer.DarkIndex (lightlevel=0) to not add the sectors light level if absolute.
        overrideLightIndex = side.Flags.LightLevelAbsolute || wall.LightLevelAbsolute ? LightBuffer.DarkIndex + 1 : 0;
        return GetLightBufferIndex(sector, LightBufferType.Wall);
    }

    public static int GetLightBufferIndex(Sector sector, SectorPlaneFace planeType, LightBufferType type, out int overrideLightIndex)
    {
        var transferLightSector = planeType == SectorPlaneFace.Floor ? sector.TransferFloorLightSector : sector.TransferCeilingLightSector;
        var plane = transferLightSector.GetSectorPlane(planeType);
        overrideLightIndex = plane.LightLevelAbsolute ? LightBuffer.DarkIndex + 1 : 0;
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

    public unsafe void UpdateToNewWorld(IWorld world)
    {
        m_lastDrawWorldCmd = default;
        m_updateLightSectors.ClearAndReset();
        m_updateColorMapSectors.ClearAndReset();
        m_updateFogColorSectors.ClearAndReset();
        m_updateLineHeights.ClearAndReset();
        m_updateLightSectors.EnsureCapacity(world.Sectors.Count);
        m_updateColorMapSectors.EnsureCapacity(world.Sectors.Count);
        m_updateFogColorSectors.EnsureCapacity(world.Sectors.Count);
        m_updateLineHeights.EnsureCapacity(world.Sectors.Count);

        m_worldRenderer.UpdateToNewWorld(world);
        m_automapRenderer.UpdateTo(world);

        if (m_world != null)
        {
            m_world.SectorLightChanged -= World_SectorLightChanged;
            m_world.SectorColorMapChanged -= World_SectorColorMapChanged;
            m_world.SectorFogColorChanged -= World_SectorFogColorChanged;
            m_world.SectorMove -= World_SectorMove;
            m_world.SectorMoveComplete -= World_SectorMoveComplete;
        }

        m_world = world;
        m_world.SectorLightChanged += World_SectorLightChanged;
        m_world.SectorColorMapChanged += World_SectorColorMapChanged;
        m_world.SectorFogColorChanged += World_SectorFogColorChanged;
        m_world.SectorMove += World_SectorMove;
        m_world.SectorMoveComplete += World_SectorMoveComplete;

        var alloc = !m_world.SameAsPreviousMap;
        SetMapDataBuffer(world, alloc);
        m_sectorLightsBuffer = InitLightBuffer(world, alloc, m_sectorLightsBuffer, InitLightBuffer, "Sector light levels", SizedInternalFormat.R8ui, 1);
        m_sectorColorMapsBuffer = InitLightBuffer(world, alloc, m_sectorColorMapsBuffer, InitSectorColorMap, "Sector colormaps", SizedInternalFormat.Rgba32f, GLBufferTextureStorage<float>.FourComponentLength);
        m_sectorFogBuffer = InitLightBuffer(world, alloc, m_sectorFogBuffer, InitSectorFogBuffer, "Sector fog", SizedInternalFormat.Rgba32f, GLBufferTextureStorage<float>.FourComponentLength);
        SetLineHeights(world, alloc);
    }

    private unsafe void SetLineHeights(IWorld world, bool alloc)
    {
        if (!world.Config.Render.VanillaRender)
        {
            m_lineHeightsBuffer?.Dispose();
            m_lineHeightsBuffer = null;
            return;
        }

        if (alloc || m_lineHeightsBuffer == null)
        {
            m_lineHeightsBuffer?.Dispose();
            m_lineHeightsBuffer = new("Line heights data buffer", new float[world.Lines.Count * GLBufferTextureStorage<float>.FourComponentLength], SizedInternalFormat.Rgba32f, GLInfo.MapPersistentBitSupported);

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

    private static unsafe GLBufferTextureStorage<T> InitLightBuffer<T>(IWorld world, bool alloc, GLBufferTextureStorage<T>? bufferStorage, 
        SetLightBufferPtrAction<T> setAction, string name, SizedInternalFormat format, int components) where T : struct
    {
        if (alloc || bufferStorage == null)
        {
            var arrayData = AllocLightSectorArray<T>(world, components);
            bufferStorage?.Dispose();
            bufferStorage = new(name, arrayData, format, GLInfo.MapPersistentBitSupported);

            bufferStorage.Map(data =>
            {
                var lightBuffer = (T*)data.ToPointer();
                setAction(world, lightBuffer, arrayData.Length);
            });
        }
        else
        {
            var lightBuffer = bufferStorage.GetMappedBufferAndBind();
            setAction(world, lightBuffer.MappedMemoryPtr, bufferStorage.DataLength());
            bufferStorage.Unbind();
        }

        return bufferStorage;
    }

    private static T[] AllocLightSectorArray<T>(IWorld world, int components) where T : struct
    {
        var sectorBufferCount = world.Sectors.Count * LightBuffer.BufferSize + LightBuffer.SectorIndexStart;
        return new T[sectorBufferCount * components];
    }

    private static unsafe void InitLightBuffer(IWorld world, byte* lightBuffer, int length)
    {
        lightBuffer[LightBuffer.DarkIndex] = 0;
        lightBuffer[LightBuffer.FullBrightIndex] = 255;

        for (int i = 0; i < LightBuffer.ColorMapCount; i++)
            lightBuffer[LightBuffer.ColorMapStartIndex + i] =
                (byte)(256 - ((LightBuffer.ColorMapCount - i) * 256 / LightBuffer.ColorMapCount));

        for (int i = 0; i < world.Sectors.Count; i++)
        {
            var sector = world.Sectors[i];
            var lightLevel = sector.GetByteLightLevel();
            SetSectorLight(lightBuffer, length, sector, lightLevel);
        }
    }

    private unsafe void InitSectorColorMap(IWorld world, float* colorMapBuffer, int length)
    {
        *(Vec3F*)&colorMapBuffer[0] = Vec3F.One;
        
        for (int i = 0; i < world.Sectors.Count; i++)
        {
            var sector = world.Sectors[i];
            SetSectorColorMap(colorMapBuffer, length, sector, sector.Colormap);
        }
    }

    private unsafe void InitSectorFogBuffer(IWorld world, float* fadeBuffer, int length)
    {
        *(Vec3F*)&fadeBuffer[0] = Vec3F.Zero;

        for (int i = 0; i < world.Sectors.Count; i++)
        {
            var sector = world.Sectors[i];
            SetSectorFog(fadeBuffer, length, sector.Id, sector.FogColor, Math.Clamp(sector.LightLevel, (short)0, (short)255), sector.FogDensity);
        }
    }

    private unsafe void SetSectorFog(float* fadeBuffer, int bufferLength, int sectorId, Color fadeColor, short lightLevel, float fogDensity)
    {
        var index = sectorId * LightBuffer.BufferSize + LightBuffer.SectorIndexStart;
        var fade = GetSectorFogDensity(fadeColor, lightLevel, fogDensity);

        if (fadeColor.Uint != 0)
            m_sectorFog = true;

        Assert.Precondition(bufferLength > index + (GLBufferTextureStorage<float>.FourComponentLength * 3), $"Invalid sector id {sectorId} for sector fog buffer");

        *(Vec4F*)&fadeBuffer[(index + LightBuffer.FloorOffset) * GLBufferTextureStorage<float>.FourComponentLength] = fade;
        *(Vec4F*)&fadeBuffer[(index + LightBuffer.CeilingOffset) * GLBufferTextureStorage<float>.FourComponentLength] = fade;
        *(Vec4F*)&fadeBuffer[(index + LightBuffer.WallOffset) * GLBufferTextureStorage<float>.FourComponentLength] = fade;
    }

    private unsafe void SetSectorColorMap(float* colorMapBuffer, int bufferLength, Sector sector, Colormap? colormap)
    {
        var index = sector.Id * LightBuffer.BufferSize + LightBuffer.SectorIndexStart;
        var setColor = GetSectorSetColor(colormap);

        if (setColor.X != 1 || setColor.Y != 1 || setColor.Z != 1)
            m_sectorColor = true;

        Assert.Precondition(bufferLength > index + (GLBufferTextureStorage<float>.FourComponentLength * 3), $"Invalid sector id {sector.Id} for sector colormap buffer");

        *(Vec3F*)&colorMapBuffer[(index + LightBuffer.FloorOffset) * GLBufferTextureStorage<float>.FourComponentLength] = setColor;
        *(Vec3F*)&colorMapBuffer[(index + LightBuffer.CeilingOffset) * GLBufferTextureStorage<float>.FourComponentLength] = setColor;
        *(Vec3F*)&colorMapBuffer[(index + LightBuffer.WallOffset) * GLBufferTextureStorage<float>.FourComponentLength] = setColor;
    }

    private static Vec4F GetSectorFogDensity(Color fadeColor, short lightLevel, float fogDensity)
    {
        const float FadeFactor = 0.004f;
        if (fadeColor.Uint == 0)
            return Vec4F.Zero;

        if (fogDensity == 0)
            fogDensity = (1.0f - lightLevel / 255.0f) * FadeFactor;
        else
            fogDensity *= FadeFactor;

        return new(fadeColor.R / 255f, fadeColor.G / 255f, fadeColor.B / 255f, fogDensity);
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
            m_mapDataBuffer?.Dispose();
            m_mapDataBuffer = null;
            return;
        }

        if (!alloc)
            return;

        m_mapDataBuffer?.Dispose();
        m_mapDataBuffer = new("Map data buffer", new float[world.Lines.Count * GLBufferTextureStorage<float>.FourComponentLength], SizedInternalFormat.Rgba32f, false);

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

    private unsafe void SetLineHeightBuffer(float* buffer, int lineId, ref StructLine line)
    {
        var prevFloorZ = (float)line.FrontFloorPlane.PrevZ;
        var floorZ = (float)line.FrontFloorPlane.Z;
        if (line.BackFloorPlane != null)
        {
            prevFloorZ = Math.Max(prevFloorZ, (float)line.BackFloorPlane.PrevZ);
            floorZ = Math.Max(floorZ, (float)line.BackFloorPlane.Z);
        }

        var index = lineId * GLBufferTextureStorage<float>.FourComponentLength;
        Assert.Precondition(m_lineHeightsBuffer!.DataLength() > index + 2, "Invalid line id for line height buffer");
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

    private void World_SectorFogColorChanged(object? sender, Sector sector)
    {
        m_updateFogColorSectors.Add(sector);
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
        UpdateFogColors();
        UpdateLineHeights();
        m_updateLightSectors.Clear();
        m_updateColorMapSectors.Clear();
        m_updateFogColorSectors.Clear();
        m_updateLineHeights.Clear();
    }

    private unsafe void UpdateLights()
    {
        if (m_updateLightSectors.UpdateSectors.Length == 0 || m_sectorLightsBuffer == null || m_sectorFogBuffer == null)
            return;

        var lightBuffer = m_sectorLightsBuffer.GetMappedBufferAndBind();
        var lightBufferLength = m_sectorLightsBuffer.DataLength();
        var lightData = lightBuffer.MappedMemoryPtr;

        for (int i = 0; i < m_updateLightSectors.UpdateSectors.Length; i++)
        {
            var sector = m_updateLightSectors.UpdateSectors[i];
            var lightLevel = sector.GetByteLightLevel();
            SetSectorLight(lightData, lightBufferLength, sector, lightLevel);
        }

        // This was done in the same loop but would cause crashes on 3.3 GPUs
        m_sectorLightsBuffer.Unbind();

        var fogBuffer = m_sectorFogBuffer.GetMappedBufferAndBind();
        var fogBufferLength = m_sectorFogBuffer.DataLength();
        var fogData = fogBuffer.MappedMemoryPtr;

        for (int i = 0; i < m_updateLightSectors.UpdateSectors.Length; i++)
        {
            var sector = m_updateLightSectors.UpdateSectors[i];
            var lightLevel = sector.GetByteLightLevel();
            SetSectorFog(fogData, fogBufferLength, sector.Id, sector.FogColor, lightLevel, sector.FogDensity);
        }

        m_sectorFogBuffer.Unbind();
    }

    private static unsafe void SetSectorLight(byte* lightData, int length, Sector sector, byte lightLevel)
    {
        var index = sector.Id * LightBuffer.BufferSize + LightBuffer.SectorIndexStart;
        Assert.Precondition(length > index + (LightBuffer.BufferSize - 1), $"Invalid sector id {sector.Id} for sector light buffer");
        lightData[index + LightBuffer.FloorOffset] = lightLevel;
        lightData[index + LightBuffer.CeilingOffset] = lightLevel;
        lightData[index + LightBuffer.WallOffset] = lightLevel;
    }

    private unsafe void UpdateColorMaps()
    {
        if (m_updateColorMapSectors.UpdateSectors.Length == 0 || m_sectorColorMapsBuffer == null)
            return;

        var mappedBuffer = m_sectorColorMapsBuffer.GetMappedBufferAndBind();
        var mappedBufferLength = m_sectorColorMapsBuffer.DataLength();
        float* colorMapBuffer = mappedBuffer.MappedMemoryPtr;

        for (int i = 0; i < m_updateColorMapSectors.UpdateSectors.Length; i++)
        {
            var sector = m_updateColorMapSectors.UpdateSectors[i];
            SetSectorColorMap(colorMapBuffer, mappedBufferLength, sector, sector.Colormap);
        }

        m_sectorColorMapsBuffer.Unbind();
    }

    private unsafe void UpdateFogColors()
    {
        if (m_updateFogColorSectors.UpdateSectors.Length == 0 || m_sectorFogBuffer == null)
            return;

        var mappedBuffer = m_sectorFogBuffer.GetMappedBufferAndBind();
        var mappedBufferLength = m_sectorFogBuffer.DataLength();
        float* fogBuffer = mappedBuffer.MappedMemoryPtr;

        for (int i = 0; i < m_updateFogColorSectors.UpdateSectors.Length; i++)
        {
            var sector = m_updateFogColorSectors.UpdateSectors[i];
            SetSectorFog(fogBuffer, mappedBufferLength, sector.Id, sector.FogColor, sector.LightLevel, sector.FogDensity);
        }

        m_sectorFogBuffer.Unbind();
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
