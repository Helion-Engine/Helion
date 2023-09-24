using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Maps;
using Helion.Maps.Components;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Vertex;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using ClearBufferMask = OpenTK.Graphics.ES30.ClearBufferMask;
using GL = OpenTK.Graphics.ES30.GL;

namespace Helion.UI.Shaders.GlowingMap;

public class GlowingMapPipeline : IRenderPipeline
{
    private readonly UIWindow m_window;

    private readonly StaticVertexBuffer<HudVertex> m_vboHud = new("Hud");
    private readonly VertexArrayObject m_vaoHud = new("Hud");
    private readonly GlowingMapHudProgram m_programHud = new();
    
    private readonly StreamVertexBuffer<HudVertex> m_vboLogo = new("Logo");
    private readonly VertexArrayObject m_vaoLogo = new("Logo");
    private readonly GlowingMapHudProgram m_programLogo = new();
    
    private readonly StaticVertexBuffer<LineVertex> m_vboLine = new("Lines");
    private readonly VertexArrayObject m_vaoLine = new("Lines");
    private readonly GlowingMapLineProgram m_programLine = new();
    
    private readonly VertexGraph m_vertexGraph;
    private readonly vec2 m_playerStart;
    private float m_accumulatedRadians;
    private DateTime? m_time;

    public GlowingMapPipeline(UIWindow window, IMap map)
    {
        m_window = window;
        m_playerStart = GetCenterPoint(map);
        m_vertexGraph = new(map);
        UploadHud();
        BindLogoAttributes();
        UploadLines();
    }

    private static vec2 GetCenterPoint(IMap map)
    {
        Vec2D sum = map.GetVertices().Aggregate(new Vec2D(0, 0), (current, vertex) => current + vertex.Position);
        Vec2F average = sum.Float / map.GetVertices().Count;
        return new(average.X, average.Y);
    }

    // private static vec2 FindPlayerStart(IMap map)
    // {
    //     foreach (IThing thing in map.GetThings())
    //     {
    //         if (thing.ThingId != 0) 
    //             continue;
    //         
    //         var (x, y) = thing.Position.XY.Float;
    //         return new(x, y);
    //     }
    //
    //     return vec2.Zero;
    // }

    private void UploadHud()
    {
        m_vboHud.Add(new HudVertex((-1, -1, 0), (0, 0)));
        m_vboHud.Add(new HudVertex((-1, 1, 0), (0, 1)));
        m_vboHud.Add(new HudVertex((1, -1, 0), (1, 0)));
        
        m_vboHud.Add(new HudVertex((1, -1, 0), (1, 0)));
        m_vboHud.Add(new HudVertex((-1, 1, 0), (0, 1)));
        m_vboHud.Add(new HudVertex((1, 1, 0), (1, 1)));
        
        Attributes.BindAndApply(m_vboHud, m_vaoHud, m_programHud.Attributes);
    }
    
    private void BindLogoAttributes()
    {
        Attributes.BindAndApply(m_vboLogo, m_vaoLogo, m_programLogo.Attributes);
    }

    private void UploadLines()
    {
        foreach (VertexNode rootNode in m_vertexGraph.Islands)
        {
            foreach (var (start, end) in m_vertexGraph.GetUniqueEdges(rootNode))
            {
                LineVertex startVertex = new(start.Position.To3D(0), true, start.DistanceFromRoot);
                LineVertex endVertex = new(end.Position.To3D(0), true, end.DistanceFromRoot);
                m_vboLine.Add(startVertex);
                m_vboLine.Add(endVertex);
            }
        }
        
        Attributes.BindAndApply(m_vboLine, m_vaoLine, m_programLine.Attributes);
    }
    
    private mat4 MakeWindowMvp(Vector2i windowSize, vec2 dim, vec2 offset, float rotateRadians)
    {
        mat4 rotate = mat4.Rotate(rotateRadians, new(0, 0, 1));
        mat4 scale = mat4.Scale(dim.x, dim.y, 0);
        mat4 translate = mat4.Translate(offset.x, offset.y, 0);
        mat4 model = translate * scale * rotate;
        
        mat4 view = mat4.Identity;
        
        mat4 projection = mat4.Ortho(0, windowSize.X, windowSize.Y, 0, 1, -1);
        return projection * view * model;
    }

