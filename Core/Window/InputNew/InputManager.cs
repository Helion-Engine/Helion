using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Util.Container;

namespace Helion.Window.InputNew
{
    /// <summary>
    /// A simple implementation of an input manager.
    /// </summary>
    public class InputManager : IInputManager
    {
        public Vec2I MouseMove { get; private set; } = (0, 0);
        private readonly ConsumableInput m_consumableInput;
        private readonly HashSet<Key> m_inputDown = new();
        private readonly HashSet<Key> m_inputPrevDown = new();
        private readonly DynamicArray<char> m_typedCharacters = new();
        private double m_mouseScroll;

        public int Scroll => (int)m_mouseScroll;
        public ReadOnlySpan<char> TypedCharacters => new(m_typedCharacters.Data, 0, m_typedCharacters.Length);

        public InputManager()
        {
            m_consumableInput = new ConsumableInput(this);
        }

        public void SetKeyDown(Key key, bool shift, bool repeat)
        {
            m_inputDown.Add(key);
            HandleTypedCharacter(key, shift, repeat);
        }
        
        public void SetKeyUp(Key key)
        {
            m_inputDown.Remove(key);
        }

        public void AddMouseMovement(Vec2I movement)
        {
            MouseMove += movement;
        }
        
        public void AddMouseScroll(double amount)
        {
            m_mouseScroll += amount;
        }
        
        public bool IsKeyDown(Key key) => m_inputDown.Contains(key);
        public bool IsKeyPrevDown(Key key) => m_inputPrevDown.Contains(key);
        public bool IsKeyHeldDown(Key key) => IsKeyDown(key) && IsKeyPrevDown(key);
        public bool IsKeyUp(Key key) => !m_inputDown.Contains(key);
        public bool IsKeyPrevUp(Key key) => !m_inputPrevDown.Contains(key);
        public bool IsKeyPressed(Key key) => IsKeyDown(key) && !IsKeyPrevDown(key);
        public bool IsKeyReleased(Key key) => !IsKeyDown(key) && IsKeyPrevDown(key);

        private void ClearStates()
        {
            MouseMove = (0, 0);
            m_mouseScroll = 0;
            m_typedCharacters.Clear();
            m_inputPrevDown.Clear();
            foreach (Key key in m_inputDown)
                m_inputPrevDown.Add(key);
        }
        
        public ConsumableInput Poll()
        {
            m_consumableInput.Reset();
            ClearStates();
            return m_consumableInput;
        }

