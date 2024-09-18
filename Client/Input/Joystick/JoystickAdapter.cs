namespace Helion.Client.Input.Joystick
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

        private InputManager m_inputManager;

        /// <summary>
        /// Get or set the dead zone for the controllers, in the range of 0-.5 (the range for an axis is [-1..+1])
        /// </summary>
        public float DeadZone
        {
            get => m_deadZone;
            set
            {
                m_deadZone = Math.Clamp(value, 0, .5f);
            }
        }

        /// <summary>
        /// Get data on the positions of each axis on each controller
        /// </summary>
        public AxisState[][] AxisStates { get; private set; }

        public JoystickAdapter(IReadOnlyList<OpenTKJoystick> joystickInputs, float axisDeadzone, InputManager inputManager)
        {
            m_windowJoystickStates = joystickInputs;
            m_inputManager = inputManager;
            AxisStates = [];
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

            AxisStates = new AxisState[activeSticks.Length][];

            for (int i = 0; i < activeSticks.Length; i++)
            {
                m_joystickStates[0][i] = new JoystickState(activeSticks[i]);
                m_joystickStates[1][i] = new JoystickState(activeSticks[i]);
                m_initialJoystickStates[i] = new JoystickState(activeSticks[i]);
                AxisStates[i] = new AxisState[activeSticks[i].AxisCount];
            }

            // Capture initial zero
            foreach (JoystickState joystick in m_initialJoystickStates)
            {
                joystick.Update(m_windowJoystickStates[joystick.JoystickId]);
            }
        }

        /// <summary>
        /// Reset the "zero" position on all active joysticks to whatever position they are now in
        /// This should be called at some point after launch, since some trigger buttons have a nonzero resting position.
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

                CheckAxes(currentState, prevState, initialState, joystick);
                CheckButtons(currentState, prevState);
            }

            m_statePointer = (m_statePointer + 1) % 2;
            m_prevStatePointer = (m_prevStatePointer + 1) % 2;
        }

        private void CheckButtons(JoystickState currentState, JoystickState prevState)
        {
            // Hats/D-pads are aliased as the last buttons on the controller, at least with XBox and DualShock controllers.
            int totalButtonCount = currentState.ButtonStates.Length;
            int hatButtonCount = currentState.HatPositions.Length * 4;
            int normalButtonCount = totalButtonCount - hatButtonCount;

            // Regular buttons
            for (int button = 0; button < normalButtonCount; button++)
            {
                bool currentlyPressed = currentState.ButtonStates[button];
                bool previouslyPressed = prevState.ButtonStates[button];

                if (button < JoystickStatic.ButtonsToKeys.Length)
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
            }

            // Hat (D-pad) buttons
            for (int hatButton = 0; hatButton < hatButtonCount; hatButton++)
            {
                bool currentlyPressed = currentState.ButtonStates[normalButtonCount + hatButton];
                bool previouslyPressed = prevState.ButtonStates[normalButtonCount + hatButton];

                if (hatButton < JoystickStatic.PadToKeys.Length)
                {
                    if (currentlyPressed && !previouslyPressed)
                    {
                        m_inputManager.SetKeyDown(JoystickStatic.PadToKeys[hatButton]);
                    }

                    if (!currentlyPressed && previouslyPressed)
                    {
                        m_inputManager.SetKeyUp(JoystickStatic.PadToKeys[hatButton]);
                    }
                }
            }
        }

        private void CheckAxes(JoystickState currentState, JoystickState prevState, JoystickState initialState, int joystick)
        {
            for (int axis = 0; axis < currentState.AxisValues.Length; axis++)
            {
                float axisValue = currentState.AxisValues[axis];
                float prevAxisValue = prevState.AxisValues[axis];

                var axisState = AxisStates[joystick][axis];

                axisState.position = axisValue;
                axisState.delta = axisValue - prevAxisValue;

                float axisMovementFromNeutral = axisValue - initialState.AxisValues[axis];
                axisState.positionCorrected = Math.Sign(axisMovementFromNeutral)
                    * Math.Clamp(Math.Abs(axisMovementFromNeutral) - m_deadZone, 0, 1)
                    / (1 - m_deadZone);
            }
        }
    }
}
