using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Values;
using Helion.Util.Container;
using Helion.Window;
using Helion.Window.Input;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.StatusBar;
using System;
using System.Collections.Generic;
using static Helion.Util.Constants;

namespace Helion.Layer.Worlds;

public partial class WorldLayer
{
    public bool ShouldRender { get; set; }

    private static readonly (string, TickCommands)[] KeyPressCommandMapping =
    {
        (Input.Forward,       TickCommands.Forward),
        (Input.Backward,      TickCommands.Backward),
        (Input.Left,          TickCommands.Left),
        (Input.Right,         TickCommands.Right),
        (Input.TurnLeft,      TickCommands.TurnLeft),
        (Input.TurnRight,     TickCommands.TurnRight),
        (Input.LookDown,      TickCommands.LookDown),
        (Input.LookUp,        TickCommands.LookUp),
        (Input.Jump,          TickCommands.Jump),
        (Input.Crouch,        TickCommands.Crouch),
        (Input.Attack,        TickCommands.Attack),
        (Input.Run,           TickCommands.Speed),
        (Input.Strafe,        TickCommands.Strafe),
        (Input.CenterView,    TickCommands.CenterView),
        (Input.Use,            TickCommands.Use),
        (Input.NextWeapon,     TickCommands.NextWeapon),
        (Input.PreviousWeapon, TickCommands.PreviousWeapon),
        (Input.WeaponSlot1,    TickCommands.WeaponSlot1),
        (Input.WeaponSlot2,    TickCommands.WeaponSlot2),
        (Input.WeaponSlot3,    TickCommands.WeaponSlot3),
        (Input.WeaponSlot4,    TickCommands.WeaponSlot4),
        (Input.WeaponSlot5,    TickCommands.WeaponSlot5),
        (Input.WeaponSlot6,    TickCommands.WeaponSlot6),
        (Input.WeaponSlot7,    TickCommands.WeaponSlot7),
    };

    // Convert analog inputs into movements, assumes analog values are in range [0..1]
    private static readonly Dictionary<TickCommands, Func<float, Vec4F>> MovementCommmands = new()
    {
        { TickCommands.TurnLeft, (value) =>     new Vec4F(0, 0, value, 0) },
        { TickCommands.TurnRight, (value) =>    new Vec4F(0, 0, -value, 0) },
        { TickCommands.LookUp, (value) =>       new Vec4F(0, 0, 0, value) },
        { TickCommands.LookDown, (value) =>     new Vec4F(0, 0, 0, -value) },
        { TickCommands.Forward, (value) =>      new Vec4F(value, 0, 0, 0) },
        { TickCommands.Backward, (value) =>     new Vec4F(-value, 0, 0, 0) },
        { TickCommands.Left, (value) =>         new Vec4F(0, -value, 0, 0) },
        { TickCommands.Right, (value) =>        new Vec4F(0, value, 0 ,0) },
    };

    private readonly DynamicArray<Key> m_pressedKeys = new();

    private bool IsCommandContinuousHold(string command, IConsumableInput input) =>
        IsCommandContinuousHold(command, input, out _);

    private bool IsCommandContinuousHold(string command, IConsumableInput input, out int scrollAmount)
    {
        return m_config.Keys.ConsumeCommandKeyPressOrContinuousHold(command, input, out scrollAmount);
    }

    private bool IsCommandPressed(string command, IConsumableInput input) =>
        IsCommandPressed(command, input, out _);

    private bool IsCommandPressed(string command, IConsumableInput input, out int scrollAmount)
    {
        return m_config.Keys.ConsumeCommandKeyPress(command, input, out scrollAmount);
    }

    private bool IsCommandDown(string command, IConsumableInput input, out int scrollAmount, out Key key)
    {
        return m_config.Keys.ConsumeCommandKeyDown(command, input, out scrollAmount, out key);
    }

    public void AddCommand(TickCommands cmd) => GetTickCommand().Add(cmd);

