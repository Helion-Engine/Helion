using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Layer;
using Helion.Layer.Worlds;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction;
using Helion.Util;
using Helion.Util.Consoles;
using Helion.Util.Container;
using Helion.Util.Loggers;
using Helion.Window.Input;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Save;
using Xunit;

namespace Helion.Tests.Unit.GameLayer;

[Collection("GameActions")]
public class GameLayerInput
{
    private static Vec3D PlayerSpeedTestPos = new(-640, -544, 0);
    private readonly SinglePlayerWorld World;
    private readonly GameLayerManager GameLayerManager;
    private readonly WorldLayer WorldLayer;
    private readonly InputManager InputManager;

    private Player Player => World.Player;

    public GameLayerInput()
    {
        HelionLoggers.Initialize(new());
        World = WorldAllocator.LoadMap("Resources/playermovement.zip", "playermovement.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
        InputManager = new InputManager();
        MockWindow window = new(InputManager);
        HelionConsole console = new(World.Config);
        SaveGameManager saveGameManager = new(World.Config);
        GameLayerManager = new(World.Config, window, console, new(), World.ArchiveCollection, World.SoundManager, saveGameManager, new());

        WorldLayer = new(GameLayerManager, World.Config, console, new(), World, World.MapInfo, new());
        GameLayerManager.Add(WorldLayer);

        World.Config.Hud.MoveBob.Set(0);
        World.Config.Keys.Add(Key.W, Constants.Input.Forward);
        World.Config.Keys.Add(Key.A, Constants.Input.Backward);
        World.Config.Keys.Add(Key.S, Constants.Input.Left);
        World.Config.Keys.Add(Key.D, Constants.Input.Right);
        World.Config.Keys.Add(Key.ControlRight, Constants.Input.Attack);
        World.Config.Keys.Add(Key.E, Constants.Input.Use);
        World.Config.Keys.Add(Key.Backtick, Constants.Input.Console);
    }

    [Fact(DisplayName = "Test input run logic key down/up")]
    public void TestInputRunLogicDownUp()
    {
        GameActions.SetEntityPosition(World, Player, PlayerSpeedTestPos);
        GameLayerManager.RunLogic(new(1, 0));
        Player.Velocity.Should().Be(Vec3D.Zero);
        AssertCommandsNotRun(TickCommands.Forward);

        InputManager.SetKeyDown(Key.W);
        GameLayerManager.RunLogic(new(1, 0));
        Player.Velocity.Should().NotBe(Vec3D.Zero);
        AssertCommandsRun(TickCommands.Forward);

        ResetPlayer();

        InputManager.SetKeyUp(Key.W);
        GameLayerManager.RunLogic(new(1, 0));
        Player.Velocity.Should().Be(Vec3D.Zero);
        AssertCommandsNotRun(TickCommands.Forward);
    }

    [Fact(DisplayName = "Test input run logic key down")]
    public void TestInputRunLogicDown()
    {
        GameActions.SetEntityPosition(World, Player, PlayerSpeedTestPos);
        GameLayerManager.RunLogic(new(1, 0));
        Player.Velocity.Should().Be(Vec3D.Zero);
        AssertCommandsNotRun(TickCommands.Forward);

        InputManager.SetKeyDown(Key.W);
        GameLayerManager.RunLogic(new(1, 0));
        Player.Velocity.Should().NotBe(Vec3D.Zero);
        AssertCommandsRun(TickCommands.Forward);

        ResetPlayer();
        Player.Velocity.Should().Be(Vec3D.Zero);

        GameLayerManager.RunLogic(new(1, 0));
        Player.Velocity.Should().NotBe(Vec3D.Zero);
        AssertCommandsRun(TickCommands.Forward);
    }

    [Fact(DisplayName = "Test input run multiple commands")]
    public void TestInputRunLogicMultiple()
    {
        GameActions.SetEntityPosition(World, Player, PlayerSpeedTestPos);
        GameLayerManager.RunLogic(new(0, 0));
        Player.Velocity.Should().Be(Vec3D.Zero);
        AssertCommandsNotRun(TickCommands.Forward, TickCommands.Left, TickCommands.Attack);

        InputManager.SetKeyDown(Key.W);
        InputManager.SetKeyDown(Key.S);
        InputManager.SetKeyDown(Key.ControlRight);
        GameLayerManager.RunLogic(new(1, 0));
        Player.Velocity.Should().NotBe(Vec3D.Zero);
        AssertCommandsRun(TickCommands.Forward, TickCommands.Left, TickCommands.Attack);

        GameLayerManager.RunLogic(new(5, 0));
        Player.Velocity.Should().NotBe(Vec3D.Zero);
        AssertCommandsRun(TickCommands.Forward, TickCommands.Left, TickCommands.Attack);

        ResetPlayer();
        InputManager.SetKeyUp(Key.W);
        InputManager.SetKeyUp(Key.S);
        InputManager.SetKeyUp(Key.ControlRight);
        GameLayerManager.RunLogic(new(1, 0));
        Player.Velocity.Should().Be(Vec3D.Zero);
        AssertCommandsNotRun(TickCommands.Forward, TickCommands.Left, TickCommands.Attack);
    }

    [Fact(DisplayName = "Test input run use command")]
    public void TestInputRunLogicUse()
    {
        GameActions.SetEntityPosition(World, Player, PlayerSpeedTestPos);
        GameLayerManager.RunLogic(new(1, 0));
        AssertCommandsNotRun(TickCommands.Use);

        InputManager.SetKeyDown(Key.E);
        GameLayerManager.RunLogic(new(1, 0));
        AssertCommandsRun(TickCommands.Use);

        // Use should not trigger unless pressed again
        GameLayerManager.RunLogic(new(1, 0));
        AssertCommandsRun(TickCommands.Use);

        InputManager.SetKeyUp(Key.E);
        InputManager.SetKeyDown(Key.E);
        GameLayerManager.RunLogic(new(1, 0));
        AssertCommandsRun(TickCommands.Use);
    }

    [Fact(DisplayName = "Show/hide menu")]
    public void Menu()
    {
        GameLayerManager.RunLogic(new(1, 0));
        GameLayerManager.MenuLayer.Should().BeNull();

        InputManager.SetKeyDown(Key.Escape);
        GameLayerManager.RunLogic(new(1, 0));
        GameLayerManager.MenuLayer.Should().NotBeNull();

        GameLayerManager.RunLogic(new(1, 0));
        GameLayerManager.MenuLayer.Should().NotBeNull();

        InputManager.SetKeyUp(Key.Escape);
        InputManager.SetKeyDown(Key.Escape);
        GameLayerManager.RunLogic(new(1, 0));
        GameLayerManager.MenuLayer.Should().BeNull();
    }

    [Fact(DisplayName = "Show/hide menu")]
    public void Console()
    {
        GameLayerManager.RunLogic(new(1, 0));
        GameLayerManager.ConsoleLayer.Should().BeNull();

        InputManager.SetKeyDown(Key.Backtick);
        GameLayerManager.RunLogic(new(1, 0));
        GameLayerManager.ConsoleLayer.Should().NotBeNull();

        GameLayerManager.RunLogic(new(1, 0));
        GameLayerManager.ConsoleLayer.Should().NotBeNull();

        InputManager.SetKeyUp(Key.Backtick);
        InputManager.SetKeyDown(Key.Backtick);
        GameLayerManager.RunLogic(new(1, 0));
        GameLayerManager.ConsoleLayer.Should().BeNull();
    }

    private static void WorldInit(SinglePlayerWorld world)
    {
        ResetPlayer(world);
    }

    private void ResetPlayer() => ResetPlayer(World);

    private static void ResetPlayer(SinglePlayerWorld world)
    {
        world.Player.Velocity = Vec3D.Zero;
        GameActions.SetEntityPosition(world, world.Player, PlayerSpeedTestPos);
    }

    private void AssertCommandsRun(params TickCommands[] cmds)
    {
        CommandsRun(cmds).Should().BeTrue();
    }

    private void AssertCommandsNotRun(params TickCommands[] cmds)
    {
        CommandsRun(cmds).Should().BeFalse();
    }

    private bool CommandsRun(params TickCommands[] cmds)
    {
        var tickCommands = Player.TickCommand.GetPreviousCommands();
        for (int i = 0; i < cmds.Length; i++)
        {
            if (!FindCommand(tickCommands, cmds[i]))
                return false;
        }

        return true;
    }

    private static bool FindCommand(DynamicArray<TickCommands> array, TickCommands cmd)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == cmd)
                return true;
        }

        return false;
    }
}
