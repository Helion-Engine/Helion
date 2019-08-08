using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Enums;
using OpenTK.Graphics.OpenGL;

namespace Helion.Subsystems.OpenTK
{
    public class OpenTKGLFunctions : GLFunctions
    {
        public override void Clear(ClearType type)
        {
            GL.Clear((ClearBufferMask)type);
        }
        
        public override void ClearColor(float r, float g, float b, float a)
        {
            GL.ClearColor(r, g, b, a);
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

        public override void Viewport(int x, int y, int width, int height)
        {
            GL.Viewport(x, y, width, height);
        }
    }
}