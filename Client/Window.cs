using Helion.Client.Input;
using Helion.Client.Input.Joystick;
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
using System;
using System.Collections.Generic;
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

    public IInputManager InputManager => m_inputManager;
    public Renderer Renderer { get; }
    public Dimension Dimension => new(Bounds.Max.X - Bounds.Min.X, Bounds.Max.Y - Bounds.Min.Y);
    public Dimension FramebufferDimension => Dimension; // Note: In the future, use `GLFW.GetFramebufferSize` maybe.
    private readonly IConfig m_config;
    private readonly IInputManagement m_inputManagement;
    private readonly InputManager m_inputManager = new();
    private SpanString m_textInput = new();
    private bool m_disposed;
    private readonly JoystickAdapter m_joystickAdapter;

    public Window(string title, IConfig config, ArchiveCollection archiveCollection, FpsTracker tracker, IInputManagement inputManagement,
        int glMajor, int glMinor, GLContextFlags flags, Action onCreate) :
        base(MakeGameWindowSettings(), MakeNativeWindowSettings(config, title, glMajor, glMinor, flags))
    {
        Log.Debug("Creating client window");
        onCreate();
        m_config = config;
        m_inputManagement = inputManagement;
        CursorState = config.Mouse.Focus ? CursorState.Grabbed : CursorState.Hidden;
        Renderer = new(this, config, archiveCollection, tracker);
        RenderFrequency = config.Render.MaxFPS;
        SetVsync(config.Render.VSync.Value);

        KeyDown += Window_KeyDown;
        KeyUp += Window_KeyUp;
        MouseDown += Window_MouseDown;
        MouseMove += Window_MouseMove;
        MouseUp += Window_MouseUp;
        MouseWheel += Window_MouseWheel;
        TextInput += Window_TextInput;

        m_joystickAdapter = new JoystickAdapter(
            JoystickStates,
            (float)m_config.Controller.GameControllerDeadZone.Value,
            m_inputManager);
        SetGameControllerPolling(m_config.Controller.EnableGameController);

        m_config.Render.MaxFPS.OnChanged += OnMaxFpsChanged;
        m_config.Render.VSync.OnChanged += OnVSyncChanged;
        m_config.Controller.EnableGameController.OnChanged += EnableGameController_OnChanged;
        m_config.Controller.GameControllerDeadZone.OnChanged += GameControllerDeadZone_OnChanged;
    }

    public void SetMousePosition(Vec2I pos)
    {
        MousePosition = (pos.X, pos.Y);
        InputManager.MousePosition = pos;
    }

    private void RedetectJoysticks(JoystickEventArgs obj)
    {
        m_joystickAdapter.RedetectJoysticks();
    }

    private void PollJoysticks(FrameEventArgs _)
    {
        m_joystickAdapter.SampleJoystickStates();
    }

    public void SetWindowState(RenderWindowState state)
    {
        switch (state)
        {
            case RenderWindowState.Fullscreen:
                WindowState = WindowState.Fullscreen;
                break;
            case RenderWindowState.Normal:
                WindowState = WindowState.Normal;
                break;
        }
    }

    public void SetDimension(Dimension dimension) =>
        Size = new(dimension.Width, dimension.Height);

    public void SetBorder(WindowBorder border) =>
        WindowBorder = border;

    public void SetDisplay(int display)
    {
        // Setting the monitor will force to fullscreen
        if (WindowState != WindowState.Fullscreen)
            return;

        CurrentMonitor = GetMonitorHandle(display);
    }

    private void SetVsync(RenderVsyncMode mode)
    {
        switch (mode)
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
            var monitorData = new MonitorData(i, info.HorizontalResolution, info.VerticalResolution, info.Handle);
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
            RenderFrequency = 500
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
            NumberOfSamples = config.Render.Multisample.Value,
            Size = new Vector2i(windowWidth, windowHeight),
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
        RenderFrequency = maxFps;
    }

    private void OnVSyncChanged(object? sender, RenderVsyncMode mode)
    {
        SetVsync(mode);
    }

    private void GameControllerDeadZone_OnChanged(object? sender, double e)
    {
        m_joystickAdapter.DeadZone = (float)e;
    }

    private void EnableGameController_OnChanged(object? sender, bool e)
    {
        SetGameControllerPolling(e);
    }

    public void SetGameControllerPolling(bool active)
    {
        if (active)
        {
            RenderFrame += PollJoysticks;
            JoystickConnected += RedetectJoysticks;
        }
        else
        {
            RenderFrame -= PollJoysticks;
            JoystickConnected -= RedetectJoysticks;
        }
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
        UpdateFrame -= PollJoysticks;
        JoystickConnected -= RedetectJoysticks;

        m_config.Render.MaxFPS.OnChanged -= OnMaxFpsChanged;
        m_config.Render.VSync.OnChanged -= OnVSyncChanged;
        m_config.Controller.EnableGameController.OnChanged -= EnableGameController_OnChanged;
        m_config.Controller.GameControllerDeadZone.OnChanged -= GameControllerDeadZone_OnChanged;

        Renderer.Dispose();

        m_disposed = true;
    }

    public new void Dispose()
    {
        GC.SuppressFinalize(this);
        base.Dispose();
        PerformDispose();
    }
}
