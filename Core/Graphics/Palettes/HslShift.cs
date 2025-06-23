namespace Helion.Graphics.Palettes;

readonly struct HslShift(double? h, double? s, double? l, double addL)
{
    public readonly double? H = h;
    public readonly double? S = s;
    public readonly double? L = l;

    public readonly double AddL = addL;
}