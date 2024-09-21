namespace Helion.Tests.Unit.GameLayer;

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
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

[Collection("GameActions")]
public class GameLayerInput
{
    private static Vec3D PlayerSpeedTestPos = new(-640, -544, 0);
    private readonly SinglePlayerWorld World;
    private readonly GameLayerManager GameLayerManager;
    private readonly WorldLayer WorldLayer;
    private readonly InputManager InputManager;
    private readonly FakeAnalogAdapter AnalogAdapter;

    private Player Player => World.Player;

    private static readonly TickerInfo ZeroTick = new(0, 0);
    private static readonly TickerInfo SingleTick = new(1, 0);

    private class FakeAnalogAdapter : IAnalogAdapter
    {
        public List<(Key, float)> AxisValues { get; set; } = new List<(Key, float)>();

        public bool KeyIsAnalogAxis(Key key)
        {
            return key.ToString().StartsWith("Axis");
        }

        public bool TryGetAnalogValueForAxis(Key key, out float axisAnalogValue)
        {
            (Key _, axisAnalogValue) = AxisValues.FirstOrDefault(a => a.Item1 == key);
            return true;
        }
    }

    public GameLayerInput()
    {
        HelionLoggers.Initialize(new());
        World = WorldAllocator.LoadMap("Resources/playermovement.zip", "playermovement.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
        InputManager = new InputManager();
        AnalogAdapter = new FakeAnalogAdapter();
        InputManager.AnalogAdapter = AnalogAdapter;
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

        World.Config.Keys.Add(Key.Axis1Minus, Constants.Input.Forward);
        World.Config.Keys.Add(Key.Axis1Plus, Constants.Input.Backward);
        World.Config.Keys.Add(Key.Axis2Plus, Constants.Input.Right);
        World.Config.Keys.Add(Key.Axis2Minus, Constants.Input.Left);
        World.Config.Keys.Add(Key.Axis3Plus, Constants.Input.Attack);

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

    [Fact(DisplayName = "Analog axes mapped to keys should show analog behavior")]
    public void AnalogMappingScaled()
    {
        ResetPlayer();
        AnalogAdapter.AxisValues.Clear();

        GameLayerManager.RunLogic(SingleTick);
        Player.Velocity.Should().Be(Vec3D.Zero);

        AnalogAdapter.AxisValues.Add((Key.Axis1Minus, 0.5f));
        AnalogAdapter.AxisValues.Add((Key.Axis2Plus, 0.5f));
        InputManager.SetKeyDown(Key.Axis1Minus);
        InputManager.SetKeyDown(Key.Axis2Plus);

        // These inputs are _scaled_, so we should enter velocity values rather than on/off commands.
        GameLayerManager.RunLogic(SingleTick);
        AssertCommandsNotRun(TickCommands.Forward, TickCommands.Backward, TickCommands.Left, TickCommands.Right);
        Player.Velocity.Should().NotBe(Vec3D.Zero);
        Vec3D velocityHalfInput = Player.Velocity;

        // It should also be truly analog--larger displacements should produce larger accelerations.
        ResetPlayer();
        AnalogAdapter.AxisValues.Clear();
        AnalogAdapter.AxisValues.Add((Key.Axis1Minus, 1.0f));
        AnalogAdapter.AxisValues.Add((Key.Axis2Plus, 1.0f));
        InputManager.SetKeyDown(Key.Axis1Minus);
        InputManager.SetKeyDown(Key.Axis2Plus);
        GameLayerManager.RunLogic(SingleTick);
        Vec3D velocityFullInput = Player.Velocity;

        Math.Abs(velocityFullInput.X).Should().BeGreaterThan(Math.Abs(velocityHalfInput.X));
        Math.Abs(velocityFullInput.Y).Should().BeGreaterThan(Math.Abs(velocityHalfInput.Y));

        Math.Abs(velocityFullInput.Y).Should().Be(Player.SideMovementSpeedRun);
        Math.Abs(velocityFullInput.X).Should().Be(Player.ForwardMovementSpeedRun);
    }

    [Fact(DisplayName = "Analog axes mapped to keys should should not affect velocity unless there is displacement")]
    public void AnalogMappingZero()
    {
        ResetPlayer();
        AnalogAdapter.AxisValues.Clear();

        GameLayerManager.RunLogic(SingleTick);
        Player.Velocity.Should().Be(Vec3D.Zero);

        // Since the inputs are scaled, setting zeroes should result in no velocity
        // This should help handle any possible cases where a virtual key input gets "stuck".
        AnalogAdapter.AxisValues.Add((Key.Axis1Minus, 0));
        AnalogAdapter.AxisValues.Add((Key.Axis2Plus, 0));
        InputManager.SetKeyDown(Key.Axis1Minus);
        InputManager.SetKeyDown(Key.Axis2Plus);

        GameLayerManager.RunLogic(SingleTick);
        Player.Velocity.Should().Be(Vec3D.Zero);
    }

    [Fact(DisplayName = "Analog axes mapped to non-analog functions should behave as command inputs")]
    public void AnalogMappingOnOffInput()
    {
        ResetPlayer();
        AnalogAdapter.AxisValues.Clear();

        GameLayerManager.RunLogic(SingleTick);
        Player.Velocity.Should().Be(Vec3D.Zero);

        // This input is mapped to "attack", so it shouldn't directly affect velocity
        AnalogAdapter.AxisValues.Add((Key.Axis3Plus, 1.0f));
        InputManager.SetKeyDown(Key.Axis3Plus);

        GameLayerManager.RunLogic(SingleTick);
        Player.Velocity.Should().Be(Vec3D.Zero);

        AssertCommandsRun(TickCommands.Attack);
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
