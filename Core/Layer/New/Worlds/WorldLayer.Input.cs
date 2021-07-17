using System;
using Helion.Input;
using Helion.Util;
using Helion.World.StatusBar;

namespace Helion.Layer.New.Worlds
{
    public partial class WorldLayer
    {
        public void HandleInput(InputEvent input)
        {
            if (m_drawAutomap)
                HandleAutoMapInput(input);

            if (!World.Paused)
            {
                HandleMovementInput(input);
                World.HandleFrameInput(input);
            }

            if (input.ConsumeKeyPressed(m_config.Controls.HudDecrease))
                ChangeHudSize(false);
            else if (input.ConsumeKeyPressed(m_config.Controls.HudIncrease))
                ChangeHudSize(true);
            else if (input.ConsumeKeyPressed(m_config.Controls.Automap))
            {
                m_drawAutomap = !m_drawAutomap;
                m_config.Hud.AutoMapOffsetX.Set(0);
                m_config.Hud.AutoMapOffsetY.Set(0);
            }
        }
        
        private void HandleAutoMapInput(InputEvent input)
        {
            if (input.ConsumeKeyPressed(m_config.Controls.AutoMapDecrease))
                ChangeAutoMapSize(false);
            else if (input.ConsumeKeyPressed(m_config.Controls.AutoMapIncrease))
                ChangeAutoMapSize(true);
            else if (input.ConsumeKeyPressed(m_config.Controls.AutoMapUp))
                ChangeAutoMapOffsetY(true);
            else if (input.ConsumeKeyPressed(m_config.Controls.AutoMapDown))
                ChangeAutoMapOffsetY(false);
            else if (input.ConsumeKeyPressed(m_config.Controls.AutoMapRight))
                ChangeAutoMapOffsetX(true);
            else if (input.ConsumeKeyPressed(m_config.Controls.AutoMapLeft))
                ChangeAutoMapOffsetX(false);
        }
        
        private void ChangeAutoMapOffsetY(bool increase)
        {
            if (increase)
                m_config.Hud.AutoMapOffsetY.Set(m_config.Hud.AutoMapOffsetY + 1);
            else
                m_config.Hud.AutoMapOffsetY.Set(m_config.Hud.AutoMapOffsetY - 1);
        }
        
        private void ChangeAutoMapOffsetX(bool increase)
        {
            if (increase)
                m_config.Hud.AutoMapOffsetX.Set(m_config.Hud.AutoMapOffsetX + 1);
            else
                m_config.Hud.AutoMapOffsetX.Set(m_config.Hud.AutoMapOffsetX - 1);
        }
        
        private void ChangeAutoMapSize(bool increase)
        {
            if (increase)
                m_config.Hud.AutoMapScale.Set(m_config.Hud.AutoMapScale.Value + 0.1);
            else
                m_config.Hud.AutoMapScale.Set(m_config.Hud.AutoMapScale.Value - 0.1);
        }
        
        private void HandleMovementInput(InputEvent input)
        {
            foreach (var (inputKey, command) in m_consumeDownKeys)
                if (input.ConsumeKeyPressedOrDown(inputKey))
                    m_tickCommand.Add(command);

            foreach (var (inputKey, command) in m_consumePressedKeys)
                if (input.ConsumeKeyPressed(inputKey))
                    m_tickCommand.Add(command);
        }
        
        private void ChangeHudSize(bool increase)
        {
            int size = (int)m_config.Hud.StatusBarSize.Value;
            if (increase)
                size++;
            else
                size--;

            size = Math.Clamp(size, 0, Enum.GetValues(typeof(StatusBarSizeType)).Length - 1);

            if ((int)m_config.Hud.StatusBarSize.Value != size)
            {
                m_config.Hud.StatusBarSize.Set((StatusBarSizeType)size);
                World.SoundManager.PlayStaticSound(Constants.MenuSounds.Change);
            }
        }
    }
}
