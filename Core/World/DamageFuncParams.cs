namespace Helion.World;

public readonly struct DamageFuncParams
{
    public readonly object? Object;
    public readonly int Arg0;
    public readonly int Arg1;
    public readonly int Arg2;

    public DamageFuncParams(object? obj = null, int arg0 = 0, int arg1 = 0, int arg2 = 0)
    {
        Object = obj;
        Arg0 = arg0;
        Arg1 = arg1;
        Arg2 = arg2;
    }
}
