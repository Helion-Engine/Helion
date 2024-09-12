
using System;
using System.Diagnostics;

namespace Helion.Layer.Consoles;

public class SetStopwatch : Stopwatch
{
    private TimeSpan m_offset;

    public void Restart(TimeSpan offset)
    {
        if (offset < TimeSpan.Zero)
            offset = TimeSpan.Zero;
        m_offset = offset;
        Restart();
    }

    public new long ElapsedMilliseconds => base.ElapsedMilliseconds + (long)m_offset.TotalMilliseconds;

    public new TimeSpan Elapsed => base.Elapsed + m_offset;
}
