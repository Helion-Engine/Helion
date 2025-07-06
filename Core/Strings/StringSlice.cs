using System;

namespace Helion.Strings;

public struct StringSlice(string source, int start, int length)
{
    public string Source = source;
    public int Start = start;
    public int Length = length;

    public static readonly StringSlice Empty = new(string.Empty, 0, 0);

    public readonly ReadOnlySpan<char> AsSpan() =>
        Source.AsSpan(Start, Length);

    public override readonly string ToString() => 
        AsSpan().ToString();
}
