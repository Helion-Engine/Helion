using System;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.Decorate.States;

public class ActorStates
{
    public readonly IDictionary<string, int> Labels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    public readonly IDictionary<string, ActorFlowOverride> FlowOverrides = new Dictionary<string, ActorFlowOverride>(StringComparer.OrdinalIgnoreCase);
    public readonly IList<ActorFrame> Frames = new List<ActorFrame>();
}
