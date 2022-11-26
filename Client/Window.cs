using System;
using System.Collections.Generic;
using Helion.Client.Input;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render;
using Helion.Render.OpenGL;
using Helion.Render.OpenGL.Context;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Configs.Components;
using Helion.Util.Timing;
using Helion.Window;
using Helion.Window.Input;
using NLog;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
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
    public IRenderer Renderer { get; }
    public Dimension Dimension => new(Bounds.Max.X - Bounds.Min.X, Bounds.Max.Y - Bounds.Min.Y);
    public Dimension FramebufferDimension => Dimension; // Note: In the future, use `GLFW.GetFramebufferSize` maybe.
    private readonly IConfig m_config;
    private readonly IInputManagement m_inputManagement;
    private readonly InputManager m_inputManager = new();
    private bool m_disposed;
    private Vec2F m_prevScroll = Vec2F.Zero;

    public Window(string title, IConfig config, ArchiveCollection archiveCollection, FpsTracker tracker, IInputManagement inputManagement) :
        base(MakeGameWindowSettings(), MakeNativeWindowSettings(config, title))
    {
        Log.Debug("Creating client window");

        m_config = config;
        m_inputManagement = inputManagement;
        CursorState = config.Mouse.Focus ? CursorState.Grabbed : CursorState.Hidden;
        Renderer = CreateRenderer(config, archiveCollection, tracker);
        RenderFrequency = config.Render.MaxFPS;
        SetVsync(config.Render.VSync.Value);

        KeyDown += Window_KeyDown;
        KeyUp += Window_KeyUp;
        MouseDown += Window_MouseDown;
        MouseMove += Window_MouseMove;
        MouseUp += Window_MouseUp;
        MouseWheel += Window_MouseWheel;
        TextInput += Window_TextInput;

        m_config.Render.MaxFPS.OnChanged += OnMaxFpsChanged;
        m_config.Render.VSync.OnChanged += OnVSyncChanged;
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

    private static NativeWindowSettings MakeNativeWindowSettings(IConfig config, string title)
    {
        (int windowWidth, int windowHeight) = config.Window.Dimension.Value;

        var settings = new NativeWindowSettings
        {
            Profile = ContextProfile.Core,
            APIVersion = new Version(3, 3),
            Flags = config.Developer.Render.Debug ? ContextFlags.Debug : ContextFlags.Default,
            NumberOfSamples = config.Render.Multisample.Value,
            Size = new Vector2i(windowWidth, windowHeight),
            Title = title,
            WindowBorder = config.Window.Border,
            WindowState = GetWindowState(config.Window.State.Value),
        };

        SetDisplay(config, settings);
        return settings;
    }

    private static WindowState GetWindowState(RenderWindowState state)
    {
        return state switch
        {
            RenderWindowState.Fullscreen => WindowState.Fullscreen,
            RenderWindowState.Maximized => WindowState.Maximized,
            _ => WindowState.Normal,
        };
    }

    private static void SetDisplay(IConfig config, NativeWindowSettings settings)
    {
        if (config.Window.Display.Value <= 0)
            return;

        var windowMonitors = Monitors.GetMonitors();
        var index = config.Window.Display.Value - 1;
        if (index < 0 || index >= windowMonitors.Count)
        {
            Log.Error($"Invalid display number: {config.Window.Display.Value}");
            return;
        }
        
        settings.CurrentMonitor = windowMonitors[index].Handle;
    }

    public void SetGrabCursor(bool set) => CursorState = set ? CursorState.Grabbed : CursorState.Hidden;

    private IRenderer CreateRenderer(IConfig config, ArchiveCollection archiveCollection, FpsTracker tracker)
    {
        return new GLRenderer(this, config, archiveCollection, new OpenTKGLFunctions(), tracker);
    }

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
        if (m_config.Mouse.RawInput || !m_inputManagement.ShouldHandleMouseMovement())
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
        m_inputManager.AddMouseScroll(args.OffsetY - m_prevScroll.Y);
        m_prevScroll.X = args.OffsetX;
        m_prevScroll.Y = args.OffsetY;
    }

    private void Window_TextInput(TextInputEventArgs args)
    {
        m_inputManager.AddTypedCharacters(args.AsString);
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

    private void PerformDispose()
    {
        if (m_disposed)
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

        m_disposed = true;
    }

    public new void Dispose()
    {
        GC.SuppressFinalize(this);
        base.Dispose();
        PerformDispose();
    }
}
