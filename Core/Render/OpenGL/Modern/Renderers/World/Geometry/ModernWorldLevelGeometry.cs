using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GlmSharp;
using Helion.Geometry.Planes;
using Helion.Geometry.Vectors;
using Helion.Render.Common;
using Helion.Render.OpenGL.Modern.Textures;
using Helion.Render.OpenGL.Pipeline;
using Helion.Render.OpenGL.Primitives;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using Helion.World.Geometry.Walls;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Modern.Renderers.World.Geometry
{
    public class ModernWorldLevelGeometry : IDisposable
    {
        private readonly ModernGLTextureManager m_textureManager;
        private readonly RenderPipeline<ModernWorldGeometryShader, ModernWorldGeometryVertex> m_pipeline;
        private readonly Dictionary<int, IntRange> m_subsectorToOffset = new();
        private readonly Dictionary<int, int> m_wallToOffset = new();
        private bool m_disposed;

        public ModernWorldLevelGeometry(IWorld world, ModernGLTextureManager textureManager)
        {
            m_textureManager = textureManager;
            m_pipeline = new($"World ({world.MapName}) geometry pipeline", BufferUsageHint.DynamicDraw, PrimitiveType.Triangles);

            PreloadTextures(world);
            PopulateSectors(world);
            PopulateWalls(world);
        }

        private void PreloadTextures(IWorld world)
        {
            var textureIndices = world.BspTree.Subsectors
                .SelectMany(s => new[] { s.Sector.Floor, s.Sector.Ceiling })
                .Select(p => p.TextureHandle)
                .ToHashSet();
            
            var wallTextureIndices = world.Walls
                .Select(w => w.TextureHandle)
                .ToHashSet();
            
            textureIndices.UnionWith(wallTextureIndices);
            m_textureManager.ArchiveTextureManager.LoadTextureImages(textureIndices);
        }

        private void PopulateSectors(IWorld world)
        {
            foreach (Subsector subsector in world.BspTree.Subsectors)
            {
                List<ModernWorldGeometryVertex> vertices = CreateSectorVertices(subsector);
                m_subsectorToOffset[subsector.Id] = IntRange.FromCount(m_pipeline.Vbo.Count, vertices.Count);
                m_pipeline.Vbo.AddRange(vertices);
            }
        }

        private List<ModernWorldGeometryVertex> CreateSectorVertices(Subsector subsector)
        {
            List<ModernWorldGeometryVertex> list = new();
            List<Vec2D> vertices = subsector.ClockwiseEdges.Select(v => v.Start).ToList();
            ByteColor color = new ByteColor(Color.White);
            float alpha = 1.0f;

            AddSectorPlane(subsector.Sector, true);
            AddSectorPlane(subsector.Sector, false);

            return list;
            
            void AddSectorPlane(Sector sector, bool floor)
            {
                SectorPlane sectorPlane = floor ? sector.Floor : sector.Ceiling;
                PlaneD plane = sectorPlane.Plane;
                Texture texture = m_textureManager.ArchiveTextureManager.GetTexture(sectorPlane.TextureHandle);
                ModernGLTexture glTexture = m_textureManager.Get(texture);
                BindlessHandle handle = new BindlessHandle(glTexture.Handle);

                List<ModernWorldGeometryVertex> worldVertices = vertices.Select(v =>
                {
                    Vec3F pos = v.To3D(plane.ToZ(v)).Float;
                    Vec2F uv = (pos.X / glTexture.Image.Width, pos.Y / glTexture.Image.Height);
                    return new ModernWorldGeometryVertex(pos, uv, handle, color, alpha);
                }).ToList();

                ModernWorldGeometryVertex root = worldVertices.FirstOrDefault();
                if (floor)
                {
                    for (int i = worldVertices.Count - 2; i > 0; i--)
                    {
                        list.Add(root);
                        list.Add(worldVertices[i + 1]);
                        list.Add(worldVertices[i]);
                    }
                }
                else
                {
                    for (int i = 2; i < worldVertices.Count; i++)
                    {
                        list.Add(root);
                        list.Add(worldVertices[i - 1]);
                        list.Add(worldVertices[i]);
                    } 
                }
            }
            
        }

        private List<ModernWorldGeometryVertex> CreateWallVertices(Wall wall)
        {
            List<ModernWorldGeometryVertex> list = new();
            
            Texture texture = m_textureManager.ArchiveTextureManager.GetTexture(wall.TextureHandle);
            ModernGLTexture glTexture = m_textureManager.Get(texture);
            BindlessHandle handle = new BindlessHandle(glTexture.Handle);
            ByteColor color = new ByteColor(Color.White);
            float alpha = 1.0f;
            
            List<(Vec3F pos, Vec2F uv)> coords = CalculateWallCoordinates();
            ModernWorldGeometryVertex topLeft = new(coords[0].pos, coords[0].uv, handle, color, alpha);
            ModernWorldGeometryVertex topRight = new(coords[1].pos, coords[1].uv, handle, color, alpha);
            ModernWorldGeometryVertex bottomLeft = new(coords[2].pos, coords[2].uv, handle, color, alpha);
            ModernWorldGeometryVertex bottomRight = new(coords[3].pos, coords[3].uv, handle, color, alpha);
            
            list.Add(topLeft);
            list.Add(bottomLeft);
            list.Add(topRight);
            
            list.Add(topRight);
            list.Add(bottomLeft);
            list.Add(bottomRight);

            return list;

            List<(Vec3F pos, Vec2F uv)> CalculateWallCoordinates()
            {
                List<(Vec3F pos, Vec2F uv)> coordinates = new();

                Sector frontSector = wall.Side.Sector;
                float floorZ = 0;
                float ceilingZ = 0;
                Vec2F start = wall.Side.Line.StartPosition.Float;
                Vec2F end = wall.Side.Line.EndPosition.Float;

                switch (wall.Location)
                {
                case WallLocation.Upper:
                    floorZ = (float)wall.Side.Line.Back!.Sector.Ceiling.Z;
                    ceilingZ = (float)frontSector.Ceiling.Z;
                    break;
                case WallLocation.Middle:
                    if (wall.Side.Line.TwoSided)
                    {
                        double backFloorZ = wall.Side.Line.Back!.Sector.Floor.Z;
                        double backCeilingZ = wall.Side.Line.Back!.Sector.Ceiling.Z;
                        floorZ = (float)(frontSector.Floor.Z.Max(backFloorZ));
                        ceilingZ = (float)(frontSector.Ceiling.Z.Min(backCeilingZ));
                    }
                    else
                    {
                        floorZ = (float)frontSector.Floor.Z;
                        ceilingZ = (float)frontSector.Ceiling.Z;  
                    }
                    break;
                case WallLocation.Lower:
                    floorZ = (float)frontSector.Floor.Z;
                    ceilingZ = (float)wall.Side.Line.Back!.Sector.Floor.Z;
                    break;
                default:
                    throw new Exception("Unexpected wall enumeration when generating rendering data");
                }

                Vec3F topLeftPos = start.To3D(ceilingZ);
                Vec3F topRightPos = end.To3D(ceilingZ);
                Vec3F bottomLeftPos = start.To3D(floorZ);
                Vec3F bottomRightPos = end.To3D(floorZ);
                Vec2F topLeftUV = (0.0f, 0.0f);
                Vec2F topRightUV = (1.0f, 0.0f);
                Vec2F bottomLeftUV = (0.0f, 1.0f);
                Vec2F bottomRightUV = (1.0f, 1.0f);
                
                bool front = wall.Side.IsFront;
                coordinates.Add((front ? topLeftPos : topRightPos, topLeftUV));
                coordinates.Add((front ? topRightPos : topLeftPos, topRightUV));
                coordinates.Add((front ? bottomLeftPos : bottomRightPos, bottomLeftUV));
                coordinates.Add((front ? bottomRightPos : bottomLeftPos, bottomRightUV)); 

                return coordinates;
            }
        }

        private void PopulateWalls(IWorld world)
        {
            foreach (Wall wall in world.Walls)
            {
                // TODO: Need to fix later if a texture changes.
                if (wall.TextureHandle == Constants.NoTextureIndex)
                    continue;
                
                List<ModernWorldGeometryVertex> vertices = CreateWallVertices(wall);
                m_wallToOffset[wall.Id] = m_pipeline.Vbo.Count;
                m_pipeline.Vbo.AddRange(vertices);
            }
        }

        ~ModernWorldLevelGeometry()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void Draw(Camera camera, mat4 mvp)
        {
            if (m_disposed)
                return;
            
            m_pipeline.Draw(s =>
            {
                s.Mvp.Set(mvp);
            });
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;
            
            m_pipeline.Dispose();

            m_disposed = true;
        }
    }
}
