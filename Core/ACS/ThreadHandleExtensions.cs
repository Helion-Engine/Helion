using HelionACS;

namespace Helion.ACS;

public static class ThreadHandleExtensions
{
    public static ThreadInfo GetThread(this ThreadHandle thread) => (thread.GetThreadInfo() as ThreadInfo)!;
}
