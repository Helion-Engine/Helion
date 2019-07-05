using System;
using System.Collections.Generic;

namespace Helion.Input
{
    public class InputEventArgs : EventArgs
    {
        public readonly HashSet<InputKey> InputDown = new HashSet<InputKey>();
        public readonly HashSet<InputKey> InputUp = new HashSet<InputKey>();
        public readonly List<char> CharactersTyped = new List<char>();
        public readonly MouseInputData MouseInput = new MouseInputData();
    }
}
