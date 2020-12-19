using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resource.Definitions.Decorate.States
{
    /// <summary>
    /// A frame in the decorate states.
    /// </summary>
    public class ActorFrame
    {
        /// <summary>
        /// The 5 letter frame (ex: POSSA).
        /// </summary>
        public readonly CIString Name;

        /// <summary>
        /// How long this frame lasts.
        /// </summary>
        public readonly int Ticks;

        /// <summary>
        /// Additional properties for the frame.
        /// </summary>
        public readonly ActorFrameProperties Properties;

        /// <summary>
        /// A function associated with the frame.
        /// </summary>
        public readonly ActorActionFunction? ActionFunction;

        /// <summary>
        /// Additional control flow information (ex: does it jump, stop, etc).
        /// </summary>
        public ActorFlowControl? FlowControl;

        public ActorFrame(CIString name, int ticks, ActorFrameProperties properties,
            ActorActionFunction? actionFunction)
        {
            Precondition(name.Length == 5, $"Expected sprite/frame to be 5 letters (got {name} instead)");

            Name = name;
            Ticks = ticks;
            Properties = properties;
            ActionFunction = actionFunction;
        }

        public override string ToString() => $"{Name} {Ticks} action={ActionFunction} flow={FlowControl}]";
    }
}