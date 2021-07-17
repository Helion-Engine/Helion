using System;

namespace Helion.Layer.New.Worlds
{
    public partial class WorldLayer : IGameLayerParent
    {
        public IntermissionLayer? Intermission { get; private set; }
        private bool m_disposed;

        public void Remove(object layer)
        {
            if (ReferenceEquals(layer, Intermission))
            {
                Intermission?.Dispose();
                Intermission = null;
            }
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
