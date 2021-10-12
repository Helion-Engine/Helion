namespace Helion.Util;

public readonly struct IntRange
{
    public readonly int Start;
    public readonly int End;

    public int Count => End - Start;

    public IntRange(int start, int end)
    {
        Start = start;
        End = end;
    }

    public static IntRange FromCount(int start, int count)
    {
        return new(start, start + count);
    }
}
