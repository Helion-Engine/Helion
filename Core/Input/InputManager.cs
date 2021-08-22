using System.Collections.Generic;
using System.Text;
using Helion.Geometry.Vectors;

namespace Helion.Input
{
    /// <summary>
    /// Manages all of the input. All methods here are based on the raw state,
    /// so they can be polled without worrying about values being cleared (in
    /// contrast to <see cref="InputEvent"/> where they are).
    /// </summary>
    public class InputManager
    {
        private readonly StringBuilder m_typedCharacters = new();
        private HashSet<Key> m_inputDown = new();
        private HashSet<Key> m_inputPrevDown = new();
        private HashSet<Key> m_inputPressed = new();
        private Vec2D m_mouseMove = Vec2D.Zero;
        private double m_mouseScroll;

        /// <summary>
        /// The mouse scrolling since the last update.
        /// </summary>
        public int MouseScroll => (int)m_mouseScroll;

        /// <summary>
        /// The mouse movement since the last update.
        /// </summary>
        public Vec2I MouseMove => m_mouseMove.Int;

        /// <summary>
        /// The characters typed on the keyboard since the last update.
        /// </summary>
        public string TypedCharacters => m_typedCharacters.ToString();

        /// <summary>
        /// Gets a read-only view of all the keys that are down.
        /// </summary>
        /// <remarks>
        /// This is useful for checking what keys are down, which helps for
        /// detecting input when the user is asked to press 'any key' for the
        /// setting of new key binds.
        /// </remarks>
        public IEnumerable<Key> KeysDown => m_inputDown;

        /// <summary>
        /// Checks if a key is down.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if so, false if not.</returns>
        public bool IsKeyDown(Key key) => m_inputDown.Contains(key);

        /// <summary>
        /// Checks if a key is not down.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if so, false if not.</returns>
        public bool IsKeyUp(Key key) => !IsKeyDown(key);

        /// <summary>
        /// Checks if a key was just pressed.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if so, false if not.</returns>
        public bool IsKeyPressed(Key key) => m_inputPressed.Contains(key);

        /// <summary>
        /// Checks if a key was just released.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if so, false if not.</returns>
        public bool IsKeyReleased(Key key) => !IsKeyDown(key) && m_inputPrevDown.Contains(key);

        private void ClearStates()
        {
            m_typedCharacters.Clear();
            m_inputPressed.Clear();
            m_inputPrevDown = m_inputDown;
            m_inputDown = new HashSet<Key>(m_inputDown);
            m_mouseMove = Vec2D.Zero;
            m_mouseScroll = 0;
        }

        private bool JustPressed(Key key) => m_inputDown.Contains(key) && !m_inputPrevDown.Contains(key);
        
        private void AddLetter(Key key, char c, bool repeat)
        {
            // NOTE: We could avoid allocations from ToString() (since we can't
            // reach inside StringBuilder to check it's contents), but I am not
            // sure if this is called so infrequently that we do not care.
            if (!m_typedCharacters.ToString().Contains(c) || repeat)
                m_typedCharacters.Append(c);
        }

