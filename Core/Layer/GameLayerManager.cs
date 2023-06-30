using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helion.Audio.Sounds;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Layer.Consoles;
using Helion.Layer.EndGame;
using Helion.Layer.Images;
using Helion.Layer.Menus;
using Helion.Layer.Worlds;
using Helion.Menus.Impl;
using Helion.Models;
using Helion.Render;
using Helion.Render.Common.Context;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Impl;
using Helion.Util.Consoles;
using Helion.Util.Consoles.Commands;
using Helion.Util.Extensions;
using Helion.Util.Profiling;
using Helion.Window;
using Helion.Window.Input;
using Helion.World;
using Helion.World.Save;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer;

/// <summary>
/// Responsible for coordinating input, logic, and rendering calls in order
/// for different kinds of layers.
/// </summary>
public class GameLayerManager : IGameLayerParent
{
    public ConsoleLayer? ConsoleLayer { get; private set; }
    public MenuLayer? MenuLayer { get; private set; }
    public ReadThisLayer? ReadThisLayer { get; private set; }
    public TitlepicLayer? TitlepicLayer { get; private set; }
    public EndGameLayer? EndGameLayer { get; private set; }
    public IntermissionLayer? IntermissionLayer { get; private set; }

    private static readonly string[] MenuIgnoreCommands = new[] { Constants.Input.Screenshot, Constants.Input.Console };

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

    private Renderer m_renderer;
    private IRenderableSurfaceContext m_ctx;
    private bool m_disposed;
    private int m_lastTick = -1;

    internal IEnumerable<IGameLayer> Layers => new List<IGameLayer?>
    {
        ConsoleLayer, MenuLayer, ReadThisLayer, TitlepicLayer, EndGameLayer, IntermissionLayer, WorldLayer
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
        m_renderDefaultAction = new(RenderDefault);
        m_renderHudAction = new(RenderHud);

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
        case WorldLayer layer:
            Remove(WorldLayer);
            WorldLayer = layer;
            break;
        case null:
            break;
        default:
            throw new ArgumentException($"Unknown object passed for layer: {gameLayer.GetType()}");
        }
    }

    public void SubmitConsoleText(string text)
    {
        m_console.ClearInputText();
        m_console.AddInput(text);
        m_console.SubmitInputText();
    }

    public void ClearAllExcept(params IGameLayer[] layers)
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
        else if (ReferenceEquals(layer, MenuLayer))
        {
            MenuLayer?.Dispose();
            MenuLayer = null;
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
    }

    public void HandleInput(IConsumableInput input)
    {
        input.NewGameTick = CheckNewGameTick();
        if (input.NewGameTick)
        {
            if (m_config.Keys.ConsumeCommandKeyPress(Constants.Input.Console, input, out _))
                ToggleConsoleLayer(input);
            ConsoleLayer?.HandleInput(input);

            if (ShouldCreateMenu(input))
            {
                if (ReadThisLayer != null)
                    Remove(ReadThisLayer);
                CreateMenuLayer();
            }

            if (!HasMenuOrConsole())
            {
                EndGameLayer?.HandleInput(input);
                TitlepicLayer?.HandleInput(input);
                IntermissionLayer?.HandleInput(input);
            }

            if (ReadThisLayer == null)
                MenuLayer?.HandleInput(input);

            if (ConsoleLayer == null)
                ReadThisLayer?.HandleInput(input);
        }

        WorldLayer?.HandleInput(input);
    }

    public bool IsNewGameTick()
    {
        if (WorldLayer == null)
            return true;

        return WorldLayer.World.GameTicker != m_lastTick;
    }

    public bool CheckNewGameTick()
    {
        if (WorldLayer == null)
            return true;

        if (WorldLayer.World.GameTicker == m_lastTick)
            return false;

        m_lastTick = WorldLayer.World.GameTicker;
        return true;
    }

    private bool ShouldCreateMenu(IConsumableInput input)
    {
        if (MenuLayer != null || ConsoleLayer != null)
            return false;

        bool hasMenuInput = (ReadThisLayer != null && input.ConsumeKeyDown(Key.Escape)) || input.Manager.HasAnyKeyPressed();

        if (TitlepicLayer != null && hasMenuInput && !CheckIgnoreMenuCommands(input))
        {
            // Have to eat the escape key if it exists, otherwise the menu will immediately close.
            input.ConsumeKeyPressed(Key.Escape);
            return true;
        }
        
        return input.ConsumeKeyPressed(Key.Escape);
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

        MenuLayer menuLayer = new(this, m_config, m_console, m_archiveCollection, m_soundManager, m_saveGameManager);
        Add(menuLayer);
    }

    public void GoToSaveOrLoadMenu(bool isSave)
    {
        if (MenuLayer == null)
            CreateMenuLayer();

        MenuLayer?.AddSaveOrLoadMenuIfMissing(isSave, true);
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
            MessageMenu confirm = new MessageMenu(m_config, m_console, m_soundManager, m_archiveCollection,
                new string[] { $"Are you sure you want to overwrite:", LastSave.Value.SaveGame.Model != null ? LastSave.Value.SaveGame.Model.Text : "Save",  "Press Y to confirm." },
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

    public void RunLogic()
    {
        ConsoleLayer?.RunLogic();
        MenuLayer?.RunLogic();
        ReadThisLayer?.RunLogic();
        EndGameLayer?.RunLogic();
        TitlepicLayer?.RunLogic();
        IntermissionLayer?.RunLogic();
        WorldLayer?.RunLogic();

        if (!HasMenuOrConsole() && m_stopwatch.ElapsedMilliseconds >= 1000.0 / Constants.TicksPerSecond)
        {
            m_stopwatch.Restart();
            EndGameLayer?.OnTick();
        }
    }

    public void OnTick()
    {
        // Nothing to tick.
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

        WorldLayer?.Render(ctx);

        m_profiler.Render.MiscLayers.Start();
        ctx.Hud(m_hudContext, m_renderHudAction);
        m_profiler.Render.MiscLayers.Stop();
    }

    private void RenderHud(IHudRenderContext hudCtx)
    {
        IntermissionLayer?.Render(m_ctx, hudCtx);
        TitlepicLayer?.Render(hudCtx);
        EndGameLayer?.Render(m_ctx, hudCtx);
        MenuLayer?.Render(hudCtx);
        ReadThisLayer?.Render(hudCtx);
        ConsoleLayer?.Render(m_ctx, hudCtx);
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
        Remove(ConsoleLayer);

        m_disposed = true;
    }
}
