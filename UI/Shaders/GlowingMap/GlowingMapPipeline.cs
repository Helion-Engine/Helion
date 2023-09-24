using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Maps;
using Helion.Maps.Components;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Vertex;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Helion.UI.Shaders.GlowingMap;

public class GlowingMapPipeline : IRenderPipeline
{
    private readonly StaticVertexBuffer<LineVertex> m_vbo = new("Lines");
    private readonly VertexArrayObject m_vao = new("Lines");
    private readonly GlowingMapLineProgram m_program = new();
    private readonly vec2 m_playerStart;
    private DateTime? m_time;
    
    public GlowingMapPipeline(IMap map)
    {
        UploadLines(map);
        m_playerStart = FindPlayerStart(map);
     
        Attributes.BindAndApply(m_vbo, m_vao, m_program.Attributes);
    }

    private void UploadLines(IMap map)
    {
        foreach (ILine line in map.GetLines())
        {
            float z = line.OneSided ? 0 : 0.5f;
            Vec3F start = line.GetStart().Position.To3D(z).Float;
            Vec3F end = line.GetEnd().Position.To3D(z).Float;
            m_vbo.Add(new LineVertex(start, line.OneSided, true));
            m_vbo.Add(new LineVertex(end, line.OneSided, false));
        }
    }

    private static vec2 FindPlayerStart(IMap map)
    {
        foreach (IThing thing in map.GetThings())
        {
            if (thing.ThingId != 0) 
                continue;
            
            var (x, y) = thing.Position.XY.Float;
            return new(x, y);
        }

        return vec2.Zero;
    }

    private mat4 MakeMvp(Vector2i windowSize)
    {
        mat4 scale = mat4.Scale(0.25f, 0.25f, 1);
        mat4 translate = mat4.Translate(-m_playerStart.x, -m_playerStart.y, 0);
        mat4 model = scale * translate;
        
        mat4 view = mat4.Identity;

        (float halfX, float halfY) = (windowSize.X * 0.5f, windowSize.Y * 0.5f);
        mat4 projection = mat4.Ortho(-halfX, halfX, -halfY, halfY, 1, -1);
        
        return projection * view * model;
    }

    private float CalculateTime()
    {
        const int DurationMs = 1000;
        
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
    }

    public void Render(Vector2i windowSize)
    {
        mat4 mvp = MakeMvp(windowSize);
        float frac = CalculateTime(); 
        
        m_program.Bind();
        m_program.Mvp(mvp);
        m_program.FracDone(frac);
        
        m_vao.Bind();
        m_vbo.UploadIfNeeded();
        
        // Pass 1: Draw the outline
        // m_program.OnlyOutline(true);
        // m_program.OffsetZ(0.5f);
        // m_vbo.DrawArrays(PrimitiveType.Lines);

        // Pass 2: Fill in the line colors
        m_program.OnlyOutline(false);
        m_program.OffsetZ(0.2f);
        m_vbo.DrawArrays(PrimitiveType.Lines);
    }
}