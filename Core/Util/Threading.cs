using System.Threading;

namespace Helion.Util;

/// <summary>
/// A utility class to help with threads.
/// </summary>
public static class Threading
{
    /// <summary>
    /// Gets the number of threads currently running.
    /// </summary>
    /// <returns>The number of running threads.</returns>
    public static int NumThreadsRunning()
    {
        // From: https://devblogs.microsoft.com/oldnewthing/20170724-00/?p=96675
        ThreadPool.GetMaxThreads(out int maxWorker, out _);
        ThreadPool.GetAvailableThreads(out int availableWorker, out _);
        return maxWorker - availableWorker;
    }
}
