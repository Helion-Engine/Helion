using System.Collections.Generic;

namespace Helion.Input
{
    /// <summary>
    /// Manages all input from some source and converts it into an internal
    /// format we can use.
    /// </summary>
    public class InputManager
    {
        private readonly HashSet<InputCollection> collections = new HashSet<InputCollection>();

        /// <summary>
        /// Creates a new collection. The collection will start being fed input
        /// events and can be polled from upon returning.
        /// </summary>
        /// <returns>A new collection</returns>
        public InputCollection RegisterCollection()
        {
            InputCollection newCollection = new InputCollection();
            collections.Add(newCollection);
            return newCollection;
        }

        public void HandleInputEvent(object sender, InputEventArgs inputEvent)
        {
            foreach (InputCollection collection in collections)
                collection.HandleInputEvent(inputEvent);
        }
    }
}
