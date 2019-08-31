namespace Helion.Resources.Definitions.Decorate.States
{
    public class ActionFunction
    {
        public readonly string FunctionName;

        public ActionFunction(string functionName)
        {
            FunctionName = functionName.ToUpper();
        }

        public override string ToString() => FunctionName;
    }
}