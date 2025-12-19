using System.Collections.Generic;

namespace Helion.Util.Profiling;

public class GCProfiler : ProfileComponent<GCProfiler>
{
    public override List<ProfilerPath> Profilers { get; } = [];
}
