using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using FluentAssertions;
using Helion.Util.Timing;
using Xunit;

namespace Helion.Tests.Unit.Util.Time;

public class TickerTest
{
    [Fact(DisplayName = "Can read nanosecond time")]
    public void ReadNanoTime()
    {
        // This OS requirement is mainly intended to avoid test failures on WSL and
        // other virtualized environments.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            long nanos = 10000L * Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond * 100L;
            nanos.Should().BeLessOrEqualTo(Ticker.NanoTime());
        }
    }

    [Fact(DisplayName = "Can start and stop the ticker")]
    public void CanStartAndStop()
    {
        Ticker ticker = new(100);
        ticker.Start();
        Thread.Sleep(50);
        ticker.Stop();

        ticker.GetTickerInfo().Ticks.Should().BeGreaterOrEqualTo(1);

        // Since it's been stopped, nothing should accumulate.
        Thread.Sleep(50);
        TickerInfo info = ticker.GetTickerInfo();
        info.Ticks.Should().Be(0);
    }
}
