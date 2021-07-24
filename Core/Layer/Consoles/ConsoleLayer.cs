using System;
using Helion.Util.Consoles;
using Helion.Util.Timing;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer.Consoles
{
    public partial class ConsoleLayer : IGameLayer
    {
        private readonly HelionConsole m_console;
        private bool m_disposed;

        public ConsoleLayer(HelionConsole console)
        {
            m_console = console;
            
            console.ClearInputText();
        }

        ~ConsoleLayer()
        {
            FailedToDispose(this);
            PerformDispose();
        }
        
        public void RunLogic()
        {
            // Not used.
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

            // TODO
            
            m_console.ClearInputText();
            m_console.LastClosedNanos = Ticker.NanoTime();

            m_disposed = true;
        }
    }
}
