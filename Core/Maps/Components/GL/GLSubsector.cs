namespace Helion.Maps.Components.GL
{
    public class GLSubsector
    {
        public readonly uint Count;
        public readonly int FirstSegmentIndex;

        public GLSubsector(uint count, uint firstSegmentIndex)
        {
            Count = count;
            FirstSegmentIndex = firstSegmentIndex;
        }
    }
}
