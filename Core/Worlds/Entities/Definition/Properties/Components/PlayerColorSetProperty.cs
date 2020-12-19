using System;
using System.Collections.Generic;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Worlds.Entities.Definition.Properties.Components
{
    public class PlayerColorSetProperty
    {
        public readonly int Number;
        public readonly string Name;
        public readonly Range Range;
        public readonly List<string> Colors;

        public PlayerColorSetProperty(int number, string name, Range range, List<string> colors)
        {
            Precondition(!colors.Empty(), "Player color set property cannot have no colors");

            Name = name;
            Number = number;
            Range = range;
            Colors = colors;
        }
    }
}