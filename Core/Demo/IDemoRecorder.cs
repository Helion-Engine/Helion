using Helion.World.Entities.Players;
using System;

namespace Helion.Demo;

public interface IDemoRecorder : IDisposable
{
    void AddTickCommand(Player player);
    void Start();
    void Stop();
    string DemoFile { get; }
    int CommandIndex { get; }
    bool Recording { get; }
}
