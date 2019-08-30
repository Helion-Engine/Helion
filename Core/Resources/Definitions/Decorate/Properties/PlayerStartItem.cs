using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions.Decorate.Properties
{
    public class PlayerStartItem
    {
        public readonly string Name;
        public readonly int? Amount;

        public PlayerStartItem(string name, int? amount)
        {
            Precondition(!name.Empty(), "Cannot have an empty named player start item");
            Precondition(amount == null || amount >= 0, "Cannot have a negative player start item amount");
            
            Name = name;
            Amount = amount;
        }
    }
}