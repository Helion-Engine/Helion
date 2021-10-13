using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Window;
using Helion.Window.Input;
using Xunit;

namespace Helion.Tests.Unit.Window.Input;

public class InputManagerTest
{
    [Fact(DisplayName = "Can set key down")]
    public void CanSetKeyDown()
    {
        InputManager inputManager = new();
        inputManager.IsKeyDown(Key.A).Should().BeFalse();
        inputManager.IsKeyUp(Key.A).Should().BeTrue();

        inputManager.SetKeyDown(Key.A);
        inputManager.IsKeyDown(Key.A).Should().BeTrue();
        inputManager.IsKeyUp(Key.A).Should().BeFalse();
    }

    [Fact(DisplayName = "Can set key up")]
    public void CanSetKeyUp()
    {
        InputManager inputManager = new();
        inputManager.IsKeyDown(Key.A).Should().BeFalse();
        inputManager.IsKeyUp(Key.A).Should().BeTrue();

        inputManager.SetKeyDown(Key.A);
        inputManager.IsKeyDown(Key.A).Should().BeTrue();
        inputManager.IsKeyUp(Key.A).Should().BeFalse();

        inputManager.SetKeyUp(Key.A);
        inputManager.IsKeyDown(Key.A).Should().BeFalse();
        inputManager.IsKeyUp(Key.A).Should().BeTrue();
    }

    [Fact(DisplayName = "Can add a typed character")]
    public void CanAddTypedCharacter()
    {
        InputManager inputManager = new();
        inputManager.TypedCharacters.IsEmpty.Should().BeTrue();

        inputManager.AddTypedCharacters("hi");
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
        inputManager.Scroll.Should().Be(4);

        inputManager.AddMouseScroll(-7);
        inputManager.Scroll.Should().Be(-3);
    }

    [Fact(DisplayName = "Check press correctly")]
    public void CheckPress()
    {
        InputManager inputManager = new();
        inputManager.IsKeyPressed(Key.B).Should().BeFalse();

        inputManager.SetKeyDown(Key.B);
        inputManager.IsKeyPressed(Key.B).Should().BeTrue();

        inputManager.Reset();
        inputManager.IsKeyPressed(Key.B).Should().BeFalse();
    }

    [Fact(DisplayName = "Check held down correctly")]
    public void CheckHeldDown()
    {
        InputManager inputManager = new();
        inputManager.IsKeyHeldDown(Key.B).Should().BeFalse();

        inputManager.SetKeyDown(Key.B);
        inputManager.IsKeyHeldDown(Key.B).Should().BeFalse();

        inputManager.Reset();
        inputManager.IsKeyHeldDown(Key.B).Should().BeTrue();

        inputManager.SetKeyUp(Key.B);
        inputManager.IsKeyHeldDown(Key.B).Should().BeFalse();
    }

    [Fact(DisplayName = "Check key previously down correctly")]
    public void CheckPrevDown()
    {
        InputManager inputManager = new();
        inputManager.IsKeyPrevDown(Key.MouseRight).Should().BeFalse();

        inputManager.SetKeyDown(Key.MouseRight);
        inputManager.IsKeyPrevDown(Key.MouseRight).Should().BeFalse();

        inputManager.Reset();
        inputManager.IsKeyPrevDown(Key.MouseRight).Should().BeTrue();

        inputManager.SetKeyUp(Key.MouseRight);
        inputManager.IsKeyPrevDown(Key.MouseRight).Should().BeTrue();

        inputManager.Reset();
        inputManager.IsKeyPrevDown(Key.MouseRight).Should().BeFalse();
    }

    [Fact(DisplayName = "Check key previously up correctly")]
    public void CheckPrevUp()
    {
        InputManager inputManager = new();
        inputManager.IsKeyPrevUp(Key.Ampersand).Should().BeTrue();

        inputManager.SetKeyDown(Key.Ampersand);
        inputManager.IsKeyPrevUp(Key.Ampersand).Should().BeTrue();

        inputManager.Reset();
        inputManager.IsKeyPrevUp(Key.Ampersand).Should().BeFalse();

        inputManager.SetKeyUp(Key.Ampersand);
        inputManager.IsKeyPrevUp(Key.Ampersand).Should().BeFalse();

        inputManager.Reset();
        inputManager.IsKeyPrevUp(Key.Ampersand).Should().BeTrue();
    }

    [Fact(DisplayName = "Check key released correctly")]
    public void CheckReleased()
    {
        InputManager inputManager = new();
        inputManager.IsKeyReleased(Key.F7).Should().BeFalse();

        inputManager.SetKeyDown(Key.F7);
        inputManager.IsKeyReleased(Key.F7).Should().BeFalse();

        inputManager.Reset();
        inputManager.IsKeyReleased(Key.F7).Should().BeFalse();

        inputManager.SetKeyUp(Key.F7);
        inputManager.IsKeyReleased(Key.F7).Should().BeTrue();

        inputManager.Reset();
        inputManager.IsKeyReleased(Key.Ampersand).Should().BeFalse();
    }

