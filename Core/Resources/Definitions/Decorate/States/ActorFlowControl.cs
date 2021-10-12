using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions.Decorate.States;

public struct ActorFlowControl
{
    public readonly ActorStateBranch FlowType;
    public readonly string Label;
    public readonly string Parent;
    public readonly int Offset;

    public ActorFlowControl(ActorStateBranch type)
    {
        Precondition(type != ActorStateBranch.Goto, "Using wrong actor flow control constructor");

        FlowType = type;
        Label = "";
        Parent = "";
        Offset = 0;
    }

    public ActorFlowControl(ActorStateBranch type, string parent, string label, int offset)
    {
        Precondition(type == ActorStateBranch.Goto, "Using wrong actor flow control constructor (should be goto)");
        Precondition(!label.Empty(), "Should not be using an label on a goto statement");
        Precondition(offset >= 0, "Should not be using a zero or negative actor goto offset");

        FlowType = type;
        Parent = parent;
        Label = label;
        Offset = offset;
    }

    public override string ToString() => $"{FlowType} (parent={Parent} label={Label} offset={Offset})";
}
