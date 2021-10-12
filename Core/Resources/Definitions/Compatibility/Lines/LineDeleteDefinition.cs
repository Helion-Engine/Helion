namespace Helion.Resources.Definitions.Compatibility.Lines;

/// <summary>
/// Information for deleting a line completely from the map.
/// </summary>
/// <remarks>
/// Commonly used in maps where there's a one-sided line that should not
/// exist and causes great difficulty with the BSP builder. A good example
/// of this is D2M14.
/// </remarks>
public class LineDeleteDefinition : ILineDefinition
{
    public int Id { get; }

    public LineDeleteDefinition(int id)
    {
        Id = id;
    }
}
