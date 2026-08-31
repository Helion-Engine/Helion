using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.World;
using System;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public partial class LegacyWorldRenderer
{
    private IBspHeuristics? m_bspHeuristics;
    private double m_smoothedBspTimeUs;
    private readonly double[] m_bspWindowSamples = new double[3];
    private int m_windowIndex;
    private bool m_windowInit;

    private double AddBspTimeSample(double time)
    {
        m_bspWindowSamples[m_windowIndex] = time;
        m_windowIndex = (m_windowIndex + 1) % 3;

        if (!m_windowInit && m_windowIndex < 2)
            return time;

        m_windowInit = true;
        double a = m_bspWindowSamples[0], b = m_bspWindowSamples[1], c = m_bspWindowSamples[2];
        double med = MathHelper.Max(MathHelper.Min(a, b), MathHelper.Min(MathHelper.Max(a, b), c));
        return med;
    }

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
        if (ageMicroseconds <= ProcessWindowUs)
            return m_bspHeuristics.Info.UseBsp;

        var threshold = m_config.Render.AdaptiveBspTimeThreshold.Value;
        var highRange = threshold * 1.15f;
        var lowRange = threshold * 0.85f;

        m_smoothedBspTimeUs = AddBspTimeSample(m_bspHeuristics.Microseconds);

        var shouldUseBsp = m_smoothedBspTimeUs < threshold;

        if (!shouldUseBsp && m_smoothedBspTimeUs < highRange)
            shouldUseBsp = true;
        else if (shouldUseBsp && m_smoothedBspTimeUs > lowRange)
            shouldUseBsp = false;

        // Don't let fast CPUs switch to BSP when it's likely not beneficial.
        if (m_bspHeuristics.SegCount > m_config.Render.AdaptiveBspSegThreshold.Value)
            shouldUseBsp = false;

        m_bspHeuristics.Info.GameTick = WorldStatic.World.GameTicker;
        m_bspHeuristics.Info.SmoothTime = (int)m_smoothedBspTimeUs;
        m_bspHeuristics.Info.UseBsp = shouldUseBsp;
        return m_bspHeuristics.Info.UseBsp;
    }
}
