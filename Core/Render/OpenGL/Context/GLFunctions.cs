using System;
using Helion.Render.OpenGL.Context.Types;

namespace Helion.Render.OpenGL.Context
{
    public abstract class GLFunctions
    {
        public abstract void BindBuffer(BufferType type, int bufferId);
        public abstract void BindVertexArray(int vaoId);
        public abstract void BlendFunc(BlendingFactorType sourceFactor, BlendingFactorType destFactor);
        public abstract void BufferData<T>(BufferType bufferType, int totalBytes, T[] data, BufferUsageType usageType) where T : struct;
        public abstract void Clear(ClearType type);
        public abstract void ClearColor(float r, float g, float b, float a);
        public abstract void CullFace(CullFaceType type);
        public abstract void DebugMessageCallback(Action<DebugLevel, string> callback);
        public abstract void DeleteBuffer(int bufferId);
        public abstract void DeleteTexture(int textureId);
        public abstract void DeleteVertexArray(int vaoId);
        public abstract void Enable(EnableType type);
        public abstract void EnableVertexAttribArray(int index);
        public abstract void FrontFace(FrontFaceType type);
        public abstract int GenBuffer();
        public abstract int GenVertexArray();
        public abstract ErrorType GetError();
        public abstract int GetInteger(GetIntegerType type);
        public abstract string GetString(GetStringType type);
        public abstract string GetString(GetStringType type, int index);
        public abstract void ObjectLabel(ObjectLabelType type, int objectId, string name);
        public abstract void PolygonMode(PolygonFaceType faceType, PolygonModeType fillType);
        public abstract void VertexAttribIPointer(int index, int size, VertexAttributeIntegralPointerType type, int stride, int offset);
        public abstract void VertexAttribPointer(int index, int byteLength, VertexAttributePointerType type, bool normalized, int stride, int offset);
        public abstract void Viewport(int x, int y, int width, int height);
    }
}