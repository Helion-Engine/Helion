namespace Helion.Render.OpenGL.Util
{
    /// <summary>
    /// What kind of renderer we want.
    /// </summary>
    public enum GLRenderType
    {
        /// <summary>
        /// Slow and archaic, runs very slowly with not too many optimizations
        /// and designed for very old hardware between 2004 - 2008.
        /// </summary>
        Legacy,
        
        /// <summary>
        /// A high speed renderer which allows for some heavy optimizations.
        /// Intended for anyone who uses a computer from 2008 onwards.
        /// </summary>
        Standard,
        
        /// <summary>
        /// Modern OpenGL is for the set of people who have access to the most
        /// modern features (in particular, bindless rendering).
        /// </summary>
        Modern,
    }
}