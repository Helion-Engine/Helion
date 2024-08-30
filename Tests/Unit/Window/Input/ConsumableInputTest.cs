using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Configs.Impl;
using Helion.Window;
using Helion.Window.Input;
using System.Collections.Generic;
using Xunit;

namespace Helion.Tests.Unit.Window.Input;

public class ConsumableInputTest
{
    [Fact(DisplayName = "Can consume keys")]
    public void CanConsumeKeys()
    {
        InputManager inputManager = new();
        ConsumableInput CreateInput()
        {
            IConsumableInput interfaceInput = inputManager.Poll(true);
            interfaceInput.Should().BeOfType<ConsumableInput>();
            return (ConsumableInput)interfaceInput;
        }

        var input = CreateInput();
        input.ConsumeKeyDown(Key.A).Should().BeFalse();
        inputManager.SetKeyDown(Key.A);
        input.ConsumeKeyDown(Key.A).Should().BeFalse();

        input = CreateInput();
        input.ConsumeKeyDown(Key.A).Should().BeTrue();
        input.ConsumeKeyDown(Key.A).Should().BeFalse();
        inputManager.IsKeyDown(Key.A).Should().BeTrue();
        inputManager.ProcessedKeys();

        inputManager.SetKeyUp(Key.A);

        input = CreateInput();
        input.ConsumeKeyDown(Key.A).Should().BeFalse();
        inputManager.IsKeyDown(Key.A).Should().BeFalse();
        inputManager.IsKeyUp(Key.A).Should().BeTrue();
        inputManager.ProcessedKeys();

        inputManager.SetKeyDown(Key.A);
        input = CreateInput();
        input.ConsumeKeyDown(Key.A).Should().BeTrue();

        input = CreateInput();
        input.ConsumeKeyDown(Key.A).Should().BeTrue();

        inputManager.ProcessedKeys();
        input = CreateInput();
        input.ConsumeKeyDown(Key.A).Should().BeTrue();
    }

    [Fact(DisplayName = "Consume press or hold")]
    public void ConsumePressOrContinuousHold()
    {
        InputManager inputManager = new();
        ConsumableInput CreateInput()
        {
            IConsumableInput interfaceInput = inputManager.Poll(true);
            interfaceInput.Should().BeOfType<ConsumableInput>();
            return (ConsumableInput)interfaceInput;
        }

        var input = CreateInput();
        input.ConsumePressOrContinuousHold(Key.A).Should().BeFalse();
        inputManager.SetKeyDown(Key.A);
        input.ConsumePressOrContinuousHold(Key.A).Should().BeFalse();

        input = CreateInput();
        input.ConsumePressOrContinuousHold(Key.A).Should().BeTrue();

        inputManager.SetKeyDown(Key.B);
        input = CreateInput();
        input.ConsumePressOrContinuousHold(Key.A).Should().BeFalse();
        input.ConsumePressOrContinuousHold(Key.B).Should().BeTrue();
    }

    [Fact(DisplayName = "Quickly toggle between key down and key up")]
    public void ConsumeKeyDownToggle()
    {
        InputManager inputManager = new();
        ConsumableInput CreateInput()
        {
            IConsumableInput interfaceInput = inputManager.Poll(true);
            interfaceInput.Should().BeOfType<ConsumableInput>();
            return (ConsumableInput)interfaceInput;
        }

        var input = CreateInput();
        input.ConsumeKeyDown(Key.A).Should().BeFalse();

        inputManager.SetKeyDown(Key.A);
        input = CreateInput();
        input.ConsumeKeyDown(Key.A).Should().BeTrue();
        inputManager.ProcessedKeys();

        inputManager.SetKeyUp(Key.A);
        inputManager.SetKeyDown(Key.A);
        inputManager.SetKeyUp(Key.A);

        input = CreateInput();
        input.ConsumeKeyDown(Key.A).Should().BeTrue();
    }

    [Fact(DisplayName = "Quickly toggle between key down and key up")]
    public void ConsumeKeyPressedToggle()
    {
        InputManager inputManager = new();
        ConsumableInput CreateInput()
        {
            IConsumableInput interfaceInput = inputManager.Poll(true);
            interfaceInput.Should().BeOfType<ConsumableInput>();
            return (ConsumableInput)interfaceInput;
        }

        var input = CreateInput();
        input.ConsumeKeyPressed(Key.A).Should().BeFalse();

        inputManager.SetKeyDown(Key.A);
        CreateInput();
        inputManager.ProcessedKeys();

        inputManager.SetKeyUp(Key.A);
        inputManager.SetKeyDown(Key.A);

        input = CreateInput();
        input.ConsumeKeyPressed(Key.A).Should().BeTrue();
    }

    [Fact(DisplayName = "Consume key and key press")]
    public void ConsumeKeyAndKeyPress()
    {
        InputManager inputManager = new();
        ConsumableInput CreateInput()
        {
            IConsumableInput interfaceInput = inputManager.Poll(true);
            interfaceInput.Should().BeOfType<ConsumableInput>();
            return (ConsumableInput)interfaceInput;
        }

        var input = CreateInput();
        input.ConsumeKeyDown(Key.A).Should().BeFalse();
        input.ConsumeKeyPressed(Key.A).Should().BeFalse();
        inputManager.ProcessedKeys();

        inputManager.SetKeyDown(Key.A);
        input = CreateInput();
        input.ConsumeKeyDown(Key.A).Should().BeTrue();
        input.ConsumeKeyPressed(Key.A).Should().BeFalse();
        inputManager.ProcessedKeys();

        inputManager.SetKeyUp(Key.A);
        CreateInput();
        inputManager.ProcessedKeys();

        inputManager.SetKeyDown(Key.A);
        input = CreateInput();
        input.ConsumeKeyPressed(Key.A).Should().BeTrue();
        input.ConsumeKeyDown(Key.A).Should().BeFalse();
    }

