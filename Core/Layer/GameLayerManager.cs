using System;
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
        private readonly IWindow m_window;
        private bool m_disposed;
        
        private Box2I WindowBox => new(Vec2I.Zero, m_window.Dimension.Vector);

        public GameLayerManager(IWindow window)
        {
            m_window = window;
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
            ConsoleLayer?.HandleInput(input);
            MenuLayer?.HandleInput(input);
            EndGameLayer?.HandleInput(input);
            TitlepicLayer?.HandleInput(input);
            IntermissionLayer?.HandleInput(input);
            WorldLayer?.HandleInput(input);
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
            ConsoleLayer = null;
            
            MenuLayer?.Dispose();
            MenuLayer = null;
            
            TitlepicLayer?.Dispose();
            TitlepicLayer = null;
            
            EndGameLayer?.Dispose();
            EndGameLayer = null;
            
            IntermissionLayer?.Dispose();
            IntermissionLayer = null;
            
            WorldLayer?.Dispose();
            WorldLayer = null;

            m_disposed = true;
        }
    }
}
