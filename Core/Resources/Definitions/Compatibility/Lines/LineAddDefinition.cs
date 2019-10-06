namespace Helion.Resources.Definitions.Compatibility.Lines
{
    public class LineAddDefinition : ILineDefinition
    {
        public int Id { get; }
        public int SideId { get; }

        public LineAddDefinition(int id, int sideId)
        {
            Id = id;
            SideId = sideId;
        }
    }
}