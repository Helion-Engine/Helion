using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Buffer.Array.Vertex;

public abstract class VertexBufferObject<T> : ArrayBufferObject<T> where T : struct
{
    protected override string LabelPrefix => "VBO";

    protected VertexBufferObject(string label, int capacity = DefaultCapacity) : base(label, capacity)
    {
    }

    public void DrawArrays(PrimitiveType type = PrimitiveType.Triangles)
    {
        if (Count == 0)
            return;

        Precondition(Uploaded, "Forgot to upload VBO data");
        GL.DrawArrays(type, 0, Count);
    }
}

public class DynamicVertexBuffer<T> : VertexBufferObject<T> where T : struct
{
    protected override BufferUsageHint Hint => BufferUsageHint.DynamicDraw;

    public DynamicVertexBuffer(string label) : base(label)
    {
    }
}

public class StaticVertexBuffer<T> : VertexBufferObject<T> where T : struct
{
    protected override BufferUsageHint Hint => BufferUsageHint.StaticDraw;

    public StaticVertexBuffer(string label, int capacity = DefaultCapacity) : base(label, capacity)
    {
    }
}

public class StreamVertexBuffer<T> : VertexBufferObject<T> where T : struct
{
    protected override BufferUsageHint Hint => BufferUsageHint.StreamDraw;

    public StreamVertexBuffer(string label) : base(label)
    {
    }
}