        private void HandleTypedCharacter(Key key, bool shift, bool repeat)
        {
            switch (key)
            {
            case Key.Zero:
                AddLetter(shift ? ')' : '0');
                break;
            case Key.One:
                AddLetter(shift ? '!' : '1');
                break;
            case Key.Two:
                AddLetter(shift ? '@' : '2');
                break;
            case Key.Three:
                AddLetter(shift ? '#' : '3');
                break;
            case Key.Four:
                AddLetter(shift ? '$' : '4');
                break;
            case Key.Five:
                AddLetter(shift ? '%' : '5');
                break;
            case Key.Six:
                AddLetter(shift ? '^' : '6');
                break;
            case Key.Seven:
                AddLetter(shift ? '&' : '7');
                break;
            case Key.Eight:
                AddLetter(shift ? '*' : '8');
                break;
            case Key.Nine:
                AddLetter(shift ? '(' : '9');
                break;
            case Key.A:
                AddLetter(shift ? 'A' : 'a');
                break;
            case Key.B:
                AddLetter(shift ? 'B' : 'b');
                break;
            case Key.C:
                AddLetter(shift ? 'C' : 'c');
                break;
            case Key.D:
                AddLetter(shift ? 'D' : 'd');
                break;
            case Key.E:
                AddLetter(shift ? 'E' : 'e');
                break;
            case Key.F:
                AddLetter(shift ? 'F' : 'f');
                break;
            case Key.G:
                AddLetter(shift ? 'G' : 'g');
                break;
            case Key.H:
                AddLetter(shift ? 'H' : 'h');
                break;
            case Key.I:
                AddLetter(shift ? 'I' : 'i');
                break;
            case Key.J:
                AddLetter(shift ? 'J' : 'j');
                break;
            case Key.K:
                AddLetter(shift ? 'K' : 'k');
                break;
            case Key.L:
                AddLetter(shift ? 'L' : 'l');
                break;
            case Key.M:
                AddLetter(shift ? 'M' : 'm');
                break;
            case Key.N:
                AddLetter(shift ? 'N' : 'n');
                break;
            case Key.O:
                AddLetter(shift ? 'O' : 'o');
                break;
            case Key.P:
                AddLetter(shift ? 'P' : 'p');
                break;
            case Key.Q:
                AddLetter(shift ? 'Q' : 'q');
                break;
            case Key.R:
                AddLetter(shift ? 'R' : 'r');
                break;
            case Key.S:
                AddLetter(shift ? 'S' : 's');
                break;
            case Key.T:
                AddLetter(shift ? 'T' : 't');
                break;
            case Key.U:
                AddLetter(shift ? 'U' : 'u');
                break;
            case Key.V:
                AddLetter(shift ? 'V' : 'v');
                break;
            case Key.W:
                AddLetter(shift ? 'W' : 'w');
                break;
            case Key.X:
                AddLetter(shift ? 'X' : 'x');
                break;
            case Key.Y:
                AddLetter(shift ? 'Y' : 'y');
                break;
            case Key.Z:
                AddLetter(shift ? 'Z' : 'z');
                break;
            case Key.Backtick:
                AddLetter('`');
                break;
            case Key.Tilde:
                AddLetter('~');
                break;
            case Key.Exclamation:
                AddLetter('!');
                break;
            case Key.At:
                AddLetter('@');
                break;
            case Key.Hash:
                AddLetter('#');
                break;
            case Key.Dollar:
                AddLetter('$');
                break;
            case Key.Percent:
                AddLetter('%');
                break;
            case Key.Caret:
                AddLetter('^');
                break;
            case Key.Ampersand:
                AddLetter('&');
                break;
            case Key.Asterisk:
                AddLetter('*');
                break;
            case Key.ParenthesisLeft:
                AddLetter('(');
                break;
            case Key.ParenthesisRight:
                AddLetter(')');
                break;
            case Key.Minus:
                AddLetter('-');
                break;
            case Key.Underscore:
                AddLetter('_');
                break;
            case Key.Equals:
                AddLetter('=');
                break;
            case Key.Plus:
                AddLetter('+');
                break;
            case Key.BracketLeft:
                AddLetter('[');
                break;
            case Key.BracketRight:
                AddLetter(']');
                break;
            case Key.CurlyLeft:
                AddLetter('{');
                break;
            case Key.CurlyRight:
                AddLetter('}');
                break;
            case Key.Backslash:
                AddLetter('\\');
                break;
            case Key.Pipe:
                AddLetter('|');
                break;
            case Key.Semicolon:
                AddLetter(';');
                break;
            case Key.Colon:
                AddLetter(':');
                break;
            case Key.Apostrophe:
                AddLetter('\'');
                break;
            case Key.Quotation:
                AddLetter('"');
                break;
            case Key.Comma:
                AddLetter(',');
                break;
            case Key.Period:
                AddLetter('.');
                break;
            case Key.DiamondLeft:
                AddLetter('<');
                break;
            case Key.DiamondRight:
                AddLetter('>');
                break;
            case Key.Slash:
                AddLetter('/');
                break;
            case Key.Question:
                AddLetter('?');
                break;
            case Key.Space:
                AddLetter(' ');
                break;
            }
            
            void AddLetter(char c)
            {
                if (repeat || !m_typedCharacters.Contains(c))
                    m_typedCharacters.Add(c);
            }
        }
    }
}
