using Helion.Render.OpenGL.Vertex;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StaticVertex
{
    [VertexAttribute("pos", size: 3)]
    public float X;
    public float Y;
    public float Z;

    [VertexAttribute("uv", size: 2)]
    public float U;
    public float V;

    [VertexAttribute]
    public float Alpha;

    [VertexAttribute]
    public float ClearAlpha;

    [VertexAttribute]
    public float LightLevelBufferIndex;
}
