using System;
using Helion.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities
{
    public class EntityRenderer : IDisposable
    {
        private readonly EntityDrawnTracker m_EntityDrawnTracker = new EntityDrawnTracker();
        
        ~EntityRenderer()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            PerformDispose();
        }

        public void Reset(WorldBase world)
        {
            m_EntityDrawnTracker.Reset(world);
        }
        
        public void Dispose()
        {
            PerformDispose();
            GC.SuppressFinalize(this);
        }

        private void PerformDispose()
        {
            // TODO
        }
    }
}