using Helion.Util;
using Helion.Util.Configs.Values;
using Helion.Util.Container;
using Helion.Window;
using Helion.Window.Input;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.StatusBar;
using System;

namespace Helion.Layer.Worlds;

public partial class WorldLayer
{
    private static readonly (string, TickCommands)[] KeyPressCommandMapping =
    {
        (Constants.Input.Forward,       TickCommands.Forward),
        (Constants.Input.Backward,      TickCommands.Backward),
        (Constants.Input.Left,          TickCommands.Left),
        (Constants.Input.Right,         TickCommands.Right),
        (Constants.Input.TurnLeft,      TickCommands.TurnLeft),
        (Constants.Input.TurnRight,     TickCommands.TurnRight),
        (Constants.Input.LookDown,      TickCommands.LookDown),
        (Constants.Input.LookUp,        TickCommands.LookUp),
        (Constants.Input.Jump,          TickCommands.Jump),
        (Constants.Input.Crouch,        TickCommands.Crouch),
        (Constants.Input.Attack,        TickCommands.Attack),
        (Constants.Input.Run,           TickCommands.Speed),
        (Constants.Input.Strafe,        TickCommands.Strafe),
        (Constants.Input.CenterView,    TickCommands.CenterView),
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

    private readonly DynamicArray<Key> m_pressedKeys = new();

    private bool IsCommandContinuousHold(string command, IConsumableInput input)
    {
        return m_config.Keys.ConsumeCommandKeyPressOrContinousHold(command, input);
    }

    private bool IsCommandPressed(string command, IConsumableInput input)
    {
        return m_config.Keys.ConsumeCommandKeyPress(command, input);
    }

    private bool IsCommandDown(string command, IConsumableInput input)
    {
        return m_config.Keys.ConsumeCommandKeyDown(command, input);
    }

    public void AddCommand(TickCommands cmd) => GetTickCommand().Add(cmd);

    public void HandleInput(IConsumableInput input)
    {
        if (IsCommandPressed(Constants.Input.Pause, input))
            HandlePausePress();

        if (m_drawAutomap)
            HandleAutoMapInput(input);

        if (!AnyLayerObscuring && !World.DrawPause)
        {
            HandleCommandInput(input);
            World.HandleFrameInput(input);
        }

        CheckSaveOrLoadGame(input);

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
        CheckCommandInput(input);
    }

    private void CheckCommandInput(IConsumableInput input)
    {
        input.Manager.GetPressedKeys(m_pressedKeys);
        for (int i = 0; i < m_pressedKeys.Length; i++)
        {
            if (!input.ConsumeKeyPressed(m_pressedKeys[i]))
                continue;
            var key = m_pressedKeys[i];
            var commands = World.Config.Keys[key];
            foreach (var command in commands)
            {
                if (World.Paused && Constants.InGameCommands.Contains(command))
                    return;
                m_parent.SubmitConsoleText(command);
            }
        }
    }

    private void CheckSaveOrLoadGame(IConsumableInput input)
    {
        if (IsCommandPressed(Constants.Input.Save, input))
        {
            m_parent.GoToSaveOrLoadMenu(true);
            return;
        }

        if (IsCommandPressed(Constants.Input.QuickSave, input))
            m_parent.QuickSave();

        if (IsCommandPressed(Constants.Input.Load, input))
            m_parent.GoToSaveOrLoadMenu(false);
    }

    private void HandlePausePress()
    {
        m_paused = !m_paused;
        if (m_paused)
        {
            World.Pause(PauseOptions.DrawPause);
            return;
        }

        if (AnyLayerObscuring)
            return;

        World.Resume();
    }

    private void HandleAutoMapInput(IConsumableInput input)
    {
        if (IsCommandContinuousHold(Constants.Input.AutoMapDecrease, input))
            ChangeAutoMapSize(false);
        else if (IsCommandContinuousHold(Constants.Input.AutoMapIncrease, input))
            ChangeAutoMapSize(true);
        else if (IsCommandContinuousHold(Constants.Input.AutoMapUp, input))
            ChangeAutoMapOffsetY(true);
        else if (IsCommandContinuousHold(Constants.Input.AutoMapDown, input))
            ChangeAutoMapOffsetY(false);
        else if (IsCommandContinuousHold(Constants.Input.AutoMapRight, input))
            ChangeAutoMapOffsetX(true);
        else if (IsCommandContinuousHold(Constants.Input.AutoMapLeft, input))
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
        if (m_autoMapScale > 0.5)
            m_autoMapScale += (increase ? 0.2 : -0.2);
        else
            m_autoMapScale += (increase ? 0.04 : -0.04);

        m_autoMapScale = MathHelper.Clamp(m_autoMapScale, 0.04, 25);
        m_config.Hud.AutoMap.Scale.Set(m_autoMapScale);
    }

    private void HandleCommandInput(IConsumableInput input)
    {
        TickCommand cmd = GetTickCommand();
        for (int i = 0; i < KeyPressCommandMapping.Length; i++)
        {
            (string command, TickCommands tickCommand) = KeyPressCommandMapping[i];
            if (IsCommandDown(command, input))
                cmd.Add(tickCommand);
        }

        int yMove = input.GetMouseMove().Y;
        if (m_config.Mouse.ForwardBackwardSpeed > 0 && yMove != 0)
            cmd.ForwardMoveSpeed += yMove * (m_config.Mouse.ForwardBackwardSpeed / 128);
    }

    private void ChangeHudSize(bool increase)
    {
        StatusBarSizeType current = m_config.Hud.StatusBarSize;
        StatusBarSizeType next = (StatusBarSizeType)((int)current + (increase ? 1 : -1));

        if (m_config.Hud.StatusBarSize.Set(next) == ConfigSetResult.Set)
            World.SoundManager.PlayStaticSound(Constants.MenuSounds.Change);
    }
}
