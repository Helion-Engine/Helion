namespace Helion.Resources.Definitions.Compatibility.Lines
{
    /// <summary>
    /// A definition for some mutation to a line in a map.
    /// </summary>
    public interface ILineDefinition
    {
        /// <summary>
        /// The ID of the line to affect.
        /// </summary>
        int Id { get; }
    }
}