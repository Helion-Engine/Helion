using System;
using Helion.Render.OpenGL.Context.Types;

namespace Helion.Render.OpenGL.Context
{
    public interface IGLFunctions
    {
        void BindBuffer(BufferType type, int bufferId);
        void BindVertexArray(int vaoId);
        void BlendFunc(BlendingFactorType sourceFactor, BlendingFactorType destFactor);
        void BufferData<T>(BufferType bufferType, int totalBytes, T[] data, BufferUsageType usageType) where T : struct;
        void Clear(ClearType type);
        void ClearColor(float r, float g, float b, float a);
        void CullFace(CullFaceType type);
        void DebugMessageCallback(Action<DebugLevel, string> callback);
        void DeleteBuffer(int bufferId);
        void DeleteTexture(int textureId);
        void DeleteVertexArray(int vaoId);
        void DrawArrays(PrimitiveDrawType triangles, int startIndex, int count);
        void Enable(EnableType type);
        void EnableVertexAttribArray(int index);
        void FrontFace(FrontFaceType type);
        int GenBuffer();
        int GenVertexArray();
        ErrorType GetError();
        int GetInteger(GetIntegerType type);
        string GetString(GetStringType type);
        string GetString(GetStringType type, int index);
        void ObjectLabel(ObjectLabelType type, int objectId, string name);
        void PolygonMode(PolygonFaceType faceType, PolygonModeType fillType);
        void VertexAttribIPointer(int index, int size, VertexAttributeIntegralPointerType type, int stride, int offset);
        void VertexAttribPointer(int index, int byteLength, VertexAttributePointerType type, bool normalized, int stride, int offset);
        void Viewport(int x, int y, int width, int height);
    }
}