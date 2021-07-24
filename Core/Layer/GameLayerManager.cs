using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Input;
using Helion.Layer.Consoles;
using Helion.Layer.EndGame;
using Helion.Layer.Menus;
using Helion.Layer.Titlepic;
using Helion.Layer.Worlds;
using Helion.Render;
using Helion.Render.Common.Context;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer
{
    public class GameLayerManager : IGameLayerParent
    {
        public ConsoleLayer? ConsoleLayer { get; private set; }
        public MenuLayer? MenuLayer { get; private set; }
        public TitlepicLayer? TitlepicLayer { get; private set; }
        public EndGameLayer? EndGameLayer { get; private set; }
        public IntermissionLayer? IntermissionLayer { get; private set; }
        public WorldLayer? WorldLayer { get; private set; }
        private readonly Config m_config;
        private readonly IWindow m_window;
        private readonly HelionConsole m_console;
        private bool m_disposed;

        private Box2I WindowBox => new(Vec2I.Zero, m_window.Dimension.Vector);
        internal IEnumerable<IGameLayer> Layers => new List<IGameLayer?>
        {
            ConsoleLayer, MenuLayer, TitlepicLayer, EndGameLayer, IntermissionLayer, WorldLayer
        }.WhereNotNull();

        public GameLayerManager(Config config, IWindow window, HelionConsole console)
        {
            m_config = config;
            m_window = window;
            m_console = console;
        }

        ~GameLayerManager()
        {
            FailedToDispose(this);
            PerformDispose();
        }

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
        
        public void HandleInput(InputEvent input)
        {
            if (input.ConsumeKeyPressed(Key.Backtick) || input.ConsumeKeyPressed(Key.Tilde))
                ToggleConsoleLayer();

            ConsoleLayer?.HandleInput(input);
            MenuLayer?.HandleInput(input);
            EndGameLayer?.HandleInput(input);
            TitlepicLayer?.HandleInput(input);
            IntermissionLayer?.HandleInput(input);
            WorldLayer?.HandleInput(input);
        }

        private void ToggleConsoleLayer()
        {
            if (ConsoleLayer == null)
                Add(new ConsoleLayer(m_console));
            else
                Remove(ConsoleLayer);
        }

        public void RunLogic()
        {
            ConsoleLayer?.RunLogic();
            MenuLayer?.RunLogic();
            EndGameLayer?.RunLogic();
            TitlepicLayer?.RunLogic();
            IntermissionLayer?.RunLogic();
            WorldLayer?.RunLogic();
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
                
                ctx.Hud(hudContext, hud =>
                {
                    IntermissionLayer?.Render(ctx, hud);
                    TitlepicLayer?.Render(hud);
                    EndGameLayer?.Render(ctx, hud);
                    MenuLayer?.Render(hud);
                    ConsoleLayer?.Render(ctx, hud);
                });
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
            
            ConsoleLayer?.Dispose();
            MenuLayer?.Dispose();
            TitlepicLayer?.Dispose();
            EndGameLayer?.Dispose();
            IntermissionLayer?.Dispose();
            WorldLayer?.Dispose();
            
            ConsoleLayer = null;
            MenuLayer = null;
            TitlepicLayer = null;
            EndGameLayer = null;
            IntermissionLayer = null;
            WorldLayer = null;
            
            m_disposed = true;
        }
    }
}
