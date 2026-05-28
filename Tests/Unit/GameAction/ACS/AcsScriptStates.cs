using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.ACS;

[Collection("GameActions")]
public class AcsScriptStates
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public AcsScriptStates()
    {
        World = WorldAllocator.LoadMap("Resources/acs-script-states.zip", "acs-script-states.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Script pause and resume")]
    public void ScriptPauseAndResume()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            // Runs script that counts up every second
            GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine).Should().BeTrue();
            GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine).Should().BeFalse();
            GameActions.TickWorld(World, 35);
            messages.Count.Should().Be(1);
            messages[^1].Args.Message.Should().Be("0");
            GameActions.TickWorld(World, 35);
            messages.Count.Should().Be(2);
            messages[^1].Args.Message.Should().Be("1");

            // Pause script
            GameActions.ActivateLine(World, Player, 8, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 70);
            messages.Count.Should().Be(2);

            // Resume and keeps counting
            GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 35);
            messages.Count.Should().Be(3);
            messages[^1].Args.Message.Should().Be("2");
        });
    }

    [Fact(DisplayName = "Script terminate")]
    public void ScriptTerminate()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            // Runs script that counts up every second
            GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 35);
            messages.Count.Should().Be(1);
            messages[^1].Args.Message.Should().Be("0");
            GameActions.TickWorld(World, 35);
            messages.Count.Should().Be(2);
            messages[^1].Args.Message.Should().Be("1");

            // Pause script
            GameActions.ActivateLine(World, Player, 12, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 70);
            messages.Count.Should().Be(2);

            // Script starts from beginning
            GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 35);
            messages.Count.Should().Be(3);
            messages[^1].Args.Message.Should().Be("0");
        });
    }

    [Fact(DisplayName = "Script with red key")]
    public void ScriptWithRedKey()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 20, ActivationContext.UseLine).Should().BeFalse();
            World.Tick();
            messages.Count.Should().Be(1);
            messages[^1].Args.Message.Should().Be("You need a red key to activate this object");

            Player.Inventory.Add(World.EntityManager.DefinitionComposer.GetByName("RedCard")!, 1);
            GameActions.ActivateLine(World, Player, 20, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(2);
            messages[^1].Args.Message.Should().Be("Unlocked");
        });
    }

    [Fact(DisplayName = "Script with red skull")]
    public void ScriptWithRedSkull()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 28, ActivationContext.UseLine).Should().BeFalse();
            World.Tick();
            messages.Count.Should().Be(1);
            messages[^1].Args.Message.Should().Be("You need a red skull to open this door");

            Player.Inventory.Add(World.EntityManager.DefinitionComposer.GetByName("RedSkull")!, 1);
            GameActions.ActivateLine(World, Player, 28, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(2);
            messages[^1].Args.Message.Should().Be("Unlocked");
        });
    }

    [Fact(DisplayName = "Script execute always")]
    public void ScriptExecuteAlways()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 35);
            messages.Count.Should().Be(1);
            messages[^1].Args.Message.Should().Be("0");

            GameActions.ActivateLine(World, Player, 16, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 70);
            messages.Count.Should().Be(5);
            messages[^4].Args.Message.Should().Be("1");
            messages[^3].Args.Message.Should().Be("0");
            messages[^2].Args.Message.Should().Be("2");
            messages[^1].Args.Message.Should().Be("1");
        });
    }

    [Fact(DisplayName = "Script execute with result 0")]
    public void ScriptExecuteWithResult0()
    {
        GameActions.ActivateLine(World, Player, 24, ActivationContext.UseLine).Should().BeFalse();
    }

    [Fact(DisplayName = "Script execute with result 1")]
    public void ScriptExecuteWithResult1()
    {
        GameActions.ActivateLine(World, Player, 32, ActivationContext.UseLine).Should().BeTrue();
    }

    [Fact(DisplayName = "Script execute name TestScript")]
    public void ScriptExecuteNameTest()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 36, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 35);
            messages.Count.Should().Be(1);
            messages[^1].Args.Message.Should().Be("Test Script 1 2 3");
        });
    }

    [Fact(DisplayName = "Script execute name AnotherScript")]
    public void ScriptExecuteNameAnother()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 40, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 35);
            messages.Count.Should().Be(1);
            messages[^1].Args.Message.Should().Be("Another Script 3 2 1");
        });
    }
}
