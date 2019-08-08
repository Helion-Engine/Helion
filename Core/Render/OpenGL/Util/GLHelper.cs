using System.Diagnostics;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Enums;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Util
{
    public static class GLHelper
    {
        /// <summary>
        /// Throws an exception of glGetError() returns an error value.
        /// </summary>
        /// <remarks>
        /// Intended for debug builds only to assert nothing is wrong.
        /// </remarks>
        /// <param name="gl">The GL functions.</param>
        /// <exception cref="HelionException">The exception thrown if an error
        /// is found.</exception>
        [Conditional("DEBUG")]
        public static void AssertNoGLError(GLFunctions gl)
        {
            ErrorType error = gl.GetError();
            Invariant(error == ErrorType.None, $"OpenGL error detected: ID = {(int)error}");
        }
    }
}