    [Fact(DisplayName = "Consume key press")]
    public void ConsumeKeyPress()
    {
        InputManager inputManager = new();
        ConsumableInput CreateInput()
        {
            IConsumableInput interfaceInput = inputManager.Poll(true);
            interfaceInput.Should().BeOfType<ConsumableInput>();
            return (ConsumableInput)interfaceInput;
        }

        var input = CreateInput();
        input.ConsumeKeyPressed(Key.A).Should().BeFalse();
        inputManager.ProcessedKeys();

        inputManager.SetKeyDown(Key.A);

        input = CreateInput();
        input.ConsumeKeyPressed(Key.A).Should().BeTrue();
        inputManager.ProcessedKeys();

        input = CreateInput();
        input.ConsumeKeyPressed(Key.A).Should().BeFalse();
        inputManager.ProcessedKeys();
    }

    [Fact(DisplayName = "Can consume mouse movement")]
    public void CanConsumeMouseMovement()
    {
        InputManager inputManager = new();
        inputManager.AddMouseMovement((1, 1));

        ConsumableInput input = (ConsumableInput)inputManager.Poll(true);
        input.ConsumeMouseMove().Should().Be(new Vec2I(1, 1));
        input.ConsumeMouseMove().Should().Be(Vec2I.Zero);
    }

    [Fact(DisplayName = "Can consume mouse scrolling")]
    public void CanConsumeMouseScroll()
    {
        InputManager inputManager = new();
        inputManager.AddMouseScroll(5);

        ConsumableInput input = (ConsumableInput)inputManager.Poll(true);
        input.ConsumeScroll().Should().Be(5);
        input.ConsumeScroll().Should().Be(0);
    }

    [Fact(DisplayName = "Can consume typed characters")]
    public void CanConsumeTypedCharacters()
    {
        InputManager inputManager = new();
        inputManager.AddTypedCharacters("hi");

        ConsumableInput input = (ConsumableInput)inputManager.Poll(true);
        input.ConsumeTypedCharacters().ToString().Should().Be("hi");
        input.ConsumeTypedCharacters().ToString().Should().Be("");
    }

    [Fact(DisplayName = "Can consume all")]
    public void CanConsumeAll()
    {
        InputManager inputManager = new();
        inputManager.SetKeyDown(Key.A);
        inputManager.AddMouseMovement((1, 1));
        inputManager.AddMouseScroll(5);
        inputManager.AddTypedCharacters("hi");

        ConsumableInput input = (ConsumableInput)inputManager.Poll(true);
        input.ConsumeAll();

        input.ConsumeKeyDown(Key.A).Should().BeFalse();
        input.ConsumeMouseMove().Should().Be(Vec2I.Zero);
        input.ConsumeScroll().Should().Be(0);
        input.ConsumeTypedCharacters().ToString().Should().Be("");
    }

    [Fact(DisplayName = "Keys not pressed are not consumed")]
    public void KeysNotPressedAreNotConsumed()
    {
        InputManager inputManager = new();

        ConsumableInput input = (ConsumableInput)inputManager.Poll(true);
        input.ConsumeKeyDown(Key.E).Should().BeFalse();
        input.IsKeyDownConsumed(Key.E).Should().BeFalse();

        input.ConsumeKeyDown(Key.E).Should().BeFalse();
        input.IsKeyDownConsumed(Key.Z).Should().BeFalse();

        inputManager.SetKeyDown(Key.E);
        inputManager.SetKeyDown(Key.Z);

        input = (ConsumableInput)inputManager.Poll(true);
        input.ConsumeKeyDown(Key.E).Should().BeTrue();
        input.ConsumeKeyDown(Key.Z).Should().BeTrue();
    }

    [Fact(DisplayName = "Iterate commands")]
    public void IterateCommands()
    {
        InputManager inputManager = new();
        ConsumableInput input = (ConsumableInput)inputManager.Poll(true);
        List<KeyCommandItem> commands = [new(Key.PrintScreen, Constants.Input.Screenshot), new(Key.F12, Constants.Input.CenterView), new(Key.F1, Constants.Input.Attack)];

        inputManager.SetKeyDown(Key.PrintScreen);
        inputManager.SetKeyDown(Key.F12);
        input = (ConsumableInput)inputManager.Poll(true);

        input.IterateCommands(commands, IterateNoConsume);
        input.IsKeyDownConsumed(Key.PrintScreen).Should().BeFalse();
        input.IsKeyDownConsumed(Key.F12).Should().BeFalse();

        input.IterateCommands(commands, IterateConsume);
        input.IsKeyDownConsumed(Key.PrintScreen).Should().BeTrue();
        input.IsKeyDownConsumed(Key.F12).Should().BeTrue();

        bool IterateNoConsume(IConsumableInput input, KeyCommandItem item)
        {
            return false;
        }

        bool IterateConsume(IConsumableInput input, KeyCommandItem item)
        {
            return true;
        }
    }
}
