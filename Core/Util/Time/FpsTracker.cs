using System.Diagnostics;

namespace Helion.Util.Time
{
    public class FpsTracker
    {
        private const long RefreshRateMs = 1000;

        public double FramesPerSecond { get; private set; }
        private readonly Stopwatch m_stopwatch = new Stopwatch();
        private double tickSum;
        private int numFrames;

        public FpsTracker()
        {
            m_stopwatch.Start();
        }

        public void FinishFrame()
        {
            tickSum += m_stopwatch.ElapsedTicks;
            numFrames++;

            UpdateFpsIfNeeded();

            m_stopwatch.Restart();
        }

        private void ResetTrackingVariables()
        {
            tickSum = 0;
            numFrames = 0;
        }

        private void UpdateFpsIfNeeded()
        {
            long millisecondsSinceUpdate = (long)(tickSum / Stopwatch.Frequency * 1000);
            if (millisecondsSinceUpdate < RefreshRateMs)
                return;

            if (numFrames == 0)
                FramesPerSecond = 0;
            else
            {
                double averageTicks = tickSum / numFrames;
                FramesPerSecond = Stopwatch.Frequency / averageTicks;
            }

            ResetTrackingVariables();
        }
    }
}