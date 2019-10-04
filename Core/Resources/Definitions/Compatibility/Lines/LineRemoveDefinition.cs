namespace Helion.Resources.Definitions.Compatibility.Lines
{
    public class LineRemoveDefinition : ILineDefinition
    {
        public int Id { get; }

        public LineRemoveDefinition(int id)
        {
            Id = id;
        }
    }
}