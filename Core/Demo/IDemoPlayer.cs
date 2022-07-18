using Helion.World.Entities.Players;
using System;

namespace Helion.Demo;

public enum DemoTickResult
{
    None,
    DemoEnded,
    SuccessContinueReading,
    SuccessStopReading,
}

public interface IDemoPlayer
{
    event EventHandler? PlaybackEnded;
    DemoTickResult SetNextTickCommand(TickCommand command, out int playerNumber);
    void Start();
    void Stop();
}
