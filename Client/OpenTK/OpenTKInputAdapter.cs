using Helion.Input;
using Helion.Util.Configs;
using OpenTK;
using OpenTK.Input;

namespace Helion.Client
{
    public class OpenTKInputAdapter
    {
        private readonly Config m_config;
        private InputEvent inputEvent = new();

        public OpenTKInputAdapter(Config config)
        {
            m_config = config;
        }

        private static InputKey ToInputKey(Key key)
        {
            return key switch
            {
                Key.ShiftLeft => InputKey.ShiftLeft,
                Key.ShiftRight => InputKey.ShiftRight,
                Key.ControlLeft => InputKey.ControlLeft,
                Key.ControlRight => InputKey.ControlRight,
                Key.AltLeft => InputKey.AltLeft,
                Key.AltRight => InputKey.AltRight,
                Key.F1 => InputKey.F1,
                Key.F2 => InputKey.F2,
                Key.F3 => InputKey.F3,
                Key.F4 => InputKey.F4,
                Key.F5 => InputKey.F5,
                Key.F6 => InputKey.F6,
                Key.F7 => InputKey.F7,
                Key.F8 => InputKey.F8,
                Key.F9 => InputKey.F9,
                Key.F10 => InputKey.F10,
                Key.F11 => InputKey.F11,
                Key.F12 => InputKey.F12,
                Key.Up => InputKey.Up,
                Key.Down => InputKey.Down,
                Key.Left => InputKey.Left,
                Key.Right => InputKey.Right,
                Key.Enter => InputKey.Enter,
                Key.Escape => InputKey.Escape,
                Key.Space => InputKey.Space,
                Key.Tab => InputKey.Tab,
                Key.BackSpace => InputKey.Backspace,
                Key.Insert => InputKey.Insert,
                Key.Delete => InputKey.Delete,
                Key.PageUp => InputKey.PageUp,
                Key.PageDown => InputKey.PageDown,
                Key.Home => InputKey.Home,
                Key.End => InputKey.End,
                Key.CapsLock => InputKey.CapsLock,
                Key.ScrollLock => InputKey.ScrollLock,
                Key.PrintScreen => InputKey.PrintScreen,
                Key.Pause => InputKey.Pause,
                Key.NumLock => InputKey.Numlock,
                Key.Keypad0 => InputKey.Zero,
                Key.Keypad1 => InputKey.One,
                Key.Keypad2 => InputKey.Two,
                Key.Keypad3 => InputKey.Three,
                Key.Keypad4 => InputKey.Four,
                Key.Keypad5 => InputKey.Five,
                Key.Keypad6 => InputKey.Six,
                Key.Keypad7 => InputKey.Seven,
                Key.Keypad8 => InputKey.Eight,
                Key.Keypad9 => InputKey.Nine,
                Key.KeypadDivide => InputKey.Slash,
                Key.KeypadMultiply => InputKey.Asterisk,
                Key.KeypadSubtract => InputKey.Minus,
                Key.KeypadAdd => InputKey.Plus,
                Key.KeypadDecimal => InputKey.Period,
                Key.KeypadEnter => InputKey.Enter,
                Key.A => InputKey.A,
                Key.B => InputKey.B,
                Key.C => InputKey.C,
                Key.D => InputKey.D,
                Key.E => InputKey.E,
                Key.F => InputKey.F,
                Key.G => InputKey.G,
                Key.H => InputKey.H,
                Key.I => InputKey.I,
                Key.J => InputKey.J,
                Key.K => InputKey.K,
                Key.L => InputKey.L,
                Key.M => InputKey.M,
                Key.N => InputKey.N,
                Key.O => InputKey.O,
                Key.P => InputKey.P,
                Key.Q => InputKey.Q,
                Key.R => InputKey.R,
                Key.S => InputKey.S,
                Key.T => InputKey.T,
                Key.U => InputKey.U,
                Key.V => InputKey.V,
                Key.W => InputKey.W,
                Key.X => InputKey.X,
                Key.Y => InputKey.Y,
                Key.Z => InputKey.Z,
                Key.Number0 => InputKey.Zero,
                Key.Number1 => InputKey.One,
                Key.Number2 => InputKey.Two,
                Key.Number3 => InputKey.Three,
                Key.Number4 => InputKey.Four,
                Key.Number5 => InputKey.Five,
                Key.Number6 => InputKey.Six,
                Key.Number7 => InputKey.Seven,
                Key.Number8 => InputKey.Eight,
                Key.Number9 => InputKey.Nine,
                Key.Tilde => InputKey.Backtick,
                Key.Minus => InputKey.Minus,
                Key.Plus => InputKey.Plus,
                Key.BracketLeft => InputKey.BracketLeft,
                Key.BracketRight => InputKey.BracketRight,
                Key.Semicolon => InputKey.Semicolon,
                Key.Quote => InputKey.Apostrophe,
                Key.Comma => InputKey.Comma,
                Key.Period => InputKey.Period,
                Key.Slash => InputKey.Slash,
                Key.BackSlash => InputKey.Backslash,
                Key.NonUSBackSlash => InputKey.Backslash,
                _ => InputKey.Unknown
            };
        }

