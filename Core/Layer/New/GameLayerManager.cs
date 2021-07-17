using System;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Input;
using Helion.Layer.New.Consoles;
using Helion.Layer.New.Menus;
using Helion.Layer.New.Titlepic;
using Helion.Layer.New.Worlds;
using Helion.Render;
using Helion.Render.Common.Context;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer.New
{
    public class GameLayerManager : IGameLayerParent
    {
        internal ConsoleLayerNew? ConsoleLayer { get; private set; }
        internal MenuLayerNew? MenuLayer { get; private set; }
        internal TitlepicLayerNew? TitlepicLayer { get; private set; }
        internal IntermissionLayerNew? IntermissionLayer { get; private set; }
        internal WorldLayer? WorldLayer { get; private set; }
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
        
        public void Remove(object layer)
        {
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
            TitlepicLayer?.HandleInput(input);
            IntermissionLayer?.HandleInput(input);
            WorldLayer?.HandleInput(input);
        }
        
        public void RunLogic()
        {
            ConsoleLayer?.RunLogic();
            MenuLayer?.RunLogic();
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
                
                WorldLayer?.Render(ctx);

                ctx.Hud(hudContext, hud =>
                {
                    IntermissionLayer?.Render(ctx, hud);
                    TitlepicLayer?.Render(hud);
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
            
            IntermissionLayer?.Dispose();
            IntermissionLayer = null;
            
            WorldLayer?.Dispose();
            WorldLayer = null;

            m_disposed = true;
        }
    }
}
