using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Util.Container;
using Helion.Window;
using Helion.Window.Input;
using System.Threading;
using Xunit;

namespace Helion.Tests.Unit.Window.Input;

public class InputManagerTest
{
    [Fact(DisplayName = "Can set key down")]
    public void CanSetKeyDown()
    {
        InputManager inputManager = new();
        inputManager.Poll(true);
        inputManager.IsKeyDown(Key.A).Should().BeFalse();
        inputManager.IsKeyUp(Key.A).Should().BeTrue();
        inputManager.ProcessedKeys();

        inputManager.SetKeyDown(Key.A);
        inputManager.Poll(true);
        inputManager.IsKeyDown(Key.A).Should().BeTrue();
        inputManager.IsKeyUp(Key.A).Should().BeFalse();
        inputManager.ProcessedKeys();
    }

    [Fact(DisplayName = "Can set key up")]
    public void CanSetKeyUp()
    {
        InputManager inputManager = new();
        inputManager.IsKeyDown(Key.A).Should().BeFalse();
        inputManager.IsKeyUp(Key.A).Should().BeTrue();

        inputManager.SetKeyDown(Key.A);
        inputManager.Poll(true);
        inputManager.IsKeyDown(Key.A).Should().BeTrue();
        inputManager.IsKeyUp(Key.A).Should().BeFalse();
        inputManager.ProcessedKeys();

        inputManager.SetKeyUp(Key.A);
        // Key doesn't get removed from down until it's reset.
        inputManager.IsKeyDown(Key.A).Should().BeTrue();
        inputManager.IsKeyUp(Key.A).Should().BeFalse();
        inputManager.ProcessedKeys();

        inputManager.Poll(true);
        inputManager.IsKeyDown(Key.A).Should().BeFalse();
        inputManager.IsKeyUp(Key.A).Should().BeTrue();
    }

    [Fact(DisplayName = "Can add a typed character")]
    public void CanAddTypedCharacter()
    {
        InputManager inputManager = new();
        inputManager.Poll(true);
        inputManager.TypedCharacters.IsEmpty.Should().BeTrue();

        inputManager.AddTypedCharacters("hi");
        inputManager.Poll(true);
        inputManager.TypedCharacters.ToString().Should().Be("hi");
    }

    [Fact(DisplayName = "Can add mouse movement")]
    public void CanAddMouseMovement()
    {
        InputManager inputManager = new();
        inputManager.MouseMove.Should().Be(Vec2I.Zero);

        Vec2I move = (1, 2);
        inputManager.AddMouseMovement(move);
        inputManager.MouseMove.Should().Be(move);

        inputManager.AddMouseMovement(move);
        inputManager.MouseMove.Should().Be(move * 2);
    }

    [Fact(DisplayName = "Can add mouse scrolling")]
    public void CanAddMouseScrolling()
    {
        InputManager inputManager = new();
        inputManager.Scroll.Should().Be(0);

        inputManager.AddMouseScroll(4);
        inputManager.AddMouseScroll(-7);

        inputManager.Poll(true);
        inputManager.Scroll.Should().Be(-3);
    }

    [Fact(DisplayName = "Check press correctly")]
    public void CheckPress()
    {
        InputManager inputManager = new();
        inputManager.IsKeyPressed(Key.B).Should().BeFalse();

        inputManager.SetKeyDown(Key.B);
        inputManager.IsKeyPressed(Key.B).Should().BeFalse();

        inputManager.Poll(true);
        inputManager.IsKeyPressed(Key.B).Should().BeTrue();
        inputManager.ProcessedKeys();

        inputManager.Poll(true);
        inputManager.IsKeyPressed(Key.B).Should().BeFalse();
    }

    [Fact(DisplayName = "Check held down correctly")]
    public void CheckHeldDown()
    {
        InputManager inputManager = new();
        inputManager.Poll(true);
        inputManager.IsKeyHeldDown(Key.B).Should().BeFalse();

        inputManager.SetKeyDown(Key.B);
        inputManager.Poll(true);
        inputManager.IsKeyHeldDown(Key.B).Should().BeFalse();
        inputManager.ProcessedKeys();

        inputManager.Poll(true);
        inputManager.IsKeyHeldDown(Key.B).Should().BeTrue();
        inputManager.ProcessedKeys();

        inputManager.SetKeyUp(Key.B);
        inputManager.Poll(true);
        inputManager.ProcessedKeys();
        inputManager.Poll(true);

        inputManager.IsKeyHeldDown(Key.B).Should().BeFalse();
    }

