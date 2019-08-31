using System;
using Helion.Util.Geometry;
using Helion.World;
using Helion.World.Bsp;
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

        public void Clear(WorldBase world)
        {
            m_EntityDrawnTracker.Reset(world);
        }

        public void RenderSubsector(Subsector subsector, Vec2D position)
        {
            // TODO
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