using Helion.World.Entities.Players;
using System;
using System.Collections.Generic;

namespace Helion.Demo;

public enum DemoTickResult
{
    None,
    DemoEnded,
    SuccessContinueReading,
    SuccessStopReading,
}

public interface IDemoPlayer : IDisposable
{
    event EventHandler? PlaybackEnded;
    DemoTickResult SetNextTickCommand(TickCommand command, out int playerNumber, out IList<DemoCheat> activatedCheats);
    void Start();
    void Stop();
    bool SetCommandIndex(int index);
}
