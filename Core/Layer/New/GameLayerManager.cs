using System;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Input;
using Helion.Layer.New.Console;
using Helion.Render;
using Helion.Render.Common.Context;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer.New
{
    public class GameLayerManager : IDisposable
    {
        private readonly IWindow m_window;
        private ConsoleLayerNew? m_consoleLayer;
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
            m_consoleLayer?.HandleInput(input);
        }
        
        public void RunLogic()
        {
            // To be called when something needs to run logic.
        }
        
        public void Render(IRenderer renderer)
        {
            renderer.DefaultSurface.Render(s =>
            {
                // TODO: Stop adding GC pressure...
                HudRenderContext hudContext = new(m_window.Dimension);
                
                s.Viewport(WindowBox);
                s.Scissor(WindowBox);
                
                // TODO: Draw the world.

                s.Hud(hudContext, hud =>
                {
                    // TODO: Draw the menu.
                    
                    s.ClearDepth();
                    m_consoleLayer?.Render(hud);
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

            m_consoleLayer?.Dispose();
            m_consoleLayer = null;

            m_disposed = true;
        }
    }
}
