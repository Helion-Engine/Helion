using System;
using Helion.RenderNew.OpenGL.Util;
using Helion.RenderNew.Textures;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Window;
using NLog;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew;

public class GLRenderer : IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static bool InfoPrinted;

    private readonly IWindow m_window;
    private readonly IConfig m_config;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly GLTextureManager m_textureManager;
    private bool m_disposed;

    public GLRenderer(IWindow window, IConfig config, ArchiveCollection archiveCollection)
    {
        m_window = window;
        m_config = config;
        m_archiveCollection = archiveCollection;
        m_textureManager = new(m_config, archiveCollection);
        
        SetGLDebugger();
        PrintGLInfo();
        SetGLStates();
    }

    private void SetGLDebugger()
    {
        // Note: This means it's not set if `RenderDebug` changes. As far
        // as I can tell, we can't unhook actions, but maybe we could do
        // some glDebugControl... setting that changes them all to don't
        // cares if we have already registered a function? See:
        // https://www.khronos.org/opengl/wiki/GLAPI/glDebugMessageControl
        if (!GLInfo.Extensions.DebugOutput || !m_config.Developer.Render.Debug)
            return;

        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);

        // We don't want to generate Low or Notification error messages.
        // Only receive Medium or High error messages.
        GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DebugSeverityNotification, 0, Array.Empty<int>(), false);
        GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DebugSeverityLow, 0, Array.Empty<int>(), false);
        
        GLHelper.DebugMessageCallback((level, message) =>
        {
            switch (level.Ordinal)
            {
                case 2:
                    Log.Warn("OpenGL minor issue: {0}", message);
                    return;
                case 3:
                    Log.Error("OpenGL warning: {0}", message);
                    return;
                case 4:
                    Log.Error("OpenGL major error: {0}", message);
                    return;
                default:
                    throw new($"Unsupported enumeration debug callback: {level}");
            }
        });
    }
    
    private static void PrintGLInfo()
    {
        if (InfoPrinted)
            return;

        Log.Info("OpenGL v{0}", GLInfo.GLVersion.Version);
        Log.Info("OpenGL Shading Language: {0}", GLInfo.ShadingVersion);
        Log.Info("OpenGL Vendor: {0}", GLInfo.Vendor);
        Log.Info("OpenGL Hardware: {0}", GLInfo.Renderer);
        Log.Info("OpenGL Extensions: {0}", GLInfo.Extensions.Count);

        InfoPrinted = true;
    }

    private void SetGLStates()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.ScissorTest);

        if (m_config.Render.Multisample > 1)
            GL.Enable(EnableCap.Multisample);

        GL.Enable(EnableCap.TextureCubeMapSeamless);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.Enable(EnableCap.CullFace);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.CullFace(CullFaceMode.Back);
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
    }
    
    public static void FlushPipeline()
    {
        GL.Finish();
    }

    public void Dispose()
    {
        if (m_disposed)
            return;
        
        m_textureManager.Dispose();

        GC.SuppressFinalize(this);
        m_disposed = true;
    }
}