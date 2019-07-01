using Helion.Render.OpenGL.Buffer.Vao;
using Helion.Render.OpenGL.Buffer.Vbo;
using Helion.Render.OpenGL.Renderers.World.Sky;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture;
using Helion.Render.Shared;
using Helion.Util.Container;
using Helion.Util.Geometry;
using Helion.World;
using Helion.World.Geometry;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.World
{
    public class WorldRenderer : IDisposable
    {
        protected bool disposed;
        private readonly GLTextureManager textureManager;
        private readonly WorldRenderableGeometry renderableGeometry;
        private WeakReference? lastProcessedWorld = null;
        private VertexArrayObject vao = new VertexArrayObject(
            new VaoAttributeF("pos", 0, 3, VertexAttribPointerType.Float),
            new VaoAttributeF("uv", 1, 2, VertexAttribPointerType.Float),
            new VaoAttributeF("alpha", 2, 1, VertexAttribPointerType.Float),
            new VaoAttributeF("unitBrightness", 3, 1, VertexAttribPointerType.Float)
        );
        private StreamVertexBuffer<WorldVertex> vbo = new StreamVertexBuffer<WorldVertex>();
        private ShaderProgram shaderProgram;
        private Dictionary<int, DynamicArray<WorldVertex>> textureIdToVertices = new Dictionary<int, DynamicArray<WorldVertex>>();
        private WorldSkyRenderer skyRenderer;
        
        public WorldRenderer(GLTextureManager glTextureManager)
        {
            textureManager = glTextureManager;
            renderableGeometry = new WorldRenderableGeometry(glTextureManager);
            vao.BindAttributesTo(vbo);
            shaderProgram = WorldShader.CreateShaderProgramOrThrow(vao);
            skyRenderer = new WorldSkyRenderer();
        }

        ~WorldRenderer() => Dispose(false);

        private bool ShouldUpdateToNewWorld(WorldBase world)
        {
            return lastProcessedWorld == null ||
                   !lastProcessedWorld.IsAlive ||
                   !ReferenceEquals(lastProcessedWorld.Target, world);
        }

        private void SetUniforms(RenderInfo renderInfo)
        {
            // TODO: Get config values for this.
            float aspectRatio = (float)renderInfo.Viewport.Width / renderInfo.Viewport.Height;
            Matrix4.CreatePerspectiveFieldOfView(Util.MathHelper.QuarterPi, aspectRatio, 16.0f, 8192.0f, out Matrix4 projection);

            // Note that we have no model matrix, everything is already in the
            // world space.
            //
            // Unfortunately, C#/OpenTK do not follow C++/glm/glsl conventions
            // of left multiplication. Instead of doing p * v * m, it has to
            // be done in the opposite direction (m * v * p) due to a design
            // decision according to a lead developer. This will seem wrong
            // for anyone used to the C++/OpenGL way of multiplying.
            Matrix4 view = Camera.ViewMatrix(renderInfo.CameraInfo);
            Matrix4 mvp = view * projection;

            shaderProgram.SetMatrix("mvp", mvp);
        }

        private void PopulateRenderingBuffersFromBSP(WorldBase world, RenderInfo renderInfo)
        {
            ushort index = world.BspTree.RootIndex;
            RenderNode(world, index, renderInfo.CameraInfo.PositionFixed);
        }

        private void ExecuteGeometryDrawCalls()
        {
            foreach (var handleListPair in textureIdToVertices)
            {
                int textureHandle = handleListPair.Key;
                textureManager.BindTextureIndex(TextureTarget.Texture2D, textureHandle);

                DynamicArray<WorldVertex> vertices = handleListPair.Value;
                
                // TODO: Should do a 'BlockCopy' here to speed up copying, not
                //       adding each vertex one by one...
                vbo.Clear();
                foreach (WorldVertex vertex in vertices)
                    vbo.Add(vertex);
                vbo.Upload();

                vbo.DrawArrays(vertices.Length);
            }
        }

        private void RenderNode(WorldBase world, ushort index, Vec2Fixed position)
        {
            if (BspNodeCompact.IsSubsectorIndex(index))
            {
                Subsector subsector = world.BspTree.Subsectors[index & BspNodeCompact.SubsectorMask];
                RenderSubsector(subsector);
                return;
            }

            // TODO: Is it worth doing the a = index, b = a ^ 1 optimization?
            BspNodeCompact node = world.BspTree.Nodes[index];

            if (node.Splitter.OnRight(position))
            {
                RenderNode(world, node.RightChild, position);
                if (IsVisible(node.LeftChild))
                    RenderNode(world, node.LeftChild, position);
            }
            else
            {
                RenderNode(world, node.LeftChild, position);
                if (IsVisible(node.RightChild))
                    RenderNode(world, node.RightChild, position);
            }
        }

        private void RenderSubsector(Subsector subsector)
        {
            RenderSubsectorSegments(subsector);
            RenderSubsectorFlats(subsector);
        }

        private void RenderSubsectorSegments(Subsector subsector)
        {
            foreach (Segment segment in subsector.ClockwiseEdges)
            {
                foreach (WorldVertexWall wall in renderableGeometry.Segments[segment.Id].Walls)
                {
                    if (wall.NoTexture)
                        continue;

                    int texHandle = wall.TextureHandle;
                    
                    if (!textureIdToVertices.TryGetValue(texHandle, out var vertexArray))
                    {
                        vertexArray = new DynamicArray<WorldVertex>();
                        textureIdToVertices[texHandle] = vertexArray;
                    }

                    // Top left triangle (vertices 0 -> 1 -> 2).
                    vertexArray.Add(wall.TopLeft);
                    vertexArray.Add(wall.BottomLeft);
                    vertexArray.Add(wall.TopRight);
                    
                    // Bottom right triangle (vertices 2 -> 1 -> 3).
                    vertexArray.Add(wall.TopRight);
                    vertexArray.Add(wall.BottomLeft);
                    vertexArray.Add(wall.BottomRight);
                }
            }
        }

        private void RenderSubsectorFlat(WorldVertexFlat flat)
        {
            int texHandle = flat.TextureHandle;

            if (!textureIdToVertices.TryGetValue(texHandle, out var vertexArray))
            {
                vertexArray = new DynamicArray<WorldVertex>();
                textureIdToVertices[texHandle] = vertexArray;
            }

            for (int i = 0; i < flat.Fan.Length - 1; i++)
            {
                vertexArray.Add(flat.Root);
                vertexArray.Add(flat.Fan[i]);
                vertexArray.Add(flat.Fan[i + 1]);
            }
        }

        private void RenderSubsectorFlats(Subsector subsector)
        {
            RenderSubsectorFlat(renderableGeometry.Subsectors[subsector.Id].Floor);
            RenderSubsectorFlat(renderableGeometry.Subsectors[subsector.Id].Ceiling);
        }

        private bool IsVisible(ushort index)
        {
            if (BspNodeCompact.IsSubsectorIndex(index))
                return renderableGeometry.CheckSubsectorVisibility(index);
            return renderableGeometry.CheckNodeVisibility(index);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                vbo.Dispose();
                vao.Dispose();
                shaderProgram.Dispose();
            }

            disposed = true;
        }

        private void ClearRenderingBuffers()
        {
            foreach (DynamicArray<WorldVertex> dynamicArray in textureIdToVertices.Values)
                dynamicArray.Clear();
            skyRenderer.Clear();
        }

        private void RenderGeometry(RenderInfo renderInfo)
        {
            vao.BindAnd(() =>
            {
                vbo.BindAnd(() =>
                {
                    shaderProgram.BindAnd(() =>
                    {
                        shaderProgram.SetInt("boundTexture", 0);
                        SetUniforms(renderInfo);
                        ExecuteGeometryDrawCalls();
                    });
                });
            });
        }

        public void Render(WorldBase world, RenderInfo renderInfo)
        {
            if (ShouldUpdateToNewWorld(world))
            {
                renderableGeometry.Load(world);
                lastProcessedWorld = new WeakReference(world);
            }

            ClearRenderingBuffers();
            PopulateRenderingBuffersFromBSP(world, renderInfo);
            RenderGeometry(renderInfo);

            skyRenderer.Render();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