    [Fact(DisplayName = "Check key previously down correctly")]
    public void CheckPrevDown()
    {
        InputManager inputManager = new();
        inputManager.IsKeyPrevDown(Key.MouseRight).Should().BeFalse();

        inputManager.SetKeyDown(Key.MouseRight);
        inputManager.IsKeyPrevDown(Key.MouseRight).Should().BeFalse();

        inputManager.Poll(true);
        inputManager.ProcessedKeys();
        inputManager.Poll(true);

        inputManager.IsKeyPrevDown(Key.MouseRight).Should().BeTrue();
        inputManager.ProcessedKeys();

        inputManager.SetKeyUp(Key.MouseRight);
        inputManager.Poll(true);
        inputManager.IsKeyPrevDown(Key.MouseRight).Should().BeTrue();
        inputManager.ProcessedKeys();

        inputManager.Poll(true);
        inputManager.IsKeyPrevDown(Key.MouseRight).Should().BeFalse();
    }

    [Fact(DisplayName = "Check any key pressed correctly")]
    public void CheckAnyKeyPressed()
    {
        InputManager inputManager = new();
        inputManager.Poll(true);
        inputManager.HasAnyKeyPressed().Should().BeFalse();

        inputManager.SetKeyDown(Key.F7);
        inputManager.Poll(true);
        inputManager.HasAnyKeyPressed().Should().BeTrue();
        inputManager.ProcessedKeys();

        inputManager.Poll(true);
        inputManager.HasAnyKeyPressed().Should().BeFalse();
        inputManager.ProcessedKeys();

        inputManager.SetKeyUp(Key.F7);
        inputManager.Poll(true);
        inputManager.HasAnyKeyPressed().Should().BeFalse();
        inputManager.ProcessedKeys();

        inputManager.Poll(true);
        inputManager.HasAnyKeyPressed().Should().BeFalse();
        inputManager.ProcessedKeys();
    }

    [Fact(DisplayName = "Reset clears all fields and moves input into previous")]
    public void ResetClearsAndPushesBackCorrectly()
    {
        InputManager inputManager = new();

        inputManager.SetKeyDown(Key.A);
        inputManager.AddMouseMovement((1, 1));
        inputManager.AddMouseScroll(5);
        inputManager.AddTypedCharacters("hi");

        inputManager.Poll(true);
        inputManager.ProcessedKeys();
        inputManager.ProcessedMouseMovement();
        inputManager.Poll(true);

        inputManager.IsKeyPrevDown(Key.A).Should().BeTrue();
        inputManager.IsKeyDown(Key.A).Should().BeTrue();
        inputManager.MouseMove.Should().Be(Vec2I.Zero);
        inputManager.Scroll.Should().Be(0);
        inputManager.TypedCharacters.ToString().Should().BeEquivalentTo("");
    }

    [Fact(DisplayName = "Polling will return a consumable that is based off of the current input manager")]
    public void PollingReturnsConsumableCorrectly()
    {
        InputManager inputManager = new();

        inputManager.SetKeyDown(Key.A);
        inputManager.AddMouseMovement((1, 1));
        inputManager.AddMouseScroll(5);
        inputManager.AddTypedCharacters("hi");

        IConsumableInput input = inputManager.Poll(true);

        input.ConsumeKeyDown(Key.A).Should().BeTrue();
        input.ConsumeKeyDown(Key.A).Should().BeFalse();
        input.ConsumeMouseMove().Should().Be(new Vec2I(1, 1));
        input.ConsumeMouseMove().Should().Be(Vec2I.Zero);
        input.Scroll.Should().Be(5);
        input.ConsumeScroll().Should().Be(5);
        input.ConsumeScroll().Should().Be(0);
        input.ConsumeTypedCharacters().ToString().Should().Be("hi");
        input.ConsumeTypedCharacters().ToString().Should().Be("");
    }

