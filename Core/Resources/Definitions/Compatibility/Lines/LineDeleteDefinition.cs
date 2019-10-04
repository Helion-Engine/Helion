namespace Helion.Resources.Definitions.Compatibility.Lines
{
    public class LineDeleteDefinition : ILineDefinition
    {
        public int Id { get; }

        public LineDeleteDefinition(int id)
        {
            Id = id;
        }
    }
}