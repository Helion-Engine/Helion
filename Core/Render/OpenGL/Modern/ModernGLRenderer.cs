using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Helion.Geometry;
using Helion.Render.Common;
using Helion.Render.Common.Framebuffer;
using Helion.Render.OpenGL.Capabilities;
using Helion.Render.OpenGL.Modern.FrameBuffers;
using Helion.Render.OpenGL.Modern.Textures;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using NLog;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Modern
{
    /// <summary>
    /// An implementation of a modern OpenGL renderer.
    /// </summary>
    public class ModernGLRenderer : IRenderer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public IWindow Window { get; }
        public IFramebuffer Default => m_defaultFramebuffer;
        public readonly ModernGLTextureManager Textures;
        private readonly Config m_config;
        private readonly GLCapabilities m_capabilities;
        private readonly ModernGlDefaultFramebuffer m_defaultFramebuffer;
        private readonly Dictionary<string, ModernGlFramebuffer> m_framebuffers = new(StringComparer.OrdinalIgnoreCase);
        private bool m_disposed;
        
        /// <summary>
        /// Holds a reference to the last registered callback. Required due to
        /// how the GC works or else we trigger SystemAccessViolations.
        /// </summary>
        /// <remarks>
        /// See: https://stackoverflow.com/questions/16544511/prevent-delegate-from-being-garbage-collected
        /// See: https://stackoverflow.com/questions/6193711/call-has-been-made-on-garbage-collected-delegate-in-c
        /// </remarks>
        private DebugProc? m_lastCallbackProcReference;

        private ModernGLRenderer(GLCapabilities capabilities, IWindow window, Config config, 
            ArchiveCollection archiveCollection)
        {
            m_config = config;
            m_capabilities = capabilities;
            Window = window;
            Textures = new ModernGLTextureManager(capabilities, config, archiveCollection);
            m_defaultFramebuffer = new ModernGlDefaultFramebuffer(window, Textures);

            PrintGLInfo();
            SetGLDebugger();
            SetGLStates();
            WarnForInvalidStates();
        }

        /// <summary>
        /// Creates a new renderer. If the system is not sufficient enough to
        /// use a modern renderer, this returns null.
        /// </summary>
        /// <param name="window">The window that this renderer will belong to.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="archiveCollection">The collection of resources.</param>
        /// <returns>The renderer, or null if it cannot be used.</returns>
        public static ModernGLRenderer? Create(IWindow window, Config config, ArchiveCollection archiveCollection)
        {
            GLCapabilities capabilities = new();
            if (!config.Developer.ForceModernRenderer && !capabilities.SupportsModernRenderer)
                return null;

            return new ModernGLRenderer(capabilities, window, config, archiveCollection);
        }

        ~ModernGLRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        private void PrintGLInfo()
        {
            Log.Info($"OpenGL v{m_capabilities.Version}");
            Log.Info($"OpenGL Shading Language: {m_capabilities.Info.ShadingVersion}");
            Log.Info($"OpenGL Vendor: {m_capabilities.Info.Vendor}");
            Log.Info($"OpenGL Hardware: {m_capabilities.Info.Renderer}");
            Log.Info($"OpenGL Extensions: {m_capabilities.Extensions.Count}");
        }

        private void SetGLStates()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.StencilTest);
            GL.Enable(EnableCap.ScissorTest);
            GL.Enable(EnableCap.TextureCubeMapSeamless);
            if (m_config.Render.Multisample.Enable)
                GL.Enable(EnableCap.Multisample);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        private void SetGLDebugger()
        {
            // For now, we will only debug if it's requested at the start.
            if (!m_config.Developer.RenderDebug)
                return;

            // If we've already attached one, don't do it again. The field is
            // not intended for this purpose, but it can be used to see if we
            // have already been here.
            if (m_lastCallbackProcReference != null)
                return;
            
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            
            // If we don't do this, the GC will collect it (since the lambda
            // below won't) and then we end up with a SystemAccessViolation.
            // See the docs of this variable for more information.
            m_lastCallbackProcReference = (_, _, _, severity, length, message, _) =>
            {
                switch (severity)
                {
                case DebugSeverity.DebugSeverityHigh:
                    Log.Error(Marshal.PtrToStringAnsi(message, length));
                    break;
                case DebugSeverity.DebugSeverityMedium:
                    Log.Warn(Marshal.PtrToStringAnsi(message, length));
                    break;
                case DebugSeverity.DebugSeverityLow:
                case DebugSeverity.DontCare:
                case DebugSeverity.DebugSeverityNotification:
                    break;
                }
            };

            GL.DebugMessageCallback(m_lastCallbackProcReference, IntPtr.Zero);
        }
        
        private void WarnForInvalidStates()
        {
            if (m_config.Render.Anisotropy.Enable)
            {
                if (m_config.Render.Anisotropy.Value <= 1.0)
                    Log.Warn("Anisotropic filter is enabled, but the desired value of 1.0 (equal to being off). Set a higher value than 1.0!");

                if (m_config.Render.TextureFilter != FilterType.Trilinear)
                    Log.Warn($"Anisotropic filter should be paired with trilinear filtering (you have {m_config.Render.TextureFilter}), you will not get the best results!");
            }
        }

        private ModernGlFramebuffer CreateFramebuffer(string name, Dimension dimension)
        {
            Precondition(!m_framebuffers.ContainsKey(name), "Trying to create a framebuffer with name that exists");

            ModernGlTextureFramebuffer framebuffer = new(name, dimension, Textures, this);
            m_framebuffers[name] = framebuffer;
            return framebuffer;
        }
        
        public IFramebuffer GetOrCreateFrameBuffer(string name, Dimension dimension)
        {
            IFramebuffer? existingFrameBuffer = GetFrameBuffer(name);
            if (existingFrameBuffer != null)
                return existingFrameBuffer;
            
            return CreateFramebuffer(name, dimension);
        }

        public IFramebuffer? GetFrameBuffer(string name)
        {
            return m_framebuffers.TryGetValue(name, out ModernGlFramebuffer? fb) ? fb : null;
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

            Textures.Dispose();

            foreach ((_, ModernGlFramebuffer fb) in m_framebuffers)
                fb.Dispose();
            m_framebuffers.Clear();
            m_defaultFramebuffer.Dispose();

            m_disposed = true;
        }
    }
}
