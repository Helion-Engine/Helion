using Helion.Input;
using Helion.Util;
using Helion.Util.Configs.Values;
using Helion.World.Entities.Players;
using Helion.World.StatusBar;

namespace Helion.Layer.Worlds
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

            if (m_config.Keys.ConsumeCommandKeyPress(Constants.Input.HudDecrease, input))
                ChangeHudSize(false);
            else if (m_config.Keys.ConsumeCommandKeyPress(Constants.Input.HudIncrease, input))
                ChangeHudSize(true);
            else if (input.ConsumeKeyPressed(Key.Tab))
            {
                m_drawAutomap = !m_drawAutomap;
                m_autoMapOffset = (0, 0);
                m_autoMapScale = m_config.Hud.AutoMap.Scale;
            }
        }
        
        private void HandleAutoMapInput(InputEvent input)
        {
            if (m_config.Keys.ConsumeCommandKeyPress(Constants.Input.AutoMapDecrease, input))
                ChangeAutoMapSize(false);
            else if (m_config.Keys.ConsumeCommandKeyPress(Constants.Input.AutoMapIncrease, input))
                ChangeAutoMapSize(true);
            else if (m_config.Keys.ConsumeCommandKeyPress(Constants.Input.AutoMapUp, input))
                ChangeAutoMapOffsetY(true);
            else if (m_config.Keys.ConsumeCommandKeyPress(Constants.Input.AutoMapDown, input))
                ChangeAutoMapOffsetY(false);
            else if (m_config.Keys.ConsumeCommandKeyPress(Constants.Input.AutoMapRight, input))
                ChangeAutoMapOffsetX(true);
            else if (m_config.Keys.ConsumeCommandKeyPress(Constants.Input.AutoMapLeft, input))
                ChangeAutoMapOffsetX(false);
        }
        
        private void ChangeAutoMapOffsetY(bool increase)
        {
            m_autoMapOffset.Y += (increase ? 1 : -1);
        }
        
        private void ChangeAutoMapOffsetX(bool increase)
        {
            m_autoMapOffset.X += (increase ? 1 : -1);
        }
        
        private void ChangeAutoMapSize(bool increase)
        {
            m_autoMapScale += increase ? 0.1 : -0.1;
        }
        
        private void HandleMovementInput(InputEvent input)
        {
            m_tickCommand.Clear();

            foreach ((string command, TickCommands tickCommand) in InstantCommandMapping)
                if (m_config.Keys.ConsumeCommandKeyPress(command, input))
                    m_tickCommand.Add(tickCommand, true);
            
            foreach ((string command, TickCommands tickCommand) in NonInstantCommandMapping)
                if (m_config.Keys.ConsumeCommandKeyPressedOrDown(command, input))
                    m_tickCommand.Add(tickCommand, false);
        }
        
        private void ChangeHudSize(bool increase)
        {
            StatusBarSizeType current = m_config.Hud.StatusBarSize;
            StatusBarSizeType next = (StatusBarSizeType)((int)current + (increase ? 1 : -1));
            
            if (m_config.Hud.StatusBarSize.Set(next) == ConfigSetResult.Set)
                World.SoundManager.PlayStaticSound(Constants.MenuSounds.Change);
        }
    }
}
