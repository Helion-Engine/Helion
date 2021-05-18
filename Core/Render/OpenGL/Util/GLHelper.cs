using System;
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

        // Defined in LegacyShader as well
        const int ColorMaps = 32;
        const int ColorMapClamp = 31;
        const int ScaleCount = 16;
        const int MaxLightScale = 23;

        private static int GetLightLevelIndex(int lightLevel, int add)
        {
            int index = Math.Clamp(lightLevel / ScaleCount, 0, ScaleCount - 1);
            int startMap = (ScaleCount - index - 1) * 2 * ColorMaps / ScaleCount;
            add = MaxLightScale - Math.Clamp(add, 0, MaxLightScale);
            return Math.Clamp(startMap - add, 0, ColorMapClamp);
        }

        /// <summary>
        /// Converts a light level to the doom light level color. This is used
        /// since the mapping of light levels to visible color is usually not
        /// correct with vanilla (ex: 128 is darker than 0.5). This formula is
        /// used in the shader as well.
        /// </summary>
        /// <param name="lightLevel">The light level.</param>
        /// <returns>A value between 0.0 and 1.0 that looks close to vanilla
        /// doom.</returns>
        public static double DoomLightLevelToColor(int lightLevel) =>
            (double)(ColorMaps - GetLightLevelIndex(lightLevel, 8)) / ColorMaps;

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
            if (!name.Empty() && capabilities.Version.Supports(4, 3))
                gl.ObjectLabel(type, objectId, name);
        }
    }
}