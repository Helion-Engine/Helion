using Helion.Geometry;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Capabilities;
using Helion.Resources;
using Helion.Util.Configs;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL
{
    /// <summary>
    /// The main renderer for handling all OpenGL calls.
    /// </summary>
    public abstract class GLRenderer : IRenderer
    {
        public IWindow Window { get; }
        protected readonly Config Config;
        protected readonly IResources Resources;
        
        public abstract IRendererTextureManager Textures { get; }
        public abstract IRenderableSurface DefaultSurface { get; }

        public GLRenderer(Config config, IWindow window, IResources resources)
        {
            Config = config;
            Window = window;
            Resources = resources;

            InitializeStates();
        }

        public abstract IRenderableSurface GetOrCreateSurface(string name, Dimension dimension);
        public abstract void Dispose();

        private void InitializeStates()
        {
            GL.Enable(EnableCap.DepthTest);

            if (Config.Render.Multisample.Enable)
                GL.Enable(EnableCap.Multisample);
            
            if (GLCapabilities.SupportsSeamlessCubeMap)
                GL.Enable(EnableCap.TextureCubeMapSeamless);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }
    }
}
