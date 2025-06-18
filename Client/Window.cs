using System;
using System.Collections.Generic;
using Helion.Client.Input;
using Helion.Client.Input.Controller;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render;
using Helion.Render.OpenGL.Context;
using Helion.Resources.Archives.Collection;
using Helion.Strings;
using Helion.Util.Configs;
using Helion.Util.Configs.Components;
using Helion.Util.Timing;
using Helion.Window;
using Helion.Window.Input;
using NLog;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client;

/// <summary>
/// A window that emits events and handles rendering.
/// </summary>
/// <remarks>
/// Allows us to override and extend the underlying game window as needed.
/// </remarks>
public class Window : GameWindow, IWindow
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public Renderer Renderer { get; }
    private readonly IConfig m_config;
    
    public IInputManager InputManager => m_inputManager;
    private readonly IInputManagement m_inputManagement;
    private readonly InputManager m_inputManager = new();
    private readonly SpanString m_textInput = new();
    public readonly ControllerAdapter JoystickAdapter;

    public Dimension ClientDimension => new((int)(ClientSize.X * m_clientScaling.X), (int)(ClientSize.Y * m_clientScaling.Y));
    private bool m_firstResizeEvent = true;
    private bool m_updatingWindowState;
    private bool m_isLinuxWayland = OperatingSystem.IsLinux() && GLFW.GetPlatform() == Platform.Wayland;
    private Vec2F m_clientScaling = new(1, 1);

    private bool m_disposed;

    public Window(string title, IConfig config, ArchiveCollection archiveCollection, FpsTracker tracker, IInputManagement inputManagement,
        int glMajor, int glMinor, GLContextFlags flags, Action onCreate) :
        base(MakeGameWindowSettings(), MakeNativeWindowSettings(config, title, glMajor, glMinor, flags))
    {
        Log.Debug("Creating client window");
        onCreate();
        m_config = config;
        UpdateWindow();
        m_inputManagement = inputManagement;
        CursorState = config.Mouse.Focus ? CursorState.Grabbed : CursorState.Hidden;
        Renderer = new(this, config, archiveCollection, tracker);

        KeyDown += Window_KeyDown;
        KeyUp += Window_KeyUp;
        MouseDown += Window_MouseDown;
        MouseMove += Window_MouseMove;
        MouseUp += Window_MouseUp;
        MouseWheel += Window_MouseWheel;
        TextInput += Window_TextInput;

        JoystickAdapter = new ControllerAdapter(
            (float)m_config.Controller.GameControllerDeadZone.Value,
            m_config.Controller.EnableGameController,
            m_config.Controller.EnableRumble,
            m_config.Controller.GyroSmoothingEnabled,
            (float)m_config.Controller.GyroSmoothingThreshold,
            m_config.Controller.GyroNoise,
            m_config.Controller.GyroDrift,
            m_inputManager);
        m_config.Controller.GyroSmoothingEnabled.OnChanged += OnGyroSmoothEnableChanged;
        m_config.Controller.GyroSmoothingThreshold.OnChanged += OnGyroSmoothFactorChanged;

        m_config.Render.MaxFPS.OnChanged += OnMaxFpsChanged;
        m_config.Render.VSync.OnChanged += OnVSyncChanged;
    }

    public void SetMousePosition(Vec2I pos)
    {
        MousePosition = (pos.X, pos.Y);
        InputManager.MousePosition = pos;
    }

    public void SetDisplay(int display)
    {
        // Setting the monitor will force to fullscreen
        if (WindowState != WindowState.Fullscreen)
            return;

        MakeFullscreen(GetMonitorHandle(display));
        UpdateWindow();
    }

    public List<MonitorData> GetMonitors(out MonitorData? currentMonitor)
    {
        currentMonitor = null;
        var currentHandle = Monitors.GetMonitorFromWindow(this);
        var windowMonitors = Monitors.GetMonitors();
        List<MonitorData> monitors = new(windowMonitors.Count);
        int i = 0;
        foreach (var info in windowMonitors)
        {
            var monitorData = new MonitorData(i, info.HorizontalResolution, info.VerticalResolution, info.CurrentVideoMode.RefreshRate, info.Handle);
            monitors.Add(monitorData);

            if (info.Handle.Pointer == currentHandle.Handle.Pointer)
                currentMonitor = monitorData;

            i++;
        }

        return monitors;
    }

    ~Window()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    private static GameWindowSettings MakeGameWindowSettings()
    {
        return new GameWindowSettings
        {
            UpdateFrequency = 500
        };
    }

    public static NativeWindowSettings MakeNativeWindowSettings(IConfig config, string title, int glMajor, int glMinor, GLContextFlags flags)
    {
        (int windowWidth, int windowHeight) = config.Window.Dimension.Value;

        var settingsFlags = ContextFlags.Default;
        if ((flags & GLContextFlags.ForwardCompatible) != 0)
            settingsFlags |= ContextFlags.ForwardCompatible;
        if (config.Developer.Render.Debug)
            settingsFlags |= ContextFlags.Debug;

        var settings = new NativeWindowSettings
        {
            Profile = ContextProfile.Core,
            APIVersion = new Version(glMajor, glMinor),
            Flags = settingsFlags,
            ClientSize = new Vector2i(windowWidth, windowHeight),
            Title = title,
            WindowBorder = config.Window.Border,
            WindowState = GetWindowState(config.Window.State.Value),
        };

        SetDisplay(config.Window.Display.Value, settings);
        return settings;
    }

    private static WindowState GetWindowState(RenderWindowState state)
    {
        return state switch
        {
            RenderWindowState.Fullscreen => WindowState.Fullscreen,
            _ => WindowState.Normal,
        };
    }


    private void UpdateScaling()
    {
        if (!m_isLinuxWayland)
        {
            return;
        }

        unsafe
        {
            // This mainly applies to Wayland on Linux, which uses some odd "virtual resolution" logic for window size.
            GLFW.GetWindowContentScale(WindowPtr, out float xScale, out float yScale);
            m_clientScaling = new(xScale, yScale);
        }
    }


    protected override void OnResize(ResizeEventArgs e)
    {
        if (m_firstResizeEvent)
        {
            // OpenTK fires a dummy window-resize event on startup.  Ignore whatever size the window is then.
            m_firstResizeEvent = false;
        }

        UpdateScaling();

        if (!m_updatingWindowState && m_config.Window.State.Value == RenderWindowState.Normal)
        {
            // If the user resizes the window manually by dragging the handles, update the config file.
            // This allows the user to persist their window resize.
            Dimension scaledDimension = ((int)(m_clientScaling.X * e.Width), (int)(m_clientScaling.Y * e.Height));

            string resolutionString = $"{scaledDimension.Width}x{scaledDimension.Height}";
            m_config.Window.Dimension.Set(resolutionString, fireChangeEvents: false);
        }

        base.OnResize(e);
    }

    /// <summary>
    /// Update the window, ensuring that all of the border/dimension/screen state parameters remain logically consistent
    /// </summary>
    public void UpdateWindow()
    {
        UpdateScaling();

        // Apply fullscreen / window / borderless fullscreen window mode, ensure size and borders
        switch (m_config.Window.State.Value)
        {
            case RenderWindowState.Fullscreen:
                WindowState = WindowState.Fullscreen;
                break;
            case RenderWindowState.Normal:
                WindowState oldWindowState = WindowState;
                WindowState = WindowState.Normal;
                Dimension dimension = m_config.Window.Dimension.Value;
                ClientSize = ((int)(dimension.Width / m_clientScaling.X), (int)(dimension.Height / m_clientScaling.Y));
                //Size
                if (WindowState != oldWindowState)
                {
                    CenterWindow();
                }
                WindowBorder = m_config.Window.Border;
                break;
            case RenderWindowState.BorderlessFullscreenWindow:
                CenterWindow();
                WindowState = WindowState.Normal;
                WindowBorder = WindowBorder.Hidden;
                MonitorInfo monitorInfo = Monitors.GetMonitorFromWindow(this);
                ClientSize = ((int)(monitorInfo.HorizontalResolution / m_clientScaling.X),
                    (int)(monitorInfo.VerticalResolution / m_clientScaling.Y));
                break;
        }

        SetSyncMode(m_config.Render.MaxFPS.Value, m_config.Render.VSync.Value);
    }

    private static void SetDisplay(int display, NativeWindowSettings settings)
    {
        settings.CurrentMonitor = GetMonitorHandle(display);
    }

    private static MonitorHandle GetMonitorHandle(int display)
    {
        var windowMonitors = Monitors.GetMonitors();
        if (display <= 0)
            return windowMonitors[0].Handle;

        var index = display - 1;
        if (index < 0 || index >= windowMonitors.Count)
        {
            Log.Error($"Invalid display number: {display}");
            return windowMonitors[0].Handle;
        }

        return windowMonitors[index].Handle;
    }

    public void SetGrabCursor(bool set) => CursorState = set ? CursorState.Grabbed : CursorState.Hidden;

    private void Window_KeyUp(KeyboardKeyEventArgs args)
    {
        Key key = OpenTKInputAdapter.ToKey(args.Key);
        if (key != Key.Unknown)
            m_inputManager.SetKeyUp(key);
    }

    private void Window_KeyDown(KeyboardKeyEventArgs args)
    {
        Key key = OpenTKInputAdapter.ToKey(args.Key);
        if (key != Key.Unknown)
            m_inputManager.SetKeyDown(key);
    }

    private void Window_MouseDown(MouseButtonEventArgs args)
    {
        Key key = OpenTKInputAdapter.ToMouseKey(args.Button);
        if (key != Key.Unknown)
            m_inputManager.SetKeyDown(key);
    }

    private void Window_MouseMove(MouseMoveEventArgs args)
    {
        m_inputManager.SetMousePosition(((int)args.Position.X, (int)args.Position.Y));
        if (!m_inputManagement.ShouldHandleMouseMovement())
            return;

        Vec2F movement = (-args.Delta.X, -args.Delta.Y);
        m_inputManager.AddMouseMovement(movement.Int);
    }

    private void Window_MouseUp(MouseButtonEventArgs args)
    {
        Key key = OpenTKInputAdapter.ToMouseKey(args.Button);
        if (key != Key.Unknown)
            m_inputManager.SetKeyUp(key);
    }

    private void Window_MouseWheel(MouseWheelEventArgs args)
    {
        m_inputManager.AddMouseScroll(args.OffsetY);
    }

    private void Window_TextInput(TextInputEventArgs args)
    {
        m_textInput.ConvertFromUtf32(args.Unicode);
        m_inputManager.AddTypedCharacters(m_textInput.AsSpan());
    }

    public void HandleRawMouseMovement(int x, int y)
    {
        m_inputManager.AddMouseMovement((x, y));
    }

    private void OnMaxFpsChanged(object? sender, int maxFps)
    {
        SetSyncMode(maxFps, m_config.Render.VSync.Value);
    }

    private void OnVSyncChanged(object? sender, RenderVsyncMode mode)
    {
        SetSyncMode(m_config.Render.MaxFPS, mode);
    }

    private void OnGyroSmoothEnableChanged(object? sender, bool e)
    {
        JoystickAdapter.SmoothingEnabled = e;
    }

    private void OnGyroSmoothFactorChanged(object? sender, double e)
    {
        JoystickAdapter.SmoothingThreshold = (float)e;
    }

    private void SetSyncMode(int maxFps, RenderVsyncMode vsync)
    {
        switch (vsync)
        {
            case RenderVsyncMode.Off:
                VSync = VSyncMode.Off;
                break;
            case RenderVsyncMode.On:
                VSync = VSyncMode.On;
                break;
            case RenderVsyncMode.Adaptive:
                VSync = VSyncMode.Adaptive;
                break;
            default:
                break;
        }

        if (maxFps == 0)
        {
            _ = GetMonitors(out MonitorData? current);
            UpdateFrequency = current?.RefreshRate > 0 && vsync != RenderVsyncMode.Off
                ? current.RefreshRate
                : 0;

            return;
        }
        UpdateFrequency = maxFps;
    }

    private void PerformDispose()
    {
        if (m_disposed || m_config == null)
            return;

        KeyDown -= Window_KeyDown;
        KeyUp -= Window_KeyUp;
        MouseDown -= Window_MouseDown;
        MouseMove -= Window_MouseMove;
        MouseUp -= Window_MouseUp;
        MouseWheel -= Window_MouseWheel;
        TextInput -= Window_TextInput;

        m_config.Render.MaxFPS.OnChanged -= OnMaxFpsChanged;
        m_config.Render.VSync.OnChanged -= OnVSyncChanged;

        m_config.Controller.GyroSmoothingEnabled.OnChanged -= OnGyroSmoothEnableChanged;
        m_config.Controller.GyroSmoothingThreshold.OnChanged -= OnGyroSmoothFactorChanged;

        Renderer.Dispose();
        JoystickAdapter.Dispose();

        m_disposed = true;
    }

    public new void Dispose()
    {
        GC.SuppressFinalize(this);
        base.Dispose();
        PerformDispose();
    }
}
