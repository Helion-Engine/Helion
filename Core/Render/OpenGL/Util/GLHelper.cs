using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Helion.Geometry;
using Helion.Render.OpenGL.Context;
using Helion.Util.Extensions;
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
    /// <param name="extraLight">The extra light value from the player.</param>
    /// <returns>A value between 0 and 255.</returns>
    public static int DoomLightLevelToColor(int lightLevel, int extraLight) =>
        Math.Clamp((ColorMaps - GetLightLevelIndex(lightLevel, 8 - extraLight)) * ScaleCount, 0, 255);

    public static double DoomLightLevelToColorStatic(int lightLevel, int extraLight)
    {
        double lightLevelFrac = Math.Clamp(lightLevel + extraLight * 16, 0, 255) / 255.0f;

        switch (lightLevelFrac)
        {
            case > 0.75:
                break;

            case > 0.4:
                {
                    lightLevelFrac = -0.6375 + (1.85 * lightLevelFrac);
                    if (lightLevelFrac < 0.08)
                        lightLevelFrac = 0.08 + (lightLevelFrac * 0.2);

                    break;
                }

            default:
                lightLevelFrac /= 5.0;
                break;
        }

        return Math.Clamp(lightLevelFrac, 0.0, 1.0);
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
            return;
        }

        // If we don't do this, the GC will collect it (since the lambda
        // below won't) and then we end up with a SystemAccessViolation.
        // See the docs of this variable for more information.
        LastCallbackReference = callback;
        LastCallbackProcReference = (source, type, id, severity, length, message, userParam) =>
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
}
