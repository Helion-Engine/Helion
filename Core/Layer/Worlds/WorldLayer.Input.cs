using Helion.Input;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Values;
using Helion.World.Entities.Players;
using Helion.World.StatusBar;

namespace Helion.Layer.Worlds
{
    public partial class WorldLayer
    {
        private (ConfigValueEnum<Key>, TickCommands)[] m_consumeDownKeys = {};
        private (ConfigValueEnum<Key>, TickCommands)[] m_consumePressedKeys = {};

        private void SetKeyBindings(Config config)
        {
            m_consumeDownKeys = new[]
            {
                (config.Controls.Forward,   TickCommands.Forward),
                (config.Controls.Backward,  TickCommands.Backward),
                (config.Controls.Left,      TickCommands.Left),
                (config.Controls.Right,     TickCommands.Right),
                (config.Controls.TurnLeft,  TickCommands.TurnLeft),
                (config.Controls.TurnRight, TickCommands.TurnRight),
                (config.Controls.LookDown,  TickCommands.LookDown),
                (config.Controls.LookUp,    TickCommands.LookUp),
                (config.Controls.Jump,      TickCommands.Jump),
                (config.Controls.Crouch,    TickCommands.Crouch),
                (config.Controls.Attack,    TickCommands.Attack),
                (config.Controls.AttackAlt, TickCommands.Attack),
                (config.Controls.Run,       TickCommands.Speed),
                (config.Controls.RunAlt,    TickCommands.Speed),
                (config.Controls.Strafe,    TickCommands.Strafe),
            };

            m_consumePressedKeys = new[]
            {
                (config.Controls.Use,            TickCommands.Use),
                (config.Controls.NextWeapon,     TickCommands.NextWeapon),
                (config.Controls.PreviousWeapon, TickCommands.PreviousWeapon),
                (config.Controls.WeaponSlot1,    TickCommands.WeaponSlot1),
                (config.Controls.WeaponSlot2,    TickCommands.WeaponSlot2),
                (config.Controls.WeaponSlot3,    TickCommands.WeaponSlot3),
                (config.Controls.WeaponSlot4,    TickCommands.WeaponSlot4),
                (config.Controls.WeaponSlot5,    TickCommands.WeaponSlot5),
                (config.Controls.WeaponSlot6,    TickCommands.WeaponSlot6),
                (config.Controls.WeaponSlot7,    TickCommands.WeaponSlot7),
            };
        }

        public void HandleInput(InputEvent input)
        {
            if (m_drawAutomap)
                HandleAutoMapInput(input);

            if (!World.Paused)
            {
                HandleMovementInput(input);
                World.HandleFrameInput(input);
            }
            
            if (input.ConsumeKeyPressed(m_config.Controls.Save))
            {
                // TODO: Go to save menu
            }
            else if (input.ConsumeKeyPressed(m_config.Controls.Load))
            {
                // TODO: Go to load menu
            }
            
            if (input.ConsumeKeyPressed(m_config.Controls.HudDecrease))
                ChangeHudSize(false);
            else if (input.ConsumeKeyPressed(m_config.Controls.HudIncrease))
                ChangeHudSize(true);
            else if (input.ConsumeKeyPressed(m_config.Controls.Automap))
            {
                m_drawAutomap = !m_drawAutomap;
                m_autoMapOffset = (0, 0);
                m_autoMapScale = m_config.Hud.AutoMapScale;
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
            
            foreach (var (inputKey, command) in m_consumeDownKeys)
                if (input.ConsumeKeyPressedOrDown(inputKey))
                    m_tickCommand.Add(command, true);

            foreach (var (inputKey, command) in m_consumePressedKeys)
                if (!input.WasPreviouslyPressed(inputKey) && input.ConsumeKeyPressed(inputKey))
                    m_tickCommand.Add(command, false);
        }
        
        private void ChangeHudSize(bool increase)
        {
            StatusBarSizeType current = m_config.Hud.StatusBarSize;
            StatusBarSizeType next = (StatusBarSizeType)((int)current + (increase ? 1 : -1));
            
            // TODO: Check result
            if (m_config.Hud.StatusBarSize.Set(next))
                World.SoundManager.PlayStaticSound(Constants.MenuSounds.Change);
        }
    }
}
