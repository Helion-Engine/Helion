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
    public class GameLayerManager : IGameLayer
    {
        internal ConsoleLayerNew? ConsoleLayer;
        internal MenuLayerNew? MenuLayer;
        internal TitlepicLayerNew? TitlepicLayer;
        internal IntermissionLayerNew? IntermissionLayer;
        internal WorldLayer? WorldLayer;
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
                
                WorldLayer?.Render(renderer);

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
