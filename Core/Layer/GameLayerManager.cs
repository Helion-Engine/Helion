using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helion.Audio.Sounds;
using Helion.Geometry.Vectors;
using Helion.Geometry;
using Helion.Layer.Consoles;
using Helion.Layer.EndGame;
using Helion.Layer.Images;
using Helion.Layer.IwadSelection;
using Helion.Layer.Menus;
using Helion.Layer.Options;
using Helion.Layer.Worlds;
using Helion.Menus.Impl;
using Helion.Render;
using Helion.Render.Common.Context;
using Helion.Render.Common.Renderers;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Consoles.Commands;
using Helion.Util.Extensions;
using Helion.Util.Profiling;
using Helion.Util.Timing;
using Helion.Window;
using Helion.World.Save;
using Helion.World.StatusBar;
using static Helion.Util.Assertion.Assert;
using Helion.Geometry.Boxes;
using Helion.Util.Configs.Components;
using Helion.Render.OpenGL.Shared;

namespace Helion.Layer;

/// <summary>
/// Responsible for coordinating input, logic, and rendering calls in order
/// for different kinds of layers.
/// </summary>
public class GameLayerManager : IGameLayerManager
{
    private static readonly string[] MenuIgnoreCommands = { Constants.Input.Screenshot, Constants.Input.Console, Constants.Input.OptionsMenu, Constants.Input.Load };

    public event EventHandler<IGameLayer>? GameLayerAdded;

    public bool LockInput { get; set; }

    public ConsoleLayer? ConsoleLayer { get; private set; }
    public OptionsLayer? OptionsLayer { get; private set; }
    public MenuLayer? MenuLayer { get; private set; }
    public ReadThisLayer? ReadThisLayer { get; private set; }
    public TitlepicLayer? TitlepicLayer { get; private set; }
    public EndGameLayer? EndGameLayer { get; private set; }
    public IntermissionLayer? IntermissionLayer { get; private set; }
    public IwadSelectionLayer? IwadSelectionLayer {  get; private set; }
    public LoadingLayer? LoadingLayer { get; private set; }
    public WorldLayer? WorldLayer { get; private set; }
    public SaveGameEvent? LastSave;
    private readonly IConfig m_config;
    private readonly IWindow m_window;
    private readonly HelionConsole m_console;
    private readonly ConsoleCommands m_consoleCommands;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly SoundManager m_soundManager;
    private readonly SaveGameManager m_saveGameManager;
    private readonly Profiler m_profiler;
    private readonly Stopwatch m_stopwatch = new();
    private readonly Action<IRenderableSurfaceContext> m_renderDefaultAction;
    private readonly Action<IHudRenderContext> m_renderHudAction;
    private readonly HudRenderContext m_hudContext = new(default);
    private readonly OptionsLayer m_optionsLayer;
    private Renderer m_renderer;
    private IRenderableSurfaceContext m_ctx;
    private bool m_disposed;

    private IEnumerable<IGameLayer> Layers => new List<IGameLayer?>
    {
        ConsoleLayer, OptionsLayer, MenuLayer, ReadThisLayer, TitlepicLayer, EndGameLayer, IntermissionLayer, WorldLayer
    }.WhereNotNull();

    public GameLayerManager(IConfig config, IWindow window, HelionConsole console, ConsoleCommands consoleCommands,
        ArchiveCollection archiveCollection, SoundManager soundManager, SaveGameManager saveGameManager,
        Profiler profiler)
    {
        m_config = config;
        m_window = window;
        m_console = console;
        m_consoleCommands = consoleCommands;
        m_archiveCollection = archiveCollection;
        m_soundManager = soundManager;
        m_saveGameManager = saveGameManager;
        m_profiler = profiler;
        m_stopwatch.Start();
        m_renderDefaultAction = RenderDefault;
        m_renderHudAction = RenderHud;
        m_renderer = null!;
        m_ctx = null!;

        m_optionsLayer = new(this, m_config, m_soundManager, m_window);

        m_saveGameManager.GameSaved += SaveGameManager_GameSaved;
    }

    ~GameLayerManager()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    private void SaveGameManager_GameSaved(object? sender, SaveGameEvent e)
    {
        if (e.Success)
            LastSave = e;
    }

    public bool HasMenuOrConsole() => MenuLayer != null || ConsoleLayer != null;

    public bool ShouldFocus()
    {
        if (ConsoleLayer != null)
            return false;

        if (TitlepicLayer != null)
            return MenuLayer == null;

        if (WorldLayer != null)
            return WorldLayer.ShouldFocus;

        return true;
    }

