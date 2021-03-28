using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Input;
using Xunit;

namespace Helion.Tests.Unit.Input
{
    public class InputManagerTest
    {
        [Fact(DisplayName = "Set key down")]
        public void PressKey()
        {
            InputManager inputManager = new();
            inputManager.IsKeyUp(Key.Hash).Should().BeTrue();
            
            inputManager.SetKeyDown(Key.Hash);
            
            inputManager.IsKeyDown(Key.Hash).Should().BeTrue();
            inputManager.IsKeyUp(Key.Hash).Should().BeFalse();
            inputManager.TypedCharacters.Should().Be("#");
            inputManager.KeysDown.Should().Contain(Key.Hash);

            InputEvent inputEvent = inputManager.PollInput();
            inputEvent.ConsumeKeyPressed(Key.Hash).Should().BeTrue();
            inputEvent.ConsumeTypedCharacters().Should().Be("#");
        }
     
        [Fact(DisplayName = "Set key up (same as clearing)")]
        public void ReleaseKey()
        {
            InputManager inputManager = new();
            
            inputManager.SetKeyDown(Key.A);
            inputManager.IsKeyDown(Key.A).Should().BeTrue();
            inputManager.IsKeyUp(Key.A).Should().BeFalse();
            
            inputManager.SetKeyUp(Key.A);
            inputManager.IsKeyDown(Key.A).Should().BeFalse();
            inputManager.IsKeyUp(Key.A).Should().BeTrue();
            
            // We don't clear it if it was pressed, since there may be a case
            // where someone presses and releases quickly before a tick ends,
            // and we want that to be considered a 'press'.
            inputManager.PollInput().ConsumeKeyPressed(Key.A).Should().BeTrue();
        }
        
        [Fact(DisplayName = "Set key down with number")]
        public void SubmitNumberKey()
        {
            InputManager inputManager = new();
            
            inputManager.SetKeyDown(Key.One);
            
            inputManager.IsKeyDown(Key.One).Should().BeTrue();
            inputManager.TypedCharacters.Should().Be("1");
            inputManager.KeysDown.Should().Contain(Key.One);
            inputManager.PollInput().ConsumeTypedCharacters().Should().Be("1");
        }

        [Fact(DisplayName = "Set key down with character")]
        public void SubmitCharacterKey()
        {
            InputManager inputManager = new();
            
            inputManager.SetKeyDown(Key.B);
            
            inputManager.IsKeyDown(Key.B).Should().BeTrue();
            inputManager.TypedCharacters.Should().Be("b");
            inputManager.KeysDown.Should().Contain(Key.B);
            inputManager.PollInput().ConsumeTypedCharacters().Should().Be("b");
        }

        [Fact(DisplayName = "Set key down with capital character")]
        public void SubmitCapitalCharacter()
        {
            InputManager inputManager = new();
            
            inputManager.SetKeyDown(Key.B, true);
            
            inputManager.IsKeyDown(Key.B).Should().BeTrue();
            inputManager.TypedCharacters.Should().Be("B");
            inputManager.KeysDown.Should().Contain(Key.B);
            inputManager.PollInput().ConsumeTypedCharacters().Should().Be("B");
        }

        [Fact(DisplayName = "Set key down with character and repeating")]
        public void SubmitRepeatingCharacter()
        {
            InputManager inputManager = new();
            
            inputManager.SetKeyDown(Key.A);
            inputManager.IsKeyDown(Key.A).Should().BeTrue();
            inputManager.TypedCharacters.Should().Be("a");
            
            // This should not add any characters since it's not 'repeat'.
            inputManager.SetKeyDown(Key.A);
            inputManager.IsKeyDown(Key.A).Should().BeTrue();
            inputManager.TypedCharacters.Should().Be("a");
            
            // Now we repeat, so it should appear twice.
            inputManager.SetKeyDown(Key.A, false, true);
            inputManager.IsKeyDown(Key.A).Should().BeTrue();
            inputManager.TypedCharacters.Should().Be("aa");
        }
        
        [Fact(DisplayName = "Checks for pressed keys")]
        public void PressedKey()
        {
            InputManager inputManager = new();
            inputManager.IsKeyPressed(Key.A).Should().BeFalse();
            
            inputManager.SetKeyDown(Key.A);
            inputManager.IsKeyPressed(Key.A).Should().BeTrue();
            
            // Nothing should change if we press it again.
            inputManager.SetKeyDown(Key.A);
            inputManager.IsKeyPressed(Key.A).Should().BeTrue();

            inputManager.PollInput();
            inputManager.IsKeyPressed(Key.A).Should().BeFalse();
            
            // Adding a key when we pressed it last time means it should still
            // not be pressed, but rather 'continually down'.
            inputManager.SetKeyDown(Key.A);
            inputManager.IsKeyPressed(Key.A).Should().BeFalse();
        }
        
        [Fact(DisplayName = "Checks for released keys")]
        public void ReleasedKey()
        {
            InputManager inputManager = new();
            inputManager.IsKeyReleased(Key.A).Should().BeFalse();
            
            inputManager.SetKeyDown(Key.A);
            inputManager.IsKeyReleased(Key.A).Should().BeFalse();
            
            inputManager.PollInput();
            inputManager.IsKeyReleased(Key.A).Should().BeFalse();
            
            inputManager.SetKeyUp(Key.A);
            inputManager.IsKeyReleased(Key.A).Should().BeTrue();
            
            // If it's released but we press it again, then it's no longer
            // a released key.
            inputManager.SetKeyDown(Key.A);
            inputManager.IsKeyReleased(Key.A).Should().BeFalse();
        }

        [Fact(DisplayName = "Add mouse movement")]
        public void AddMouseMovement()
        {
            InputManager inputManager = new();
            inputManager.PollInput().ConsumeMouseDelta().Should().Be(Vec2I.Zero);
            
            inputManager.AddMouseMovement(1, -2);
            inputManager.MouseMove.Should().Be(new Vec2I(1, -2));
            inputManager.PollInput().ConsumeMouseDelta().Should().Be(new Vec2I(1, -2));
            
            inputManager.AddMouseMovement(1, 1);
            inputManager.MouseMove.Should().Be(new Vec2I(1, 1));
            inputManager.AddMouseMovement(-2, 4);
            inputManager.MouseMove.Should().Be(new Vec2I(-1, 5));
            inputManager.PollInput().ConsumeMouseDelta().Should().Be(new Vec2I(-1, 5));
        }

        [Fact(DisplayName = "Add scroll")]
        public void AddScrolling()
        {
            InputManager inputManager = new();
            inputManager.MouseScroll.Should().Be(0);
            inputManager.PollInput().ConsumeMouseScroll().Should().Be(0);
            
            inputManager.AddScroll(5);
            inputManager.MouseScroll.Should().Be(5);
            inputManager.PollInput().ConsumeMouseScroll().Should().Be(5);
            
            inputManager.AddScroll(2);
            inputManager.MouseScroll.Should().Be(2);
            inputManager.AddScroll(-8);
            inputManager.MouseScroll.Should().Be(-6);
            inputManager.PollInput().ConsumeMouseScroll().Should().Be(-6);
        }
    }
}
