using System;
using System.Collections.Generic;
using GlmSharp;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using Helion.Render.Shared;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Geometry.Lines;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Automap
{
    public class LegacyAutomapRenderer : IDisposable
    {
        private static readonly vec3 Red = new(1, 0, 0);
        private static readonly vec3 Brown = new(0.82f, 0.7f, 0.55f);
        private static readonly vec3 Yellow = new(1, 1, 0);
        private static readonly VertexArrayAttributes Attributes = new(new VertexPointerFloatAttribute("pos", 0, 2));

        private readonly IGLFunctions gl;
        private readonly LegacyAutomapShader m_shader;
        private readonly StreamVertexBuffer<vec2> m_vbo;
        private readonly VertexArrayObject m_vao;
        private readonly DynamicArray<vec2> m_redLines = new();
        private readonly DynamicArray<vec2> m_yellowLines = new();
        private readonly DynamicArray<vec2> m_brownLines = new();
        private readonly List<(int start, vec3 color)> m_vboRanges = new();
        private bool m_disposed;
        
        public LegacyAutomapRenderer(GLCapabilities capabilities, IGLFunctions glFunctions)
        {
            gl = glFunctions;
            m_vao = new VertexArrayObject(capabilities, gl, Attributes, "VAO: Attributes for Automap");
            m_vbo = new StreamVertexBuffer<vec2>(capabilities, gl, m_vao, "VBO: Geometry for Automap");
            
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

            if (m_redLines.Length > 0)
            {
                m_vboRanges.Add((m_vbo.Count, Red));
                foreach (vec2 line in m_redLines)
                    AddLine(line);
            }
            
            if (m_brownLines.Length > 0)
            {
                m_vboRanges.Add((m_vbo.Count, Brown));
                foreach (vec2 line in m_brownLines)
                    m_vbo.Add(new vec2(line.x, line.y));
            }
            
            if (m_yellowLines.Length > 0)
            {
                m_vboRanges.Add((m_vbo.Count, Yellow));
                foreach (vec2 line in m_yellowLines)
                    m_vbo.Add(new vec2(line.x, line.y));
            }

            box2F = ((minX, maxX), (minY, maxY));
            
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
            m_redLines.Clear();
            m_brownLines.Clear();
            m_yellowLines.Clear();

            foreach (Line line in world.Lines)
            {
                Vec2D start = line.StartPosition;
                Vec2D end = line.EndPosition;
                
                if (line.Back == null)
                {
                    vec2 startVec = new vec2((float)start.X, (float)start.Y);
                    vec2 endVec = new vec2((float)end.X, (float)end.Y);
                    m_redLines.Add(startVec);
                    m_redLines.Add(endVec);
                    continue;
                }
                
                // Floor changes (brown) overrides ceiling changes (yellow).
                bool floorChanges = line.Front.Sector.Floor.Z != line.Back.Sector.Floor.Z;
                if (floorChanges)
                {
                    m_brownLines.Add(new vec2((float)start.X, (float)start.Y));
                    m_brownLines.Add(new vec2((float)end.X, (float)end.Y));
                }
                else
                {
                    m_yellowLines.Add(new vec2((float)start.X, (float)start.Y));
                    m_yellowLines.Add(new vec2((float)end.X, (float)end.Y));
                }
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
