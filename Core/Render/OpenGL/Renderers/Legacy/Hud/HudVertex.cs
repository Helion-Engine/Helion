using Helion.Graphics;
using Helion.Render.OpenGL.Vertex;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HudVertex
{
    [VertexAttribute("pos", size: 3)]
    public readonly float X;
    public readonly float Y;
    public readonly float Z;

    [VertexAttribute("uv", size: 2)]
    public readonly float U;
    public readonly float V;

    [VertexAttribute("rgbMultiplier", size: 4)]
    public readonly float MultiplierR;
    public readonly float MultiplierG;
    public readonly float MultiplierB;
    public readonly float MultiplierFactor;

    [VertexAttribute]
    public readonly float Alpha;

    [VertexAttribute("hasInvulnerability")]
    public readonly float DrawInvulnerability;

    public HudVertex(float x, float y, float z, float u, float v, byte mulR, byte mulG, byte mulB, byte mulFactor, float alpha, bool drawInvul)
    {
        X = x;
        Y = y;
        Z = z;
        U = u;
        V = v;
        MultiplierR = mulR / 255.0f;
        MultiplierG = mulG / 255.0f;
        MultiplierB = mulB / 255.0f;
        MultiplierFactor = mulFactor / 255.0f;
        Alpha = alpha;
        DrawInvulnerability = drawInvul ? 1.0f : 0.0f;
    }

    // TODO: Updated color to be RGBA, but still need to do step 2 and abandon division for RGBA.
    public HudVertex(float x, float y, float z, float u, float v, Color multiplierColor, float alpha, bool drawInvul) : 
        this(x, y, z, u, v, multiplierColor.R, multiplierColor.G, multiplierColor.B, multiplierColor.A, alpha, drawInvul)
    {
    }
}
