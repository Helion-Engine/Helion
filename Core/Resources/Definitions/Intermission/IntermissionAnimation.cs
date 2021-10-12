using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;

namespace Helion.Resources.Definitions.Intermission;

public class IntermissionAnimation
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Tics { get; set; }
    public int Tic { get; set; }
    public IntermissionAnimationType Type { get; set; }
    public string MapName { get; set; } = string.Empty;
    public bool Once { get; set; }
    public IList<string> Items { get; set; } = Array.Empty<string>();
    public int ItemIndex { get; set; }
    public bool ShouldDraw { get; set; }

    public Vec2I Vector => (X, Y);
}

