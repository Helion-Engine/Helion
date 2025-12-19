using System;
using System.Diagnostics;
using System.Threading;

namespace Helion.Client;

public sealed class FrameLimiter
{
    private readonly Stopwatch m_stopwatch = Stopwatch.StartNew();
    private double m_drift;

    public void Limit(int targetFps)
    {
        if (targetFps > 0)
        {
            var target = 1000.0 / targetFps;
            var driftTarget = Math.Max(target - m_drift, 0);
            m_drift = 0;
            while (true)
            {
                var elapsed = m_stopwatch.Elapsed.TotalMilliseconds;
                if (elapsed >= driftTarget)
                {
                    m_drift = Math.Clamp(elapsed - driftTarget, 0, target);
                    break;
                }

                var sleepTime = driftTarget - elapsed;
                if (sleepTime <= 0)
                    break;

                if (sleepTime < 2)
                    Thread.Sleep(0);
                else
                    Thread.Sleep((int)(sleepTime - 1));
            }
        }

        m_stopwatch.Restart();
    }
}
