using System;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Enums;

namespace Helion.Render.Common.Renderers
{
    /// <summary>
    /// Performs HUD drawing commands. 
    /// </summary>
    public interface IHudRenderContext : IDisposable
    {
        /// <summary>
        /// The current (virtual) window dimension.
        /// </summary>
        Dimension Dimension { get; }
        
        int Width => Dimension.Width;
        int Height => Dimension.Height;
        
        /// <summary>
        /// Checks if an image exists. If true, then it can be drawn from with
        /// the image drawing commands.
        /// </summary>
        /// <param name="name">The case insensitive name.</param>
        /// <returns>True if such an image exists, false otherwise.</returns>
        bool ImageExists(string name);
        
        /// <summary>
        /// Equivalent to filling the viewport with the color provided.
        /// </summary>
        /// <remarks>
        /// Does not clear the depth or stencil buffer, only does color filling.
        /// </remarks>
        /// <param name="color">The color to fill with.</param>
        void Clear(Color color);
        
        void Point(Vec2I point, Color color, Align window = Align.TopLeft);
        
        void Points(Vec2I[] points, Color color, Align window = Align.TopLeft);
        
        void Line(Seg2D seg, Color color, Align window = Align.TopLeft);
        
        void Lines(Seg2D[] segs, Color color, Align window = Align.TopLeft);

        void DrawBox(HudBox box, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft);
        
        void DrawBoxes(HudBox[] boxes, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft);

        void FillBox(HudBox box, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft);
        
        void FillBoxes(HudBox[] boxes, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft);

        void Image(string texture, HudBox? area = null, Vec2I? origin = null, Align window = Align.TopLeft,
            Align anchor = Align.TopLeft, Align? both = null, Color? color = null, float alpha = 1.0f)
        {
            Image(texture, out _, area, origin, window, anchor, both, color, alpha);
        }
        
        void Image(string texture, out HudBox drawArea, HudBox? area = null, Vec2I? origin = null, 
            Align window = Align.TopLeft, Align anchor = Align.TopLeft, Align? both = null, Color? color = null, 
            float alpha = 1.0f);

        void Text(string text, string font, int fontSize, Vec2I origin, TextAlign textAlign = TextAlign.Left,
            Align window = Align.TopLeft, Align anchor = Align.TopLeft, Align? both = null, int maxWidth = int.MaxValue,
            int maxHeight = int.MaxValue, Color? color = null, float alpha = 1.0f)
        {
            Text(text, font, fontSize, origin, out _, textAlign, window, anchor, both, maxWidth, maxHeight, color, alpha);
        }
        
        void Text(string text, string font, int fontSize, Vec2I origin, out Dimension drawArea, TextAlign textAlign = TextAlign.Left, 
            Align window = Align.TopLeft, Align anchor = Align.TopLeft, Align? both = null, int maxWidth = int.MaxValue, 
            int maxHeight = int.MaxValue, Color? color = null, float alpha = 1.0f);

        /// <summary>
        /// See <see cref="PushVirtualDimension"/>. Designed such that it will
        /// not pop the virtual dimension.
        /// </summary>
        /// <param name="dimension">The dimension to render at.</param>
        /// <param name="action">The actions to do with the virtual resolution.
        /// </param>
        void VirtualDimension(Dimension dimension, Action action)
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
        void VirtualDimension(Dimension dimension, ResolutionScale scale, Action action)
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
    }
}
