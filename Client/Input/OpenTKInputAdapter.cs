using Helion.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Helion.Client.Input
{
    /// <summary>
    /// A helper class for converting OpenTK input to our input.
    /// </summary>
    public static class OpenTKInputAdapter
    {
        /// <summary>
        /// Converts a key into a known key type.
        /// </summary>
        /// <param name="key">The OpenTK key to convert.</param>
        /// <returns>The converted key.</returns>
        public static Key ToKey(Keys key)
        {
            return key switch
            {
                Keys.LeftShift => Key.ShiftLeft,
                Keys.RightShift => Key.ShiftRight,
                Keys.LeftControl => Key.ControlLeft,
                Keys.RightControl => Key.ControlRight,
                Keys.LeftAlt => Key.AltLeft,
                Keys.RightAlt => Key.AltRight,
                Keys.F1 => Key.F1,
                Keys.F2 => Key.F2,
                Keys.F3 => Key.F3,
                Keys.F4 => Key.F4,
                Keys.F5 => Key.F5,
                Keys.F6 => Key.F6,
                Keys.F7 => Key.F7,
                Keys.F8 => Key.F8,
                Keys.F9 => Key.F9,
                Keys.F10 => Key.F10,
                Keys.F11 => Key.F11,
                Keys.F12 => Key.F12,
                Keys.Up => Key.Up,
                Keys.Down => Key.Down,
                Keys.Left => Key.Left,
                Keys.Right => Key.Right,
                Keys.Enter => Key.Enter,
                Keys.Escape => Key.Escape,
                Keys.Space => Key.Space,
                Keys.Tab => Key.Tab,
                Keys.Backspace => Key.Backspace,
                Keys.Insert => Key.Insert,
                Keys.Delete => Key.Delete,
                Keys.PageUp => Key.PageUp,
                Keys.PageDown => Key.PageDown,
                Keys.Home => Key.Home,
                Keys.End => Key.End,
                Keys.CapsLock => Key.CapsLock,
                Keys.ScrollLock => Key.ScrollLock,
                Keys.PrintScreen => Key.PrintScreen,
                Keys.Pause => Key.Pause,
                Keys.NumLock => Key.Numlock,
                Keys.KeyPad0 => Key.Zero,
                Keys.KeyPad1 => Key.One,
                Keys.KeyPad2 => Key.Two,
                Keys.KeyPad3 => Key.Three,
                Keys.KeyPad4 => Key.Four,
                Keys.KeyPad5 => Key.Five,
                Keys.KeyPad6 => Key.Six,
                Keys.KeyPad7 => Key.Seven,
                Keys.KeyPad8 => Key.Eight,
                Keys.KeyPad9 => Key.Nine,
                Keys.KeyPadDivide => Key.Slash,
                Keys.KeyPadMultiply => Key.Asterisk,
                Keys.KeyPadSubtract => Key.Minus,
                Keys.KeyPadAdd => Key.Plus,
                Keys.KeyPadDecimal => Key.Period,
                Keys.KeyPadEnter => Key.Enter,
                Keys.Equal => Key.Equals,
                Keys.A => Key.A,
                Keys.B => Key.B,
                Keys.C => Key.C,
                Keys.D => Key.D,
                Keys.E => Key.E,
                Keys.F => Key.F,
                Keys.G => Key.G,
                Keys.H => Key.H,
                Keys.I => Key.I,
                Keys.J => Key.J,
                Keys.K => Key.K,
                Keys.L => Key.L,
                Keys.M => Key.M,
                Keys.N => Key.N,
                Keys.O => Key.O,
                Keys.P => Key.P,
                Keys.Q => Key.Q,
                Keys.R => Key.R,
                Keys.S => Key.S,
                Keys.T => Key.T,
                Keys.U => Key.U,
                Keys.V => Key.V,
                Keys.W => Key.W,
                Keys.X => Key.X,
                Keys.Y => Key.Y,
                Keys.Z => Key.Z,
                Keys.D0 => Key.Zero,
                Keys.D1 => Key.One,
                Keys.D2 => Key.Two,
                Keys.D3 => Key.Three,
                Keys.D4 => Key.Four,
                Keys.D5 => Key.Five,
                Keys.D6 => Key.Six,
                Keys.D7 => Key.Seven,
                Keys.D8 => Key.Eight,
                Keys.D9 => Key.Nine,
                Keys.GraveAccent => Key.Backtick,
                Keys.Minus => Key.Minus,
                Keys.LeftBracket => Key.BracketLeft,
                Keys.RightBracket => Key.BracketRight,
                Keys.Semicolon => Key.Semicolon,
                Keys.Comma => Key.Comma,
                Keys.Period => Key.Period,
                Keys.Slash => Key.Slash,
                Keys.Backslash => Key.Backslash,
                _ => Key.Unknown
            };
        }

        /// <summary>
        /// Converts a mouse 'key' into a known key type.
        /// </summary>
        /// <param name="mouseButton">The OpenTK button to convert.</param>
        /// <returns>The converted mouse button.</returns>
        public static Key ToMouseKey(MouseButton mouseButton)
        {
            return mouseButton switch
            {
                // Note that these three map onto mouse custom key 1 through 3.
                MouseButton.Left => Key.MouseLeft,
                MouseButton.Middle => Key.MouseMiddle,
                MouseButton.Right => Key.MouseRight,
                MouseButton.Button4 => Key.MouseCustom4,
                MouseButton.Button5 => Key.MouseCustom5,
                MouseButton.Button6 => Key.MouseCustom6,
                MouseButton.Button7 => Key.MouseCustom7,
                MouseButton.Button8 => Key.MouseCustom8,
                _ => Key.Unknown
            };
        }
    }
}
