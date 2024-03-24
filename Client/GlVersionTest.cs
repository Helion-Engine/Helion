using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace Helion.Client;

public class GlVersionTest
{
    public static unsafe void Test(NativeWindowSettings settings)
    {
        GLFWProvider.EnsureInitialized();
        switch (settings.WindowBorder)
        {
            case WindowBorder.Hidden:
                GLFW.WindowHint(WindowHintBool.Decorated, value: false);
                break;
            case WindowBorder.Resizable:
                GLFW.WindowHint(WindowHintBool.Resizable, value: true);
                break;
            case WindowBorder.Fixed:
                GLFW.WindowHint(WindowHintBool.Resizable, value: false);
                break;
        }

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

        GLFW.WindowHint(WindowHintInt.ContextVersionMajor, settings.APIVersion.Major);
        GLFW.WindowHint(WindowHintInt.ContextVersionMinor, settings.APIVersion.Minor);
        var APIVersion = settings.APIVersion;
        var Flags = settings.Flags;
        if (settings.Flags.HasFlag(ContextFlags.ForwardCompatible))
        {
            GLFW.WindowHint(WindowHintBool.OpenGLForwardCompat, value: true);
        }

        if (settings.Flags.HasFlag(ContextFlags.Debug))
        {
            GLFW.WindowHint(WindowHintBool.OpenGLDebugContext, value: true);
        }

        var Profile = settings.Profile;
        switch (settings.Profile)
        {
            case ContextProfile.Any:
                GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Any);
                break;
            case ContextProfile.Compatability:
                GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Compat);
                break;
            case ContextProfile.Core:
                GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        GLFW.WindowHint(WindowHintBool.Focused, settings.StartFocused);
        GLFW.WindowHint(WindowHintBool.Visible, false);
        GLFW.WindowHint(WindowHintInt.Samples, settings.NumberOfSamples);
        GLFW.WindowHint(WindowHintBool.SrgbCapable, settings.SrgbCapable);
        Monitor* monitor = settings.CurrentMonitor.ToUnsafePtr<Monitor>();
        VideoMode* videoMode = GLFW.GetVideoMode(monitor);
        GLFW.WindowHint(WindowHintInt.RedBits, settings.RedBits ?? videoMode->RedBits);
        GLFW.WindowHint(WindowHintInt.GreenBits, settings.GreenBits ?? videoMode->GreenBits);
        GLFW.WindowHint(WindowHintInt.BlueBits, settings.BlueBits ?? videoMode->BlueBits);
        if (settings.AlphaBits.HasValue)
        {
            GLFW.WindowHint(WindowHintInt.AlphaBits, settings.AlphaBits.Value);
        }

        if (settings.DepthBits.HasValue)
        {
            GLFW.WindowHint(WindowHintInt.DepthBits, settings.DepthBits.Value);
        }

        if (settings.StencilBits.HasValue)
        {
            GLFW.WindowHint(WindowHintInt.StencilBits, settings.StencilBits.Value);
        }

        OpenTK.Windowing.GraphicsLibraryFramework.Window* windowPtr;
        GLFW.WindowHint(WindowHintInt.RefreshRate, videoMode->RefreshRate);

        windowPtr = GLFW.CreateWindow(640, 480, "", null, (OpenTK.Windowing.GraphicsLibraryFramework.Window*)(void*)(settings.SharedContext?.WindowPtr ?? IntPtr.Zero));
        var context = new GLFWGraphicsContext(windowPtr);
        GLFW.DestroyWindow(windowPtr);
    }
}
