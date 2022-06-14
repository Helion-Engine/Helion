namespace Helion.Window;

public class MonitorData
{
    public MonitorData(int index, int horizontalResolution, int verticalResolution, object handle)
    {
        Index = index;
        HorizontalResolution = horizontalResolution;
        VerticalResolution = verticalResolution;
        Handle = handle;
    }

    public int Index { get; set; }
    public int HorizontalResolution { get; set; }
    public int VerticalResolution { get; set; }
    public object Handle { get; set; }
}
