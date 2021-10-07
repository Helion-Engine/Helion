using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Helion.Geometry;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Capabilities;
using Helion.Render.OpenGL.Renderers.Hud;
using Helion.Render.OpenGL.Renderers.World;
using Helion.Render.OpenGL.Surfaces;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Textures.Buffer;
using Helion.Render.OpenGL.Util;
using Helion.Resources;
using Helion.Util.Configs;
using Helion.Window;
using NLog;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL
{
    /// <summary>
    /// The main renderer for handling all OpenGL calls.
    /// </summary>
    public class GLRenderer : IRenderer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        // Need a persistent handle on this to stop the GC.
        // See: https://stackoverflow.com/questions/16544511/prevent-delegate-from-being-garbage-collected
        // See: https://stackoverflow.com/questions/6193711/call-has-been-made-on-garbage-collected-delegate-in-c
        private static Action<DebugLevel, string>? LastCallbackReference;
        private static DebugProc? LastCallbackProcReference;
        
        public IWindow Window { get; }
        private readonly IConfig m_config;
        private readonly IResources m_resources;
        private readonly GLTextureManager m_textureManager;
        private readonly GLDefaultRenderableSurface m_defaultSurface;
        private readonly GLHudRenderer m_hudRenderer;
        private readonly GLWorldRenderer m_worldRenderer;
        private readonly Dictionary<string, GLRenderableSurface> m_surfaces = new(StringComparer.OrdinalIgnoreCase);
        private readonly GLTextureDataBuffer m_textureDataBuffer;
        private bool m_disposed;
        
        public IRendererTextureManager Textures => m_textureManager;
        public IRenderableSurface DefaultSurface => m_defaultSurface;
        
        public GLRenderer(IConfig config, IWindow window, IResources resources)
        {
            ThrowIfNotCapable();

            m_config = config;
            Window = window;
            m_resources = resources;
            
            // This comes first because it registers a debug callback, and we
            // want that to be active in case something goes wrong on any of
            // the initializations of the following fields.
            InitializeStates(config);

            m_textureDataBuffer = new GLTextureDataBuffer(resources);
            m_textureManager = new GLTextureManager(resources, m_textureDataBuffer);
            m_hudRenderer = new GLHudRenderer(this, m_textureManager);
            m_worldRenderer = new GLWorldRenderer(m_textureManager, m_textureDataBuffer);
            m_defaultSurface = new GLDefaultRenderableSurface(this, m_hudRenderer, m_worldRenderer);
            
            m_surfaces[IRenderableSurface.DefaultName] = m_defaultSurface;
        }

        ~GLRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        private static void ThrowIfNotCapable()
        {
            if (GLCapabilities.Limits.MaxVertexShaderTextures < 1)
            {
                Log.Error($"Expected 1 vertex shader texture slots, got {GLCapabilities.Limits.MaxVertexShaderTextures}, GPU insufficient");
                throw new Exception("Insufficient number of vertex shader textures");
            }

            if (GLCapabilities.Limits.MaxFragmentShaderTextures < 2)
            {
                Log.Error($"Expected 2 fragment shader texture slots, got {GLCapabilities.Limits.MaxFragmentShaderTextures}, GPU insufficient");
                throw new Exception("Insufficient number of fragment shader textures");
            }

            if (GLCapabilities.Limits.MaxTextureUnits < 2)
            {
                Log.Error($"Expected 2 texture units, got {GLCapabilities.Limits.MaxTextureUnits}, GPU insufficient");
                throw new Exception("Insufficient number of total texture units");
            }
        }

        private static void InitializeStates(IConfig config)
        {
            GL.Enable(EnableCap.DepthTest);

            if (config.Render.Multisample > 1)
                GL.Enable(EnableCap.Multisample);
            
            if (GLCapabilities.SupportsSeamlessCubeMap)
                GL.Enable(EnableCap.TextureCubeMapSeamless);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            if (config.Developer.Render.Debug && GLCapabilities.Version.Supports(4, 3))
                SetDebugCallbackHandler();
        }

        private static void SetDebugCallbackHandler()
        {
            DebugMessageCallback((level, msg) =>
            {
                switch (level)
                {
                    case DebugLevel.Low:
                        Log.Debug($"[GL] {msg}");
                        break;
                    case DebugLevel.Medium:
                        Log.Warn($"[GL] {msg}");
                        break;
                    case DebugLevel.High:
                        Log.Error($"[GL] {msg}");
                        break;
                }
            });
        }

        private static void DebugMessageCallback(Action<DebugLevel, string> callback)
        {
            // This is the unfortunate thing about OpenGL being a big state
            // machine. We'll assume this is bad.
            if (LastCallbackReference != null || LastCallbackProcReference != null)
            {
                Log.Error("Trying to register an OpenGL debug callback more than once");
                return;
            }
            
            // If we don't do this, the GC will collect it (since the lambda
            // below won't) and then we end up with a SystemAccessViolation.
            // See the docs of this variable for more information.
            LastCallbackReference = callback;
            LastCallbackProcReference = (_, _, _, severity, length, message, _) =>
            {
                switch (severity)
                {
                    case DebugSeverity.DebugSeverityHigh:
                        callback(DebugLevel.High, Marshal.PtrToStringAnsi(message, length));
                        break;
                    case DebugSeverity.DebugSeverityMedium:
                        callback(DebugLevel.Medium, Marshal.PtrToStringAnsi(message, length));
                        break;
                    case DebugSeverity.DebugSeverityLow:
                        callback(DebugLevel.Low, Marshal.PtrToStringAnsi(message, length));
                        break;
                }
            };

            Log.Debug("Registered OpenGL debug callback handler");
            GL.DebugMessageCallback(LastCallbackProcReference, IntPtr.Zero);
        }

        public IRenderableSurface GetOrCreateSurface(string name, Dimension dimension)
        {
            if (m_surfaces.TryGetValue(name, out GLRenderableSurface? existingSurface))
                return existingSurface;
        
            var surface = GLRenderableFramebufferTextureSurface.Create(this, dimension, m_hudRenderer, m_worldRenderer);
            if (surface == null)
                return m_defaultSurface;
            
            m_surfaces[name] = surface;
            return surface;
        }

        public void PerformThrowableErrorChecks()
        {
#if DEBUG
            GLUtil.ThrowIfGLError();
#endif
        }

        public void FlushPipeline()
        {
            GL.Finish();
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

            m_hudRenderer.Dispose();
            m_worldRenderer.Dispose();
            m_textureManager.Dispose();
            
            foreach (GLRenderableSurface surface in m_surfaces.Values)
                surface.Dispose();
            m_surfaces.Clear();
            
            m_textureDataBuffer.Dispose();
            
            // Note: This technically gets disposed of twice, but that is okay
            // because the API says it's okay to call it twice. This way if
            // anything ever changes, a disposing issue won't be introduced.
            m_defaultSurface.Dispose();
            
            m_disposed = true;
        }
    }
}
