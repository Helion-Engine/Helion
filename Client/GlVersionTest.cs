using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Reflection;

namespace Helion.Client;

public class GlVersionTest
{
    public static unsafe bool Test(NativeWindowSettings settings)
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

            GLFW.WindowHint(WindowHintInt.ContextVersionMajor, 3);
            GLFW.WindowHint(WindowHintInt.ContextVersionMinor, 3);
            GLFW.WindowHint(WindowHintBool.OpenGLForwardCompat, value: true);

            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Compat);
            GLFW.WindowHint(WindowHintBool.Visible, false);

            OpenTK.Windowing.GraphicsLibraryFramework.Window* windowPtr = GLFW.CreateWindow(640, 480, "", null, (OpenTK.Windowing.GraphicsLibraryFramework.Window*)(void*)IntPtr.Zero);
            GLFW.MakeContextCurrent(windowPtr);

            var assembly = Assembly.Load("OpenTK.Graphics");
            LoadBindings(assembly, "ES11");
            LoadBindings(assembly, "ES20");
            LoadBindings(assembly, "ES30");
            LoadBindings(assembly, "OpenGL");
            LoadBindings(assembly, "OpenGL4");

            GetGlVersion(out int major, out int minor);
            GLFW.DestroyWindow(windowPtr);
            return IsVersionSupported(major, minor, settings.APIVersion.Major, settings.APIVersion.Minor);
        }
        catch
        {
            return false;
        }
    }

    private static unsafe void GetGlVersion(out int major, out int minor)
    {
        var version = GL.GetString(StringName.Version);
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

    static void LoadBindings(Assembly assembly, string typeNamespace)
    {
        GLFWBindingsContext provider = new();
        Type type = assembly.GetType("OpenTK.Graphics." + typeNamespace + ".GL");
        if (!(type == null))
        {
            type.GetMethod("LoadBindings").Invoke(null, new object[1] { provider });
        }
    }
}
