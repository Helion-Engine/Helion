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
    private readonly IMap m_map;
    private readonly int m_shaderType;
    private IRenderPipeline m_pipeline = null!;
    
    public UIWindow(string[] args) : base(GameWindowSettings.Default, new NativeWindowSettings { WindowState = WindowState.Fullscreen })
    {
        ArchiveCollection archiveCollection = new(new FilesystemArchiveLocator(), new Config(), new DataCache());
        if (!archiveCollection.Load(new[] { args[0] }, null, false))
            throw new($"Cannot load {args[0]}");
        
        m_map = archiveCollection.FindMap(args[1]) ?? throw new($"Cannot find map {args[1]}");
        
        m_shaderType = int.Parse(args[2]);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        LoadPipeline();
        LoadTextures();
    }

    private void LoadTextures()
    {
        HelionTexture = new("Images/helion.png");
    }

    private void LoadPipeline()
    {
        m_pipeline = m_shaderType switch
        {
            1 => new GlowingMapPipeline(m_map),
            2 => throw new("Shader type 2 not written yet"),
            3 => throw new("Shader type 3 not written yet"),
            _ => throw new($"Unexpected shader type index {m_shaderType}")
        };
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
            Console.Error.WriteLine("Usage: app.exe <wad> <map> <1-3>");
            Console.Error.WriteLine("    The 1-3 argument is the shader demo to run");
            Console.Error.WriteLine("    app.exe C:/DOOM.WAD E2M4 1");
            Console.Error.WriteLine("    app.exe C:/something.wad MAP01 3");
            Environment.Exit(1); 
        }
        
        UIWindow window = new(args);
        window.Run();
    }
}