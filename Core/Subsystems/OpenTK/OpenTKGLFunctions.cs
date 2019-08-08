using System;
using System.Runtime.InteropServices;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using OpenTK.Graphics.OpenGL;

namespace Helion.Subsystems.OpenTK
{
    public class OpenTKGLFunctions : GLFunctions
    {
        public override void BlendFunc(BlendingFactorType sourceFactor, BlendingFactorType destFactor)
        {
            GL.BlendFunc((BlendingFactor)sourceFactor, (BlendingFactor)destFactor);
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
        
        public override void Enable(EnableType type)
        {
            GL.Enable((EnableCap)type);
        }

        public override void FrontFace(FrontFaceType type)
        {
            GL.FrontFace((FrontFaceDirection)type);
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

        public override void PolygonMode(PolygonFaceType faceType, PolygonModeType fillType)
        {
            GL.PolygonMode((MaterialFace)faceType, (PolygonMode)fillType);
        }
        
        public override void Viewport(int x, int y, int width, int height)
        {
            GL.Viewport(x, y, width, height);
        }
    }
}