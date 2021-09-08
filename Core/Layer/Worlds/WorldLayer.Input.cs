using Helion.Util;
using Helion.Util.Configs.Values;
using Helion.Window;
using Helion.World.Entities.Players;
using Helion.World.StatusBar;

namespace Helion.Layer.Worlds
{
    public partial class WorldLayer
    {
        private static readonly (string, TickCommands)[] KeyDownCommandMapping = 
        {
            (Constants.Input.Forward,   TickCommands.Forward),
            (Constants.Input.Backward,  TickCommands.Backward),
            (Constants.Input.Left,      TickCommands.Left),
            (Constants.Input.Right,     TickCommands.Right),
            (Constants.Input.TurnLeft,  TickCommands.TurnLeft),
            (Constants.Input.TurnRight, TickCommands.TurnRight),
            (Constants.Input.LookDown,  TickCommands.LookDown),
            (Constants.Input.LookUp,    TickCommands.LookUp),
            (Constants.Input.Jump,      TickCommands.Jump),
            (Constants.Input.Crouch,    TickCommands.Crouch),
            (Constants.Input.Attack,    TickCommands.Attack),
            (Constants.Input.Run,       TickCommands.Speed),
            (Constants.Input.Strafe,    TickCommands.Strafe),
        };
        
        private static readonly (string, TickCommands)[] KeyPressCommandMapping = 
        {
            (Constants.Input.Use,            TickCommands.Use),
            (Constants.Input.NextWeapon,     TickCommands.NextWeapon),
            (Constants.Input.PreviousWeapon, TickCommands.PreviousWeapon),
            (Constants.Input.WeaponSlot1,    TickCommands.WeaponSlot1),
            (Constants.Input.WeaponSlot2,    TickCommands.WeaponSlot2),
            (Constants.Input.WeaponSlot3,    TickCommands.WeaponSlot3),
            (Constants.Input.WeaponSlot4,    TickCommands.WeaponSlot4),
            (Constants.Input.WeaponSlot5,    TickCommands.WeaponSlot5),
            (Constants.Input.WeaponSlot6,    TickCommands.WeaponSlot6),
            (Constants.Input.WeaponSlot7,    TickCommands.WeaponSlot7),
        };

        private bool IsCommandPressed(string command, IConsumableInput input)
        {
            return m_config.Keys.ConsumeCommandKeyPress(command, input);
        }
        
        private bool IsCommandDown(string command, IConsumableInput input)
        {
            return m_config.Keys.ConsumeCommandKeyDown(command, input);
        }

        public void HandleInput(IConsumableInput input)
        {
            if (m_drawAutomap)
                HandleAutoMapInput(input);

            if (!World.Paused)
            {
                HandleMovementInput(input);
                World.HandleFrameInput(input);
            }

            if (IsCommandPressed(Constants.Input.Save, input))
                m_parent.GoToSaveOrLoadMenu(true);
            else if (IsCommandPressed(Constants.Input.Load, input))
                m_parent.GoToSaveOrLoadMenu(false);
            
            if (IsCommandPressed(Constants.Input.HudDecrease, input))
                ChangeHudSize(false);
            else if (IsCommandPressed(Constants.Input.HudIncrease, input))
                ChangeHudSize(true);
            else if (IsCommandPressed(Constants.Input.Automap, input))
            {
                m_drawAutomap = !m_drawAutomap;
                m_autoMapOffset = (0, 0);
                m_autoMapScale = m_config.Hud.AutoMap.Scale;
            }

            input.ConsumeScroll();
        }
        
        private void HandleAutoMapInput(IConsumableInput input)
        {
            if (IsCommandPressed(Constants.Input.AutoMapDecrease, input))
                ChangeAutoMapSize(false);
            else if (IsCommandPressed(Constants.Input.AutoMapIncrease, input))
                ChangeAutoMapSize(true);
            else if (IsCommandPressed(Constants.Input.AutoMapUp, input))
                ChangeAutoMapOffsetY(true);
            else if (IsCommandPressed(Constants.Input.AutoMapDown, input))
                ChangeAutoMapOffsetY(false);
            else if (IsCommandPressed(Constants.Input.AutoMapRight, input))
                ChangeAutoMapOffsetX(true);
            else if (IsCommandPressed(Constants.Input.AutoMapLeft, input))
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
        
        private void HandleMovementInput(IConsumableInput input)
        {
            m_tickCommand.Clear();
            
            foreach ((string command, TickCommands tickCommand) in KeyPressCommandMapping)
                if (IsCommandPressed(command, input))
                    m_tickCommand.Add(tickCommand);
            
            foreach ((string command, TickCommands tickCommand) in KeyDownCommandMapping)
                if (IsCommandDown(command, input))
                    m_tickCommand.Add(tickCommand, true);
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
