﻿namespace Helion.Client.Input.Joystick
{
    using Helion.Window.Input;
    using OpenTK.Windowing.Common.Input;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OpenTKJoystick = OpenTK.Windowing.GraphicsLibraryFramework.JoystickState;

    public class JoystickAdapter
    {
        private IReadOnlyList<OpenTKJoystick> m_windowJoystickStates;

        private readonly JoystickState[][] m_joystickStates;
        private JoystickState[] m_initialJoystickStates;
        private int m_statePointer = 0;
        private int m_prevStatePointer = 1;
        private float m_deadZone;

        public event HatEventHandler? HatMoved;
        public event ButtonEventHandler? ButtonPressed;
        public event AxisEventHandler? AxisMoved;

        public event HatEventHandler? HatHeld;
        public event ButtonEventHandler? ButtonHeld;
        public event AxisEventHandler? AxisHeld;

        private InputManager? m_inputManager;

        /// <summary>
        /// Get or set the dead zone for this controller, which is in the range 0..1
        /// </summary>
        public float DeadZone
        {
            get => m_deadZone;
            set
            {
                m_deadZone = Math.Clamp(value, 0, 1);
            }
        }

        /// <summary>
        /// Get the last polled states for all active joysticks
        /// </summary>
        public JoystickState[] CurrentStates => m_joystickStates[m_statePointer];

        public JoystickAdapter(IReadOnlyList<OpenTKJoystick> joystickInputs, float axisDeadzone, InputManager? inputManager)
        {
            m_windowJoystickStates = joystickInputs;
            m_inputManager = inputManager;

            m_joystickStates = new JoystickState[2][];
            m_initialJoystickStates = Array.Empty<JoystickState>();
            m_deadZone = axisDeadzone;

            RedetectJoysticks();
        }

        /// <summary>
        /// Redetect active joysticks after a configuration change
        /// </summary>
        public void RedetectJoysticks()
        {
            OpenTKJoystick[] activeSticks = m_windowJoystickStates
                .Where(stick => stick != null)
                .ToArray();

            m_joystickStates[0] = new JoystickState[activeSticks.Length];
            m_joystickStates[1] = new JoystickState[activeSticks.Length];
            m_initialJoystickStates = new JoystickState[activeSticks.Length];


            for (int i = 0; i < activeSticks.Length; i++)
            {
                m_joystickStates[0][i] = new JoystickState(activeSticks[i]);
                m_joystickStates[1][i] = new JoystickState(activeSticks[i]);
                m_initialJoystickStates[i] = new JoystickState(activeSticks[i]);
            }

            // Capture initial zero
            foreach (JoystickState joystick in m_initialJoystickStates)
            {
                joystick.Update(m_windowJoystickStates[joystick.JoystickId]);
            }
        }

        /// <summary>
        /// Reset the "zero" position on all active joysticks to whatever position they are now in
        /// </summary>
        public void Zero()
        {
            foreach (JoystickState joystick in m_initialJoystickStates)
            {
                joystick.Update(m_windowJoystickStates[joystick.JoystickId]);
            }
        }

        /// <summary>
        /// Get the positions and button states of all active joysticks
        /// </summary>
        public void SampleJoystickStates()
        {
            if (m_initialJoystickStates.Length == 0)
            {
                // Do minimal amount of work if controller input is enabled but no controllers are available
                return;
            }

            // Sample current input state
            foreach (JoystickState joystick in m_joystickStates[m_statePointer])
            {
                joystick.Update(m_windowJoystickStates[joystick.JoystickId]);
            }

            // Diff against input state since the last time we sampled
            // Note that we're using this, instead of OpenTK's built-in "Previous" states, because we may or may not want
            // to sample at the same rate as the parent window.
            JoystickState currentState;
            JoystickState prevState;
            JoystickState initialState;

            for (int joystick = 0; joystick < m_joystickStates[0].Length; joystick++)
            {
                currentState = m_joystickStates[m_statePointer][joystick];
                prevState = m_joystickStates[m_prevStatePointer][joystick];
                initialState = m_initialJoystickStates[joystick];

                if (AxisMoved != null || AxisHeld != null)
                {
                    CheckAxes(currentState, prevState, initialState, joystick);
                }

                if (m_inputManager != null || HatMoved != null || HatHeld != null)
                {
                    // Maybe we should check our coats, too..
                    CheckHats(currentState, prevState, joystick);
                }

                if ((m_inputManager != null) || ButtonPressed != null || ButtonHeld != null)
                {
                    // Unfortunately, hats (D-pads) are aliased as buttons on some controllers
                    CheckButtons(currentState, prevState, joystick);
                }
            }

            m_statePointer = (m_statePointer + 1) % 2;
            m_prevStatePointer = (m_prevStatePointer + 1) % 2;
        }

        private void CheckHats(JoystickState currentState, JoystickState prevState, int joystick)
        {
            for (int hat = 0; hat < currentState.HatPositions.Length; hat++)
            {
                Hat currentDirection = currentState.HatPositions[hat];
                Hat prevDirection = prevState.HatPositions[hat];

                if (HatMoved != null && currentDirection != prevDirection)
                {
                    HatMoved(this, new(joystick, hat, currentState.HatPositions[hat]));
                }
                if (HatHeld != null && currentDirection != Hat.Centered)
                {
                    HatHeld(this, new(joystick, hat, currentState.HatPositions[hat]));
                }
                if (m_inputManager != null && hat < JoystickStatic.PadToKeys.Length && currentDirection != prevDirection)
                {
                    var (left, right, up, down) = JoystickStatic.PadToKeys[hat];
                    Hat directionsAdded = (Hat)Math.Max((byte)currentDirection - (byte)prevDirection, (byte)0);
                    Hat directionsRemoved = (Hat)Math.Max((byte)prevDirection - (byte)currentDirection, (byte)0);

                    if (directionsAdded > 0)
                    {
                        if ((directionsAdded & Hat.Left) != 0)
                            m_inputManager.SetKeyDown(left);
                        if ((directionsAdded & Hat.Right) != 0)
                            m_inputManager.SetKeyDown(right);
                        if ((directionsAdded & Hat.Up) != 0)
                            m_inputManager.SetKeyDown(up);
                        if ((directionsAdded & Hat.Down) != 0)
                            m_inputManager.SetKeyDown(down);
                    }

                    if (directionsRemoved > 0)
                    {
                        if ((directionsRemoved & Hat.Left) != 0)
                            m_inputManager.SetKeyUp(left);
                        if ((directionsRemoved & Hat.Right) != 0)
                            m_inputManager.SetKeyUp(right);
                        if ((directionsRemoved & Hat.Up) != 0)
                            m_inputManager.SetKeyUp(up);
                        if ((directionsRemoved & Hat.Down) != 0)
                            m_inputManager.SetKeyUp(down);
                    }
                }
            }
        }

        private void CheckButtons(JoystickState currentState, JoystickState prevState, int joystick)
        {

            for (int button = 0; button < currentState.ButtonStates.Length; button++)
            {
                bool currentlyPressed = currentState.ButtonStates[button];
                bool previouslyPressed = prevState.ButtonStates[button];

                if (ButtonPressed != null && currentlyPressed != previouslyPressed)
                {
                    ButtonPressed(this, new(joystick, button, currentlyPressed));
                }

                if (m_inputManager != null && button < JoystickStatic.ButtonsToKeys.Length)
                {
                    if (currentlyPressed && !previouslyPressed)
                    {
                        m_inputManager.SetKeyDown(JoystickStatic.ButtonsToKeys[button]);
                    }

                    if (!currentlyPressed && previouslyPressed)
                    {
                        m_inputManager.SetKeyUp(JoystickStatic.ButtonsToKeys[button]);
                    }
                }

                if (ButtonHeld != null && currentlyPressed)
                {
                    ButtonHeld(this, new(joystick, button, true));
                }
            }
        }

        private void CheckAxes(JoystickState currentState, JoystickState prevState, JoystickState initialState, int joystick)
        {
            for (int axis = 0; axis < currentState.AxisValues.Length; axis++)
            {
                float axisValue;
                float prevAxisValue;
                float axisDelta;
                float axisMovementFromNeutral;
                float axisPositionCorrected;
                bool axisNotAtZero;

                if (AxisMoved != null || AxisHeld != null)
                {
                    axisValue = currentState.AxisValues[axis];
                    prevAxisValue = prevState.AxisValues[axis];
                    axisDelta = axisValue - prevAxisValue;
                    axisMovementFromNeutral = axisValue - initialState.AxisValues[axis];
                    axisPositionCorrected = Math.Sign(axisMovementFromNeutral) * (Math.Abs(axisMovementFromNeutral) - m_deadZone) / (1 - m_deadZone);
                    axisNotAtZero = Math.Abs(axisMovementFromNeutral) > m_deadZone;

                    if (AxisMoved != null && axisNotAtZero && axisValue != prevAxisValue)
                    {
                        AxisMoved(this, new(
                            joystick,
                            axis,
                            axisValue - prevAxisValue,
                            currentState.AxisValues[axis],
                            axisPositionCorrected));
                    }

                    if (AxisHeld != null && axisNotAtZero)
                    {
                        AxisHeld(this, new(
                            joystick,
                            axis,
                            axisValue - prevAxisValue,
                            currentState.AxisValues[axis],
                            axisPositionCorrected));
                    }
                }
            }
        }
    }
}
