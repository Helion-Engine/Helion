namespace Helion.Client.Input.Joystick
{
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

        public JoystickAdapter(IReadOnlyList<OpenTKJoystick> joystickInputs, float axisDeadzone)
        {
            m_windowJoystickStates = joystickInputs;

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

                for (int button = 0; button < currentState.ButtonStates.Length; button++)
                {
                    if (ButtonPressed != null && currentState.ButtonStates[button] != prevState.ButtonStates[button])
                    {
                        ButtonPressed(this, new(joystick, button, currentState.ButtonStates[button]));
                    }

                    if (ButtonHeld != null && currentState.ButtonStates[button] == true)
                    {
                        ButtonHeld(this, new(joystick, button, true));
                    }
                }

                for (int hat = 0; hat < currentState.HatPositions.Length; hat++)
                {
                    if (HatMoved != null && currentState.HatPositions[hat] != prevState.HatPositions[hat])
                    {
                        HatMoved(this, new(joystick, hat, currentState.HatPositions[hat]));
                    }
                    if (HatHeld != null && currentState.HatPositions[hat] != OpenTK.Windowing.Common.Input.Hat.Centered)
                    {
                        HatHeld(this, new(joystick, hat, currentState.HatPositions[hat]));
                    }
                }
            }

            m_statePointer = (m_statePointer + 1) % 2;
            m_prevStatePointer = (m_prevStatePointer + 1) % 2;
        }
    }
}
