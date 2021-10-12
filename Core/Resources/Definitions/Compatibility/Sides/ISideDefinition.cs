namespace Helion.Resources.Definitions.Compatibility.Sides;

/// <summary>
/// A definition for some mutation to a side in a map.
/// </summary>
public interface ISideDefinition
{
    /// <summary>
    /// The ID of the side to affect.
    /// </summary>
    int Id { get; }
}
