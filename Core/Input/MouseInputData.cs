using Helion.Util.Geometry;

namespace Helion.Input
{
    /// <summary>
    /// Contains all the mouse movement/scroll data.
    /// </summary>
    public class MouseInputData
    {
        /// <summary>
        /// How many units the mouse has moved. This likely is with respect to
        /// the number of pixels it moved.
        /// </summary>
        public Vec2i Delta = new Vec2i(0, 0);

        /// <summary>
        /// How many scroll events were found.
        /// </summary>
        public int ScrollDelta;

        /// <summary>
        /// Adds the values in the mouse input data to the current object.
        /// </summary>
        /// <param name="mouseInput">The data to add.</param>
        public void Add(MouseInputData mouseInput)
        {
            Delta += mouseInput.Delta;
            ScrollDelta += mouseInput.ScrollDelta;
        }

        /// <summary>
        /// Resets the input to the default values.
        /// </summary>
        public void Reset()
        {
            Delta = new Vec2i(0, 0);
            ScrollDelta = 0;
        }
    }
}
