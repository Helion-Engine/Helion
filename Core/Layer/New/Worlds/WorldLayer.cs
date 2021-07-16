using System;
using Helion.Input;

namespace Helion.Layer.New.Worlds
{
    public class WorldLayer : IGameLayerParent
    {
        public IntermissionLayer? Intermission { get; private set; } = null;
        private bool m_disposed;

        public void Remove(object layer)
        {
            // TODO
        }

        public void HandleInput(InputEvent input)
        {
            // TODO
        }

        public void RunLogic()
        {
            // TODO
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;
            
            Intermission?.Dispose();
            Intermission = null;

            m_disposed = true;
        }
    }
}
