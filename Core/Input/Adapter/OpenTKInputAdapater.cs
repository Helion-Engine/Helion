using Helion.Util.Geometry;
using OpenTK;
using OpenTK.Input;

namespace Helion.Input.Adapter
{
    public class OpenTKInputAdapter : InputAdapter
    {
        private static InputKey ToInputKey(Key key)
        {
            switch (key)
            {
            // TODO: Mapping here from Key -> InputKey
            default:
                return InputKey.Unknown;
            }
        }

        public void HandleMouseMovement(MouseMoveEventArgs e)
        {
            InputEventArgs inputEvent = new InputEventArgs();
            inputEvent.MouseInput.Delta = new Vec2i(e.XDelta, e.YDelta);
            EmitEvent(inputEvent);
        }

        public void HandleMouseWheelInput(MouseWheelEventArgs e)
        {
            // Note: We can also access e.DeltaPrecise to support better
            // scrolling in high quality mice.

            InputEventArgs inputEvent = new InputEventArgs();
            inputEvent.MouseInput.ScrollDelta = e.Delta;
            EmitEvent(inputEvent);
        }

        public void HandleKeyPress(KeyPressEventArgs e)
        {
            InputEventArgs inputEvent = new InputEventArgs();
            inputEvent.CharactersTyped.Add(e.KeyChar);
            EmitEvent(inputEvent);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            InputEventArgs inputEvent = new InputEventArgs();

            InputKey inputKey = ToInputKey(e.Key);
            if (inputKey != InputKey.Unknown)
                inputEvent.InputDown.Add(inputKey);

            // TODO: Handle shift/ctrl/etc.

            EmitEvent(inputEvent);
        }

        public void HandleKeyUp(KeyboardKeyEventArgs e)
        {
            InputEventArgs inputEvent = new InputEventArgs();

            InputKey inputKey = ToInputKey(e.Key);
            if (inputKey != InputKey.Unknown)
                inputEvent.InputUp.Add(inputKey);

            // TODO: Handle shift/ctrl/etc.

            EmitEvent(inputEvent);
        }

        public override void PollInput()
        {
            // Due to how the implementation is set up, we don't need to poll
            // because the listeners on the GameWindow do all of that for us.
        }
    }
}
