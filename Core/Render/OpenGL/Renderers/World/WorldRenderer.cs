using Helion.Render.OpenGL.Buffer.Vao;
using Helion.Render.OpenGL.Buffer.Vbo;
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

        public WorldRenderer(GLTextureManager glTextureManager)
        {
            textureManager = glTextureManager;
            renderableGeometry = new WorldRenderableGeometry(glTextureManager);
            vao.BindAttributesTo(vbo);
            shaderProgram = WorldShader.CreateShaderProgramOrThrow(vao);
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
            // TODO: Get config values for zNear and zFar.
            float aspectRatio = (float)renderInfo.Viewport.Width / renderInfo.Viewport.Height;
            Matrix4.CreatePerspectiveFieldOfView(Util.MathHelper.QuarterPi, aspectRatio, 0.1f, 10000.0f, out Matrix4 projection);

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

        private void RenderBspTree(WorldBase world, RenderInfo renderInfo)
        {
            ushort index = world.BspTree.RootIndex;
            RenderNode(world, index, renderInfo.CameraInfo.PositionFixed);
        }

        private void RenderGeometry()
        {
            foreach (var handleListPair in textureIdToVertices)
            {
                int textureHandle = handleListPair.Key;
                textureManager.BindTextureIndex(TextureTarget.Texture2D, textureHandle);

                DynamicArray<WorldVertex> vertices = handleListPair.Value;
                vbo.Clear();
                for (int i = 0; i < vertices.Length; i++)
                    vbo.Add(vertices[i]);
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
            // TODO: We can do better by analyzing every segment first,
            // as a bad case could be a series of segments changing the
            // textures every second texture (ex: A, B, A, B, etc).

            foreach (Segment segment in subsector.ClockwiseEdges)
            {
                foreach (WorldVertexWall wall in renderableGeometry.Segments[segment.Id].Walls)
                {
                    if (wall.NoTexture)
                        continue;

                    if (textureIdToVertices.TryGetValue(wall.TextureHandle, out DynamicArray<WorldVertex> L))
                    {
                        L.Add(wall.TopLeft);
                        L.Add(wall.BottomLeft);
                        L.Add(wall.TopRight);
                        L.Add(wall.TopRight);
                        L.Add(wall.BottomLeft);
                        L.Add(wall.BottomRight);
                    }
                    else
                    {
                        // Note: We only ever allocate the list once.
                        DynamicArray<WorldVertex> newList = new DynamicArray<WorldVertex>(256);
                        newList.Add(wall.TopLeft);
                        newList.Add(wall.BottomLeft);
                        newList.Add(wall.TopRight);
                        newList.Add(wall.TopRight);
                        newList.Add(wall.BottomLeft);
                        newList.Add(wall.BottomRight);
                        textureIdToVertices[wall.TextureHandle] = newList;
                    }
                }
            }
        }

        private void RenderSubsectorFlat(WorldVertexFlat flat)
        {
            int texHandle = flat.TextureHandle;

            if (textureIdToVertices.TryGetValue(texHandle, out DynamicArray<WorldVertex> L))
            {
                for (int i = 0; i < flat.Fan.Length - 1; i++)
                {
                    L.Add(flat.Root);
                    L.Add(flat.Fan[i]);
                    L.Add(flat.Fan[i + 1]);
                }
            }
            else
            {
                // This is only allocated once.
                DynamicArray<WorldVertex> newList = new DynamicArray<WorldVertex>();
                for (int i = 0; i < flat.Fan.Length - 1; i++)
                {
                    newList.Add(flat.Root);
                    newList.Add(flat.Fan[i]);
                    newList.Add(flat.Fan[i + 1]);
                }
                textureIdToVertices[texHandle] = newList;
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
            else
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

        public void Render(WorldBase world, RenderInfo renderInfo)
        {
            if (ShouldUpdateToNewWorld(world))
            {
                renderableGeometry.Load(world);
                lastProcessedWorld = new WeakReference(world);
            }

            foreach (DynamicArray<WorldVertex> dynamicArray in textureIdToVertices.Values)
                dynamicArray.Clear();

            vao.Bind();
            vbo.Bind();
            shaderProgram.Bind();

            shaderProgram.SetInt("boundTexture", 0);
            SetUniforms(renderInfo);
            RenderBspTree(world, renderInfo);
            RenderGeometry();

            shaderProgram.Unbind();
            vbo.Unbind();
            vao.Unbind();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
