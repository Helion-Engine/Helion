using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using GlmSharp;
using Helion.Geometry;
using Helion.Graphics.Palettes;
using Helion.Render.OpenGL.Context;
using NLog;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Util;

public static class GLHelper
{
    /// <summary>
    /// A constant for GL_TRUE.
    /// </summary>
    public const int GLTrue = 1;

    /// <summary>
    /// Holds a reference to the last registered callback. Required due to
    /// how the GC works or else we trigger SystemAccessViolations.
    /// </summary>
    /// <remarks>
    /// See: https://stackoverflow.com/questions/16544511/prevent-delegate-from-being-garbage-collected
    /// See: https://stackoverflow.com/questions/6193711/call-has-been-made-on-garbage-collected-delegate-in-c
    /// </remarks>
    private static Action<LogLevel, string>? LastCallbackReference;
    private static DebugProc? LastCallbackProcReference;

    // GLM's 1D value accessor creates a whole new array for each invocation, which shows up
    // as a significant generator of garbage due to us running at very high FPS now. This is
    // a way of getting around allocations, by reusing it.
    private static readonly float[] MvpBuffer = new float[16];

    public static int ColorMapIndex(int lightLevel, int extraLight)
    {
        int lightNum = lightLevel / 8;
        int startMap = (30 - lightNum) * 2;
        return Math.Clamp((startMap - 47 / 2) - extraLight, 0, 31);
    }

    public static int DoomLightLevelToColor(int lightLevel, int extraLight)
    {
        int lightColorIndex = ColorMapIndex(lightLevel, extraLight);
        return Math.Clamp((32 - lightColorIndex) * 8, 0, 255);
    }

    /// <summary>
    /// Throws an exception of glGetError() returns an error value.
    /// </summary>
    /// <remarks>
    /// Intended for debug builds only to assert nothing is wrong.
    /// </remarks>
    [Conditional("DEBUG")]
    public static void AssertNoGLError()
    {
        ErrorCode error = GL.GetError();
        Invariant(error == ErrorCode.NoError, $"OpenGL error detected: ID {(int)error} ({error})");
    }

    /// <summary>
    /// Attaches an object label for the provided GL object.
    /// </summary>
    /// <param name="type">The type of object.</param>
    /// <param name="objectId">The integral GL object name.</param>
    /// <param name="name">The label to attach.</param>
    [Conditional("DEBUG")]
    public static void ObjectLabel(ObjectLabelIdentifier type, int objectId, string name)
    {
        if (name != "" && GLExtensions.LabelDebug)
            GL.ObjectLabel(type, objectId, name.Length, name);
    }

    /// <summary>
    /// Registers a callback, and stores it so it will not get GC'd.
    /// </summary>
    /// <param name="callback">The callback to register with OpenGL.</param>
    public static void DebugMessageCallback(Action<LogLevel, string> callback)
    {
        if (LastCallbackReference != null)
        {
#if DEBUG
            throw new("Trying to register a debug callback twice");
#endif
        }

        // If we don't do this, the GC will collect it (since the lambda
        // below won't) and then we end up with a SystemAccessViolation.
        // See the docs of this variable for more information.
        LastCallbackReference = callback;
        LastCallbackProcReference = (_, _, _, severity, length, message, _) =>
        {
            switch (severity)
            {
            case DebugSeverity.DebugSeverityHigh:
                callback(LogLevel.Error, Marshal.PtrToStringAnsi(message, length));
                break;
            case DebugSeverity.DebugSeverityMedium:
                callback(LogLevel.Warn, Marshal.PtrToStringAnsi(message, length));
                break;
            case DebugSeverity.DebugSeverityLow:
                callback(LogLevel.Info, Marshal.PtrToStringAnsi(message, length));
                break;
            }
        };

        GL.DebugMessageCallback(LastCallbackProcReference, IntPtr.Zero);
    }

    public static int CalculateMipmapLevels(Dimension dimension)
    {
        int smallerAxis = Math.Min(dimension.Width, dimension.Height);
        return (int)Math.Floor(Math.Log(smallerAxis, 2));
    }
    
    public static int ToInt(this TextureUnit unit) => unit - TextureUnit.Texture0;

    // This is not thread safe because we write into a static buffer to avoid allocations.
    public static float[] ToUniformArray(this mat4 mat)
    {
        // This is the same as the result from mat4.Values1D.
        MvpBuffer[0] = mat.m00;
        MvpBuffer[1] = mat.m01;
        MvpBuffer[2] = mat.m02;
        MvpBuffer[3] = mat.m03;
        MvpBuffer[4] = mat.m10;
        MvpBuffer[5] = mat.m11;
        MvpBuffer[6] = mat.m12;
        MvpBuffer[7] = mat.m13;
        MvpBuffer[8] = mat.m20;
        MvpBuffer[9] = mat.m21;
        MvpBuffer[10] = mat.m22;
        MvpBuffer[11] = mat.m23;
        MvpBuffer[12] = mat.m30;
        MvpBuffer[13] = mat.m31;
        MvpBuffer[14] = mat.m32;
        MvpBuffer[15] = mat.m33;

        return MvpBuffer;
    }
}
