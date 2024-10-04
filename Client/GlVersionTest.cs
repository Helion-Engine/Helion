using Helion.Render.OpenGL.Context;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace Helion.Client;

public class GlVersionTest
{
    public static unsafe bool Test(NativeWindowSettings settings, Action? onSuccess = null)
    {
        try
        {
            GLFWProvider.EnsureInitialized();

            switch (settings.API)
            {
                case ContextAPI.NoAPI:
                    GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
                    break;
                case ContextAPI.OpenGLES:
                    GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlEsApi);
                    break;
                case ContextAPI.OpenGL:
                    GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            bool forwardCompatible = GlVersion.Flags.HasFlag(GLContextFlags.ForwardCompatible);
            GLFW.WindowHint(WindowHintInt.ContextVersionMajor, settings.APIVersion.Major);
            GLFW.WindowHint(WindowHintInt.ContextVersionMinor, settings.APIVersion.Major);
            GLFW.WindowHint(WindowHintBool.OpenGLForwardCompat, value: forwardCompatible);

            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Compat);
            GLFW.WindowHint(WindowHintBool.Visible, false);

            OpenTK.Windowing.GraphicsLibraryFramework.Window* windowPtr = GLFW.CreateWindow(640, 480, "", null, (OpenTK.Windowing.GraphicsLibraryFramework.Window*)(void*)IntPtr.Zero);
            GLFW.MakeContextCurrent(windowPtr);

            LoadBindingsES11();
            LoadBindingsES20();
            LoadBindingsES30();
            LoadBindingsOpenGL();
            LoadBindingsOpenGL4();

            // Pull the version string from the created context to get the version since it was requested at the lowest (MacOS).
            // Otherwise the context is created with this version and no test is required (Windows/Linux).
            if (forwardCompatible)
            {
                GetGlVersion(out int major, out int minor);
                GlVersion.Major = major;
                GlVersion.Minor = minor;
            }

            onSuccess?.Invoke();
            GLFW.DestroyWindow(windowPtr);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static unsafe void GetGlVersion(out int major, out int minor)
    {
        var version = GL.GetString(StringName.Version).Trim();
        if (version.Contains(' '))
            version = version.Split(' ')[0];
        var split = version.Split('.');

        if (!int.TryParse(split[0], out int parseMajor))
            parseMajor = -1;
        if (!int.TryParse(split[1], out int parseMinor))
            parseMinor = -1;

        if (parseMajor == -1 || parseMinor == -1)
        {
            major = -1;
            minor = -1;
            return;
        }

        major = parseMajor;
        minor = parseMinor;
    }

    public static bool IsVersionSupported(int glMajor, int glMinor, int major, int minor)
    {
        if (glMajor > major)
            return true;

        return glMajor == major && glMinor >= minor;
    }

    static void LoadBindingsES11()
    {
        GLFWBindingsContext provider = new();
        OpenTK.Graphics.ES11.GL.LoadBindings(provider);
    }

    static void LoadBindingsES20()
    {
        GLFWBindingsContext provider = new();
        OpenTK.Graphics.ES20.GL.LoadBindings(provider);
    }

    static void LoadBindingsES30()
    {
        GLFWBindingsContext provider = new();
        OpenTK.Graphics.ES30.GL.LoadBindings(provider);
    }

    static void LoadBindingsOpenGL()
    {
        GLFWBindingsContext provider = new();
        OpenTK.Graphics.OpenGL.GL.LoadBindings(provider);
    }

    static void LoadBindingsOpenGL4()
    {
        GLFWBindingsContext provider = new();
        OpenTK.Graphics.OpenGL4.GL.LoadBindings(provider);
    }
}
