using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions.Decorate.States
{
    /// <summary>
    /// A wrapper around the case where a label is immediately followed by some
    /// flow command. This comes up with inheritance and redirecting states or
    /// removing them.
    /// </summary>
    public class ActorFlowOverride
    {
        public readonly ActorStateBranch BranchType = ActorStateBranch.Stop;
        public readonly string? Parent;
        public readonly string? Label;
        public readonly int? Offset;

        public ActorFlowOverride()
        {
        }
        
        public ActorFlowOverride(string label, string? parent, int? offset)
        {
            Precondition(!label.Empty(), "Cannot do a Goto label override with an empty label");
                
            BranchType = ActorStateBranch.Goto;
            Parent = parent;
            Label = label;
            Offset = offset;
        }
    }
}