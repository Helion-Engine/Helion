using System;
using System.Runtime.InteropServices;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using OpenTK.Graphics.OpenGL;

namespace Helion.Subsystems.OpenTK
{
    public class OpenTKGLFunctions : GLFunctions
    {
        public override void BindBuffer(BufferType type, int bufferId)
        {
            GL.BindBuffer((BufferTarget)type, bufferId);
        }

        public override void BindVertexArray(int vaoId)
        {
            GL.BindVertexArray(vaoId);
        }

        public override void BlendFunc(BlendingFactorType sourceFactor, BlendingFactorType destFactor)
        {
            GL.BlendFunc((BlendingFactor)sourceFactor, (BlendingFactor)destFactor);
        }

        public override void BufferData<T>(BufferType bufferType, int totalBytes, T[] data, BufferUsageType usageType)
        {
            GL.BufferData((BufferTarget)bufferType, totalBytes, data, (BufferUsageHint)usageType);
        }
        
        public override void Clear(ClearType type)
        {
            GL.Clear((ClearBufferMask)type);
        }
        
        public override void ClearColor(float r, float g, float b, float a)
        {
            GL.ClearColor(r, g, b, a);
        }

        public override void CullFace(CullFaceType type)
        {
            GL.CullFace((CullFaceMode)type);
        }

        public override void DebugMessageCallback(Action<DebugLevel, string> callback)
        {
            GL.DebugMessageCallback((source, type, id, severity, length, message, userParam) =>
            {
                string msg = Marshal.PtrToStringAnsi(message, length);
                callback((DebugLevel)severity, msg);
            }, IntPtr.Zero);
        }

        public override void DeleteBuffer(int bufferId)
        {
            GL.DeleteBuffer(bufferId);
        }

        public override void DeleteTexture(int textureId)
        {
            GL.DeleteTexture(textureId);
        }

        public override void DeleteVertexArray(int vaoId)
        {
            GL.DeleteVertexArray(vaoId);
        }

        public override void Enable(EnableType type)
        {
            GL.Enable((EnableCap)type);
        }

        public override void EnableVertexAttribArray(int index)
        {
            GL.EnableVertexAttribArray(index);
        }

        public override void FrontFace(FrontFaceType type)
        {
            GL.FrontFace((FrontFaceDirection)type);
        }

        public override int GenBuffer()
        {
            return GL.GenBuffer();
        }

        public override int GenVertexArray()
        {
            return GL.GenVertexArray();
        }

        public override ErrorType GetError()
        {
            ErrorCode errorCode = GL.GetError();
            return errorCode == ErrorCode.NoError ? ErrorType.None : (ErrorType)errorCode;
        }
        
        public override int GetInteger(GetIntegerType type)
        {
            return GL.GetInteger((GetPName)type);
        }

        public override string GetString(GetStringType type)
        {
            return GL.GetString((StringName)type);
        }

        public override string GetString(GetStringType type, int index)
        {
            return GL.GetString((StringNameIndexed)type, index);
        }
        
        public override void ObjectLabel(ObjectLabelType type, int objectId, string name)
        {
            GL.ObjectLabel((ObjectLabelIdentifier)type, objectId, name.Length, name);
        }

        public override void PolygonMode(PolygonFaceType faceType, PolygonModeType fillType)
        {
            GL.PolygonMode((MaterialFace)faceType, (PolygonMode)fillType);
        }

        public override void VertexAttribIPointer(int index, int size, VertexAttributeIntegralPointerType type, int stride, int offset)
        {
            GL.VertexAttribIPointer(index, size, (VertexAttribIntegerType)type, stride, new IntPtr(offset));
        }

        public override void VertexAttribPointer(int index, int byteLength, VertexAttributePointerType type, bool normalized, int stride, int offset)
        {
            GL.VertexAttribPointer(index, byteLength, (VertexAttribPointerType)type, normalized, stride, new IntPtr(offset));
        }

        public override void Viewport(int x, int y, int width, int height)
        {
            GL.Viewport(x, y, width, height);
        }
    }
}