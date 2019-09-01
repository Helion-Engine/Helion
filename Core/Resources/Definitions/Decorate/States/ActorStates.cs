using System.Collections.Generic;
using Helion.Util;

namespace Helion.Resources.Definitions.Decorate.States
{
    public class ActorStates
    {
        public IDictionary<CIString, int> Labels = new Dictionary<CIString, int>();
        public IDictionary<CIString, ActorFlowOverride> FlowOverrides = new Dictionary<CIString, ActorFlowOverride>();
        public IList<ActorFrame> Frames = new List<ActorFrame>();
    }
}