namespace Helion.World;

public readonly struct DamageFuncParams(bool ignorePlayerRefire, object? obj = null, int arg0 = 0, int arg1 = 0, int arg2 = 0)
{
    public readonly object? Object = obj;
    public readonly int Arg0 = arg0;
    public readonly int Arg1 = arg1;
    public readonly int Arg2 = arg2;
    public readonly bool IgnorePlayerRefire = ignorePlayerRefire;
}
