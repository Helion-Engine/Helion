using System;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.Decorate.Properties
{
    public class PlayerColorSetProperty
    {
        public readonly string Name;
        public readonly int Number;
        public readonly Range Range;
        public readonly List<string> Color;

        public PlayerColorSetProperty(string name, int number, Range range, params string[] colors)
        {
            Name = name;
            Number = number;
            Range = range;
            Color = new List<string>(colors);
        }
    }
}