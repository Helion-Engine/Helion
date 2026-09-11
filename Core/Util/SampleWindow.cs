using System;

namespace Helion.Util;

public class SampleWindow
{
    private double[] m_samples = new double[32];
    private double[] m_sorted = new double[32];
    private bool m_init;
    private int m_index;
    private int m_windowSize;

    public SampleWindow(int windowSize)
    {
        SetWindowSize(windowSize);
    }

    public bool IsInitialized => m_init;

    public void Clear()
    {
        m_index = 0;
        m_init = false;
        Array.Clear(m_samples, 0, m_windowSize);
    }

    public ReadOnlySpan<double> GetSampleWindow() => m_samples.AsSpan(0, m_windowSize);

    public void SetWindowSize(int size)
    {
        if ((size & 1) != 0)
            size++;

        size = Math.Max(size, 4);

        if (size > m_samples.Length)
        {
            Array.Resize(ref m_samples, size);
            Array.Resize(ref m_sorted, size);
        }

        m_windowSize = size;
    }

    public double AddSampleAndCalcMedian(double sample)
    {
        m_samples[m_index] = sample;
        m_index = (m_index + 1) % m_windowSize;

        // Not enough samples
        if (!m_init && m_index != 0)
            return sample;

        m_init = true;

        Array.Copy(m_samples, m_sorted, m_windowSize);
        Array.Sort(m_sorted, 0, m_windowSize);

        int mid = m_windowSize / 2;
        return 0.5 * (m_sorted[mid - 1] + m_sorted[mid]);
    }
}
