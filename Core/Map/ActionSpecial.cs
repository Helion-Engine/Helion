namespace Helion.Map
{
    /// <summary>
    /// An action special that is attached to a line in a map.
    /// </summary>
    public class ActionSpecial
    {
        /// <summary>
        /// How many arguments are supported by the action special.
        /// </summary>
        public const int ARG_COUNT = 5;

        /// <summary>
        /// The action special identifier.
        /// </summary>
        public ActionSpecialID Special = ActionSpecialID.None;

        /// <summary>
        /// The arguments for the action special.
        /// </summary>
        public byte[] Args { get; } = new byte[] { 0, 0, 0, 0, 0 };

        /// <summary>
        /// Creates a default action special, which has no action special type.
        /// </summary>
        public ActionSpecial() { }

        /// <summary>
        /// Creates an action special with the provided arguments.
        /// </summary>
        /// <param name="type">The action special type.</param>
        /// <param name="arg0">The first argument.</param>
        /// <param name="arg1">The second argument.</param>
        /// <param name="arg2">The third argument.</param>
        /// <param name="arg3">The fourth argument.</param>
        /// <param name="arg4">The fifth argument.</param>
        public ActionSpecial(ActionSpecialID type, byte arg0, byte arg1, byte arg2, byte arg3, byte arg4)
        {
            Special = type;
            Args = new byte[] { arg0, arg1, arg2, arg3, arg4 };
        }
    };
}
