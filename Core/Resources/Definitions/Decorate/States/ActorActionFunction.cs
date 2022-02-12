using System.Collections.Generic;

namespace Helion.Resources.Definitions.Decorate.States;

public class ActorActionFunction
{
    public readonly string FunctionName;
    public readonly IList<object> Args;

    public ActorActionFunction(string functionName, IList<object> args)
    {
        FunctionName = functionName;
        Args = args;
    }

    public override string ToString() => FunctionName;
}
