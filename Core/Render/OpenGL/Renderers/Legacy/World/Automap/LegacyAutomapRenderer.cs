using System;
using System.Collections.Generic;
using System.Drawing;
using GlmSharp;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using Helion.Render.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Locks;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Geometry.Lines;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Automap
{
    public class LegacyAutomapRenderer : IDisposable
    {
        private static readonly VertexArrayAttributes Attributes = new(new VertexPointerFloatAttribute("pos", 0, 2));

        private readonly IGLFunctions gl;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly LegacyAutomapShader m_shader;
        private readonly StreamVertexBuffer<vec2> m_vbo;
        private readonly VertexArrayObject m_vao;
        private readonly List<DynamicArray<vec2>> m_colorEnumToLines = new();
        private readonly List<(int start, vec3 color)> m_vboRanges = new();
        private bool m_disposed;
        
        public LegacyAutomapRenderer(GLCapabilities capabilities, IGLFunctions glFunctions, ArchiveCollection archiveCollection)
        {
            gl = glFunctions;
            m_archiveCollection = archiveCollection;
            m_vao = new VertexArrayObject(capabilities, gl, Attributes, "VAO: Attributes for Automap");
            m_vbo = new StreamVertexBuffer<vec2>(capabilities, gl, m_vao, "VBO: Geometry for Automap");
            
            foreach (AutomapColor _ in Enum.GetValues<AutomapColor>())
                m_colorEnumToLines.Add(new DynamicArray<vec2>());
            
            using (var shaderBuilder = LegacyAutomapShader.MakeBuilder(gl))
                m_shader = new LegacyAutomapShader(gl, shaderBuilder, Attributes);
        }

        ~LegacyAutomapRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void Render(IWorld world, RenderInfo renderInfo)
        {
            PopulateData(world, out Box2F worldBounds);

            m_shader.BindAnd(() =>
            {
                m_shader.Mvp.Set(gl, CalculateMvp(renderInfo, worldBounds));

                for (int i = 0; i < m_vboRanges.Count; i++)
                {
                    (int first, vec3 color) = m_vboRanges[i];
                    int count = i == m_vboRanges.Count - 1 ? m_vbo.Count - first : m_vboRanges[i + 1].start - first;
                    
                    m_shader.Color.Set(gl, color);
                    m_vao.BindAnd(() =>
                    {
                        GL.DrawArrays(PrimitiveType.Lines, first, count);
                    });
                }
            });
        }

        private mat4 CalculateMvp(RenderInfo renderInfo, Box2F worldBounds)
        {
            vec2 scale = CalculateScale(renderInfo, worldBounds);
            vec3 camera = renderInfo.Camera.Position.GlmVector;

            mat4 model = mat4.Scale(scale.x, scale.y, 1.0f);
            mat4 view = mat4.Translate(-camera.x, -camera.y, 0);
            mat4 proj = mat4.Identity;

            return model * view * proj;
        }

        private static vec2 CalculateScale(RenderInfo renderInfo, Box2F worldBounds)
        {
            // Note: we're translating to NDC coordinates, so everything should
            // end up between [-1.0, 1.0].
            (float w, float h) = worldBounds.Sides;
            (float vW, float vH) = (renderInfo.Viewport.Width, renderInfo.Viewport.Height);
            float aspect = vW / vH;

            // TODO: Do this properly...

            return new vec2(1 / vW, 1 / vH);
        }

        private void PopulateData(IWorld world, out Box2F box2F)
        {
            m_vbo.Clear();
            PopulateColoredLines(world);
            TransferLineDataIntoBuffer(out box2F);
            m_vbo.UploadIfNeeded();
        }

        private void TransferLineDataIntoBuffer(out Box2F box2F)
        {
            float minX = Single.PositiveInfinity;
            float minY = Single.PositiveInfinity;
            float maxX = Single.NegativeInfinity;
            float maxY = Single.NegativeInfinity;
            
            m_vboRanges.Clear();

            for (int i = 0; i < m_colorEnumToLines.Count; i++)
            {
                DynamicArray<vec2> lines = m_colorEnumToLines[i];
                if (lines.Empty())
                    continue;

                AutomapColor color = (AutomapColor)i;
                vec3 colorVec = color.ToColor();
                m_vboRanges.Add((m_vbo.Count, colorVec));
                
                foreach (vec2 line in lines)
                    AddLine(line);
            }

            // This is a backup case in the event there are no lines.
            if (minX == Single.PositiveInfinity)
            {
                minX = 0;
                minY = 0;
                maxX = 1;
                maxY = 1;
            }
            
            box2F = ((minX, minY), (maxX, maxY));
            
            void AddLine(vec2 line)
            {
                m_vbo.Add(line);

                if (line.x < minX)
                    minX = line.x;
                if (line.y < minY)
                    minY = line.y;
                if (line.x > maxX)
                    maxX = line.x;
                if (line.y > maxY)
                    maxY = line.y;
            }
        }

        private void PopulateColoredLines(IWorld world)
        {
            foreach (DynamicArray<vec2> lineList in m_colorEnumToLines)
                lineList.Clear();

            foreach (Line line in world.Lines)
            {
                if (!line.SeenForAutomap)
                    continue;
                
                Vec2D start = line.StartPosition;
                Vec2D end = line.EndPosition;

                if (line.Special.LineSpecialType == ZDoomLineSpecialType.DoorLockedRaise)
                {
                    LockDef? lockDef = m_archiveCollection.Definitions.LockDefininitions.GetLockDef(line.Args.Arg3);
                    if (lockDef != null)
                    {
                        if (lockDef.MapColor == Color.Red)
                        {
                            AddLine(AutomapColor.Red, start, end);
                            continue;
                        } 
                        if (lockDef.MapColor == Color.Yellow)
                        {
                            AddLine(AutomapColor.Yellow, start, end);
                            continue;
                        } 
                        if (lockDef.MapColor == Color.Blue)
                        {
                            AddLine(AutomapColor.Blue, start, end);
                            continue;
                        } 
                    }
                }
                
                if (line.Back == null)
                {
                    AddLine(AutomapColor.White, start, end);
                    continue;
                }

                // TODO: bool floorChanges = line.Front.Sector.Floor.Z != line.Back.Sector.Floor.Z;
                AddLine(AutomapColor.Gray, start, end);
            }

            void AddLine(AutomapColor color, Vec2D start, Vec2D end)
            {
                DynamicArray<vec2> array = m_colorEnumToLines[(int)color];
                array.Add(new vec2((float)start.X, (float)start.Y));
                array.Add(new vec2((float)end.X, (float)end.Y));
            }
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
            
            m_shader.Dispose();
            m_vbo.Dispose();
            m_vao.Dispose();

            m_disposed = true;
        }
    }
}
