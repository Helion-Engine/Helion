using System;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Graphics.String;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Textures;

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
        
        /// <summary>
        /// The texture manager that this context uses.
        /// </summary>
        IRendererTextureManager Textures { get; }
        
        int Width => Dimension.Width;
        int Height => Dimension.Height;

        /// <summary>
        /// Equivalent to filling the viewport with the color provided.
        /// </summary>
        /// <remarks>
        /// Does not clear the depth or stencil buffer, only does color filling.
        /// </remarks>
        /// <param name="color">The color to fill with.</param>
        /// <param name="alpha">The alpha value for the color.</param>
        void Clear(Color color, float alpha = 1.0f);
        
        void Point(Vec2I point, Color color, Align window = Align.TopLeft, float alpha = 1.0f);
        
        void Points(Vec2I[] points, Color color, Align window = Align.TopLeft, float alpha = 1.0f);
        
        void Line(Seg2D seg, Color color, Align window = Align.TopLeft, float alpha = 1.0f);
        
        void Lines(Seg2D[] segs, Color color, Align window = Align.TopLeft, float alpha = 1.0f);

        void DrawBox(HudBox box, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft, float alpha = 1.0f);
        
        void DrawBoxes(HudBox[] boxes, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft, float alpha = 1.0f);

        void FillBox(HudBox box, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft, float alpha = 1.0f);
        
        void FillBoxes(HudBox[] boxes, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft, float alpha = 1.0f);

        void Image(string texture, Vec2I origin, Align window = Align.TopLeft, Align anchor = Align.TopLeft, 
            Align? both = null, Color? color = null, float scale = 1.0f, float alpha = 1.0f)
        {
            Image(texture, origin, out _, window, anchor, both, color, scale, alpha);
        }
        
        void Image(string texture, HudBox area, Align window = Align.TopLeft, Align anchor = Align.TopLeft, 
            Align? both = null, Color? color = null, float scale = 1.0f, float alpha = 1.0f)
        {
            Image(texture, area, out _, window, anchor, both, color, scale, alpha);
        }
        
        void Image(string texture, HudBox area, out HudBox drawArea, Align window = Align.TopLeft, 
            Align anchor = Align.TopLeft, Align? both = null, Color? color = null, float scale = 1.0f, 
            float alpha = 1.0f);
        
        void Image(string texture, Vec2I origin, out HudBox drawArea, Align window = Align.TopLeft, 
            Align anchor = Align.TopLeft, Align? both = null, Color? color = null, float scale = 1.0f, 
            float alpha = 1.0f);

        void Text(string text, string font, int fontSize, Vec2I origin, TextAlign textAlign = TextAlign.Left,
            Align window = Align.TopLeft, Align anchor = Align.TopLeft, Align? both = null, int maxWidth = int.MaxValue,
            int maxHeight = int.MaxValue, Color? color = null, float scale = 1.0f, float alpha = 1.0f)
        {
            Text(text, font, fontSize, origin, out _, textAlign, window, anchor, both, maxWidth, maxHeight, color, scale, alpha);
        }
        
        void Text(ColoredString text, string font, int fontSize, Vec2I origin, TextAlign textAlign = TextAlign.Left,
            Align window = Align.TopLeft, Align anchor = Align.TopLeft, Align? both = null, int maxWidth = int.MaxValue,
            int maxHeight = int.MaxValue, float scale = 1.0f, float alpha = 1.0f)
        {
            Text(text, font, fontSize, origin, out _, textAlign, window, anchor, both, maxWidth, maxHeight, scale, alpha);
        }
        
        void Text(ColoredString text, string font, int fontSize, Vec2I origin, out Dimension drawArea, TextAlign textAlign = TextAlign.Left, 
            Align window = Align.TopLeft, Align anchor = Align.TopLeft, Align? both = null, int maxWidth = int.MaxValue, 
            int maxHeight = int.MaxValue, float scale = 1.0f, float alpha = 1.0f);

        void Text(string text, string font, int fontSize, Vec2I origin, out Dimension drawArea, TextAlign textAlign = TextAlign.Left, 
            Align window = Align.TopLeft, Align anchor = Align.TopLeft, Align? both = null, int maxWidth = int.MaxValue, 
            int maxHeight = int.MaxValue, Color? color = null, float scale = 1.0f, float alpha = 1.0f);

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
        /// Due to how common this is, this function pushes and pops a virtual
        /// doom resolution.
        /// </summary>
        /// <param name="action">The actions to take in the new resolution.</param>
        /// <param name="resolutionScale">The scale to use, by default is none.</param>
        void DoomVirtualResolution(Action action, ResolutionScale resolutionScale = ResolutionScale.None)
        {
            VirtualDimension((320, 200), resolutionScale, action);
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
