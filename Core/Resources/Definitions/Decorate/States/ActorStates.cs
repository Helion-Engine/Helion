using System.Collections.Generic;
using Helion.Util;

namespace Helion.Resources.Definitions.Decorate.States
{
    public class ActorStates
    {
        public readonly IDictionary<CIString, int> Labels = new Dictionary<CIString, int>();
        public readonly IDictionary<CIString, ActorFlowOverride> FlowOverrides = new Dictionary<CIString, ActorFlowOverride>();
        public readonly IList<ActorFrame> Frames = new List<ActorFrame>();
    }
}