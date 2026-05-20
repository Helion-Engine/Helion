using Helion.Util;
using Helion.World;
using Helion.World.Entities;
using HelionACS;

namespace Helion.ACS;

public static class ArgsExtensions
{
    public static int Get(this uint[] args, int index, int defaultValue = 0)
    {
        if (index >= args.Length)
            return defaultValue;
        return (int)args[index];
    }

    public static uint GetU(this uint[] args, int index, uint defaultValue = 0u)
    {
        if (index >= args.Length)
            return defaultValue;
        return args[index];
    }

    public static string GetString(this uint[] args, ThreadHandle thread, uint index, string defaultValue = "")
    {
        if (index >= args.Length)
            return defaultValue;

        var str = thread.GetString(args[index]);
        return str;
    }

    public static Entity? GetTidOrActivator(this uint[] args, ThreadHandle thread, IWorld world, uint index)
    {
        if (index >= args.Length)
            return null;

        var tid = args[index];
        if (tid == 0)
        {
            return thread.GetActivator(world);
        }
        return world.FindByTid((int)tid).Head?.Value;
    }

    public static double GetDouble(this uint[] args, uint index, double defaultValue = 0.0)
    {
        if (index >= args.Length)
            return defaultValue;

        return MathHelper.FromFixed(unchecked((int)args[index]));
    }

    public static bool GetBool(this uint[] args, uint index, bool defaultValue = false)
    {
        if (index >= args.Length)
            return defaultValue;

        return args[index] != 0;
    }
}
