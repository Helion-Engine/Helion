using System;
using System.Diagnostics;
using Helion.Geometry.Vectors;
using Helion.RenderNew.Interfaces.World;
using Helion.RenderNew.OpenGL.Buffers;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.Renderers.World.Geometry;

public class WorldGeometryRenderer : IDisposable
{
    private readonly ShaderStorageBufferObject<SectorPlaneRenderData> m_sectorPlaneSsbo = new("Sector plane SSBO", 8, BufferUsageHint.DynamicDraw);
    private readonly ShaderStorageBufferObject<WallRenderData> m_wallSsbo = new("Wall SSBO", 8, BufferUsageHint.DynamicDraw);
    private bool m_disposed;

    ~WorldGeometryRenderer()
    {
        ReleaseUnmanagedResources();
    }

    internal void UpdateTo(IRenderableWorld world)
    {
        UpdateAllSectors(world);
        UpdateAllLines(world);
    }

    private void UpdateAllSectors(IRenderableWorld world)
    {
        DynamicArray<SectorPlaneRenderData> data = new(1024);
        
        foreach (IRenderableSector sector in world.GetRenderableSectors())
        {
            // The floor always comes first when indexing based on sector index.
            foreach (IRenderableSectorPlane plane in new[] { sector.GetFloor(), sector.GetCeiling() })
            {
                SectorPlaneRenderData renderData = new(plane.GetZ(), plane.GetZ(), sector.GetLightLevel(), plane.GetTextureIdx());
                data.Add(renderData);
            }
        }

        m_sectorPlaneSsbo.Bind();
        m_sectorPlaneSsbo.ClearAndUpload(data);
        m_sectorPlaneSsbo.Unbind();
    }

    private void UpdateAllLines(IRenderableWorld world)
    {
        DynamicArray<WallRenderData> data = new(4096);
        
        // We always want to add 6 walls in this order:
        //     0. Front lower
        //     1. Front middle
        //     2. Front upper
        //     3. Back lower
        //     4. Back middle
        //     5. Back upper
        // Indexing into the line/side/wall at the time of writing are based on this.
        foreach (IRenderableLine line in world.GetRenderableLines())
        {
            AddNewSide(line.GetFront(), line.GetBack(), true);
            AddNewSide(line.GetBack(), line.GetFront(), false);
        }

        m_wallSsbo.Bind();
        m_wallSsbo.ClearAndUpload(data);
        m_wallSsbo.Unbind();
        
        void AddNewSide(IRenderableSide? side, IRenderableSide? oppositeSide, bool isFront)
        {
            IRenderableSector? facingSector = side?.GetSector();
            IRenderableSector? oppositeSector = oppositeSide?.GetSector();
            
            AddNewWall(side?.GetLower(), side, facingSector, oppositeSector, isFront, WallSection.Lower);
            AddNewWall(side?.GetMiddle(), side, facingSector, oppositeSector, isFront, WallSection.Middle);
            AddNewWall(side?.GetUpper(), side, facingSector, oppositeSector, isFront, WallSection.Upper);
        }

        void AddNewWall(IRenderableWall? wall, IRenderableSide? side, IRenderableSector? facingSector, IRenderableSector? oppositeSector,
            bool isFront, WallSection section)
        {
            WallRenderData wallData = new();
            
            if (wall != null)
            {
                Debug.Assert(side != null, "A wall must belong to a side");
                Debug.Assert(facingSector != null, "A wall must belong to a side that has a sector");
                
                int textureIdx = wall.GetTextureIndex();
                int lightLevel = side?.GetLightLevel() ?? 0;
                int facingSectorIdx = facingSector?.GetIndex() ?? 0;
                int oppositeSectorIdx = oppositeSector?.GetIndex() ?? facingSectorIdx;
                Vec2F scroll = side?.GetScroll() ?? (0, 0);
                int flags = WallRenderData.MakeFlags(isFront, section);
            
                wallData = new(textureIdx, lightLevel, facingSectorIdx, oppositeSectorIdx, scroll.X, scroll.Y, flags);
            }
            
            data.Add(wallData);  
        }
    }

    public void Render(WorldRenderingInfo renderInfo)
    {
        // TODO
    }

    private void ReleaseUnmanagedResources()
    {
        if (m_disposed)
            return;
        
        m_sectorPlaneSsbo.Dispose();
        m_wallSsbo.Dispose();
        
        m_disposed = true;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}
