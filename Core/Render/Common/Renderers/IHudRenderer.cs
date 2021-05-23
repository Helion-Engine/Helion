using System;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Enums;
using Helion.Render.Common.FrameBuffer;

namespace Helion.Render.Common.Renderers
{
    /// <summary>
    /// Performs HUD drawing commands. 
    /// </summary>
    public interface IHudRenderer : IDisposable
    {
        void Clear(Color color);
        
        void Point(Vec2I point, Color color, Align window = Align.TopLeft);
        
        void Points(Vec2I[] points, Color color, Align window = Align.TopLeft);
        
        void Line(Seg2D seg, Color color, Align window = Align.TopLeft);
        
        void Lines(Seg2D[] segs, Color color, Align window = Align.TopLeft);

        void DrawBox(Box2I box, Color color, Align window = Align.TopLeft);
        
        void DrawBoxes(Box2I[] boxes, Color color, Align window = Align.TopLeft);

        void FillBox(Box2I box, Color color, Align window = Align.TopLeft);
        
        void FillBoxes(Box2I[] boxes, Color color, Align window = Align.TopLeft);
        
        void Image(string texture, Vec2I origin, Dimension? dimension = null, Align window = Align.TopLeft, 
            Align image = Align.TopLeft, Align? both = null, Color? color = null, float alpha = 1.0f);

        void Text(string text, string font, int height, Vec2I origin, Color? color = null);
        
        void FrameBuffer(IFrameBuffer frameBuffer, Vec2I origin, Dimension? dimension = null, Align window = Align.TopLeft, 
            Align image = Align.TopLeft, Align? both = null, Color? color = null, float alpha = 1.0f);

        /// <summary>
        /// See <see cref="PushVirtualDimension"/>. Designed such that it will
        /// not pop the virtual dimension.
        /// </summary>
        /// <param name="dimension">The dimension to render at.</param>
        /// <param name="action">The actions to do with the virtual resolution.
        /// </param>
        void Virtual(Dimension dimension, Action action)
        {
            PushVirtualDimension(dimension);
            action();
            PopVirtualDimension();
        }   
        
        /// <summary>
        /// See <see cref="PushVirtualDimension"/>. Designed such that it will
        /// not pop the virtual dimension.
        /// </summary>
        /// <param name="dimension">The dimension to render at.</param>
        /// <param name="scale">The scale to use. If null, uses the previous
        /// resolution scale, or if none exists, uses None.</param>
        /// <param name="action">The actions to do with the virtual resolution.
        /// </param>
        void Virtual(Dimension dimension, ResolutionScale scale, Action action)
        {
            PushVirtualDimension(dimension, scale);
            action();
            PopVirtualDimension();
        }   
        
        /// <summary>
        /// Starts rendering at a virtual dimension. All subsequent rendering
        /// calls will use this dimension until <see cref="PopVirtualDimension"/>
        /// is called.
        /// </summary>
        /// <remarks>
        /// It is done this way without a lambda to avoid creating new objects.
        /// </remarks>
        /// <param name="dimension">The dimension to render at.</param>
        /// <param name="scale">The scale to use. If null, uses the previous
        /// resolution scale, or if none exists, uses None.</param>
        void PushVirtualDimension(Dimension dimension, ResolutionScale? scale = null);

        /// <summary>
        /// Pops a previous <see cref="PushVirtualDimension"/>. Should be called
        /// when done with a previous push invocation. This is safe to call and
        /// it will do nothing if the stack is empty.
        /// </summary>
        void PopVirtualDimension();

        /// <summary>
        /// Renders all of the accumulated commands.
        /// </summary>
        /// <param name="viewport">The dimensions of the viewport.</param>
        void Render(Dimension viewport);
    }
}
