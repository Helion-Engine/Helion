namespace Helion.Layer.Options;

public enum SizeMetric { Pixel, Percent };

public readonly record struct RenderSize(int Size, SizeMetric SizeMetric)
{
    public int GetSize(int parentDimension)
    {
        if (SizeMetric == SizeMetric.Pixel)
            return Size;

        return (int)(parentDimension * (Size / 100.0));
    }
}
