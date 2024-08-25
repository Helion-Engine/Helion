using GlmSharp;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;
using System;
using System.Linq;

namespace Helion.Render.OpenGL.Renderers;

/// <summary>
/// Base class for level/screen transitions
/// </summary>
public abstract class TransitionProgram : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_mvpLocation;

    public TransitionProgram() : base("Transition")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_mvpLocation = Uniforms.GetLocation("mvp");
    }

    public void SetUniforms(TextureUnit unit, mat4 mvp)
    {
        Uniforms.Set(unit, m_boundTextureLocation);
        Uniforms.Set(mvp, m_mvpLocation);
    }

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec2 pos;
        layout(location = 1) in vec2 uv;

        out vec2 uvFrag;

        uniform mat4 mvp;

        void main()
        {
            uvFrag = uv;
            gl_Position = mvp * vec4(pos, 0, 1);
        }
    ";
}

/// <summary>
/// Transition that melts an image away, like in vanilla.
/// </summary>
/// <remarks>
/// Strips can take up to 15 ticks to start moving, and 27 to fall completely:
/// Gravity is 2px/tick² up to 8px/tick, so this takes 4 ticks to cover 16px.
/// After that, it takes 23 ticks to cover the remaining 184px.
/// So, this should run for 42 ticks (1.2s).
/// </remarks>
public class MeltTransitionProgram : TransitionProgram
{
    private readonly int m_ticksLocation;
    private readonly int m_stripCountLocation;
    private readonly float[] m_stripDelays;
    private readonly int m_stripDelaysLocation;

    public MeltTransitionProgram() : base()
    {
        m_ticksLocation = Uniforms.GetLocation("ticks");
        m_stripCountLocation = Uniforms.GetLocation("stripCount");
        m_stripDelays = GenerateStripDelays();
        m_stripDelaysLocation = Uniforms.GetLocation("stripDelays[0]");
    }

    private static float[] GenerateStripDelays()
    {
        Random r = new();
        int[] result = new int[256];
        // In vanilla strip delays are between 0-15 ticks
        // and each differs from the last by +-1.
        // SubSteps can be set higher here for a smaller difference between strips.
        const int SubSteps = 1;
        result[0] = r.Next(16 * SubSteps);
        for (int i = 1; i < result.Length; i++)
        {
            int offset = r.Next(-SubSteps, SubSteps + 1);
            int clamped = Math.Clamp(result[i - 1] + offset, 0, 16 * SubSteps - 1);
            result[i] = clamped;
        }
        return result.Select(x => x * 1f / SubSteps).ToArray();
    }

    public void SetUniforms(TextureUnit unit, mat4 mvp, float elapsedTicks, int stripCount)
    {
        SetUniforms(unit, mvp);
        Uniforms.Set(elapsedTicks, m_ticksLocation);
        Uniforms.Set(stripCount, m_stripCountLocation);
        Uniforms.Set(m_stripDelays, m_stripDelaysLocation);
    }

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;

        out vec4 fragColor;

        uniform float ticks;
        uniform int stripCount;
        uniform float stripDelays[256];
        uniform sampler2D boundTexture;

        void main()
        {
            vec2 uv = uvFrag;
            float sliceDelay = stripDelays[int(floor(uv.x * stripCount)) & 0xff];
            float ticksFalling = max(0, ticks - sliceDelay);
            float shift = (ticksFalling <= 4)
                ? pow(ticksFalling, 2)
                : 16 + 8 * (ticksFalling - 4);
            uv.y += shift / 200;
            fragColor = (uv.y > 1)
                ? vec4(0,0,0,0)
                : texture(boundTexture, uv.st);
        }
    ";
}

/// <summary>
/// Transition that fades an image out.
/// </summary>
public class FadeTransitionProgram : TransitionProgram
{
    private readonly int m_progressLocation;

    public FadeTransitionProgram() : base()
    {
        m_progressLocation = Uniforms.GetLocation("progress");
    }

    public void SetUniforms(TextureUnit unit, mat4 mvp, float progress)
    {
        SetUniforms(unit, mvp);
        Uniforms.Set(progress, m_progressLocation);
    }

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;

        out vec4 fragColor;

        uniform float progress;
        uniform sampler2D boundTexture;

        void main()
        {
            fragColor = texture(boundTexture, uvFrag.st);
            fragColor.a = 1 - progress;
        }
    ";
}

/// <summary>
/// Transition that just displays the last framebuffer.
/// </summary>
public class NoTransitionProgram : TransitionProgram
{
    public NoTransitionProgram() : base() { }

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;

        out vec4 fragColor;

        uniform float progress;
        uniform sampler2D boundTexture;

        void main()
        {
            fragColor = texture(boundTexture, uvFrag.st);
        }
    ";
}
