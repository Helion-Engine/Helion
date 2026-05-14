using HelionACS;

namespace Helion.ACS;

public static class ArgsExtensions
{
    public static int Get(this uint[] args, int index)
    {
        if (index >= args.Length)
            return 0;
        return (int)args[index];
    }

    public static uint GetU(this uint[] args, int index)
    {
        if (index >= args.Length)
            return 0;
        return args[index];
    }

    public static string GetString(this uint[] args, ThreadHandle thread, uint index)
    {
        if (index >= args.Length)
            return "";

        var str = thread.GetString(args[index]);
        return str;
    }
}
