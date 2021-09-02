using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Window.Input;
using Xunit;

namespace Helion.Tests.Unit.Window.Input
{
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
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Check key previously up correctly")]
        public void CheckPrevUp()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Check key released correctly")]
        public void CheckReleased()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Check any key pressed correctly")]
        public void CheckAnyKeyPressed()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Reset clears all fields and moves input into previous")]
        public void ResetClearsAndPushesBackCorrectly()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Polling will return a consumable that is based off of the current input manager")]
        public void PollingReturnsConsumableCorrectly()
        {
            false.Should().BeTrue();
        }
    }
}