    private mat4 MakeMvp(Vector2i windowSize)
    {
        const float Zoom = 0.1f;
        
        mat4 model = mat4.Identity;
        
        mat4 view = mat4.Scale(Zoom, Zoom, 1) * mat4.Translate(-m_playerStart.x, -m_playerStart.y, 0);
        
        (float halfX, float halfY) = (windowSize.X * 0.5f, windowSize.Y * 0.5f);
        mat4 projection = mat4.Ortho(-halfX, halfX, -halfY, halfY, 1, -1);
        
        return projection * view * model;
    }

    private float CalculateTime()
    {
        const int DurationMs = 7000;
        
        if (m_time == null)
        {
            m_time = DateTime.Now;
            return 0;
        }
        
        TimeSpan delta = DateTime.Now - m_time.Value;
        return MathHelper.Clamp((float)(delta.TotalMilliseconds / DurationMs), 0.0f, 1.0f);
    }

    public void Restart()
    {
        m_time = null;
        m_accumulatedRadians = 0;
    }

    private void DrawBackground()
    {
        Vec3F colorScale = (0.2f, 0.2f, 0.2f);
        
        m_window.BackgroundTexture.Bind();
        
        m_programHud.Bind();
        m_programHud.Mvp(mat4.Identity);
        m_programHud.Tex(TextureUnit.Texture0);
        m_programHud.ColorScale(colorScale);
        
        m_vaoHud.Bind();
        m_vboHud.UploadIfNeeded();
        m_vboHud.DrawArrays();
    }
    
    private void DrawLogo(Vector2i windowSize, float frac)
    {
        const int LogoDim = 100;
        const float rotationDampener = 0.1f; // Increase to spin faster
        
        GL.Clear(ClearBufferMask.DepthBufferBit);

        var radians = MathF.PI * frac * rotationDampener;
        m_accumulatedRadians += radians;
        mat4 mvp = MakeWindowMvp(windowSize, new(LogoDim, LogoDim), new(windowSize.X - LogoDim/2, windowSize.Y - LogoDim/2), m_accumulatedRadians);
        
        m_vboLogo.Clear();
        m_vboLogo.Add(new HudVertex((-0.5f, -0.5f, 0), (0, 0))); 
        m_vboLogo.Add(new HudVertex((-0.5f, 0.5f, 0), (0, 1)));
        m_vboLogo.Add(new HudVertex((0.5f, -0.5f, 0), (1, 0)));
        m_vboLogo.Add(new HudVertex((0.5f, -0.5f, 0), (1, 0)));
        m_vboLogo.Add(new HudVertex((-0.5f, 0.5f, 0), (0, 1)));
        m_vboLogo.Add(new HudVertex((0.5f, 0.5f, 0), (1, 1)));
        
        m_window.HelionTexture.Bind();
        
        m_programLogo.Bind();
        m_programLogo.Mvp(mvp);
        m_programLogo.Tex(TextureUnit.Texture0);
        m_programLogo.ColorScale(Vec3F.One);
        
        m_vaoLogo.Bind();
        m_vboLogo.UploadIfNeeded();
        m_vboLogo.DrawArrays();
        m_vaoLogo.Unbind();
    }
    
    private void DrawLines(mat4 mvp, float frac)
    {
        GL.Clear(ClearBufferMask.DepthBufferBit);
        
        m_programLine.Bind();
        m_programLine.Mvp(mvp);
        m_programLine.RootDistance(frac * m_vertexGraph.MaxIslandDistance);
        m_programLine.OffsetZ(0.0f);

        m_vaoLine.Bind();
        m_vboLine.UploadIfNeeded();
        m_vboLine.DrawArrays(PrimitiveType.Lines);
        m_vaoLine.Unbind();
    }

    public void Render(Vector2i windowSize)
    {
        mat4 linesMvp = MakeMvp(windowSize);
        float frac = CalculateTime();

        DrawBackground();
        DrawLogo(windowSize, frac);
        DrawLines(linesMvp, frac);
    }
}