using Helion.Render.OpenGL.Buffer.Vao;
using Helion.Render.OpenGL.Buffer.Vbo;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture;
using Helion.Render.Shared;
using Helion.World;
using Helion.World.Geometry;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;

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

        private void SetUniforms(Camera camera)
        {
            // We have no model transformation as the world geometry is already
            // in the world space.
            Matrix4 view = camera.ViewMatrix();

            // TODO: Get actual values for the fov, aspect, zNear, and zFar.
            Matrix4.CreatePerspectiveFieldOfView(Util.MathHelper.QuarterPi, 1.3333f, 0.1f, 10000.0f, out Matrix4 projection);

            // Unfortunately, C#/OpenTK do not follow C++/glm/glsl conventions
            // of left multiplication. Instead of doing p * v * m, it has to
            // be done in the opposite direction (m * v * p) due to a design
            // decision according to a lead developer. This will seem wrong
            // for anyone used to the C++/OpenGL way of multiplying.
            Matrix4 mvp = view * projection;

            shaderProgram.SetMatrix("mvp", mvp);
        }

        private void RenderBspTree(WorldBase world, Camera camera)
        {
            ushort index = world.BspTree.RootIndex;
            RenderNode(world, index, camera);
        }

        private void RenderNode(WorldBase world, ushort index, Camera camera)
        {
            if (BspNodeCompact.IsSubsectorIndex(index))
            {
                Subsector subsector = world.BspTree.Subsectors[index & BspNodeCompact.SubsectorMask];
                RenderSubsector(world, subsector);
                return;
            }

            BspNodeCompact node = world.BspTree.Nodes[index];

            if (node.Splitter.OnRight(camera.PositionFixed))
            {
                RenderNode(world, node.RightChild, camera);
                if (IsVisible(node.LeftChild))
                    RenderNode(world, node.LeftChild, camera);
            }
            else
            {
                RenderNode(world, node.LeftChild, camera);
                if (IsVisible(node.RightChild))
                    RenderNode(world, node.RightChild, camera);
            }
        }

        private void RenderSubsector(WorldBase world, Subsector subsector)
        {
            RenderSubsectorSegments(subsector);
            RenderSubsectorFlats(subsector);
        }

        private void RenderSubsectorSegments(Subsector subsector)
        {
            // OpenGL uses uint's for the texture indices, so the chances of a
            // texture allocating a bitwise match to this are probably either
            // near, or at zero.
            // TODO: This has no memory of our previous draw invocation, we
            // should consider making this a variable inside the object.
            int lastTextureHandle = -1;

            foreach (Segment segment in subsector.ClockwiseEdges)
            {
                foreach (WorldVertexWall wall in renderableGeometry.Segments[segment.Id].Walls)
                {
                    if (wall.NoTexture)
                        continue;

                    // TODO: We can do better by analyzing every segment first,
                    // as a bad case could be a series of segments changing the
                    // textures every second texture (ex: A, B, A, B, etc).
                    if (wall.TextureHandle != lastTextureHandle)
                    {
                        vbo.BindAndDrawIfNotEmpty();

                        textureManager.BindTextureIndex(TextureTarget.Texture2D, wall.TextureHandle);
                        lastTextureHandle = wall.TextureHandle;
                    }

                    vbo.Add(wall.TopLeft, wall.BottomLeft, wall.TopRight);
                    vbo.Add(wall.TopRight, wall.BottomLeft, wall.BottomRight);
                }
            }

            vbo.BindAndDrawIfNotEmpty();
        }

        private void RenderSubsectorFlat(WorldVertexFlat flat, ref int lastTextureHandle)
        {
            if (flat.TextureHandle != lastTextureHandle)
            {
                vbo.BindAndDrawIfNotEmpty();

                textureManager.BindTextureIndex(TextureTarget.Texture2D, flat.TextureHandle);
                lastTextureHandle = flat.TextureHandle;
            }

            for (int i = 1; i < flat.Fan.Length - 1; i++)
                vbo.Add(flat.Root, flat.Fan[i], flat.Fan[i + 1]);
        }

        private void RenderSubsectorFlats(Subsector subsector)
        {
            // See RenderSubsectorSegments() for more info on this.
            int lastTextureHandle = -1;

            RenderSubsectorFlat(renderableGeometry.Subsectors[subsector.Id].Floor, ref lastTextureHandle);
            RenderSubsectorFlat(renderableGeometry.Subsectors[subsector.Id].Ceiling, ref lastTextureHandle);
            vbo.BindAndDrawIfNotEmpty();
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

        public void Render(WorldBase world, Camera camera)
        {
            if (ShouldUpdateToNewWorld(world))
            {
                renderableGeometry.Load(world);
                lastProcessedWorld = new WeakReference(world);
            }

            shaderProgram.BindAnd(() => 
            {
                SetUniforms(camera);
                RenderBspTree(world, camera);
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
