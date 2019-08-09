using System.Diagnostics;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Util
{
    public static class GLHelper
    {
        /// <summary>
        /// A constant for GL_TRUE.
        /// </summary>
        public const int GLTrue = 1;
        
        /// <summary>
        /// Throws an exception of glGetError() returns an error value.
        /// </summary>
        /// <remarks>
        /// Intended for debug builds only to assert nothing is wrong.
        /// </remarks>
        /// <param name="gl">The GL functions.</param>
        [Conditional("DEBUG")]
        public static void AssertNoGLError(IGLFunctions gl)
        {
            ErrorType error = gl.GetError();
            Invariant(error == ErrorType.None, $"OpenGL error detected: ID {(int)error}");
        }
        
        /// <summary>
        /// Attaches an object label for the provided GL object.
        /// </summary>
        /// <param name="gl">The GL functions.</param>
        /// <param name="capabilities">The GL capabilities.</param>
        /// <param name="type">The type of object.</param>
        /// <param name="objectId">The integral GL object name.</param>
        /// <param name="name">The label to attach.</param>
        [Conditional("DEBUG")]
        public static void ObjectLabel(IGLFunctions gl, GLCapabilities capabilities, ObjectLabelType type, 
            int objectId, string name)
        {
            if (name.Empty() || !capabilities.Version.Supports(4, 3))
                return;
         
            // TODO:
//            if (name.Length > capabilities.Limits.MaxLabelLength)
//                name = name.Substring(0, capabilities.Limits.MaxLabelLength);
            gl.ObjectLabel(type, objectId, name);
        }
    }
}