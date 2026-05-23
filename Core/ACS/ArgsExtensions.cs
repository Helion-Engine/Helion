using Helion.Util;
using Helion.World;
using Helion.World.Entities;
using HelionACS;
using System;

namespace Helion.ACS;

public static class ArgsExtensions
{
    public static int Get(this ReadOnlySpan<uint> args, int index, int defaultValue = 0)
    {
        if (index >= args.Length)
            return defaultValue;
        return (int)args[index];
    }

    public static uint GetU(this ReadOnlySpan<uint> args, int index, uint defaultValue = 0u)
    {
        if (index >= args.Length)
            return defaultValue;
        return args[index];
    }

    public static string GetString(this ReadOnlySpan<uint> args, ThreadHandle thread, uint index, string defaultValue = "")
    {
        if (index >= args.Length)
            return defaultValue;

        var str = thread.GetString(args[(int)index]);
        return str;
    }

    public static ReadOnlySpan<char> GetStringSpan(this ReadOnlySpan<uint> args, ThreadHandle thread, uint index, string defaultValue = "")
    {
        if (index >= args.Length)
            return defaultValue;

        var str = thread.GetStringSpan(index, args[(int)index]);
        return str;
    }

    public static Entity? GetTidOrActivator(this ReadOnlySpan<uint> args, ThreadHandle thread, IWorld world, uint index)
    {
        if (index >= args.Length)
            return null;

        var tid = args[(int)index];
        if (tid == 0)
        {
            return thread.GetActivator(world);
        }
        return world.FindByTid((int)tid).Head?.Value;
    }

    public static double GetDouble(this ReadOnlySpan<uint> args, uint index, double defaultValue = 0.0)
    {
        if (index >= args.Length)
            return defaultValue;

        return MathHelper.FromFixed(unchecked((int)args[(int)index]));
    }

    public static bool GetBool(this ReadOnlySpan<uint> args, uint index, bool defaultValue = false)
    {
        if (index >= args.Length)
            return defaultValue;

        return args[(int)index] != 0;
    }
}
