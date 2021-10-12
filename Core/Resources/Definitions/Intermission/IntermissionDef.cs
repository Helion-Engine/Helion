using System;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.Intermission;

public class IntermissionDef
{
    public string Background { get; set; } = string.Empty;
    public string Splat { get; set; } = string.Empty;
    public IList<string> Pointer { get; set; } = Array.Empty<string>();
    public IList<IntermissionAnimation> Animations { get; set; } = Array.Empty<IntermissionAnimation>();
    public IList<IntermissionSpot> Spots { get; set; } = Array.Empty<IntermissionSpot>();
}
