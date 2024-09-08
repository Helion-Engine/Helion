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
using Helion.Util.Timing;
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

    private static readonly TickerInfo ZeroTick = new(0, 0);
    private static readonly TickerInfo SingleTick = new(1, 0);

    public GameLayerInput()
    {
        HelionLoggers.Initialize(new());
        World = WorldAllocator.LoadMap("Resources/playermovement.zip", "playermovement.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
        InputManager = new InputManager();
        MockWindow window = new(InputManager);
        HelionConsole console = new(new DataCache(), World.Config);
        SaveGameManager saveGameManager = new(World.Config, World.ArchiveCollection, null);
        GameLayerManager = new(World.Config, window, console, new(), World.ArchiveCollection, World.SoundManager, saveGameManager, new());

        WorldLayer = new(GameLayerManager, World.Config, console, new(), World, World.MapInfo, new());
        GameLayerManager.Add(WorldLayer);

        World.Config.Keys.Add(Key.W, Constants.Input.Forward);
        World.Config.Keys.Add(Key.A, Constants.Input.Left);
        World.Config.Keys.Add(Key.S, Constants.Input.Backward);
        World.Config.Keys.Add(Key.D, Constants.Input.Right);
        World.Config.Keys.Add(Key.ControlRight, Constants.Input.Attack);
        World.Config.Keys.Add(Key.E, Constants.Input.Use);
        World.Config.Keys.Add(Key.Backtick, Constants.Input.Console);
        World.Config.Keys.Add(Key.Escape, Constants.Input.Menu);

        World.Config.Keys.Add(Key.F10, "mouselook");
        World.Config.Keys.Add(Key.F10, "autoaim");
    }

    [Fact(DisplayName = "Test input run logic key down/up")]
    public void TestInputRunLogicDownUp()
    {
        ResetPlayer();
        GameLayerManager.RunLogic(SingleTick);
        Player.Velocity.Should().Be(Vec3D.Zero);
        AssertCommandsNotRun(TickCommands.Forward);

        InputManager.SetKeyDown(Key.W);
        GameLayerManager.RunLogic(SingleTick);
        Player.Velocity.Should().NotBe(Vec3D.Zero);
        AssertCommandsRun(TickCommands.Forward);

        ResetPlayer();

        InputManager.SetKeyUp(Key.W);
        GameLayerManager.RunLogic(SingleTick);
        Player.Velocity.Should().Be(Vec3D.Zero);
        AssertCommandsNotRun(TickCommands.Forward);
    }

    [Fact(DisplayName = "Test input run logic key down")]
    public void TestInputRunLogicDown()
    {
        ResetPlayer();
        GameLayerManager.RunLogic(SingleTick);
        Player.Velocity.Should().Be(Vec3D.Zero);
        AssertCommandsNotRun(TickCommands.Forward);

        InputManager.SetKeyDown(Key.W);
        GameLayerManager.RunLogic(SingleTick);
        Player.Velocity.Should().NotBe(Vec3D.Zero);
        AssertCommandsRun(TickCommands.Forward);

        ResetPlayer();
        Player.Velocity.Should().Be(Vec3D.Zero);

        GameLayerManager.RunLogic(SingleTick);
        Player.Velocity.Should().NotBe(Vec3D.Zero);
        AssertCommandsRun(TickCommands.Forward);
    }

    [Fact(DisplayName = "Test input run multiple commands")]
    public void TestInputRunLogicMultiple()
    {
        ResetPlayer();
        GameLayerManager.RunLogic(ZeroTick);
        Player.Velocity.Should().Be(Vec3D.Zero);
        AssertCommandsNotRun(TickCommands.Forward, TickCommands.Left, TickCommands.Attack);

        InputManager.SetKeyDown(Key.W);
        InputManager.SetKeyDown(Key.A);
        InputManager.SetKeyDown(Key.ControlRight);
        GameLayerManager.RunLogic(SingleTick);
        Player.Velocity.Should().NotBe(Vec3D.Zero);
        AssertCommandsRun(TickCommands.Forward, TickCommands.Left, TickCommands.Attack);

        GameLayerManager.RunLogic(new(5, 0));
        Player.Velocity.Should().NotBe(Vec3D.Zero);
        AssertCommandsRun(TickCommands.Forward, TickCommands.Left, TickCommands.Attack);

        ResetPlayer();
        InputManager.SetKeyUp(Key.W);
        InputManager.SetKeyUp(Key.A);
        InputManager.SetKeyUp(Key.ControlRight);
        GameLayerManager.RunLogic(SingleTick);
        Player.Velocity.Should().Be(Vec3D.Zero);
        AssertCommandsNotRun(TickCommands.Forward, TickCommands.Left, TickCommands.Attack);
    }

    [Fact(DisplayName = "Test input run use command")]
    public void TestInputRunLogicUse()
    {
        ResetPlayer();
        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsNotRun(TickCommands.Use);

        InputManager.SetKeyDown(Key.E);
        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsRun(TickCommands.Use);

        // Use should not trigger unless pressed again
        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsRun(TickCommands.Use);

        InputManager.SetKeyUp(Key.E);
        InputManager.SetKeyDown(Key.E);
        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsRun(TickCommands.Use);
    }

    [Fact(DisplayName = "Show/hide menu")]
    public void Menu()
    {
        GameLayerManager.RunLogic(SingleTick);
        GameLayerManager.MenuLayer.Should().BeNull();

        InputManager.SetKeyDown(Key.Escape);
        GameLayerManager.RunLogic(SingleTick);
        GameLayerManager.MenuLayer.Should().NotBeNull();

        GameLayerManager.RunLogic(SingleTick);
        GameLayerManager.MenuLayer.Should().NotBeNull();

        InputManager.SetKeyUp(Key.Escape);
        InputManager.SetKeyDown(Key.Escape);
        GameLayerManager.RunLogic(SingleTick);
        GameLayerManager.ExpireAnimations();
        GameLayerManager.RunLogic(SingleTick);
        GameLayerManager.MenuLayer.Should().BeNull();
    }

    [Fact(DisplayName = "Show/hide console")]
    public void Console()
    {
        GameLayerManager.RunLogic(SingleTick);
        GameLayerManager.ConsoleLayer.Should().BeNull();

        InputManager.SetKeyDown(Key.Backtick);
        GameLayerManager.RunLogic(SingleTick);
        GameLayerManager.ConsoleLayer.Should().NotBeNull();

        GameLayerManager.RunLogic(SingleTick);
        GameLayerManager.ConsoleLayer.Should().NotBeNull();

        InputManager.SetKeyUp(Key.Backtick);
        InputManager.SetKeyDown(Key.Backtick);
        GameLayerManager.RunLogic(SingleTick);
        GameLayerManager.ExpireAnimations();
        GameLayerManager.RunLogic(SingleTick);
        GameLayerManager.ConsoleLayer.Should().BeNull();
    }

    [Fact(DisplayName = "Commands")]
    public void Commands()
    {
        World.Config.Mouse.Look.Value.Should().BeFalse();
        World.Config.Game.AutoAim.Value.Should().BeTrue();
        Player.PitchRadians.Should().Be(0);

        InputManager.AddMouseMovement((0, 10));
        GameLayerManager.RunLogic(SingleTick);
        var items = GameLayerManager.GetConsoleSubmittedInput();
        items.Count.Should().Be(0);

        InputManager.SetKeyDown(Key.F10);
        InputManager.SetKeyDown(Key.W);
        GameLayerManager.RunLogic(SingleTick);
        items = GameLayerManager.GetConsoleSubmittedInput();
        items.Count.Should().Be(2);
        items.Contains("mouselook").Should().BeTrue();
        items.Contains("autoaim").Should().BeTrue();
        AssertCommandsRun(TickCommands.Forward);
    }

    [Fact(DisplayName = "Toggle keys up and down")]
    public void InputKeyToggle()
    {
        for (int i = 0; i < 10; i++)
            GameLayerManager.RunLogic(ZeroTick);
        AssertCommandsNotRun(TickCommands.Backward);

        InputManager.SetKeyDown(Key.S);
        for (int i = 0; i < 10; i++)
        {
            GameLayerManager.RunLogic(ZeroTick);
            InputManager.SetKeyUp(Key.S);
            InputManager.SetKeyDown(Key.S);
        }
        // Didn't run any ticks - key presses should not be processed
        AssertCommandsNotRun(TickCommands.Backward);

        // Has key down in events, should process
        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsRun(TickCommands.Backward);

        // Last key event was down should continue to process
        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsRun(TickCommands.Backward);

        for (int i = 0; i < 10; i++)
        {
            GameLayerManager.RunLogic(ZeroTick);
            InputManager.SetKeyUp(Key.S);
            InputManager.SetKeyDown(Key.S);
        }

        InputManager.SetKeyUp(Key.S);

        // Has key down in events, should process
        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsRun(TickCommands.Backward);

        // Last key event was down should not process key down this time
        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsNotRun(TickCommands.Backward);
    }


    [Fact(DisplayName = "Same input down multiple times")]
    public void InputKeyDownMultiple()
    {
        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsNotRun(TickCommands.Left, TickCommands.Forward);

        InputManager.SetKeyDown(Key.W);
        for (int i = 0; i < 10; i++)
        {
            InputManager.SetKeyDown(Key.A);
            GameLayerManager.RunLogic(ZeroTick);
        }
        AssertCommandsNotRun(TickCommands.Left, TickCommands.Forward);

        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsRun(TickCommands.Left, TickCommands.Forward);

        InputManager.SetKeyDown(Key.A);
        InputManager.SetKeyUp(Key.A);
        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsRun(TickCommands.Left, TickCommands.Forward);

        InputManager.SetKeyDown(Key.A);
        InputManager.SetKeyUp(Key.A);
        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsRun(TickCommands.Left, TickCommands.Forward);

        InputManager.SetKeyDown(Key.A);
        InputManager.SetKeyUp(Key.A);
        for (int i = 0; i < 10; i++)
            GameLayerManager.RunLogic(ZeroTick);

        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsRun(TickCommands.Left, TickCommands.Forward);

        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsNotRun(TickCommands.Left);
        AssertCommandsRun(TickCommands.Forward);
    }

    private static void WorldInit(SinglePlayerWorld world)
    {
        ResetPlayer(world);
        world.Config.Hud.MoveBob.Set(0);
        world.Config.Mouse.Look.Set(false);
        world.Config.Game.AutoAim.Set(true);
    }

    private void ResetPlayer() => ResetPlayer(World);

    private static void ResetPlayer(SinglePlayerWorld world)
    {
        world.Player.Velocity = Vec3D.Zero;
        world.Player.ViewAngleRadians = 0;
        world.Player.ViewPitchRadians = 0;
        world.Player.AngleRadians = 0;
        world.Player.PitchRadians = 0;
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
