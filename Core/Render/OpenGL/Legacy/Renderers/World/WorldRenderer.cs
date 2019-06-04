using Helion.Render.OpenGL.Legacy.Texture;
using Helion.Render.OpenGL.Shared.Buffer.Vao;
using Helion.Render.OpenGL.Shared.Buffer.Vbo;
using Helion.Render.OpenGL.Shared.Shader;
using Helion.Render.Shared;
using Helion.World;
using Helion.World.Geometry;
using NLog;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Legacy.Renderers.World
{
    public class WorldRenderer : IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        protected bool disposed;
        private GLLegacyTextureManager textureManager;
        private WeakReference? lastProcessedWorld = null;
        private WorldRenderableGeometry renderableGeometry = new WorldRenderableGeometry();
        private VertexArrayObject vao = new VertexArrayObject(
            new VaoAttributeF("pos", 0, 3, VertexAttribPointerType.Float),
            new VaoAttributeF("uv", 1, 2, VertexAttribPointerType.Float),
            new VaoAttributeF("alpha", 2, 1, VertexAttribPointerType.Float),
            new VaoAttributeF("unitBrightness", 3, 1, VertexAttribPointerType.Float)
        );
        private StreamVertexBuffer<WorldVertex> vbo = new StreamVertexBuffer<WorldVertex>();
        private ShaderProgram shaderProgram;

        public WorldRenderer(GLLegacyTextureManager glTextureManager)
        {
            textureManager = glTextureManager;
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
            // TODO: Make MVP matrix from camera pos/view.
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
            // TODO: Draw sprites by texture type
            // TODO: Draw elements by texture type
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
                renderableGeometry.Load(world);

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
