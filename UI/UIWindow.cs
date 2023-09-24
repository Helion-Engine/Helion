using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Locator;
using Helion.UI.Shaders;
using Helion.UI.Shaders.GlowingMap;
using Helion.Util;
using Helion.Util.Configs.Impl;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Helion.UI;

public class UIWindow : GameWindow
{
    public Texture HelionTexture = null!;
    public Texture BackgroundTexture = null!;
    private readonly IMap m_map;
    private readonly string m_backgroundPath;
    private IRenderPipeline m_pipeline = null!;
    
    public UIWindow(string[] args) : 
        base(GameWindowSettings.Default, new NativeWindowSettings { WindowState = WindowState.Fullscreen })
    {
        ArchiveCollection archiveCollection = new(new FilesystemArchiveLocator(), new Config(), new DataCache());
        if (!archiveCollection.Load(new[] { args[1] }, args[0]))
            throw new($"Cannot load either {args[0]} or {args[1]}");
        
        m_map = archiveCollection.FindMap(args[2]) ?? throw new($"Cannot find map {args[1]}");
        
        m_backgroundPath = args[3];
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        LoadTextures();
        LoadPipeline();
    }

    private void LoadTextures()
    {
        HelionTexture = new("Images/helion.png");
        BackgroundTexture = new(m_backgroundPath);
    }

    private void LoadPipeline()
    {
        m_pipeline = new GlowingMapPipeline(this, m_map);
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        
        if (e.Key == Keys.R)
            m_pipeline.Restart();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Enable(EnableCap.Multisample);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One); // Additive
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha); // Classic

        GL.ClearColor(0, 0, 0, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.Viewport(0, 0, Size.X, Size.Y);
        
        m_pipeline.Render(Size);
        
        SwapBuffers();
    }

    public static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: app.exe <wad> <map> <background.png>");
            Console.Error.WriteLine("    app.exe C:/DOOM.WAD E2M4 C:/background.png");
            Environment.Exit(1); 
        }
        
        UIWindow window = new(args);
        window.Run();
    }
}