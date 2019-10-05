namespace Helion.Resources.Definitions.Compatibility.Lines
{
    public class LineRemoveSideDefinition : ILineDefinition
    {
        public int Id { get; }

        public LineRemoveSideDefinition(int id)
        {
            Id = id;
        }
    }
}