    public void Add(IGameLayer? gameLayer)
    {
        switch (gameLayer)
        {
            case ConsoleLayer layer:
                Remove(ConsoleLayer);
                ConsoleLayer = layer;
                break;
            case MenuLayer layer:
                Remove(MenuLayer);
                MenuLayer = layer;
                break;
            case ReadThisLayer layer:
                Remove(ReadThisLayer);
                ReadThisLayer = layer;
                break;
            case EndGameLayer layer:
                Remove(EndGameLayer);
                EndGameLayer = layer;
                break;
            case TitlepicLayer layer:
                Remove(TitlepicLayer);
                TitlepicLayer = layer;
                break;
            case IntermissionLayer layer:
                Remove(IntermissionLayer);
                IntermissionLayer = layer;
                break;
            case OptionsLayer layer:
                Remove(OptionsLayer);
                OptionsLayer = layer;
                break;
            case WorldLayer layer:
                Remove(WorldLayer);
                WorldLayer = layer;
                break;
            case IwadSelectionLayer layer:
                Remove(IwadSelectionLayer);
                IwadSelectionLayer = layer;
                break;
            case LoadingLayer layer:
                Remove(LoadingLayer);
                LoadingLayer = layer;
                break;
            case null:
                break;
            default:
                throw new ArgumentException($"Unknown object passed for layer: {gameLayer.GetType()}");
        }

        if (gameLayer != null)
        {
            gameLayer.OnShow();
            GameLayerAdded?.Invoke(this, gameLayer);
        }
    }

    public void SubmitConsoleText(string text)
    {
        m_console.ClearInputText();
        m_console.AddInput(text);
        m_console.SubmitInputText();
    }

    public List<string> GetConsoleSubmittedInput() => m_console.SubmittedInput;

    public void ClearAllExcept(params IGameLayer?[] layers)
    {
        foreach (IGameLayer existingLayer in Layers)
            if (!layers.Contains(existingLayer))
                Remove(existingLayer);
    }

    public void Remove(object? layer)
    {
        if (layer == null)
            return;

        if (ReferenceEquals(layer, ConsoleLayer))
        {
            ConsoleLayer?.Dispose();
            ConsoleLayer = null;
        }
        else if (ReferenceEquals(layer, OptionsLayer))
        {
            OptionsLayer = null;
            m_window.InputManager.ClearMouse();
            m_window.SetMousePosition(Vec2I.Zero);
        }
        else if (ReferenceEquals(layer, MenuLayer))
        {
            MenuLayer?.Dispose();
            MenuLayer = null;
            m_window.InputManager.ClearMouse();
            m_window.SetMousePosition(Vec2I.Zero);
        }
        else if (ReferenceEquals(layer, ReadThisLayer))
        {
            ReadThisLayer?.Dispose();
            ReadThisLayer = null;
        }
        else if (ReferenceEquals(layer, EndGameLayer))
        {
            EndGameLayer?.Dispose();
            EndGameLayer = null;
        }
        else if (ReferenceEquals(layer, TitlepicLayer))
        {
            TitlepicLayer?.Dispose();
            TitlepicLayer = null;
        }
        else if (ReferenceEquals(layer, IntermissionLayer))
        {
            IntermissionLayer?.Dispose();
            IntermissionLayer = null;
        }
        else if (ReferenceEquals(layer, WorldLayer))
        {
            WorldLayer?.Dispose();
            WorldLayer = null;
        }
        else if (ReferenceEquals(layer, IwadSelectionLayer))
        {
            IwadSelectionLayer?.Dispose();
            IwadSelectionLayer = null;
        }
        else if (ReferenceEquals(layer, LoadingLayer))
        {
            LoadingLayer?.Dispose();
            LoadingLayer = null;
        }
    }

    private void GameLayer_OnRemove(object? sender, EventArgs e)
    {
        Remove(sender);
    }

    private void HandleInput(IInputManager inputManager, TickerInfo tickerInfo)
    {
        if (LockInput)
            return;

        IConsumableInput input = inputManager.Poll(tickerInfo.Ticks > 0);
        HandleInput(input);

        // Only clear keys if new tick since they are only processed each tick.
        // Mouse movement is always processed to render most up to date view.
        if (input.HandleKeyInput)
            inputManager.ProcessedKeys();

        inputManager.ProcessedMouseMovement();
    }

