using System.Runtime.InteropServices;

namespace Helion.Client;

public static class WinNative
{
    const string WinMM = "winmm.dll";

    [DllImport(WinMM, EntryPoint = "timeBeginPeriod", SetLastError = true)]
    private static extern uint _TimeBeginPeriod(uint uMilliseconds);

    [DllImport(WinMM, EntryPoint = "timeEndPeriod", SetLastError = true)]
    private static extern uint _TimeEndPeriod(uint uMilliseconds);

    public static bool TimeBeginPeriod(uint uMilliseconds)
    {
        return _TimeBeginPeriod(uMilliseconds) == 0;
    }

    public static bool TimeEndPeriod(uint uMilliseconds)
    {
        return _TimeBeginPeriod(uMilliseconds) == 0;
    }
}
