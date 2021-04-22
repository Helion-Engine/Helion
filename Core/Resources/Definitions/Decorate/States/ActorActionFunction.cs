namespace Helion.Resources.Definitions.Decorate.States
{
    public class ActorActionFunction
    {
        public readonly string FunctionName;

        public ActorActionFunction(string functionName)
        {
            FunctionName = functionName;
        }

        public override string ToString() => FunctionName;
    }
}