    [Fact(DisplayName = "Check any key pressed correctly")]
    public void CheckAnyKeyPressed()
    {
        InputManager inputManager = new();
        inputManager.HasAnyKeyPressed().Should().BeFalse();

        inputManager.SetKeyDown(Key.F7);
        inputManager.HasAnyKeyPressed().Should().BeTrue();

        inputManager.Reset();
        inputManager.HasAnyKeyPressed().Should().BeFalse();

        inputManager.SetKeyUp(Key.F7);
        inputManager.HasAnyKeyPressed().Should().BeFalse();

        inputManager.Reset();
        inputManager.HasAnyKeyPressed().Should().BeFalse();
    }

    [Fact(DisplayName = "Reset clears all fields and moves input into previous")]
    public void ResetClearsAndPushesBackCorrectly()
    {
        InputManager inputManager = new();

        inputManager.SetKeyDown(Key.A);
        inputManager.AddMouseMovement((1, 1));
        inputManager.AddMouseScroll(5);
        inputManager.AddTypedCharacters("hi");

        inputManager.Reset();

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

        IConsumableInput input = inputManager.Poll();

        input.ConsumeKeyDown(Key.A).Should().BeTrue();
        input.ConsumeKeyDown(Key.A).Should().BeFalse();
        input.ConsumeMouseMove().Should().Be(new Vec2I(1, 1));
        input.ConsumeMouseMove().Should().Be(Vec2I.Zero);
        input.ConsumeScroll().Should().Be(5);
        input.ConsumeScroll().Should().Be(0);
        input.ConsumeTypedCharacters().ToString().Should().Be("hi");
        input.ConsumeTypedCharacters().ToString().Should().Be("");
    }

    [Fact(DisplayName = "Detect the special print screen case")]
    public void CanDetectPrintScreen()
    {
        // Prev: NO   Now: NO
        InputManager inputManager = new();
        inputManager.IsKeyDown(Key.PrintScreen).Should().BeFalse();
        inputManager.IsKeyPrevDown(Key.PrintScreen).Should().BeFalse();
        inputManager.IsKeyHeldDown(Key.PrintScreen).Should().BeFalse();
        inputManager.IsKeyUp(Key.PrintScreen).Should().BeTrue();
        inputManager.IsKeyPrevUp(Key.PrintScreen).Should().BeTrue();
        inputManager.IsKeyPressed(Key.PrintScreen).Should().BeFalse();
        inputManager.IsKeyReleased(Key.PrintScreen).Should().BeFalse();
        inputManager.HasAnyKeyPressed().Should().BeFalse();
        inputManager.HasAnyKeyDown().Should().BeFalse();

        // Prev: NO   Now: YES
        inputManager.SetKeyDown(Key.PrintScreen);
        inputManager.IsKeyDown(Key.PrintScreen).Should().BeTrue();
        inputManager.IsKeyPrevDown(Key.PrintScreen).Should().BeFalse();
        inputManager.IsKeyHeldDown(Key.PrintScreen).Should().BeFalse();
        inputManager.IsKeyUp(Key.PrintScreen).Should().BeFalse();
        inputManager.IsKeyPrevUp(Key.PrintScreen).Should().BeTrue();
        inputManager.IsKeyPressed(Key.PrintScreen).Should().BeTrue();
        inputManager.IsKeyReleased(Key.PrintScreen).Should().BeFalse();
        inputManager.HasAnyKeyPressed().Should().BeTrue();
        inputManager.HasAnyKeyDown().Should().BeTrue();

        // Prev: YES   Now: NO
        inputManager.Reset();
        inputManager.IsKeyDown(Key.PrintScreen).Should().BeFalse();
        inputManager.IsKeyPrevDown(Key.PrintScreen).Should().BeTrue();
        inputManager.IsKeyHeldDown(Key.PrintScreen).Should().BeFalse();
        inputManager.IsKeyUp(Key.PrintScreen).Should().BeTrue();
        inputManager.IsKeyPrevUp(Key.PrintScreen).Should().BeFalse();
        inputManager.IsKeyPressed(Key.PrintScreen).Should().BeFalse();
        inputManager.IsKeyReleased(Key.PrintScreen).Should().BeTrue();
        inputManager.HasAnyKeyPressed().Should().BeFalse();
        inputManager.HasAnyKeyDown().Should().BeFalse();

        // Prev: YES   Now: YES
        inputManager.SetKeyDown(Key.PrintScreen);
        inputManager.IsKeyDown(Key.PrintScreen).Should().BeTrue();
        inputManager.IsKeyPrevDown(Key.PrintScreen).Should().BeTrue();
        inputManager.IsKeyHeldDown(Key.PrintScreen).Should().BeTrue();
        inputManager.IsKeyUp(Key.PrintScreen).Should().BeFalse();
        inputManager.IsKeyPrevUp(Key.PrintScreen).Should().BeFalse();
        inputManager.IsKeyPressed(Key.PrintScreen).Should().BeFalse();
        inputManager.IsKeyReleased(Key.PrintScreen).Should().BeFalse();
        inputManager.HasAnyKeyPressed().Should().BeFalse();
        inputManager.HasAnyKeyDown().Should().BeTrue();
    }
}