        public void HandleMouseMovement(int deltaX, int deltaY)
        {
            inputEvent.MouseInput.Delta.Add(-deltaX, -deltaY);
        }

        public void HandleMouseWheelInput(MouseWheelEventArgs e)
        {
            // Note: We can also access e.DeltaPrecise to support better
            // scrolling in high quality mice.
            inputEvent.MouseInput.ScrollDelta += e.Delta;
        }

        public void HandleKeyPress(KeyPressEventArgs e)
        {
            inputEvent.CharactersTyped.Add(e.KeyChar);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            InputKey inputKey = ToInputKey(e.Key);
            if (inputKey == InputKey.Unknown)
                return;

            InputCommand? command = m_config.InputKeyToCommand(inputKey);
            if (command != null)
                inputEvent.InputDown.Add(inputKey);
        }

        public void HandleKeyUp(KeyboardKeyEventArgs e)
        {
            InputKey inputKey = ToInputKey(e.Key);
            if (inputKey != InputKey.Unknown)
                inputEvent.InputDown.Remove(inputKey);
        }

        public void HandleMouseDown(MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
                    inputEvent.InputDown.Add(InputKey.MouseLeft);
                    break;
                case MouseButton.Middle:
                    inputEvent.InputDown.Add(InputKey.MouseMiddle);
                    break;
                case MouseButton.Right:
                    inputEvent.InputDown.Add(InputKey.MouseRight);
                    break;
                case MouseButton.Button1:
                    inputEvent.InputDown.Add(InputKey.MouseCustom1);
                    break;
                case MouseButton.Button2:
                    inputEvent.InputDown.Add(InputKey.MouseCustom2);
                    break;
                case MouseButton.Button3:
                    inputEvent.InputDown.Add(InputKey.MouseCustom3);
                    break;
                case MouseButton.Button4:
                    inputEvent.InputDown.Add(InputKey.MouseCustom4);
                    break;
                case MouseButton.Button5:
                    inputEvent.InputDown.Add(InputKey.MouseCustom5);
                    break;
                case MouseButton.Button6:
                    inputEvent.InputDown.Add(InputKey.MouseCustom6);
                    break;
                case MouseButton.Button7:
                    inputEvent.InputDown.Add(InputKey.MouseCustom7);
                    break;
                case MouseButton.Button8:
                    inputEvent.InputDown.Add(InputKey.MouseCustom8);
                    break;
                case MouseButton.Button9:
                    inputEvent.InputDown.Add(InputKey.MouseCustom9);
                    break;
            }
        }

        public void HandleMouseUp(MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
                    inputEvent.InputDown.Remove(InputKey.MouseLeft);
                    break;
                case MouseButton.Middle:
                    inputEvent.InputDown.Remove(InputKey.MouseMiddle);
                    break;
                case MouseButton.Right:
                    inputEvent.InputDown.Remove(InputKey.MouseRight);
                    break;
                case MouseButton.Button1:
                    inputEvent.InputDown.Remove(InputKey.MouseCustom1);
                    break;
                case MouseButton.Button2:
                    inputEvent.InputDown.Remove(InputKey.MouseCustom2);
                    break;
                case MouseButton.Button3:
                    inputEvent.InputDown.Remove(InputKey.MouseCustom3);
                    break;
                case MouseButton.Button4:
                    inputEvent.InputDown.Remove(InputKey.MouseCustom4);
                    break;
                case MouseButton.Button5:
                    inputEvent.InputDown.Remove(InputKey.MouseCustom5);
                    break;
                case MouseButton.Button6:
                    inputEvent.InputDown.Remove(InputKey.MouseCustom6);
                    break;
                case MouseButton.Button7:
                    inputEvent.InputDown.Remove(InputKey.MouseCustom7);
                    break;
                case MouseButton.Button8:
                    inputEvent.InputDown.Remove(InputKey.MouseCustom8);
                    break;
                case MouseButton.Button9:
                    inputEvent.InputDown.Remove(InputKey.MouseCustom9);
                    break;
            }
        }

        public InputEvent PollInput()
        {
            inputEvent = new InputEvent(inputEvent);
            return inputEvent;
        }
    }
}
