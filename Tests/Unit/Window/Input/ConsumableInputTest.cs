using System;
using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Window;
using Helion.Window.Input;
using Xunit;

namespace Helion.Tests.Unit.Window.Input;

public class ConsumableInputTest
{
    [Fact(DisplayName = "Can consume keys")]
    public void CanConsumeKeys()
    {
        // Want to get the following:
        //
        //       Frame n    Frame n-1
        //  A      down        down
        //  B      down         up
        //  C       up         down
        //  D       up          up
        ConsumableInput MakeInput()
        {
            InputManager inputManager = new();

            inputManager.SetKeyDown(Key.A);
            inputManager.SetKeyDown(Key.C);
            inputManager.Reset();
            inputManager.SetKeyDown(Key.B);
            inputManager.SetKeyUp(Key.C);

            IConsumableInput interfaceInput = inputManager.Poll();
            interfaceInput.Should().BeOfType<ConsumableInput>();
            return (ConsumableInput)interfaceInput;
        }

        // This wants us to pass in a consuming function (ex: like ConsumeKeyDown)
        // and check its results as to whether keys A, B, C, and D should result
        // true or false based on the function type provided.
        void TestConsumeFunc(Func<ConsumableInput, Key, bool> consumeFunc, bool a, bool b, bool c, bool d)
        {
            foreach ((Key key, bool expected) in new[] { (Key.A, a), (Key.B, b), (Key.C, c), (Key.D, d) })
            {
                ConsumableInput input = MakeInput();
                consumeFunc(input, key).Should().Be(expected);
                consumeFunc(input, key).Should().BeFalse();
            }
        }

        TestConsumeFunc((input, key) => input.ConsumeKeyDown(key), true, true, false, false);
        TestConsumeFunc((input, key) => input.ConsumeKeyPrevDown(key), true, false, true, false);
        TestConsumeFunc((input, key) => input.ConsumeKeyHeldDown(key), true, false, false, false);
        TestConsumeFunc((input, key) => input.ConsumeKeyUp(key), false, false, true, true);
        TestConsumeFunc((input, key) => input.ConsumeKeyPrevUp(key), false, true, false, true);
        TestConsumeFunc((input, key) => input.ConsumeKeyPressed(key), false, true, false, false);
        TestConsumeFunc((input, key) => input.ConsumeKeyReleased(key), false, false, true, false);
    }

    [Fact(DisplayName = "Can consume mouse movement")]
    public void CanConsumeMouseMovement()
    {
        InputManager inputManager = new();
        inputManager.AddMouseMovement((1, 1));

        ConsumableInput input = (ConsumableInput)inputManager.Poll();
        input.ConsumeMouseMove().Should().Be(new Vec2I(1, 1));
        input.ConsumeMouseMove().Should().Be(Vec2I.Zero);
    }

    [Fact(DisplayName = "Can consume mouse scrolling")]
    public void CanConsumeMouseScroll()
    {
        InputManager inputManager = new();
        inputManager.AddMouseScroll(5);

        ConsumableInput input = (ConsumableInput)inputManager.Poll();
        input.ConsumeScroll().Should().Be(5);
        input.ConsumeScroll().Should().Be(0);
    }

    [Fact(DisplayName = "Can consume typed characters")]
    public void CanConsumeTypedCharacters()
    {
        InputManager inputManager = new();
        inputManager.AddTypedCharacters("hi");

        ConsumableInput input = (ConsumableInput)inputManager.Poll();
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

        ConsumableInput input = (ConsumableInput)inputManager.Poll();
        input.ConsumeAll();

        input.ConsumeKeyDown(Key.A).Should().BeFalse();
        input.ConsumeMouseMove().Should().Be(Vec2I.Zero);
        input.ConsumeScroll().Should().Be(0);
        input.ConsumeTypedCharacters().ToString().Should().Be("");
    }
}
