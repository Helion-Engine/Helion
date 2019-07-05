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
            case Key.ShiftLeft:
                return InputKey.ShiftLeft;
            case Key.ShiftRight:
                return InputKey.ShiftRight;
            case Key.ControlLeft:
                return InputKey.ControlLeft;
            case Key.ControlRight:
                return InputKey.ControlRight;
            case Key.AltLeft:
                return InputKey.AltLeft;
            case Key.AltRight:
                return InputKey.AltRight;
            case Key.F1:
                return InputKey.F1;
            case Key.F2:
                return InputKey.F2;
            case Key.F3:
                return InputKey.F3;
            case Key.F4:
                return InputKey.F4;
            case Key.F5:
                return InputKey.F5;
            case Key.F6:
                return InputKey.F6;
            case Key.F7:
                return InputKey.F7;
            case Key.F8:
                return InputKey.F8;
            case Key.F9:
                return InputKey.F9;
            case Key.F10:
                return InputKey.F10;
            case Key.F11:
                return InputKey.F11;
            case Key.F12:
                return InputKey.F12;
            case Key.Up:
                return InputKey.Up;
            case Key.Down:
                return InputKey.Down;
            case Key.Left:
                return InputKey.Left;
            case Key.Right:
                return InputKey.Right;
            case Key.Enter:
                return InputKey.Enter;
            case Key.Escape:
                return InputKey.Escape;
            case Key.Space:
                return InputKey.Space;
            case Key.Tab:
                return InputKey.Tab;
            case Key.BackSpace:
                return InputKey.Backspace;
            case Key.Insert:
                return InputKey.Insert;
            case Key.Delete:
                return InputKey.Delete;
            case Key.PageUp:
                return InputKey.PageUp;
            case Key.PageDown:
                return InputKey.PageDown;
            case Key.Home:
                return InputKey.Home;
            case Key.End:
                return InputKey.End;
            case Key.CapsLock:
                return InputKey.CapsLock;
            case Key.ScrollLock:
                return InputKey.ScrollLock;
            case Key.PrintScreen:
                return InputKey.PrintScreen;
            case Key.Pause:
                return InputKey.Pause;
            case Key.NumLock:
                return InputKey.Numlock;
            case Key.Keypad0:
                return InputKey.Zero;
            case Key.Keypad1:
                return InputKey.One;
            case Key.Keypad2:
                return InputKey.Two;
            case Key.Keypad3:
                return InputKey.Three;
            case Key.Keypad4:
                return InputKey.Four;
            case Key.Keypad5:
                return InputKey.Five;
            case Key.Keypad6:
                return InputKey.Six;
            case Key.Keypad7:
                return InputKey.Seven;
            case Key.Keypad8:
                return InputKey.Eight;
            case Key.Keypad9:
                return InputKey.Nine;
            case Key.KeypadDivide:
                return InputKey.Slash;
            case Key.KeypadMultiply:
                return InputKey.Asterisk;
            case Key.KeypadSubtract:
                return InputKey.Minus;
            case Key.KeypadAdd:
                return InputKey.Plus;
            case Key.KeypadDecimal:
                return InputKey.Period;
            case Key.KeypadEnter:
                return InputKey.Enter;
            case Key.A:
                return InputKey.A;
            case Key.B:
                return InputKey.B;
            case Key.C:
                return InputKey.C;
            case Key.D:
                return InputKey.D;
            case Key.E:
                return InputKey.E;
            case Key.F:
                return InputKey.F;
            case Key.G:
                return InputKey.G;
            case Key.H:
                return InputKey.H;
            case Key.I:
                return InputKey.I;
            case Key.J:
                return InputKey.J;
            case Key.K:
                return InputKey.K;
            case Key.L:
                return InputKey.L;
            case Key.M:
                return InputKey.M;
            case Key.N:
                return InputKey.N;
            case Key.O:
                return InputKey.O;
            case Key.P:
                return InputKey.P;
            case Key.Q:
                return InputKey.Q;
            case Key.R:
                return InputKey.R;
            case Key.S:
                return InputKey.S;
            case Key.T:
                return InputKey.T;
            case Key.U:
                return InputKey.U;
            case Key.V:
                return InputKey.V;
            case Key.W:
                return InputKey.W;
            case Key.X:
                return InputKey.X;
            case Key.Y:
                return InputKey.Y;
            case Key.Z:
                return InputKey.Z;
            case Key.Number0:
                return InputKey.Zero;
            case Key.Number1:
                return InputKey.One;
            case Key.Number2:
                return InputKey.Two;
            case Key.Number3:
                return InputKey.Three;
            case Key.Number4:
                return InputKey.Four;
            case Key.Number5:
                return InputKey.Five;
            case Key.Number6:
                return InputKey.Six;
            case Key.Number7:
                return InputKey.Seven;
            case Key.Number8:
                return InputKey.Eight;
            case Key.Number9:
                return InputKey.Nine;
            case Key.Tilde:
                return InputKey.Backtick;
            case Key.Minus:
                return InputKey.Minus;
            case Key.Plus:
                return InputKey.Plus;
            case Key.BracketLeft:
                return InputKey.BracketLeft;
            case Key.BracketRight:
                return InputKey.BracketRight;
            case Key.Semicolon:
                return InputKey.Semicolon;
            case Key.Quote:
                return InputKey.Apostrophe;
            case Key.Comma:
                return InputKey.Comma;
            case Key.Period:
                return InputKey.Period;
            case Key.Slash:
                return InputKey.Slash;
            case Key.BackSlash:
                return InputKey.Backslash;
            case Key.NonUSBackSlash:
                return InputKey.Backslash;
            default:
                return InputKey.Unknown;
            }
        }

        public void HandleMouseMovement(Vec2I deltaPixels /* MouseMoveEventArgs e */)
        {
            InputEventArgs inputEvent = new InputEventArgs();
            inputEvent.MouseInput.Delta = new Vec2I(-deltaPixels.X, -deltaPixels.Y);
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

            EmitEvent(inputEvent);
        }

        public void HandleKeyUp(KeyboardKeyEventArgs e)
        {
            InputEventArgs inputEvent = new InputEventArgs();

            InputKey inputKey = ToInputKey(e.Key);
            if (inputKey != InputKey.Unknown)
                inputEvent.InputUp.Add(inputKey);

            EmitEvent(inputEvent);
        }

        public override void PollInput()
        {
            // Due to how the implementation is set up, we don't need to poll
            // because the listeners on the GameWindow do all of that for us.
        }
    }
}
