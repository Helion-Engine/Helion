using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;

namespace Helion.Window.InputNew
{
    /// <summary>
    /// A basic implementation of consumable input.
    /// </summary>
    public class ConsumableInput : IConsumableInput
    {
        private readonly InputManager m_inputManager;
        private readonly HashSet<Key> m_inputConsumed = new();
        private bool m_typedCharsConsumed;
        private Vec2I m_mouseMove = (0, 0);
        private int m_mouseScroll;

        public IInputManager InputManager => m_inputManager;

        public ConsumableInput(InputManager inputManager)
        {
            m_inputManager = inputManager;
        }

        public bool ConsumeKeyDown(Key key)
        {
            if (m_inputConsumed.Contains(key))
                return false;
            
            m_inputConsumed.Add(key);
            return InputManager.IsKeyDown(key);
        }
        
        public bool ConsumeKeyPrevDown(Key key)
        {
            if (m_inputConsumed.Contains(key))
                return false;
            
            m_inputConsumed.Add(key);
            return InputManager.IsKeyPrevDown(key);
        }
        
        public bool ConsumeKeyHeldDown(Key key)
        {
            if (m_inputConsumed.Contains(key))
                return false;
            
            m_inputConsumed.Add(key);
            return InputManager.IsKeyHeldDown(key);
        }
        
        public bool ConsumeKeyUp(Key key)
        {
            if (m_inputConsumed.Contains(key))
                return false;
            
            m_inputConsumed.Add(key);
            return InputManager.IsKeyUp(key);
        }
        
        public bool ConsumeKeyPrevUp(Key key)
        {
            if (m_inputConsumed.Contains(key))
                return false;
            
            m_inputConsumed.Add(key);
            return InputManager.IsKeyPrevUp(key);
        }
        
        public bool ConsumeKeyPressed(Key key)
        {
            if (m_inputConsumed.Contains(key))
                return false;
            
            m_inputConsumed.Add(key);
            return InputManager.IsKeyPressed(key);
        }
        
        public bool ConsumeKeyReleased(Key key)
        {
            if (m_inputConsumed.Contains(key))
                return false;
            
            m_inputConsumed.Add(key);
            return InputManager.IsKeyReleased(key);
        }
        
        public ReadOnlySpan<char> ConsumeTypedCharacters()
        {
            if (m_typedCharsConsumed)
                return ReadOnlySpan<char>.Empty;

            m_typedCharsConsumed = true;
            return InputManager.TypedCharacters;
        }

        public Vec2I ConsumeMouseMove()
        {
            Vec2I result = m_mouseMove;
            m_mouseMove = (0, 0);
            return result;
        }
        
        public int ConsumeScroll()
        {
            int result = m_mouseScroll;
            m_mouseScroll = 0;
            return result;
        }
        
        internal void Reset()
        {
            m_inputConsumed.Clear();
            m_typedCharsConsumed = false;
            m_mouseMove = InputManager.MouseMove;
            m_mouseScroll = InputManager.Scroll;
        }
    }
}
