using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.Util.Loggers;
using Helion.World;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public partial class LegacyWorldRenderer
{
    private IBspHeuristics? m_bspHeuristics;
    private double m_smoothedBspTimeUs;
    private readonly SampleWindow m_bspTimeWindow = new(32);
    private readonly SampleWindow m_fpsWindow = new(10);

    private int m_aboveThresholdCount;
    private int m_belowThresholdCount;
    private int m_lastProcessedId;
    private int m_adaptiveSuggestionsHitCount;
    private bool m_loggedAdaptiveSuggestion;

    private bool UseBspBasedOnHeuristic(IWorld world)
    {
        if (m_config.Render.Mode.Value == AdaptiveRenderMode.Bsp)
        {
            m_bspHeuristics?.Info.UseBsp = true;
            return true;
        }

        if (m_config.Render.Mode.Value == AdaptiveRenderMode.Static || m_bspHeuristics?.Valid == false)
        {
            CheckAdaptiveSuggest(world);
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

        var threshold = m_config.Render.Adaptive.TimeThreshold.Value;
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

        int thresholdCount = m_config.Render.Adaptive.SwitchCount.Value;
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
        if (m_bspHeuristics.SegCount > m_config.Render.Adaptive.SegThreshold.Value)
            shouldUseBsp = false;

        m_bspHeuristics.Info.GameTick = WorldStatic.World.GameTicker;
        m_bspHeuristics.Info.SmoothTime = (int)m_smoothedBspTimeUs;
        m_bspHeuristics.Info.AboveThresholdCount = m_aboveThresholdCount;
        m_bspHeuristics.Info.BelowThresholdCount = m_belowThresholdCount;
        m_bspHeuristics.Info.SegCount = m_bspHeuristics.SegCount;
        m_bspHeuristics.Info.UseBsp = shouldUseBsp;
        return m_bspHeuristics.Info.UseBsp;
    }

    private void CheckAdaptiveSuggest(IWorld world)
    {
        if (m_bspHeuristics == null || m_loggedAdaptiveSuggestion || world.GameTicker < 70 ||
            m_lastProcessedId == m_bspHeuristics.LastProcessedId || !m_bspHeuristics.Valid)
        {
            return;
        }
        
        m_lastProcessedId = m_bspHeuristics.LastProcessedId;

        var fpsValue = m_fpsWindow.AddSampleAndCalcMedian(m_fpsTracker.AverageFramesPerSecond);
        if (!m_fpsWindow.IsInitialized)
            return;

        if (fpsValue > 60 || (m_config.Render.MaxFPS.Value != 0 && fpsValue > m_config.Render.MaxFPS.Value))
            return;

        if (m_bspHeuristics.Microseconds >= m_config.Render.Adaptive.TimeThreshold.Value * 0.6)
            return;
        
        m_adaptiveSuggestionsHitCount++;
        if (m_adaptiveSuggestionsHitCount >= 3)
        {
            m_loggedAdaptiveSuggestion = true;
            HelionLog.Info("Low FPS detected. Considering switching to adaptive rendering mode. (render.mode 2)");
        }        
    }

    private double AddBspTimeSample(double time)
    {
        m_bspTimeWindow.SetWindowSize(m_config.Render.Adaptive.TimeWindow.Value);
        return m_bspTimeWindow.AddSampleAndCalcMedian(time);
    }
}