    [Fact(DisplayName = "Has any key down")]
    public void HasAnyKeyDown()
    {
        InputManager inputManager = new();
        inputManager.HasAnyKeyDown().Should().BeFalse();

        inputManager.SetKeyDown(Key.A);
        inputManager.Poll(true);
        inputManager.HasAnyKeyDown().Should().BeTrue();
        inputManager.ProcessedKeys();

        inputManager.Poll(true);
        inputManager.HasAnyKeyDown().Should().BeTrue();
        inputManager.ProcessedKeys();

        inputManager.SetKeyDown(Key.B);
        inputManager.SetKeyDown(Key.C);
        inputManager.SetKeyDown(Key.D);
        inputManager.Poll(true);
        inputManager.HasAnyKeyDown().Should().BeTrue();
        inputManager.ProcessedKeys();

        inputManager.SetKeyUp(Key.A);
        inputManager.SetKeyUp(Key.B);
        inputManager.SetKeyUp(Key.C);
        inputManager.SetKeyUp(Key.D);
        inputManager.Poll(true);
        inputManager.HasAnyKeyDown().Should().BeFalse();
        inputManager.ProcessedKeys();
    }

    [Fact(DisplayName = "Has any quick toggle")]
    public void HasAnyKeyDownQuickToggle()
    {
        InputManager inputManager = new();
        inputManager.HasAnyKeyDown().Should().BeFalse();

        inputManager.SetKeyDown(Key.A);
        inputManager.SetKeyUp(Key.A);
        inputManager.Poll(true);
        inputManager.HasAnyKeyDown().Should().BeTrue();
        inputManager.ProcessedKeys();
    }

    [Fact(DisplayName = "Get pressed keys")]
    public void GetPressedKeys()
    {
        InputManager inputManager = new();
        DynamicArray<Key> keys = new();
        inputManager.Poll(true);
        inputManager.GetPressedKeys(keys);
        keys.Length.Should().Be(0);
        inputManager.ProcessedKeys();

        inputManager.SetKeyDown(Key.A);
        inputManager.SetKeyDown(Key.B);
        inputManager.SetKeyDown(Key.C);

        inputManager.Poll(true);
        inputManager.GetPressedKeys(keys);
        inputManager.ProcessedKeys();
        keys.Length.Should().Be(3);

        inputManager.Poll(true);
        keys.Clear();
        inputManager.GetPressedKeys(keys);
        inputManager.ProcessedKeys();
        keys.Length.Should().Be(0);

        inputManager.SetKeyDown(Key.D);
        inputManager.SetKeyUp(Key.D);
        inputManager.SetKeyDown(Key.D);
        inputManager.Poll(true);
        keys.Clear();
        inputManager.GetPressedKeys(keys);
        inputManager.ProcessedKeys();
        keys.Length.Should().Be(1);
    }

    [Fact(DisplayName = "Clear input")]
    public void Clear()
    {
        InputManager inputManager = new();
        inputManager.SetKeyDown(Key.A);
        inputManager.AddMouseMovement((2, 2));
        inputManager.AddMouseScroll(4);
        inputManager.AddTypedCharacters("test");

        inputManager.Poll(true);
        inputManager.IsKeyDown(Key.A).Should().BeTrue();
        inputManager.MouseMove.Should().Be(new Vec2I(2, 2));
        inputManager.Scroll.Should().Be(4);
        inputManager.TypedCharacters.ToString().Should().Be("test");

        inputManager.Clear();
        inputManager.IsKeyDown(Key.A).Should().BeFalse();
        inputManager.IsKeyUp(Key.A).Should().BeTrue();
        inputManager.MouseMove.Should().Be(new Vec2I(0, 0));
        inputManager.Scroll.Should().Be(0);
        inputManager.TypedCharacters.ToString().Should().Be("");
        inputManager.HasAnyKeyDown().Should().BeFalse();
        inputManager.HasAnyKeyPressed().Should().BeFalse();
    }

    [Fact(DisplayName = "Continuous hold")]
    public void IsKeyContinuousHold()
    {
        InputManager inputManager = new();
        inputManager.IsKeyContinuousHold(Key.A).Should().BeFalse();

        inputManager.SetKeyDown(Key.A);
        inputManager.Poll(true);
        inputManager.IsKeyContinuousHold(Key.A).Should().BeFalse();
        inputManager.ProcessedKeys();

        Thread.Sleep(1000);
        inputManager.Poll(true);
        inputManager.IsKeyContinuousHold(Key.A).Should().BeTrue();
        inputManager.ProcessedKeys();

        Thread.Sleep(1000);
        inputManager.Poll(true);
        inputManager.IsKeyContinuousHold(Key.A).Should().BeTrue();
        inputManager.ProcessedKeys();

        inputManager.SetKeyUp(Key.A);
        inputManager.SetKeyDown(Key.A);
        inputManager.SetKeyUp(Key.A);
        inputManager.Poll(true);
        inputManager.IsKeyContinuousHold(Key.A).Should().BeFalse();
    }
}
