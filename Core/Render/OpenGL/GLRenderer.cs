using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using GlmSharp;
using Helion.Render.Commands;
using Helion.Render.Commands.Types;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Renderers;
using Helion.Render.OpenGL.Renderers.Legacy.Hud;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Util;
using Helion.Render.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL
{
    public class GLRenderer : IRenderer, IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static bool InfoPrinted;

        private readonly Config m_config;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly GLCapabilities m_capabilities;
        private readonly IGLFunctions gl;
        private readonly IGLTextureManager m_textureManager;
        private readonly WorldRenderer m_worldRenderer;
        private readonly HudRenderer m_hudRenderer;

        public IImageDrawInfoProvider ImageDrawInfoProvider => m_textureManager.ImageDrawInfoProvider;

        public GLRenderer(Config config, ArchiveCollection archiveCollection, IGLFunctions functions)
        {
            m_config = config;
            m_archiveCollection = archiveCollection;
            m_capabilities = new GLCapabilities(functions);
            gl = functions;

            PrintGLInfo(m_capabilities);
            SetGLDebugger();
            SetGLStates();
            WarnForInvalidStates(config);

            GLRenderType renderType = GetRenderTypeFromCapabilities();
            m_textureManager = CreateTextureManager(renderType, archiveCollection);
            m_worldRenderer = CreateWorldRenderer(renderType);
            m_hudRenderer = CreateHudRenderer(renderType);
        }

        ~GLRenderer()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }

        public static mat4 CalculateMvpMatrix(RenderInfo renderInfo, float fovRadiansX)
        {
            Precondition(fovRadiansX > 0 && fovRadiansX <= MathHelper.Pi, $"Field of view X radians are out of range: {fovRadiansX}");
            
            float w = renderInfo.Viewport.Width;
            float h = renderInfo.Viewport.Height;
            float aspectRatio = w / h;
            float fovY = Camera.FieldOfViewXToY(fovRadiansX, aspectRatio);
            
            mat4 model = mat4.Identity;
            mat4 view = renderInfo.Camera.CalculateViewMatrix();
            // TODO: Should base this off of the actor radius and config view
            //       distance or the length of the level.
            mat4 projection = mat4.PerspectiveFov(fovY, w, h, 7.9f, 8192.0f);
            
            return projection * view * model;
        }

        private static void WarnForInvalidStates(Config config)
        {
            if (config.Engine.Render.Anisotropy.Enable)
            {
                if (config.Engine.Render.Anisotropy.Value <= 1.0)
                    Log.Warn("Anisotropic filter is enabled, but the desired value of 1.0 (equal to being off). Set a higher value than 1.0!");

                if (config.Engine.Render.Filter != FilterType.Trilinear)
                    Log.Warn("Anisotropic filter should be paired with trilinear filtering (you have {0}), you will not get the best results!", config.Engine.Render.Filter.Get());
            }
        }

        public void Render(RenderCommands renderCommands)
        {
            m_hudRenderer.Clear();
            
            // This has to be tracked beyond just the rendering command, and it
            // also prevents something from going terribly wrong if there is no
            // call to setting the viewport.
            Rectangle viewport = new Rectangle(0, 0, 800, 600);
            IReadOnlyList<IRenderCommand> commands = renderCommands.GetCommands();

            for (int i = 0; i < commands.Count; i++)
            {
                IRenderCommand renderCommand = commands[i];
                switch (renderCommand)
                {
                case ClearRenderCommand cmd:
                    HandleClearCommand(cmd);
                    break;
                case DrawImageCommand cmd:
                    HandleDrawImage(cmd);
                    break;
                case DrawShapeCommand cmd:
                    HandleDrawShape(cmd);
                    break;
                case DrawTextCommand cmd:
                    HandleDrawText(cmd);
                    break;
                case DrawWorldCommand cmd:
                    HandleRenderWorldCommand(cmd, viewport);
                    break;
                case ViewportCommand cmd:
                    HandleViewportCommand(cmd, out viewport);
                    break;
                default:
                    Fail($"Unsupported render command type: {renderCommand}");
                    break;
                }
            }
            
            DrawHudImagesIfAnyQueued(viewport);
            
            GLHelper.AssertNoGLError(gl);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
        
        private static void PrintGLInfo(GLCapabilities capabilities)
        {
            if (InfoPrinted)
                return;
            
            Log.Info("OpenGL v{0}", capabilities.Version);
            Log.Info("OpenGL Shading Language: {0}", capabilities.Info.ShadingVersion);
            Log.Info("OpenGL Vendor: {0}", capabilities.Info.Vendor);
            Log.Info("OpenGL Hardware: {0}", capabilities.Info.Renderer);
            Log.Info("OpenGL Extensions: {0}", capabilities.Extensions.Count);

            InfoPrinted = true;
        }

        private void SetGLStates()
        {
            gl.Enable(EnableType.DepthTest);
            
            if (m_config.Engine.Render.Multisample.Enable)
                gl.Enable(EnableType.Multisample);

            if (m_capabilities.Version.Supports(3, 2))
                gl.Enable(EnableType.TextureCubeMapSeamless);

            gl.Enable(EnableType.Blend);
            gl.BlendFunc(BlendingFactorType.SrcAlpha, BlendingFactorType.OneMinusSrcAlpha);

            gl.Enable(EnableType.CullFace);
            gl.FrontFace(FrontFaceType.CounterClockwise);
            gl.CullFace(CullFaceType.Back);
            gl.PolygonMode(PolygonFaceType.FrontAndBack, PolygonModeType.Fill);
        }
        
        [Conditional("DEBUG")]
        private void SetGLDebugger()
        {
            // Note: This means it's not set if `RenderDebug` changes. As far
            // as I can tell, we can't unhook actions, but maybe we could do
            // some glDebugControl... setting that changes them all to don't
            // cares if we have already registered a function? See:
            // https://www.khronos.org/opengl/wiki/GLAPI/glDebugMessageControl
            if (!m_capabilities.Version.Supports(4, 3) || !m_config.Engine.Developer.RenderDebug) 
                return;
            
            gl.Enable(EnableType.DebugOutput);
            gl.Enable(EnableType.DebugOutputSynchronous);
            
            // TODO: We should filter messages we want to get since this could
            //       pollute us with lots of messages and we wouldn't know it.
            //       https://www.khronos.org/opengl/wiki/GLAPI/glDebugMessageControl
            gl.DebugMessageCallback((level, message) =>
            {
                switch (level)
                {
                case DebugLevel.Low:
                    Log.Warn("OpenGL minor issue: {0}", message);
                    return;
                case DebugLevel.Medium:
                    Log.Error("OpenGL warning: {0}", message);
                    return;
                case DebugLevel.High:
                    Log.Error("OpenGL major error: {0}", message);
                    return;
                default:
                    throw new ArgumentOutOfRangeException($"Unsupported enumeration debug callback: {level}");
                }
            });
        }

        private GLRenderType GetRenderTypeFromCapabilities()
        {
            if (m_capabilities.Version.Supports(3, 1))
            {
                Log.Info("Using legacy OpenGL renderer");
                return GLRenderType.Legacy;
            }
            
            throw new HelionException("OpenGL implementation too old or not supported");
        }
        
        private IGLTextureManager CreateTextureManager(GLRenderType renderType, ArchiveCollection archiveCollection)
        {
            switch (renderType)
            {
            case GLRenderType.Modern:
                throw new NotImplementedException("Modern GL renderer not implemented yet");
            case GLRenderType.Standard:
                throw new NotImplementedException("Standard GL renderer not implemented yet");
            default:
                return new LegacyGLTextureManager(m_config, m_capabilities, gl, archiveCollection);
            }
        }

        private WorldRenderer CreateWorldRenderer(GLRenderType renderType)
        {
            switch (renderType)
            {
            case GLRenderType.Modern:
                throw new NotImplementedException("Modern GL renderer not implemented yet");
            case GLRenderType.Standard:
                throw new NotImplementedException("Standard GL renderer not implemented yet");
            default:
                Precondition(m_textureManager is LegacyGLTextureManager, "Created wrong type of texture manager (should be legacy)");
                return new LegacyWorldRenderer(m_config, m_archiveCollection, m_capabilities, gl, (LegacyGLTextureManager)m_textureManager);
            }
        }

        private HudRenderer CreateHudRenderer(GLRenderType renderType)
        {
            switch (renderType)
            {
            case GLRenderType.Modern:
                throw new NotImplementedException("Modern GL renderer not implemented yet");
            case GLRenderType.Standard:
                throw new NotImplementedException("Standard GL renderer not implemented yet");
            default:
                Precondition(m_textureManager is LegacyGLTextureManager, "Created wrong type of texture manager (should be legacy)");
                return new LegacyHudRenderer(m_capabilities, gl, (LegacyGLTextureManager)m_textureManager);
            }
        }
        
        private void HandleClearCommand(ClearRenderCommand clearRenderCommand)
        {
            Color color = clearRenderCommand.ClearColor;
            gl.ClearColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
            
            ClearType clearMask = 0;
            if (clearRenderCommand.Color)
                clearMask |= ClearType.ColorBufferBit;
            if (clearRenderCommand.Depth)
                clearMask |= ClearType.DepthBufferBit;
            if (clearRenderCommand.Stencil)
                clearMask |= ClearType.StencilBufferBit;
            
            gl.Clear(clearMask);
        }
        
        private void HandleDrawImage(DrawImageCommand cmd)
        {
            if (cmd.AreaIsTextureDimension)
            {
                Vec2I topLeft = new Vec2I(cmd.DrawArea.X, cmd.DrawArea.Y);
                m_hudRenderer.DrawImage(cmd.TextureName, topLeft, cmd.MixColor, cmd.MultiplyColor, cmd.Alpha);
            }
            else
                m_hudRenderer.DrawImage(cmd.TextureName, cmd.DrawArea, cmd.MixColor, cmd.MultiplyColor, cmd.Alpha);
        }

        private void HandleDrawShape(DrawShapeCommand cmd)
        {
            m_hudRenderer.DrawShape(cmd.Rectangle, cmd.Color, cmd.Alpha);
        }
        
        private void HandleDrawText(DrawTextCommand cmd)
        {
            m_hudRenderer.DrawText(cmd.Text, cmd.FontName, cmd.FontSize, cmd.Location, cmd.Alpha);
        }
        
        private void HandleRenderWorldCommand(DrawWorldCommand cmd, Rectangle currentViewport)
        {
            DrawHudImagesIfAnyQueued(currentViewport);
            
            RenderInfo renderInfo = new RenderInfo(cmd.Camera, cmd.GametickFraction, currentViewport, cmd.ViewerEntity);
            m_worldRenderer.Render(cmd.World, renderInfo);
        }

        private void HandleViewportCommand(ViewportCommand viewportCommand, out Rectangle currentViewport)
        {
            Vec2I offset = viewportCommand.Offset;
            Dimension dimension = viewportCommand.Dimension;
            currentViewport = new Rectangle(offset.X, offset.Y, dimension.Width, dimension.Height);
            
            gl.Viewport(offset.X, offset.Y, dimension.Width, dimension.Height);
        }
        
        private void DrawHudImagesIfAnyQueued(Rectangle viewport)
        {
            m_hudRenderer.Render(viewport);
        }

        private void ReleaseUnmanagedResources()
        {
            m_textureManager.Dispose();
            m_hudRenderer.Dispose();
            m_worldRenderer.Dispose();
        }
    }
}