    public void HandleInput(IConsumableInput input)
    {
        if (!AnyLayerObscuring && !World.DrawPause)
        {
            if (input.HandleKeyInput)
            {
                if (DrawAutomap)
                    HandleAutoMapInput(input);
                HandleCommandInput(input);
                World.HandleKeyInput(input);
            }
            World.HandleMouseMovement(input);
        }

        if (!input.HandleKeyInput)
            return;

        if (IsCommandPressed(Input.Pause, input))
            HandlePausePress();

        if (IsCommandPressed(Input.HudDecrease, input))
            ChangeHudSize(false);
        else if (IsCommandPressed(Input.HudIncrease, input))
            ChangeHudSize(true);
        else if (IsCommandPressed(Input.Automap, input))
        {
            DrawAutomap = !DrawAutomap;
            m_autoMapOffset = (0, 0);
            m_autoMapScale = m_config.Hud.AutoMap.Scale;
        }

        if (m_parent.LoadingLayer == null && m_parent.TransitionLayer == null)
            input.IterateCommands(World.Config.Keys.GetKeyMapping(), m_checkCommandAction);
    }

    private bool CheckCommand(IConsumableInput input, KeyCommandItem cmd)
    {
        // Handling this at the GameLayerManager level so screenshots can work on title/options etc.
        if (cmd.Command == Input.Screenshot)
            return false;

        if ((m_parent.LoadingLayer != null || m_parent.TransitionLayer != null || World.Paused) && InGameCommands.Contains(cmd.Command))
            return false;

        // This layer should eat all regular base commands whenever it's on top, to prevent things like "move automap"
        // commands from getting passed down to the console when the automap isn't raised (it'll always be on top and
        // thus will have had a chance to consume its inputs first).
        if (BaseCommands.Contains(cmd.Command))
            return false;

        m_parent.SubmitConsoleText(cmd.Command);
        return true;
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
        int scrollAmount = 0;
        if (IsCommandContinuousHold(Constants.Input.AutoMapDecrease, input, out scrollAmount))
            ChangeAutoMapSize(GetChangeAmount(input, -1, scrollAmount));
        else if (IsCommandContinuousHold(Constants.Input.AutoMapIncrease, input, out scrollAmount))
            ChangeAutoMapSize(GetChangeAmount(input, 1, scrollAmount));
        else if (IsCommandContinuousHold(Constants.Input.AutoMapUp, input))
            ChangeAutoMapOffsetY(true);
        else if (IsCommandContinuousHold(Constants.Input.AutoMapDown, input))
            ChangeAutoMapOffsetY(false);
        else if (IsCommandContinuousHold(Constants.Input.AutoMapRight, input))
            ChangeAutoMapOffsetX(true);
        else if (IsCommandContinuousHold(Constants.Input.AutoMapLeft, input))
            ChangeAutoMapOffsetX(false);
        else if (IsCommandPressed(Constants.Input.AutoMapAddMarker, input))
            m_console.SubmitInputText("mark.add");
        else if (IsCommandPressed(Constants.Input.AutoMapRemoveNearbyMarkers, input))
            m_console.SubmitInputText("mark.remove");
        else if (IsCommandPressed(Constants.Input.AutoMapClearAllMarkers, input))
            m_console.SubmitInputText("mark.clear");
    }

    private static int GetChangeAmount(IConsumableInput input, int baseAmount, int scrollAmount)
    {
        if (scrollAmount == 0)
            return baseAmount;

        return baseAmount * Math.Abs(scrollAmount);
    }

    private void ChangeAutoMapOffsetY(bool increase)
    {
        m_autoMapOffset.Y += (increase ? 1 : -1);
    }

    private void ChangeAutoMapOffsetX(bool increase)
    {
        m_autoMapOffset.X += (increase ? 1 : -1);
    }

    private void ChangeAutoMapSize(int amount)
    {
        if (m_autoMapScale > 0.5)
            m_autoMapScale += amount * 0.2;
        else
            m_autoMapScale += amount * 0.04;

        m_autoMapScale = MathHelper.Clamp(m_autoMapScale, 0.04, 25);
        m_config.Hud.AutoMap.Scale.Set(m_autoMapScale);
    }