        private void HandleTypedCharacter(Key key, bool shift, bool repeat)
        {
            switch (key)
            {
            case Key.Zero:
                AddLetter(key, shift ? ')' : '0', repeat);
                break;
            case Key.One:
                AddLetter(key, shift ? '!' : '1', repeat);
                break;
            case Key.Two:
                AddLetter(key, shift ? '@' : '2', repeat);
                break;
            case Key.Three:
                AddLetter(key, shift ? '#' : '3', repeat);
                break;
            case Key.Four:
                AddLetter(key, shift ? '$' : '4', repeat);
                break;
            case Key.Five:
                AddLetter(key, shift ? '%' : '5', repeat);
                break;
            case Key.Six:
                AddLetter(key, shift ? '^' : '6', repeat);
                break;
            case Key.Seven:
                AddLetter(key, shift ? '&' : '7', repeat);
                break;
            case Key.Eight:
                AddLetter(key, shift ? '*' : '8', repeat);
                break;
            case Key.Nine:
                AddLetter(key, shift ? '(' : '9', repeat);
                break;
            case Key.A:
                AddLetter(key, shift ? 'A' : 'a', repeat);
                break;
            case Key.B:
                AddLetter(key, shift ? 'B' : 'b', repeat);
                break;
            case Key.C:
                AddLetter(key, shift ? 'C' : 'c', repeat);
                break;
            case Key.D:
                AddLetter(key, shift ? 'D' : 'd', repeat);
                break;
            case Key.E:
                AddLetter(key, shift ? 'E' : 'e', repeat);
                break;
            case Key.F:
                AddLetter(key, shift ? 'F' : 'f', repeat);
                break;
            case Key.G:
                AddLetter(key, shift ? 'G' : 'g', repeat);
                break;
            case Key.H:
                AddLetter(key, shift ? 'H' : 'h', repeat);
                break;
            case Key.I:
                AddLetter(key, shift ? 'I' : 'i', repeat);
                break;
            case Key.J:
                AddLetter(key, shift ? 'J' : 'j', repeat);
                break;
            case Key.K:
                AddLetter(key, shift ? 'K' : 'k', repeat);
                break;
            case Key.L:
                AddLetter(key, shift ? 'L' : 'l', repeat);
                break;
            case Key.M:
                AddLetter(key, shift ? 'M' : 'm', repeat);
                break;
            case Key.N:
                AddLetter(key, shift ? 'N' : 'n', repeat);
                break;
            case Key.O:
                AddLetter(key, shift ? 'O' : 'o', repeat);
                break;
            case Key.P:
                AddLetter(key, shift ? 'P' : 'p', repeat);
                break;
            case Key.Q:
                AddLetter(key, shift ? 'Q' : 'q', repeat);
                break;
            case Key.R:
                AddLetter(key, shift ? 'R' : 'r', repeat);
                break;
            case Key.S:
                AddLetter(key, shift ? 'S' : 's', repeat);
                break;
            case Key.T:
                AddLetter(key, shift ? 'T' : 't', repeat);
                break;
            case Key.U:
                AddLetter(key, shift ? 'U' : 'u', repeat);
                break;
            case Key.V:
                AddLetter(key, shift ? 'V' : 'v', repeat);
                break;
            case Key.W:
                AddLetter(key, shift ? 'W' : 'w', repeat);
                break;
            case Key.X:
                AddLetter(key, shift ? 'X' : 'x', repeat);
                break;
            case Key.Y:
                AddLetter(key, shift ? 'Y' : 'y', repeat);
                break;
            case Key.Z:
                AddLetter(key, shift ? 'Z' : 'z', repeat);
                break;
            case Key.Backtick:
                AddLetter(key, '`', repeat);
                break;
            case Key.Tilde:
                AddLetter(key, '~', repeat);
                break;
            case Key.Exclamation:
                AddLetter(key, '!', repeat);
                break;
            case Key.At:
                AddLetter(key, '@', repeat);
                break;
            case Key.Hash:
                AddLetter(key, '#', repeat);
                break;
            case Key.Dollar:
                AddLetter(key, '$', repeat);
                break;
            case Key.Percent:
                AddLetter(key, '%', repeat);
                break;
            case Key.Caret:
                AddLetter(key, '^', repeat);
                break;
            case Key.Ampersand:
                AddLetter(key, '&', repeat);
                break;
            case Key.Asterisk:
                AddLetter(key, '*', repeat);
                break;
            case Key.ParenthesisLeft:
                AddLetter(key, '(', repeat);
                break;
            case Key.ParenthesisRight:
                AddLetter(key, ')', repeat);
                break;
            case Key.Minus:
                AddLetter(key, '-', repeat);
                break;
            case Key.Underscore:
                AddLetter(key, '_', repeat);
                break;
            case Key.Equals:
                AddLetter(key, '=', repeat);
                break;
            case Key.Plus:
                AddLetter(key, '+', repeat);
                break;
            case Key.BracketLeft:
                AddLetter(key, '[', repeat);
                break;
            case Key.BracketRight:
                AddLetter(key, ']', repeat);
                break;
            case Key.CurlyLeft:
                AddLetter(key, '{', repeat);
                break;
            case Key.CurlyRight:
                AddLetter(key, '}', repeat);
                break;
            case Key.Backslash:
                AddLetter(key, '\\', repeat);
                break;
            case Key.Pipe:
                AddLetter(key, '|', repeat);
                break;
            case Key.Semicolon:
                AddLetter(key, ';', repeat);
                break;
            case Key.Colon:
                AddLetter(key, ':', repeat);
                break;
            case Key.Apostrophe:
                AddLetter(key, '\'', repeat);
                break;
            case Key.Quotation:
                AddLetter(key, '"', repeat);
                break;
            case Key.Comma:
                AddLetter(key, ',', repeat);
                break;
            case Key.Period:
                AddLetter(key, '.', repeat);
                break;
            case Key.DiamondLeft:
                AddLetter(key, '<', repeat);
                break;
            case Key.DiamondRight:
                AddLetter(key, '>', repeat);
                break;
            case Key.Slash:
                AddLetter(key, '/', repeat);
                break;
            case Key.Question:
                AddLetter(key, '?', repeat);
                break;
            case Key.Space:
                AddLetter(key, ' ', repeat);
                break;
            }
        }

        /// <summary>
        /// Sets the key to be released.
        /// </summary>
        /// <param name="key">The key to be released.</param>
        public void SetKeyUp(Key key)
        {
            m_inputDown.Remove(key);
        }

        /// <summary>
        /// Sets the key to a down state.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="shift">If shift was down.</param>
        /// <param name="repeat">If this is a repetition event.</param>
        public void SetKeyDown(Key key, bool shift = false, bool repeat = false)
        {
            m_inputDown.Add(key);
            HandleTypedCharacter(key, shift, repeat);

            if (JustPressed(key) || repeat)
                m_inputPressed.Add(key);
        }

        /// <summary>
        /// Adds mouse movement.
        /// </summary>
        /// <param name="deltaX">The X delta.</param>
        /// <param name="deltaY">The Y delta.</param>
        public void AddMouseMovement(double deltaX, double deltaY)
        {
            m_mouseMove += new Vec2D(deltaX, deltaY);
        }

        /// <summary>
        /// Adds scroll information.
        /// </summary>
        /// <param name="vertical">The vertical scroll amount.</param>
        public void AddScroll(double vertical)
        {
            m_mouseScroll += vertical;
        }

        /// <summary>
        /// Polls the input and advances internal states.
        /// </summary>
        /// <returns>The event to be consumed.</returns>
        public InputEvent PollInput()
        {
            InputEvent inputEvent = new(this, m_inputDown, m_inputPressed, m_inputPrevDown);
            ClearStates();
            return inputEvent;
        }
    }
}
