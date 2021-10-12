using static Helion.Maps.Components.GL.GLComponents;

namespace Helion.Maps.Components.GL;

public class GLSegment
{
    public readonly uint StartVertex;
    public readonly bool IsStartVertexGL;
    public readonly uint EndVertex;
    public readonly bool IsEndVertexGL;
    public readonly uint? Linedef;
    public readonly bool IsRightSide;
    public readonly uint? PartnerSegment;

    public bool IsMiniseg => Linedef == null;

    private GLSegment(uint startVertex, bool isStartGL, uint endVertex, bool isEndGL, uint? linedef, bool isRightSide,
        uint? partnerSegment)
    {
        StartVertex = startVertex;
        IsStartVertexGL = isStartGL;
        EndVertex = endVertex;
        IsEndVertexGL = isEndGL;
        Linedef = linedef;
        IsRightSide = isRightSide;
        PartnerSegment = partnerSegment;
    }

    public static GLSegment FromV2(uint startVertex, uint endVertex, uint linedef, bool isRightSide,
        uint partnerSegment)
    {
        return new(
            startVertex & ~VertexIsGLV2,
            (startVertex & VertexIsGLV2) == VertexIsGLV2,
            endVertex & ~VertexIsGLV2,
            (endVertex & VertexIsGLV2) == VertexIsGLV2,
            linedef == LineIsMinisegV2 ? null : linedef,
            isRightSide,
            partnerSegment == NoPartnerSegmentV2 ? null : partnerSegment
        );
    }

    public static GLSegment FromV5(uint startVertex, uint endVertex, uint linedef, bool isRightSide,
        uint partnerSegment)
    {
        return new(
            startVertex & ~VertexIsGLV5,
            (startVertex & VertexIsGLV5) == VertexIsGLV5,
            endVertex & ~VertexIsGLV5,
            (endVertex & VertexIsGLV5) == VertexIsGLV5,
            linedef == LineIsMinisegV5 ? null : linedef,
            isRightSide,
            partnerSegment == NoPartnerSegmentV5 ? null : partnerSegment
        );
    }
}

