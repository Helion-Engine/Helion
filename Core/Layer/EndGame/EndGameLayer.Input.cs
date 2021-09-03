using System;
using Helion.Window;

namespace Helion.Layer.EndGame
{
    public partial class EndGameLayer
    {
        private bool m_invokedNextMapFunc;

        public void HandleInput(IConsumableInput input)
        {
            if (input.Manager.HasAnyKeyPressed())
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
