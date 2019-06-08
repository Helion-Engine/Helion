namespace Helion.Util.Extensions
{
    /// <summary>
    /// A list of extensions for OpenTK classes.
    /// </summary>
    public static class OpenTKExtensions
    {
        public static OpenTK.Vector2 Interpolate(this OpenTK.Vector2 start, OpenTK.Vector2 end, float t) => start + (t * (end - start));
        public static OpenTK.Vector3 Interpolate(this OpenTK.Vector3 start, OpenTK.Vector3 end, float t) => start + (t * (end - start));
    }
}