    public void HandleInput(IConsumableInput input)
    {        
        if (input.HandleKeyInput)
        {
            if (IwadSelectionLayer == null && ConsumeCommandPressed(Constants.Input.Console, input))
                ToggleConsoleLayer(input);
            ConsoleLayer?.HandleInput(input);
            
            if (ShouldCreateMenu(input))
            {
                if (ReadThisLayer != null)
                    Remove(ReadThisLayer);
                CreateMenuLayer();
            }

            CheckMenuShortcuts(input);

            if (!HasMenuOrConsole())
            {
                EndGameLayer?.HandleInput(input);
                TitlepicLayer?.HandleInput(input);
                IntermissionLayer?.HandleInput(input);
            }

            if (ReadThisLayer == null)
            {
                OptionsLayer?.HandleInput(input);
                MenuLayer?.HandleInput(input);
                IwadSelectionLayer?.HandleInput(input);
            }

            if (ConsoleLayer == null)
                ReadThisLayer?.HandleInput(input);
        }

        WorldLayer?.HandleInput(input);
    }

    private void CheckMenuShortcuts(IConsumableInput input)
    {
        if (OptionsLayer != null || MenuLayer != null)
            return;

        if (ConsumeCommandPressed(Constants.Input.Save, input))
        {
            GoToSaveOrLoadMenu(true);
            return;
        }

        if (ConsumeCommandPressed(Constants.Input.QuickSave, input))
            QuickSave();

        if (ConsumeCommandPressed(Constants.Input.Load, input))
            GoToSaveOrLoadMenu(false);

        if (ConsumeCommandPressed(Constants.Input.OptionsMenu, input))
            ShowOptionsMenu();
    }

    private bool ShouldCreateMenu(IConsumableInput input)
    {
        if (MenuLayer != null || ConsoleLayer != null || IwadSelectionLayer != null)
            return false;

        bool hasMenuInput = (ReadThisLayer != null && ConsumeCommandDown(Constants.Input.Menu, input))
            || input.Manager.HasAnyKeyPressed();

        if (TitlepicLayer != null && hasMenuInput && !CheckIgnoreMenuCommands(input))
        {
            // Eat everything including escape key if it exists, otherwise the menu will immediately close.
            input.ConsumeAll();
            return true;
        }
        
        return ConsumeCommandPressed(Constants.Input.Menu, input);
    }

    private bool ConsumeCommandPressed(string command, IConsumableInput input)
    {
        return m_config.Keys.ConsumeCommandKeyPress(command, input, out _);
    }

    private bool ConsumeCommandDown(string command, IConsumableInput input)
    {
        return m_config.Keys.ConsumeCommandKeyPress(command, input, out _);
    }

    private bool CheckIgnoreMenuCommands(IConsumableInput input)
    {
        for (int i = 0; i < MenuIgnoreCommands.Length; i++)
        {
            if (m_config.Keys.IsCommandKeyDown(MenuIgnoreCommands[i], input))
                return true;
        }

        return false;
    }

    private void ToggleConsoleLayer(IConsumableInput input)
    {
        input.ConsumeAll();

        if (ConsoleLayer == null)
            Add(new ConsoleLayer(m_archiveCollection.GameInfo.TitlePage, m_config, m_console, m_consoleCommands));
        else
            Remove(ConsoleLayer);
    }


    private void CreateMenuLayer()
    {
        m_soundManager.PlayStaticSound(Constants.MenuSounds.Activate);
        MenuLayer menuLayer = new(this, m_config, m_console, m_archiveCollection, m_soundManager, m_saveGameManager, m_optionsLayer);
        Add(menuLayer);
    }

    public void GoToSaveOrLoadMenu(bool isSave)
    {
        if (MenuLayer == null)
            CreateMenuLayer();

        MenuLayer?.AddSaveOrLoadMenuIfMissing(isSave, true);
    }

    public void ShowOptionsMenu()
    {
        if (MenuLayer == null)
            CreateMenuLayer();

        m_optionsLayer.SetMouseStartPosition();
        MenuLayer!.ShowOptionsMenu();
    }

    public void QuickSave()
    {
        if (WorldLayer == null  || !LastSave.HasValue)
        {
            GoToSaveOrLoadMenu(true);
            return;
        }

        if (m_config.Game.QuickSaveConfirm)
        {
            MessageMenu confirm = new(m_config, m_console, m_soundManager, m_archiveCollection,
                new[] { "Are you sure you want to overwrite:", LastSave.Value.SaveGame.Model != null ? LastSave.Value.SaveGame.Model.Text : "Save", "Press Y to confirm." },
                isYesNoConfirm: true, clearMenus: true);
            confirm.Cleared += Confirm_Cleared;

            CreateMenuLayer();
            MenuLayer?.ShowMessage(confirm);
            return;
        }

        WriteQuickSave();
    }

    private void Confirm_Cleared(object? sender, bool e)
    {
        if (!e || !LastSave.HasValue)
            return;

        WriteQuickSave();
    }

