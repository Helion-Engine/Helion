using Helion.World.Entities.Players;

namespace Helion.Tests.Unit.GameAction.Util;

internal class TestTickCommand : TickCommand
{
    public override void TickHandled()
    {
        // The WorldLayer handles clearing the tick command. Just clear it on tick handled for unit tests.
        base.TickHandled();
        Clear();
    }
}
