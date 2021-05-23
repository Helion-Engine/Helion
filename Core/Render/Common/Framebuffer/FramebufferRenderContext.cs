using System;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.World;
using Helion.Util.Extensions;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.Common.Framebuffer
{
    /// <summary>
    /// A collection of commands to render with the framebuffer.
    /// </summary>
    public class FramebufferRenderContext
    {
        private readonly IFramebuffer m_framebuffer;
        private readonly IHudRenderer m_hudRenderer;
        private readonly IWorldRenderer m_worldRenderer;
        private Box2I m_viewport;
        private Box2I m_scissor;

        public Dimension Dimension => m_framebuffer.Dimension;

        public FramebufferRenderContext(IFramebuffer framebuffer, IHudRenderer hudRenderer, IWorldRenderer worldRenderer)
        {
            m_framebuffer = framebuffer;
            m_hudRenderer = hudRenderer;
            m_worldRenderer = worldRenderer;
            m_viewport = ((0, 0), Dimension.Vector);
            m_scissor = ((0, 0), Dimension.Vector);
        }

        /// <summary>
        /// Given a point inside the bounds, clamp the dimension variable such
        /// that it never goes outside of the bounds relative to the point
        /// </summary>
        /// <param name="origin">The bottom left corner where the dimension
        /// shoots out of.</param>
        /// <param name="dimension">The desired dimension.</param>
        /// <param name="bounds">The bounds to constrain our dimension.</param>
        /// <returns>A dimension that is either equal to the passed in dimension,
        /// or a truncated one that overflowed the bounds provided.</returns>
        private static Dimension ClampDimension(Vec2I origin, Dimension dimension, Box2I bounds)
        {
            Vec2I maxDim = bounds.Max - origin;
            int width = dimension.Width.Clamp(0, maxDim.X);
            int height = dimension.Height.Clamp(0, maxDim.Y);
            return (width, height);
        }

        /// <summary>
        /// Clears the color, depth, and stencil buffer based on what the
        /// arguments are set to.
        /// </summary>
        /// <param name="color">The color to clear.</param>
        /// <param name="depth">True to clear the depth buffer.</param>
        /// <param name="stencil">True to clear the stencil buffer.</param>
        public void Clear(Color color, bool depth, bool stencil)
        {
            GL.ClearColor(color);

            ClearBufferMask mask = ClearBufferMask.ColorBufferBit;
            if (depth)
                mask |= ClearBufferMask.DepthBufferBit;
            if (stencil)
                mask |= ClearBufferMask.StencilBufferBit;
            
            GL.Clear(mask);
        }

        /// <summary>
        /// Clears the depth buffer only.
        /// </summary>
        public void ClearDepth()
        {
            GL.Clear(ClearBufferMask.DepthBufferBit);
        }
        
        /// <summary>
        /// Clears the stencil buffer only.
        /// </summary>
        public void ClearStencil()
        {
            GL.Clear(ClearBufferMask.StencilBufferBit);
        }
        
        /// <summary>
        /// Begins a session for drawing with the hud renderer.
        /// </summary>
        /// <param name="action">The rendering actions.</param>
        public void Hud(Action<IHudRenderer> action)
        {
            action(m_hudRenderer);
            
            m_hudRenderer.Render(m_viewport.Dimension);
        }

        /// <summary>
        /// Begins a session for drawing with the world renderer.
        /// </summary>
        /// <param name="context">The rendering data.</param>
        /// <param name="action">The rendering actions.</param>
        public void World(WorldRenderContext context, Action<IWorldRenderer> action)
        {
            action(m_worldRenderer);
            
            // This sucks, but we have no choice but to update the context with
            // the viewport that we're planning on using. The user should not
            // set this anyways because they do not know the viewport.
            context.Viewport = m_viewport.Dimension;
            
            m_worldRenderer.Render(context);
        }

        /// <summary>
        /// Sets the viewport to the origin and dimension provided. All drawing
        /// commands inside the action will use the viewport provided. This will
        /// allow for nested calls which continually shrink the window. As an
        /// example, if this is called at (100, 100) for the origin, and then
        /// again with (100, 100), it will have an origin at (200, 200) because
        /// the next viewport call is relative to this call. If this nesting is
        /// not desired, use <see cref="ViewportAbsolute"/>.
        /// </summary>
        /// <param name="origin">The bottom left corner. This is relative to
        /// the bottom left corner of the window as per OpenGL standards.
        /// </param>
        /// <param name="dimension">The window width and height. This will be
        /// either the value provided, or smaller if the dimension goes beyond
        /// its drawable area.</param>
        /// <param name="action">The action to perform.</param>
        public void Viewport(Vec2I origin, Dimension dimension, Action<Dimension> action)
        {
            if (origin == Vec2I.Zero && dimension.Vector == m_viewport.Sides)
            {
                action(dimension);
                return;
            }
            
            Box2I current = m_viewport;
            
            Vec2I newOrigin = m_viewport.Min + origin;
            Dimension newDimension = ClampDimension(newOrigin, dimension, m_viewport);
            Box2I newViewport = (newOrigin, newOrigin + newDimension.Vector);

            m_viewport = newViewport;
            
            GL.Viewport(newViewport.Min.X, newViewport.Min.Y, newViewport.Width, newViewport.Height);
            action(newDimension);
            GL.Viewport(current.Min.X, current.Min.Y, current.Width, current.Height);

            m_viewport = current;
        }

        /// <summary>
        /// Sets the viewport to the absolute coordinate based on the lower left
        /// corner.
        /// </summary>
        /// <param name="origin">The bottom left corner. This is relative to
        /// the bottom left corner of the window as per OpenGL standards.
        /// </param>
        /// <param name="dimension">The window width and height. This will be
        /// either the value provided, or smaller if the dimension goes beyond
        /// its drawable area.</param>
        /// <param name="action">The action to perform.</param>
        public void ViewportAbsolute(Vec2I origin, Dimension dimension, Action action)
        {
            Box2I current = m_viewport;
            Box2I newViewport = (origin, origin + dimension.Vector);
            
            m_viewport = newViewport;
            
            GL.Viewport(newViewport.Min.X, newViewport.Min.Y, newViewport.Width, newViewport.Height);
            action();
            GL.Viewport(current.Min.X, current.Min.Y, current.Width, current.Height);

            m_viewport = current;
        }

        /// <summary>
        /// Sets the scissor box to the origin and dimension provided. All
        /// drawing commands inside the action will use the scissor box
        /// provided. This will allow for nested calls which continually shrink
        /// the window. As an example, if this is called at (100, 100) for the
        /// origin, and then again with (100, 100), it will have an origin at
        /// (200, 200) because the next viewport call is relative to this call.
        /// If this nesting is not desired, use <see cref="ScissorAbsolute"/>.
        /// </summary>
        /// <param name="origin">The bottom left corner. This is relative to
        /// the bottom left corner of the window as per OpenGL standards.
        /// </param>
        /// <param name="dimension">The window width and height. This will be
        /// either the value provided, or smaller if the dimension goes beyond
        /// its drawable area.</param>
        /// <param name="action">The action to perform.</param>
        public void Scissor(Vec2I origin, Dimension dimension, Action action)
        {
            if (origin == Vec2I.Zero && dimension.Vector == m_scissor.Sides)
            {
                action();
                return;
            }
            
            Box2I current = m_scissor;
            
            Vec2I newOrigin = m_scissor.Min + origin;
            Dimension newDimension = ClampDimension(newOrigin, dimension, m_scissor);
            Box2I newScissor = (newOrigin, newOrigin + newDimension.Vector);

            m_scissor = newScissor;
            
            GL.Scissor(newScissor.Min.X, newScissor.Min.Y, newScissor.Width, newScissor.Height);
            action();
            GL.Scissor(current.Min.X, current.Min.Y, current.Width, current.Height);

            m_scissor = current;
        }
        
        /// <summary>
        /// Sets the scissor box to the absolute coordinate based on the lower
        /// left corner.
        /// </summary>
        /// <param name="origin">The bottom left corner. This is relative to
        /// the bottom left corner of the window as per OpenGL standards.
        /// </param>
        /// <param name="dimension">The window width and height.</param>
        /// <param name="action">The action to perform.</param>
        public void ScissorAbsolute(Vec2I origin, Dimension dimension, Action action)
        {
            Box2I current = m_scissor;
            Box2I newScissor = (origin, origin + dimension.Vector);

            m_scissor = newScissor;
            
            GL.Scissor(newScissor.Min.X, newScissor.Min.Y, newScissor.Width, newScissor.Height);
            action();
            GL.Scissor(current.Min.X, current.Min.Y, current.Width, current.Height);

            m_scissor = current;
        }
        
        /// <summary>
        /// Performs a relative viewport and scissor command. The scissor box
        /// is set to whatever the viewport ends up being. This is stackable.
        /// See <see cref="Viewport"/> for a detailed description.
        /// </summary>
        /// <param name="origin">The bottom left corner. This is relative to
        /// the bottom left corner of the window as per OpenGL standards.
        /// </param>
        /// <param name="dimension">The window width and height. This will be
        /// either the value provided, or smaller if the dimension goes beyond
        /// its drawable area.</param>
        /// <param name="action">The action to perform.</param>
        public void ViewportScissor(Vec2I origin, Dimension dimension, Action<Dimension> action)
        {
            Viewport(origin, dimension, viewportDim =>
            {
                ScissorAbsolute(m_viewport.Min, m_viewport.Dimension, () =>
                {
                    action(viewportDim);
                });
            });
        }
        
        /// <summary>
        /// Same as <see cref="ViewportScissor"/> but for absolute coordinates.
        /// </summary>
        /// <param name="origin">The bottom left corner. This is relative to
        /// the bottom left corner of the window as per OpenGL standards.
        /// </param>
        /// <param name="dimension">The window width and height.</param>
        /// <param name="action">The action to perform.</param>
        public void ViewportScissorAbsolute(Vec2I origin, Dimension dimension, Action<Dimension> action)
        {
            ViewportAbsolute(origin, dimension, () =>
            {
                ScissorAbsolute(origin, dimension, () =>
                {
                    action(dimension);
                });
            });
        }
    }
}
