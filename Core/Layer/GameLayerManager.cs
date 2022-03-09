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
using Helion.Render;
using Helion.Render.Common.Context;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Consoles.Commands;
using Helion.Util.Extensions;
using Helion.Util.Profiling;
using Helion.Window;
using Helion.Window.Input;
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
    private readonly IConfig m_config;
    private readonly IWindow m_window;
    private readonly HelionConsole m_console;
    private readonly ConsoleCommands m_consoleCommands;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly SoundManager m_soundManager;
    private readonly SaveGameManager m_saveGameManager;
    private readonly Profiler m_profiler;
    private readonly Stopwatch m_stopwatch = new();
    private bool m_disposed;

    private Box2I WindowBox => new(Vec2I.Zero, m_window.Dimension.Vector);
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
    }

    ~GameLayerManager()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public bool HasMenuOrConsole() => MenuLayer != null || ConsoleLayer != null;

    public bool ShouldFocus()
    {
        if (ConsoleLayer != null)
            return false;

        if (TitlepicLayer != null)
            return MenuLayer != null;

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
        if (m_config.Keys.ConsumeCommandKeyPress(Constants.Input.Console, input))
            ToggleConsoleLayer(input);
        ConsoleLayer?.HandleInput(input);

        if (ShouldCreateMenu(input))
        {
            if (ReadThisLayer != null)
                Remove(ReadThisLayer);
            CreateMenuLayer();
        }

        MenuLayer?.HandleInput(input);
        if (!HasMenuOrConsole())
        {
            EndGameLayer?.HandleInput(input);
            ReadThisLayer?.HandleInput(input);
            TitlepicLayer?.HandleInput(input);
            IntermissionLayer?.HandleInput(input);
        }
        WorldLayer?.HandleInput(input);
    }

    private bool ShouldCreateMenu(IConsumableInput input)
    {
        if (MenuLayer != null || ConsoleLayer != null)
            return false;

        bool hasMenuInput = (ReadThisLayer != null && input.ConsumeKeyDown(Key.Escape)) || input.Manager.HasAnyKeyPressed();

        if (TitlepicLayer != null && hasMenuInput &&
            !MenuIgnoreCommands.Any(x => m_config.Keys.IsCommandKeyDown(x, input)))
        {
            // Have to eat the escape key if it exists, otherwise the menu will immediately close.
            input.ConsumeKeyPressed(Key.Escape);
            return true;
        }
        
        return input.ConsumeKeyPressed(Key.Escape);
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

        MenuLayer?.AddSaveOrLoadMenuIfMissing(isSave);
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

    public void Render(IRenderer renderer)
    {
        renderer.DefaultSurface.Render(ctx =>
        {
            HudRenderContext hudContext = new(m_window.Dimension);

            ctx.Viewport(WindowBox);
            ctx.Scissor(WindowBox);
            ctx.Clear(IRenderer.DefaultBackground, true, true);

            WorldLayer?.Render(ctx);

            m_profiler.Render.MiscLayers.Start();
            ctx.Hud(hudContext, hud =>
            {
                IntermissionLayer?.Render(ctx, hud);
                TitlepicLayer?.Render(hud);
                EndGameLayer?.Render(ctx, hud);
                ReadThisLayer?.Render(hud);
                MenuLayer?.Render(hud);
                ConsoleLayer?.Render(ctx, hud);
            });
            m_profiler.Render.MiscLayers.Stop();
        });
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
