using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Special.SectorMovement;
using Helion.World.Static;
using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Util;
using NLog;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public class StaticCacheGeometryRenderer : IDisposable
{
    private const SectorDynamic IgnoreFlags = SectorDynamic.Movement;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Sector DefaultSector = Sector.CreateDefault();

    private readonly LegacyGLTextureManager m_textureManager;
    private readonly GeometryRenderer m_geometryRenderer;
    private readonly RenderProgram m_program;
    private readonly List<GeometryData> m_geometry = new();

    private readonly DynamicArray<GeometryData> m_runtimeGeometry = new();
    private readonly TextureGeometryLookup m_textureToGeometryLookup = new();

    private readonly HashSet<int> m_runtimeGeometryTextures = new();
    private readonly FreeGeometryManager m_freeManager = new();
    private readonly LegacySkyRenderer m_skyRenderer;

    private readonly DynamicArray<Sector> m_updateLightSectors = new();
    private readonly DynamicArray<int> m_updatelightSectorsLookup = new();

    private readonly SkyGeometryManager m_skyGeometry = new();
    private readonly LookupArray<List<Sector>?> m_transferHeightsLookup = new();
    private readonly DynamicArray<DynamicArray<StaticGeometryData>?> m_bufferData = new();
    private readonly DynamicArray<DynamicArray<StaticGeometryData>?> m_bufferDataClamp = new();
    private readonly DynamicArray<DynamicArray<StaticGeometryData>> m_bufferLists = new();

    private bool m_staticMode;
    private bool m_disposed;
    private IWorld? m_world;
    private GLBufferTexture? m_lightBuffer;
    private int m_counter;
    // These are the flags to ignore when setting a side back to static.
    private SectorDynamic m_sideDynamicIgnore;

    public StaticCacheGeometryRenderer(IConfig config, ArchiveCollection archiveCollection, LegacyGLTextureManager textureManager, 
        RenderProgram program, GeometryRenderer geometryRenderer)
    {
        m_textureManager = textureManager;
        m_geometryRenderer = geometryRenderer;
        m_program = program;
        m_skyRenderer = new(config, archiveCollection, textureManager);
    }

    static int GeometryIndexCompare(StaticGeometryData x, StaticGeometryData y)
    {
        return x.Index.CompareTo(y.Index);
    }

    static int TransparentGeometryCompare(GeometryData x, GeometryData y)
    {
        if (x.Texture.TransparentPixelCount == y.Texture.TransparentPixelCount)
            return x.Texture.TextureId.CompareTo(y.Texture.TextureId);

        return x.Texture.TransparentPixelCount.CompareTo(y.Texture.TransparentPixelCount);
    }

    ~StaticCacheGeometryRenderer()
    {
        Dispose(false);
    }

    public void UpdateTo(IWorld world, GLBufferTexture lightBuffer)
    {
        m_lightBuffer = lightBuffer;
        ClearData();
        m_skyRenderer.Reset();

        m_runtimeGeometry.FlushReferences();
        m_updateLightSectors.FlushReferences();
        for (int i = 0; i < m_bufferData.Length; i++)
        {
            var list = m_bufferData.Data[i];
            if (list != null)
                list.FlushStruct();
        }

        m_world = world;
        m_staticMode = world.Config.Render.StaticMode;
        
        SetSideDynamicIgnore();

        if (!m_staticMode)
            return;
        
        m_world.SectorMoveStart += World_SectorMoveStart;
        m_world.SectorMoveComplete += World_SectorMoveComplete;
        m_world.SideTextureChanged += World_SideTextureChanged;
        m_world.PlaneTextureChanged += World_PlaneTextureChanged;
        m_world.SectorLightChanged += World_SectorLightChanged;

        SetLightBufferData(world, lightBuffer);

        m_geometryRenderer.SetTransferHeightView(TransferHeightView.Middle);
        m_geometryRenderer.SetViewSector(DefaultSector);
        m_geometryRenderer.SetBuffer(false);

        for (int i = 0; i < world.Sectors.Count; i++)
        {
            var sector = world.Sectors[i];
            AddTransferSector(sector);

            if ((sector.Floor.Dynamic & IgnoreFlags) == 0)
                AddSectorPlane(sector, true);
            if ((sector.Ceiling.Dynamic & IgnoreFlags) == 0)
                AddSectorPlane(sector, false);
        }

        for (int i = 0; i < world.Lines.Count; i++)
            AddLine(world.Lines[i]);

        for (int i = 0; i < world.Sectors.Count; i++)
        {
            var sector = world.Sectors[i];
            // Sectors can be actively moving loading a save game.
            if (!sector.IsMoving)
                continue;

            WorldBase worldBase = (WorldBase)world;
            if (sector.ActiveFloorMove != null)
                HandleSectorMoveStart(worldBase, sector.Floor);
            if (sector.ActiveCeilingMove != null)
                HandleSectorMoveStart(worldBase, sector.Ceiling);
        }

        foreach (var data in m_geometry)
        {
            data.Vbo.Bind();
            data.Vbo.UploadIfNeeded();
        }

        UpdateLookup(m_updatelightSectorsLookup, world.Sectors.Count);
    }

    public void CheckForFloodFill(Side facingSide, Side otherSide, Sector otherSector, bool isFront)
    {
        SideTexture previous = facingSide.FloodTextures;
        StaticDataApplier.SetFloodFillSide(m_world, facingSide, otherSide);
        if (previous == facingSide.FloodTextures)
            return;

        if ((previous & SideTexture.Lower) == 0)
        {
            if ((facingSide.FloodTextures & SideTexture.Lower) != 0 && facingSide.LowerFloodKey == 0)
                m_geometryRenderer.Portals.AddStaticFloodFillSide(facingSide, otherSide, otherSector, SideTexture.Lower, isFront);
        }
        else
        {
            if ((facingSide.FloodTextures & SideTexture.Lower) == 0 && facingSide.UpperFloodKey == 0)
            {
                if (facingSide.LowerFloodKey > 0)
                    m_geometryRenderer.Portals.ClearStaticWall(facingSide.LowerFloodKey);
                if (facingSide.LowerFloodKey2 > 0)
                    m_geometryRenderer.Portals.ClearStaticWall(facingSide.LowerFloodKey2);

                facingSide.LowerFloodKey = 0;
                facingSide.LowerFloodKey2 = 0;
            }
        }

        if ((previous & SideTexture.Upper) == 0 && facingSide.UpperFloodKey == 0)
        {
            if ((facingSide.FloodTextures & SideTexture.Upper) != 0)
                m_geometryRenderer.Portals.AddStaticFloodFillSide(facingSide, otherSide, otherSector, SideTexture.Upper, isFront);
        }
        else
        {
            if ((facingSide.FloodTextures & SideTexture.Upper) == 0 && facingSide.UpperFloodKey == 0)
            {
                if (facingSide.UpperFloodKey > 0)
                    m_geometryRenderer.Portals.ClearStaticWall(facingSide.UpperFloodKey);
                if (facingSide.UpperFloodKey2 > 0)
                    m_geometryRenderer.Portals.ClearStaticWall(facingSide.UpperFloodKey2);

                facingSide.UpperFloodKey = 0;
                facingSide.UpperFloodKey2 = 0;
            }
        }
    }

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
    
    private unsafe void SetLightBufferData(IWorld world, GLBufferTexture lightBufferTexture)
    {
        lightBufferTexture.Map(data =>
        {
            float* lightBuffer = (float*)data.ToPointer();
            lightBuffer[Constants.LightBuffer.DarkIndex] = 0;
            lightBuffer[Constants.LightBuffer.FullBrightIndex] = 255;

            for (int i = 0; i < Constants.LightBuffer.ColorMapCount; i++)
                lightBuffer[Constants.LightBuffer.ColorMapStartIndex + i] = 
                    256 - ((Constants.LightBuffer.ColorMapCount - i) * 256/ Constants.LightBuffer.ColorMapCount);

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

    private void SetSideDynamicIgnore()
    {
        // Alpha textures are currently sorted on the CPU and can't be rendered statically.
        m_sideDynamicIgnore = SectorDynamic.Alpha;
    }

    private static void UpdateLookup(DynamicArray<int> array, int count)
    {
        if (array.Capacity < count)
            array.Resize(count);

        for (int i = 0; i < array.Capacity; i++)
            array.Data[i] = -1;
    }

    private void AddTransferSector(Sector sector)
    {
        if (sector.TransferHeights != null)
            AddTransferSector(sector, sector.TransferHeights.ControlSector.Id, m_transferHeightsLookup);
    }

    private static void AddTransferSector(Sector sector, int controlSectorId, LookupArray<List<Sector>> lookup)
    {
        if (!lookup.TryGetValue(controlSectorId, out var sectors))
        {
            sectors = new();
            lookup.Set(controlSectorId, sectors);
        }

        sectors.Add(sector);
    }

    private void AddLine(Line line, bool update = false)
    {
        if (line.OneSided)
        {
            if ((line.Front.Middle.Dynamic & m_sideDynamicIgnore) != 0)
                return;

            bool dynamic = line.Front.IsDynamic || line.Front.Sector.IsMoving;
            var sector = line.Front.Sector;
            if (dynamic && (sector.Floor.Dynamic == SectorDynamic.Movement || sector.Ceiling.Dynamic == SectorDynamic.Movement))
                return;

            m_geometryRenderer.SetRenderOneSided(line.Front);
            m_geometryRenderer.RenderOneSided(line.Front, out var sideVertices, out var skyVertices);
            AddSkyGeometry(line.Front, WallLocation.Middle, null, skyVertices, line.Front.Sector, update);

            if (sideVertices != null)
            {
                var wall = line.Front.Middle;
                UpdateVertices(wall.Static.GeometryData, wall.TextureHandle, wall.Static.Index, sideVertices,
                    null, line.Front, wall, true);
            }

            return;
        }

        AddSide(line.Front, true, update);
        if (line.Back != null)
            AddSide(line.Back, false, update);
    }

    private void AddSide(Side side, bool isFrontSide, bool update)
    {
        Side otherSide = side.PartnerSide!;
        if (update && (side.Sector.IsMoving || otherSide.Sector.IsMoving))
            return;

        Sector facingSector = side.Sector.GetRenderSector(TransferHeightView.Middle);
        Sector otherSector = otherSide.Sector.GetRenderSector(TransferHeightView.Middle);

        bool floorDynamic = (side.Sector.Floor.Dynamic & SectorDynamic.Movement) != 0 || (otherSide.Sector.Floor.Dynamic & SectorDynamic.Movement) != 0;
        bool ceilingDynamic = (side.Sector.Ceiling.Dynamic & SectorDynamic.Movement) != 0 || (otherSide.Sector.Ceiling.Dynamic & SectorDynamic.Movement) != 0;
        bool upper = !(ceilingDynamic && side.Upper.IsDynamic) && (side.Upper.Dynamic & m_sideDynamicIgnore) == 0;
        bool lower = !(floorDynamic && side.Lower.IsDynamic) && (side.Lower.Dynamic & m_sideDynamicIgnore) == 0;
        bool middle = !((floorDynamic || ceilingDynamic) && side.Middle.IsDynamic) && (side.Middle.Dynamic & m_sideDynamicIgnore) == 0;

        m_geometryRenderer.SetRenderTwoSided(side);

        bool upperVisible = m_geometryRenderer.UpperOrSkySideIsVisible(side, facingSector, otherSector, out bool skyHack);
        if (upper && upperVisible)
        {
            m_geometryRenderer.RenderTwoSidedUpper(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVertices, out var skyVertices, out var skyVertices2);

            // TODO this is dumb
            if (skyVertices2 != null)
            {
                // The side has to be marked to be re-calculated on movement because it can completely change how the sky is rendered.
                side.Upper.Sky = true;
                skyVertices = skyVertices2;
            }

            SetSideVertices(side, side.Upper, update, sideVertices, upperVisible, true);
            AddSkyGeometry(side, WallLocation.Upper, null, skyVertices, side.Sector, update);

            if (!update && !skyHack && (side.FloodTextures & SideTexture.Upper) != 0)
                m_geometryRenderer.Portals.AddStaticFloodFillSide(side, otherSide, otherSector, SideTexture.Upper, isFrontSide);
        }

        bool lowerVisible = m_geometryRenderer.LowerIsVisible(side, facingSector, otherSector);
        if (lower && lowerVisible)
        {
            m_geometryRenderer.RenderTwoSidedLower(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVertices, out var skyVertices);
            SetSideVertices(side, side.Lower, update, sideVertices, lowerVisible, true);
            AddSkyGeometry(side, WallLocation.Lower, null, skyVertices, side.Sector, update);

            if (!update && skyVertices == null && (side.FloodTextures & SideTexture.Lower) != 0)
                m_geometryRenderer.Portals.AddStaticFloodFillSide(side, otherSide, otherSector, SideTexture.Lower, isFrontSide);
        }

        if (middle && side.Middle.TextureHandle != Constants.NoTextureIndex)
        {
            m_geometryRenderer.RenderTwoSidedMiddle(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVertices);
            SetSideVertices(side, side.Middle, update, sideVertices, true, repeatY: false);
        }
    }

    private void AddSkyGeometry(Side? side, WallLocation wallLocation, SectorPlane? plane,
        SkyGeometryVertex[]? vertices, Sector sector, bool update)
    {
        if (vertices == null)
            return;

        if (update)
        {
            if (side != null)
                m_skyGeometry.UpdateSide(side, wallLocation, vertices);

            if (plane != null && vertices != null)
                m_skyGeometry.UpdatePlane(plane, vertices);

            return;
        }

        if (!m_skyRenderer.GetOrCreateSky(sector.SkyTextureHandle, sector.FlipSkyTexture, out var sky))
            return;

        int index = sky.Vbo.Count;
        sky.Add(vertices, vertices.Length);

        if (plane != null && vertices != null)
        {
            m_skyGeometry.AddPlane(sky, plane, vertices, index);
            return;
        }

        if (side == null)
            return;

        m_skyGeometry.AddSide(sky, side, wallLocation, vertices, index);
    }

    private static unsafe void AddVertices(DynamicArray<StaticVertex> staticVertices, LegacyVertex[] vertices)
    {
        int staticStartIndex = staticVertices.Length;
        fixed(LegacyVertex* startVertex = &vertices[0])
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                LegacyVertex* v = startVertex + i;
                staticVertices.EnsureCapacity(staticVertices.Length + vertices.Length);
                staticVertices.Data[staticStartIndex + i] = new StaticVertex(v->X, v->Y, v->Z, v->U, v->V, 
                    v->Alpha, v->AddAlpha, v->LightLevelBufferIndex);
            }

            staticVertices.SetLength(staticVertices.Length + vertices.Length);
        }
    }

    private static unsafe void CopyVertices(StaticVertex[] staticVertices, LegacyVertex[] vertices, int index)
    {
        fixed (LegacyVertex* startVertex = &vertices[0])
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                LegacyVertex* v = startVertex + i;
                staticVertices[index + i] = new StaticVertex(v->X, v->Y, v->Z, v->U, v->V,
                    v->Alpha, v->AddAlpha, v->LightLevelBufferIndex);
            }
        }
    }

    private void SetSideVertices(Side side, Wall wall, bool update, LegacyVertex[]? sideVertices, bool visible, bool repeatY)
    {
        if (sideVertices == null || !visible)
            return;
        
        if (update)
        {
            UpdateVertices(wall.Static.GeometryData, wall.TextureHandle, wall.Static.Index, sideVertices,
                null, side, wall, repeatY);
            return;
        }

        var vertices = GetTextureVertices(wall.TextureHandle, repeatY);
        SetSideData(wall, wall.TextureHandle, vertices.Length, sideVertices.Length, repeatY, null);
        AddVertices(vertices, sideVertices);
    }

    private void SetSideData(Wall wall, int textureHandle, int vboIndex, int vertexCount, bool repeatY, GeometryData? geometryData)
    {
        if (geometryData == null && !m_textureToGeometryLookup.TryGetValue(textureHandle, repeatY, out geometryData))
            return;

        wall.Static.GeometryData = geometryData;
        wall.Static.Index = vboIndex;
        wall.Static.Length = vertexCount;
    }

    private DynamicArray<StaticVertex> GetTextureVertices(int textureHandle, bool repeatY)
    {
        if (!m_textureToGeometryLookup.TryGetValue(textureHandle, repeatY, out GeometryData? geometryData))
            AllocateGeometryData(textureHandle, repeatY, out geometryData);

        return geometryData.Vbo.Data;
    }

    private void AllocateGeometryData(int textureHandle, bool repeat, out GeometryData data)
    {
        VertexArrayObject vao = new($"Geometry (handle {textureHandle}, repeat {repeat})");
        StaticVertexBuffer<StaticVertex> vbo = new($"Geometry (handle {textureHandle}, repeat {repeat})");

        Attributes.BindAndApply(vbo, vao, m_program.Attributes);

        var texture = m_textureManager.GetTexture(textureHandle, repeat);
        data = new GeometryData(textureHandle, texture, vbo, vao);
        m_geometry.Add(data);
        // Sorts textures that do not have transparent pixels first.
        // This is to get around the issue of middle textures with transparent pixels being drawn first and discarding stuff behind that should not be.
        m_geometry.Sort(TransparentGeometryCompare);
        m_textureToGeometryLookup.Add(textureHandle, repeat, data);
    }

    private void ClearData()
    {
        if (m_world != null)
        {
            m_world.SectorMoveStart -= World_SectorMoveStart;
            m_world.SectorMoveComplete -= World_SectorMoveComplete;
            m_world.SideTextureChanged -= World_SideTextureChanged;
            m_world.PlaneTextureChanged -= World_PlaneTextureChanged;
            m_world.SectorLightChanged -= World_SectorLightChanged;
            m_world = null;
        }

        foreach (var data in m_geometry)
        {
            data.Vbo.Dispose();
            data.Vao.Dispose();
        }

        m_geometry.Clear();
        m_textureToGeometryLookup.Clear();
        m_freeManager.Clear();
        m_skyRenderer.Clear();
        m_skyGeometry.Clear();
        m_runtimeGeometry.Clear();
        m_updateLightSectors.Clear();

        m_transferHeightsLookup.SetAll(null);

        ClearBufferData(m_bufferData);
        ClearBufferData(m_bufferDataClamp);

        m_bufferLists.FlushReferences();
    }

    private static void ClearBufferData(DynamicArray<DynamicArray<StaticGeometryData>?> bufferData)
    {
        for (int i = 0; i < bufferData.Length; i++)
        {
            var list = bufferData.Data[i];
            if (list != null)
                list.FlushStruct();
        }
    }

    private void AddSectorPlane(Sector sector, bool floor, bool update = false)
    {
        var renderSector = sector.GetRenderSector(TransferHeightView.Middle);
        var renderPlane = floor ? renderSector.Floor : renderSector.Ceiling;
        // Need to set to actual plane, not potential transfer heights plane.
        var plane = floor ? sector.Floor : sector.Ceiling;
        m_geometryRenderer.RenderSectorFlats(sector, renderPlane, floor, out var renderedVertices, out var renderedSkyVertices);

        AddSkyGeometry(null, WallLocation.None, plane, renderedSkyVertices, sector, update);

        if (renderedVertices == null)
            return;

        if (update)
        {
            UpdateVertices(plane.Static.GeometryData, plane.TextureHandle, plane.Static.Index,
                renderedVertices, renderPlane, null, null, true);
            return;
        }

        var vertices = GetTextureVertices(renderPlane.TextureHandle, true);
        if (m_textureToGeometryLookup.TryGetValue(renderPlane.TextureHandle, true, out var geometryData))
        {
            plane.Static.GeometryData = geometryData;
            plane.Static.Index = vertices.Length;
            plane.Static.Length = renderedVertices.Length;
        }

        AddVertices(vertices, renderedVertices);
    }

    public void Render()
    {
        if (!m_staticMode)
            return;

        UpdateRunTimeBuffers();

        if (m_lightBuffer != null)
        {
            GL.ActiveTexture(TextureUnit.Texture1);
            m_lightBuffer.BindTexture();
            m_lightBuffer.BindTexBuffer();
        }

        for (int i = 0; i < m_geometry.Count; i++)
        {
            var data = m_geometry[i];
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GLLegacyTexture texture = m_textureManager.GetTexture(data.TextureHandle, (data.Texture.Flags & Texture.TextureFlags.ClampY) == 0);
            texture.Bind();
            
            data.Vao.Bind();
            data.Vbo.Bind();
            data.Vbo.DrawArrays();
        }

        m_counter++;
    }

    private void UpdateRunTimeBuffers()
    {
        // These are textures added at run time. Need to be uploaded then cleared.
        if (m_runtimeGeometry.Length > 0)
        {
            for (int i = 0; i < m_runtimeGeometry.Length; i++)
            {
                var data = m_runtimeGeometry[i];
                data.Vbo.Bind();
                data.Vbo.UploadIfNeeded();
            }

            m_runtimeGeometry.Clear();
            m_runtimeGeometryTextures.Clear();
        }

        UpdateLights();
        UpdateBufferData();
    }

    private void UpdateBufferData()
    {
        for (int bufferIndex = 0; bufferIndex < m_bufferLists.Length; bufferIndex++)
        {
            DynamicArray<StaticGeometryData> list = m_bufferLists[bufferIndex];
            if (list.Length == 0)
                continue;

            GeometryData? geometryData = list[0].GeometryData;
            if (geometryData == null)
                continue;

            list.Sort(GeometryIndexCompare);

            int startIndex = list[0].Index;
            int lastIndex = startIndex + list[0].Length;
            for (int i = 1; i < list.Length; i++)
            {
                if (lastIndex != list[i].Index)
                {
                    geometryData.Vbo.Bind();
                    geometryData.Vbo.UploadSubData(startIndex, lastIndex - startIndex);
                    startIndex = list[i].Index;
                    lastIndex = startIndex + list[i].Length;
                    continue;
                }

                lastIndex += list[i].Length;
            }

            geometryData.Vbo.Bind();
            geometryData.Vbo.UploadSubData(startIndex, lastIndex - startIndex);

            list.Clear();
        }

        m_bufferLists.Clear();
    }

    private unsafe void UpdateLights()
    {
        if (m_updateLightSectors.Length == 0 || m_lightBuffer == null)
            return;

        m_lightBuffer.BindBuffer();
        GLMappedBuffer<float> lightBuffer = m_lightBuffer.MapWithDisposable();

        for (int i = 0; i < m_updateLightSectors.Length; i++)
        {
            Sector sector = m_updateLightSectors[i];
            float level = sector.LightLevel;
            int index = sector.Id * Constants.LightBuffer.BufferSize + Constants.LightBuffer.SectorIndexStart;
            lightBuffer[index + Constants.LightBuffer.FloorOffset] = level;
            lightBuffer[index + Constants.LightBuffer.CeilingOffset] = level;
            lightBuffer[index + Constants.LightBuffer.WallOffset] = level;
        }

        lightBuffer.Dispose();
        m_lightBuffer.UnbindBuffer();

        m_updateLightSectors.Clear();
    }

    private unsafe void UpdateTransferLight(int sectorId, float lightLevel, bool floor, LookupArray<List<Sector>?> lookup,
        GLMappedBuffer<float> lightBuffer)
    {
        return;
        if (!lookup.TryGetValue(sectorId, out var sectors))
            return;

        var lightBufferType = floor ? LightBufferType.Floor : LightBufferType.Ceiling;
        for (int i = 0; i < sectors.Count; i++)
            lightBuffer[GetLightBufferIndex(sectors[i], lightBufferType)] = lightLevel;
    }

    public void RenderSkies(RenderInfo renderInfo)
    {
        m_skyRenderer.Render(renderInfo);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        foreach (var data in m_geometry)
        {
            data.Vbo.Dispose();
            data.Vao.Dispose();
        }
        
        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void World_SectorMoveStart(object? sender, SectorPlane plane)
    {
        WorldBase world = (WorldBase)sender!;
        if (m_transferHeightsLookup.TryGetValue(plane.Sector.Id, out var sectors))
        {
            for (int i = 0; i < sectors.Count; i++)
            {
                Sector sector = sectors[i];
                HandleSectorMoveStart(world, sector.GetSectorPlane(plane.Facing));
            }
        }

        HandleSectorMoveStart(world, plane);
    }

    private void HandleSectorMoveStart(WorldBase world, SectorPlane plane)
    {
        if ((plane.Dynamic & SectorDynamic.Movement) != 0)
            return;

        bool floor = plane.Facing == SectorPlaneFace.Floor;
        bool ceiling = plane.Facing == SectorPlaneFace.Ceiling;
        StaticDataApplier.SetSectorDynamic(world, plane.Sector, floor, ceiling, SectorDynamic.Movement);
        ClearGeometryVertices(plane.Static);

        m_skyGeometry.ClearGeometryVertices(plane);

        for (int i = 0; i < plane.Sector.Lines.Count; i++)
        {
            var line = plane.Sector.Lines[i];
            if (line.Front.Upper.IsDynamic || line.Front.Upper.Sky)
            {
                ClearGeometryVertices(line.Front.Upper.Static);
                m_skyGeometry.ClearGeometryVertices(line.Front, WallLocation.Upper);
            }
            if (line.Front.Lower.IsDynamic)
            {
                ClearGeometryVertices(line.Front.Lower.Static);
                m_skyGeometry.ClearGeometryVertices(line.Front, WallLocation.Lower);
            }
            if (line.Front.Middle.IsDynamic)
            {
                ClearGeometryVertices(line.Front.Middle.Static);
                m_skyGeometry.ClearGeometryVertices(line.Front, WallLocation.Middle);
            }

            if (line.Back == null)
                continue;

            if (line.Back.Upper.IsDynamic || line.Back.Upper.Sky)
            {
                ClearGeometryVertices(line.Back.Upper.Static);
                m_skyGeometry.ClearGeometryVertices(line.Back, WallLocation.Upper);
            }
            if (line.Back.Lower.IsDynamic)
            {
                ClearGeometryVertices(line.Back.Lower.Static);
                m_skyGeometry.ClearGeometryVertices(line.Back, WallLocation.Lower);
            }
            if (line.Back.Middle.IsDynamic)
            {
                ClearGeometryVertices(line.Back.Middle.Static);
                m_skyGeometry.ClearGeometryVertices(line.Back, WallLocation.Middle);
            }
        }
    }

    private void World_SectorMoveComplete(object? sender, SectorPlane plane)
    {
        WorldBase world = (WorldBase)sender!;
        if (m_transferHeightsLookup.TryGetValue(plane.Sector.Id, out var sectors))
        {
            for (int i = 0; i < sectors.Count; i++)
            {
                Sector sector = sectors[i];
                HandleSectorMoveComplete(world, sector.GetSectorPlane(plane.Facing));
            }
        }

        HandleSectorMoveComplete(world, plane);
    }

    private void HandleSectorMoveComplete(IWorld world, SectorPlane plane)
    {
        StaticDataApplier.ClearSectorDynamicMovement(world, plane);
        bool floor = plane.Facing == SectorPlaneFace.Floor;
        m_geometryRenderer.SetBuffer(false);
        m_geometryRenderer.SetTransferHeightView(TransferHeightView.Middle);
        m_geometryRenderer.SetViewSector(DefaultSector);

        if (floor)
            m_geometryRenderer.SetRenderFloor(plane);
        else
            m_geometryRenderer.SetRenderCeiling(plane);

        AddSectorPlane(plane.Sector, floor, true);

        for (int i = 0; i < plane.Sector.Lines.Count; i++)
        {
            var line = plane.Sector.Lines[i];
            AddLine(line, true);

            if (line.Back == null)
                continue;

            CheckForFloodFill(line.Front, line.Back, line.Back.Sector, true);
            CheckForFloodFill(line.Back, line.Front, line.Front.Sector, false);
        }
    }

    private void World_SideTextureChanged(object? sender, SideTextureEvent e)
    {
        ClearGeometryVertices(e.Wall.Static);
        m_freeManager.Add(e.PreviousTextureHandle, e.Wall.Static);
        e.Wall.Static.GeometryData = null;
        m_geometryRenderer.SetTransferHeightView(TransferHeightView.Middle);
        m_geometryRenderer.SetViewSector(DefaultSector);
        AddLine(e.Side.Line, update: true);
    }

    private void World_PlaneTextureChanged(object? sender, PlaneTextureEvent e)
    {
        ClearGeometryVertices(e.Plane.Static);
        m_freeManager.Add(e.PreviousTextureHandle, e.Plane.Static);
        e.Plane.Static.GeometryData = null;
        m_geometryRenderer.SetTransferHeightView(TransferHeightView.Middle);
        m_geometryRenderer.SetViewSector(DefaultSector);
        AddSectorPlane(e.Plane.Sector, e.Plane.Facing == SectorPlaneFace.Floor, update: true);
    }

    private void World_SectorLightChanged(object? sender, Sector e)
    {
        if (m_updatelightSectorsLookup.Data[e.Id] == m_counter)
            return;

        m_updatelightSectorsLookup.Data[e.Id] = m_counter;
        m_updateLightSectors.Add(e);
    }

    private static void ClearGeometryVertices(in StaticGeometryData data)
    {
        if (data.GeometryData == null)
            return;

        ClearGeometryVertices(data.GeometryData, data.Index, data.Length);
    }

    private void UpdateVertices(GeometryData? geometryData, int textureHandle, int startIndex, LegacyVertex[] vertices,
        SectorPlane? plane, Side? side, Wall? wall, bool repeat)
    {
        if (geometryData == null)
        {
            AddRuntimeGeometry(textureHandle, vertices, plane, side, wall, repeat);
            return;
        }

        CopyVertices(geometryData.Vbo.Data.Data, vertices, startIndex);
        geometryData.Vbo.Bind();
        geometryData.Vbo.UploadSubData(startIndex, vertices.Length);
    }

    private void AddRuntimeGeometry(int textureHandle, LegacyVertex[] vertices, SectorPlane? plane, Side? side, Wall? wall, bool repeat)
    {
        if (m_freeManager.GetAndRemove(textureHandle, vertices.Length, out StaticGeometryData? existing))
        {
            if (plane != null)
                plane.Static = existing.Value;
            else if (wall != null)
                wall.Static = existing.Value;

            UpdateVertices(existing.Value.GeometryData, textureHandle, existing.Value.Index,
                vertices, plane, side, wall, repeat);
            return;
        }

        // This texture exists, append to the vbo
        if (m_textureToGeometryLookup.TryGetValue(textureHandle, repeat, out GeometryData? data))
        {
            SetRuntimeGeometryData(plane, side, wall, textureHandle, data, vertices, repeat);
            AddVertices(data.Vbo.Data, vertices);
            // TODO this causes the entire vbo to be uploaded when we could use sub-buffer
            data.Vbo.SetNotUploaded();
            if (!m_runtimeGeometryTextures.Contains(textureHandle))
            {
                m_runtimeGeometry.Add(data);
                m_runtimeGeometryTextures.Add(textureHandle);
            }
            return;
        }

        AllocateGeometryData(textureHandle, repeat, out data);
        SetRuntimeGeometryData(plane, side, wall, textureHandle, data, vertices, repeat);
        AddVertices(data.Vbo.Data, vertices);
        data.Vbo.SetNotUploaded();
        m_runtimeGeometry.Add(data);
    }

    private void SetRuntimeGeometryData(SectorPlane? plane, Side? side, Wall? wall, int textureHandle, GeometryData geometryData, LegacyVertex[] vertices, bool repeat)
    {
        if (side != null && wall != null)
        {
            SetSideData(wall, textureHandle, geometryData.Vbo.Count, vertices.Length, repeat, geometryData);
            return;
        }

        if (plane != null)
        {
            plane.Static.GeometryData = geometryData;
            plane.Static.Index = geometryData.Vbo.Count;
            plane.Static.Length = vertices.Length;
        }
    }

    private DynamicArray<StaticGeometryData> GetOrCreateBufferList(GeometryData geometryData)
    {
        if ((geometryData.Texture.Flags & TextureFlags.ClampY) == 0)
            return GetOrCreateBufferList(m_bufferData, geometryData.TextureHandle);

        return GetOrCreateBufferList(m_bufferDataClamp, geometryData.TextureHandle);
    }

    private DynamicArray<StaticGeometryData> GetOrCreateBufferList(DynamicArray<DynamicArray<StaticGeometryData>?> bufferData, int textureHandle)
    {
        if (bufferData.Capacity <= textureHandle)
            bufferData.Resize(textureHandle + 1024);

        var list = bufferData.Data[textureHandle];
        if (list == null)
        {
            list = new DynamicArray<StaticGeometryData>(32);
            bufferData.Data[textureHandle] = list;
        }

        m_bufferLists.Add(list);
        return list;
    }

    private static unsafe void ClearGeometryVertices(GeometryData geometryData, int startIndex, int length)
    {
        for (int i = 0; i < length; i++)
        {
            int index = startIndex + i;
            fixed (StaticVertex* vertex = &geometryData.Vbo.Data.Data[index])
            {
                vertex->Alpha = 0;
                vertex->AddAlpha = 0;
                vertex->X = 0;
                vertex->Y = 0;
                vertex->Z = 0;
                vertex->U = 0;
                vertex->V = 0;
            }
        }

        geometryData.Vbo.Bind();
        geometryData.Vbo.UploadSubData(startIndex, length);
    }
}
