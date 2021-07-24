using System;
using Helion.Input;

namespace Helion.Layer.EndGame
{
    public partial class EndGameLayer
    {
        private bool m_invokedNextMapFunc;

        public void HandleInput(InputEvent input)
        {
            if (input.HasAnyKeyPressed())
                AdvanceState();
        }
        
        private void AdvanceState()
        {
            m_forceState = true;

            if (m_invokedNextMapFunc) 
                return;

            if (m_drawState == EndGameDrawState.TextComplete)
            {
                m_invokedNextMapFunc = true;
                Exited?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
