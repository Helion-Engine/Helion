using System.Runtime.InteropServices;

namespace Helion.Client;

public static partial class WinNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TimeCaps
    {
        public uint wPeriodMin;
        public uint wPeriodMax;
    }

    const string WinMM = "winmm.dll";

    [LibraryImport(WinMM, EntryPoint = "timeBeginPeriod", SetLastError = true)]
    private static partial uint _TimeBeginPeriod(uint uMilliseconds);

    [LibraryImport(WinMM, EntryPoint = "timeEndPeriod", SetLastError = true)]
    private static partial uint _TimeEndPeriod(uint uMilliseconds);

    [LibraryImport(WinMM, EntryPoint = "timeGetDevCaps", SetLastError = true)]
    private static partial uint _TimeGetDevCaps(ref TimeCaps pTC, uint cbTC);

    public static bool TimeBeginPeriod(uint uMilliseconds)
    {
        return _TimeBeginPeriod(uMilliseconds) == 0;
    }

    public static bool TimeEndPeriod(uint uMilliseconds)
    {
        return _TimeEndPeriod(uMilliseconds) == 0;
    }

    public static bool TimeGetDevCaps(out TimeCaps timeCaps)
    {
        timeCaps = new TimeCaps();
        var result = _TimeGetDevCaps(ref timeCaps, (uint)Marshal.SizeOf<TimeCaps>());
        return result == 0;
    }
}
