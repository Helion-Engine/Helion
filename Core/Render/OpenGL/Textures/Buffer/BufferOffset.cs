namespace Helion.Render.OpenGL.Textures.Buffer
{
    public readonly struct BufferOffset
    {
        public readonly int RowStart;
        public readonly int RowCount;

        public BufferOffset(int rowStart, int rowCount)
        {
            RowStart = rowStart;
            RowCount = rowCount;
        }
    }
}
