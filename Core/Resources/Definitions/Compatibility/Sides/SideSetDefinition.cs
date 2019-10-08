namespace Helion.Resources.Definitions.Compatibility.Sides
{
    /// <summary>
    /// Information for a change of data to a side.
    /// </summary>
    public class SideSetDefinition : ISideDefinition
    {
        public int Id { get; }
        
        /// <summary>
        /// The new lower texture, if not null.
        /// </summary>
        public string? Lower;
        
        /// <summary>
        /// The new middle texture, if not null.
        /// </summary>
        public string? Middle;
        
        /// <summary>
        /// The new upper texture, if not null.
        /// </summary>
        public string? Upper;

        public SideSetDefinition(int id)
        {
            Id = id;
        }
    }
}