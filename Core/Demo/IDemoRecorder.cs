using Helion.World.Entities.Players;

namespace Helion.Demo;

public interface IDemoRecorder
{
    void AddTickCommand(Player player);
    void Start();
    void Stop();
}
