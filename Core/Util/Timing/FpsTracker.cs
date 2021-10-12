using System;
using System.Diagnostics;

namespace Helion.Util.Timing;

/// <summary>
/// A helper class that allows tracking of how many frames have elapsed.
/// </summary>
/// <remarks>
/// Starts tracking as soon as construction finishes.
/// </remarks>
public class FpsTracker
{
    private const long RefreshRateMs = 600;

    public double AverageFramesPerSecond { get; private set; }
    public double MaxFramesPerSecond { get; private set; }
    public double MinFramesPerSecond { get; private set; }
    private readonly Stopwatch m_stopwatch = new();
    private double tickSum;
    private double minTicksSeen = long.MaxValue;
    private double maxTicksSeen;
    private int numFrames;

    public FpsTracker()
    {
        m_stopwatch.Start();
    }

    public void FinishFrame()
    {
        tickSum += m_stopwatch.ElapsedTicks;
        maxTicksSeen = Math.Max(maxTicksSeen, m_stopwatch.ElapsedTicks);
        minTicksSeen = Math.Min(minTicksSeen, m_stopwatch.ElapsedTicks);
        numFrames++;

        UpdateFpsIfNeeded();

        m_stopwatch.Restart();
    }

    private void ResetTrackingVariables()
    {
        tickSum = 0;
        numFrames = 0;
        minTicksSeen = long.MaxValue;
        maxTicksSeen = 0;
    }

    private void UpdateFpsIfNeeded()
    {
        long millisecondsSinceUpdate = (long)(tickSum / Stopwatch.Frequency * 1000);
        if (millisecondsSinceUpdate < RefreshRateMs)
            return;

        if (numFrames == 0)
        {
            AverageFramesPerSecond = 0;
            MaxFramesPerSecond = 0;
            MinFramesPerSecond = 0;
        }
        else
        {
            double averageTicks = tickSum / numFrames;
            AverageFramesPerSecond = Stopwatch.Frequency / averageTicks;

            // Remember: Our maximum FPS is the smallest amount of ticks
            // that we've seen; same logic for the minimum FPS.
            MaxFramesPerSecond = Stopwatch.Frequency / minTicksSeen;
            MinFramesPerSecond = Stopwatch.Frequency / maxTicksSeen;
        }

        ResetTrackingVariables();
    }
}