    private void HandleCommandInput(IConsumableInput input)
    {
        int weaponScroll = 0;
        TickCommand cmd = GetTickCommand();
        Vec4F analogInput = Vec4F.Zero; // (We'll use the order X="run", Y="strafe", Z="turn", W="pitch")

        for (int i = 0; i < KeyPressCommandMapping.Length; i++)
        {
            (string command, TickCommands tickCommand) = KeyPressCommandMapping[i];
            if (IsCommandDown(command, input, out int scrollAmount, out Key key))
            {
                if (tickCommand == TickCommands.NextWeapon || tickCommand == TickCommands.PreviousWeapon)
                    weaponScroll += GetWeaponScroll(scrollAmount, key, tickCommand);

                bool cancelKey = false;
                if (m_config.Controller.EnableGameController)
                {
                    // If there is an analog input that corresponds to this key, build up a set of analog movement vectors
                    // rather than using the on/off value from the keypress.
                    if (MovementCommmands.TryGetValue(tickCommand, out var movementVector)
                        && input.Manager.AnalogAdapter?.KeyIsAnalogAxis(key) == true
                        && input.Manager.AnalogAdapter?.TryGetAnalogValueForAxis(key, out float analogValue) == true)
                    {
                        analogInput += movementVector(analogValue);
                        cancelKey = true;
                    }
                }

                if (!cancelKey)
                    cmd.Add(tickCommand);
            }
        }

        if (analogInput != Vec4F.Zero)
        {
            // Perform unit-circle correction for the run and strafe axes -- most players will probably make them share a (round)
            // stick, and they need to be able to max out run and strafe at the same time.  We're not bothering to do this for pitch
            // and turn as this would make freelook overly sensitive.
            CircleCoordsToSquare(analogInput.X, analogInput.Y, out analogInput.X, out analogInput.Y);

            cmd.ForwardMoveSpeed = Math.Clamp(analogInput.X * m_config.Controller.GameControllerRunScale, -1, 1) * Player.GetForwardMovementSpeed();
            cmd.SideMoveSpeed = Math.Clamp(analogInput.Y * m_config.Controller.GameControllerStrafeScale, -1, 1) * Player.GetSideMovementSpeed();
            cmd.AngleTurn = analogInput.Z * Player.FastTurnSpeed * m_config.Controller.GameControllerTurnScale;
            cmd.PitchTurn = analogInput.W * Player.FastTurnSpeed * m_config.Controller.GameControllerPitchScale;
        }

        cmd.WeaponScroll = weaponScroll;
        int yMove = input.GetMouseMove().Y;
        if (!m_config.Mouse.Look && m_config.Mouse.ForwardBackwardSpeed > 0 && yMove != 0)
            cmd.ForwardMoveSpeed += yMove * (m_config.Mouse.ForwardBackwardSpeed / 128);
    }

    private static void CircleCoordsToSquare(float u, float v, out float x, out float y)
    {
        if (u == 0)
        {
            x = 0;
            y = v;
            return;
        }
        if (v == 0)
        {
            x = u;
            y = 0;
            return;
        }

        double radius = Math.Sqrt(u * u + v * v);
        if (Math.Abs(u) < Math.Abs(v))
        {
            x = (float)(u * radius / Math.Abs(v));
            y = (float)(radius * Math.Sign(v));
        }
        else
        {
            x = (float)(radius * Math.Sign(u));
            y = (float)(v * radius / Math.Abs(u));
        }
    }

    private int GetWeaponScroll(int scrollAmount, Key key, TickCommands tickCommand)
    {
        // Invert scroll amount if keys are opposite to the command
        if ((key == Key.MouseWheelUp && tickCommand != TickCommands.NextWeapon) ||
            (key == Key.MouseWheelDown && tickCommand != TickCommands.PreviousWeapon))
            return scrollAmount * -1;
        return scrollAmount;
    }

    private void ChangeHudSize(bool increase)
    {
        StatusBarSizeType current = m_config.Hud.StatusBarSize;
        StatusBarSizeType next = (StatusBarSizeType)((int)current + (increase ? 1 : -1));

        if (m_config.Hud.StatusBarSize.Set(next) == ConfigSetResult.Set)
            World.SoundManager.PlayStaticSound(Constants.MenuSounds.Change);
    }
}