    private void WriteQuickSave()
    {
        if (WorldLayer == null || LastSave == null)
            return;

        var world = WorldLayer.World;
        var save = LastSave.Value;
        m_saveGameManager.WriteSaveGame(world, world.MapInfo.GetMapNameWithPrefix(world.ArchiveCollection), save.SaveGame);
        world.DisplayMessage(world.Player, null, SaveMenu.SaveMessage);
    }

    public void RunLogic(TickerInfo tickerInfo)
    {
        HandleInput(m_window.InputManager, tickerInfo);

        ConsoleLayer?.RunLogic(tickerInfo);
        OptionsLayer?.RunLogic(tickerInfo);
        IwadSelectionLayer?.RunLogic(tickerInfo);
        MenuLayer?.RunLogic(tickerInfo);
        ReadThisLayer?.RunLogic(tickerInfo);
        EndGameLayer?.RunLogic(tickerInfo);
        TitlepicLayer?.RunLogic(tickerInfo);
        IntermissionLayer?.RunLogic(tickerInfo);
        WorldLayer?.RunLogic(tickerInfo);

        if (!HasMenuOrConsole() && m_stopwatch.ElapsedMilliseconds >= 1000.0 / Constants.TicksPerSecond)
        {
            m_stopwatch.Restart();
            EndGameLayer?.OnTick();
        }
    }

    public void Render(Renderer renderer)
    {
        m_renderer = renderer;
        m_renderer.Default.Render(m_renderDefaultAction);
    }

    private void RenderDefault(IRenderableSurfaceContext ctx)
    {
        m_ctx = ctx;        
        m_hudContext.Dimension = m_renderer.RenderDimension;
        ctx.Viewport(m_renderer.RenderDimension.Box);
        ctx.Clear(Renderer.DefaultBackground, true, true);

        if (WorldLayer != null && WorldLayer.ShouldRender)
        {
            var offset = HudView.GetViewPortOffset(m_config.Hud.StatusBarSize, ctx.Surface.Dimension);
            if (WorldLayer.World.DrawHud && (offset.X != 0 || offset.Y != 0))
            {
                var box = new Box2I((offset.X, offset.Y), (ctx.Surface.Dimension.Width + offset.X, ctx.Surface.Dimension.Height + offset.Y));
                ctx.Viewport(box);
            }

            WorldLayer.RenderWorld(ctx);
        }

        m_profiler.Render.MiscLayers.Start();
        ctx.Hud(m_hudContext, m_renderHudAction);
        m_profiler.Render.MiscLayers.Stop();
    }

    private void RenderHud(IHudRenderContext hudCtx)
    {
        // Only use virtual dimensions when drawing the world.
        // Restore back to window dimensions when drawing anything else so that text etc are not stretched.
        bool resetViewport = m_ctx.Surface.Dimension != m_renderer.Window.Dimension;
        if (resetViewport)
        {
            m_hudContext.Dimension = m_renderer.Window.Dimension;
            m_ctx.Surface.SetOverrideDimension(m_renderer.Window.Dimension);
            m_ctx.Viewport(m_ctx.Surface.Dimension.Box);
        }

        m_ctx.DrawVirtualFrameBuffer();

        if (WorldLayer != null && WorldLayer.ShouldRender)
        {            
            WorldLayer.RenderAutomap(m_ctx);
            WorldLayer.RenderHud(m_ctx);
        }
        
        m_ctx.ClearDepth();

        IntermissionLayer?.Render(m_ctx, hudCtx);
        TitlepicLayer?.Render(hudCtx);
        EndGameLayer?.Render(m_ctx, hudCtx);
        MenuLayer?.Render(hudCtx);
        OptionsLayer?.Render(m_ctx, hudCtx);
        ReadThisLayer?.Render(hudCtx);
        IwadSelectionLayer?.Render(m_ctx, hudCtx);
        LoadingLayer?.Render(m_ctx, hudCtx);
        ConsoleLayer?.Render(m_ctx, hudCtx);
        m_ctx.Surface.ClearOverrideDimension();
    }

    public void Dispose()
    {
        PerformDispose();
        GC.SuppressFinalize(this);
    }

    private void PerformDispose()
    {
        if (m_disposed)
            return;

        Remove(WorldLayer);
        Remove(IntermissionLayer);
        Remove(EndGameLayer);
        Remove(TitlepicLayer);
        Remove(ReadThisLayer);
        Remove(MenuLayer);
        Remove(OptionsLayer);
        Remove(ConsoleLayer);
        Remove(LoadingLayer);

        m_disposed = true;
    }
}
