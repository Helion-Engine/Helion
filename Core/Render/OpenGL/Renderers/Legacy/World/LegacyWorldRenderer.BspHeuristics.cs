using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.World;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public partial class LegacyWorldRenderer
{
    private IBspHeuristics? m_bspHeuristics;
    private double m_smoothedBspTimeUs;
    private readonly TimeWindow m_bspTimeWindow = new(32);

    private int m_aboveThresholdCount;
    private int m_belowThresholdCount;
    private int m_lastProcessedId;

    private bool UseBspBasedOnHeuristic()
    {
        if (m_config.Render.Mode.Value == AdaptiveRenderMode.Bsp)
        {
            m_bspHeuristics?.Info.UseBsp = true;
            return true;
        }

        if (m_config.Render.Mode.Value == AdaptiveRenderMode.Static || m_bspHeuristics?.Valid == false)
        {
            m_bspHeuristics?.Info.UseBsp = false;
            return false;
        }

        if (m_bspHeuristics == null)
            return false;

        var now = Stopwatch.GetTimestamp();
        var ageTicks = now - m_bspHeuristics.LastProcessedTimeStamp;
        var ageMicroseconds = ageTicks * (1_000_000.0 / Stopwatch.Frequency);

        // It needs sometime to process
        const double ProcessWindowUs = 500.0;
        if (ageMicroseconds <= ProcessWindowUs || m_lastProcessedId == m_bspHeuristics.LastProcessedId)
            return m_bspHeuristics.Info.UseBsp;

        m_lastProcessedId = m_bspHeuristics.LastProcessedId;
        m_smoothedBspTimeUs = AddBspTimeSample(m_bspHeuristics.Microseconds);

        var threshold = m_config.Render.AdaptiveBspTimeThreshold.Value;
        if (m_smoothedBspTimeUs < threshold)
        {
            m_belowThresholdCount++;
            m_aboveThresholdCount = 0;
        }
        else
        {
            m_aboveThresholdCount++;
            m_belowThresholdCount = 0;
        }

        int thresholdCount = m_config.Render.AdaptiveBspSwitchCount.Value;
        var shouldUseBsp = m_smoothedBspTimeUs < threshold;
        if (!shouldUseBsp && m_belowThresholdCount >= thresholdCount)
            shouldUseBsp = true;
        if (shouldUseBsp && m_aboveThresholdCount >= thresholdCount)
            shouldUseBsp = false;

        // Don't let the BSP heuristics flip-flop too quickly. If the smoothed time is within 10% of the threshold, don't switch.
        if (shouldUseBsp != m_bspHeuristics.Info.UseBsp)
        {
            const double PercentRange = 0.1;
            var highRange = threshold * (1 + PercentRange);
            var lowRange = threshold * (1 - PercentRange);
            if (m_smoothedBspTimeUs >= lowRange && m_smoothedBspTimeUs <= highRange)
                shouldUseBsp = m_bspHeuristics.Info.UseBsp;
        }

        // Don't let fast CPUs switch to BSP when it's likely not beneficial.
        if (m_bspHeuristics.SegCount > m_config.Render.AdaptiveBspSegThreshold.Value)
            shouldUseBsp = false;

        m_bspHeuristics.Info.GameTick = WorldStatic.World.GameTicker;
        m_bspHeuristics.Info.SmoothTime = (int)m_smoothedBspTimeUs;
        m_bspHeuristics.Info.AboveThresholdCount = m_aboveThresholdCount;
        m_bspHeuristics.Info.BelowThresholdCount = m_belowThresholdCount;
        m_bspHeuristics.Info.SegCount = m_bspHeuristics.SegCount;
        m_bspHeuristics.Info.UseBsp = shouldUseBsp;
        return m_bspHeuristics.Info.UseBsp;
    }

    private double AddBspTimeSample(double time)
    {
        m_bspTimeWindow.SetWindowSize(m_config.Render.AdaptiveBspTimeWindow.Value);
        return m_bspTimeWindow.AdddTimeSample(time);
    }
}
