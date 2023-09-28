using System.Diagnostics;
using Helion.Render.OpenGLNew.Capabilities;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Util;

public static class GLUtil
{
    public const int NoObject = 0;
    public const int GLTrue = 1;

    [Conditional("DEBUG")]
    public static void AssertNoGLError()
    {
        ErrorCode error = GL.GetError();
        Debug.Assert(error == ErrorCode.NoError, $"OpenGL error detected: ID {(int)error} ({error})");
    }
    
    public static void ObjectLabel(ObjectLabelIdentifier type, int objectId, string name)
    {
        if (name != "" && GLExtensions.LabelDebug)
            GL.ObjectLabel(type, objectId, name.Length, name);
    }
}