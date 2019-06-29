using System;
using System.Collections.Generic;

namespace Helion.Input
{
    public class InputEventArgs : EventArgs
    {
        public HashSet<InputKey> InputDown = new HashSet<InputKey>();
        public HashSet<InputKey> InputUp = new HashSet<InputKey>();
        public List<char> CharactersTyped = new List<char>();
        public MouseInputData MouseInput = new MouseInputData();
    }
}
